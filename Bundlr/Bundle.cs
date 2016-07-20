using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;

namespace Bundlr
{
	/// <summary>
	/// 数据包访问类
	/// </summary>
	internal class Bundle : IDisposable
	{
		private Dictionary<string, FileMeta> dictMetadata = new Dictionary<string, FileMeta> ();
		private FileStream fs;
		private int headerLen;
		private long bodyBeginPos;

		private object fsSync = new object ();

		internal Action<Bundle> onDisposed;

		/// <summary>
		/// 数据包的唯一ID
		/// </summary>
		/// <value>The uid.</value>
		internal string Uid{ get; private set; }

		/// <summary>
		/// 数据包版本
		/// </summary>
		/// <value>The version.</value>
		internal Version Version { get; private set; }

		/// <summary>
		/// 数据包对应的磁盘文件路径
		/// </summary>
		/// <value>The file path.</value>
		internal string FilePath{ get; private set; }

		/// <summary>
		/// 包内所有文件相对路径的列表
		/// </summary>
		/// <value>The file list.</value>
		internal string[] RelativePaths {
			get {
				return dictMetadata.Keys.ToArray ();
			}
		}

		internal Bundle (string filePath)
		{
			FilePath = Utils.Repath (filePath);

			if (!File.Exists (FilePath))
				throw new FileNotFoundException ("Cannot file file: " + filePath);

			Monitor.Enter (fsSync);
			try {
				Uid = Guid.NewGuid ().ToString ();
				LoadMetadata ();
				if (Bundles.Caching == BundleCaching.AlwaysCached)
					OpenFileStream ();
			} catch (Exception e) {
				throw e;
			} finally {
				Monitor.Exit (fsSync);
			}
		}

		/// <summary>
		/// 加载数据包中的元数据信息
		/// </summary>
		private void LoadMetadata ()
		{
			try {
				Profiler.StartSample ("waitOne");
				Monitor.Enter (fsSync);
				Profiler.EndSample ("waitOne");

				OpenFileStream ();

				fs.Seek (0, SeekOrigin.Begin);
				headerLen = fs.ReadInt32 () + sizeof(int);

				Version = Version.Deserialize (fs);
				bodyBeginPos = fs.ReadInt64 ();

				while (fs.Position < headerLen) {
					var fm = FileMeta.Deserialize (fs);
					fm.bundleUid = Uid;
					dictMetadata [fm.relativePath] = fm;
				}
			} catch (Exception e) {
				throw e;
			} finally {
				CloseFileStream ();

				Profiler.StartSample ("release");
				Monitor.Exit (fsSync);
				Profiler.EndSample ("release");
			}
		}

		/// <summary>
		/// 检查数据包中是否存在指定的相对路径
		/// </summary>
		/// <returns>若相对路径存在则返回<c>true</c>，否则返回<c>false</c>.</returns>
		/// <param name="relativePath">文件相对路径(不区分大小写)</param>
		internal bool Has (string relativePath)
		{
			return dictMetadata.ContainsKey (relativePath.ToLower ());
		}

		/// <summary>
		/// 根据相对路径获取数据包中指定的文件元数据
		/// </summary>
		/// <returns>文件元数据；若文件不存在则返回<c>null</c></returns>
		/// <param name="relativePath">文件相对路径（不区分大小写）</param>
		internal FileMeta GetMetadata (string relativePath)
		{
			relativePath = relativePath.ToLower ();
			if (!Has (relativePath))
				return null;

			return dictMetadata [relativePath];
		}

		/// <summary>
		/// 从指定文件的指定位置读取指定大小的字节到数组
		/// </summary>
		/// <param name="meta">文件元数据</param>
		/// <param name="dst">将数据读取到该数组</param>
		/// <param name="dstStartIndex">在输出数组中的起始写入位置</param>
		/// <param name="readFilePos">文件的起始读取位置</param>
		/// <param name="readSize">要读取的字节长度</param>
		internal void Read (FileMeta meta, byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			if (meta == null)
				throw new ArgumentNullException ("meta");

			if (meta.bundleUid != Uid)
				throw new ArgumentException ("File does not belong to this bundle.");

			Utils.CheckReadParameters (dst, dstStartIndex, readFilePos, readSize, meta.size);

			try {
				Profiler.StartSample ("waitOne");
				Monitor.Enter (fsSync);
				Profiler.EndSample ("waitOne");

				OpenFileStream ();
				
				// 计算新的读取位置
				long newPos = bodyBeginPos + meta.pos + readFilePos;
				// 移动指针到读取位置
				long offset2Current = newPos - fs.Position;
				if (offset2Current != 0) {
					Profiler.StartSample ("seek");
					fs.Seek (offset2Current, SeekOrigin.Current);
					Profiler.EndSample ("seek");
				}
				
				Profiler.StartSample ("read");
				fs.Read (dst, dstStartIndex, readSize);
				Profiler.EndSample ("read");
			} catch (Exception e) {
				throw e;
			} finally {
				CloseFileStream ();
				
				Profiler.StartSample ("release");
				Monitor.Exit (fsSync);
				Profiler.EndSample ("release");
			}
		}

		/// <summary>
		/// Releases all resource used by the <see cref="Bundlr.Bundle"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Bundlr.Bundle"/>. The <see cref="Dispose"/>
		/// method leaves the <see cref="Bundlr.Bundle"/> in an unusable state. After calling <see cref="Dispose"/>, you must
		/// release all references to the <see cref="Bundlr.Bundle"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Bundlr.Bundle"/> was occupying.</remarks>
		public void Dispose ()
		{
			try {
				Monitor.Enter (fsSync);
				CloseFileStream (true);
				dictMetadata.Clear ();

				if (onDisposed != null) {
					onDisposed (this);
					onDisposed = null;
				}
			} catch (Exception e) {
				throw e;
			} finally {
				Monitor.Exit (fsSync);
			}
		}

		private void OpenFileStream ()
		{
			// 避免重复创建文件流
			if (fs != null)
				return;
			
			Profiler.StartSample ("open");
			fs = new FileStream (FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
			Profiler.EndSample ("open");
		}

		private void CloseFileStream (bool isDisposing = false)
		{
			// 始终缓存文件流时，如果不是数据包释放操作，则忽略关闭文件流的操作
			if (!isDisposing && Bundles.Caching == BundleCaching.AlwaysCached)
				return;
			
			Profiler.StartSample ("close");
			if (fs == null)
				return;
			fs.Close ();
			fs = null;
			Profiler.EndSample ("close");
		}
	}
}

