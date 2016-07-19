using System;
using System.IO;
using System.Collections.Generic;

namespace Bundlr
{
	/// <summary>
	/// 资源文件访问对象抽象基类
	/// </summary>
	public abstract class ResourceFile
	{
		/// <summary>
		/// 全局设定，指定本地磁盘文件的相对路径搜索位置
		/// </summary>
		public static string DiskFileRoot = "";

		/// <summary>
		/// 获取文件大小
		/// </summary>
		/// <value>The size.</value>
		public abstract long Size { get; }

		/// <summary>
		/// 获取文件相对路径
		/// </summary>
		/// <value>The relative path.</value>
		public abstract string RelativePath { get; }

		/// <summary>
		/// 将文件指定位置、长度的数据读取到输出数组中
		/// </summary>
		/// <param name="dst">读取数据的输出数组</param>
		/// <param name="dstStartIndex">输出数组的起始写入位置</param>
		/// <param name="readFilePos">文件的起始读取位置</param>
		/// <param name="readSize">要读取的字节长度</param>
		public abstract void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize);

		/// <summary>
		/// 通过相对路径打开文件，自动判断文件的获取位置
		/// </summary>
		/// <param name="relativePath">文件的相对路径</param>
		public static ResourceFile Open (string relativePath)
		{
			ResourceFile file = Bundles.File (relativePath);
			if (file == null)
				file = DiskFile.OpenFile (relativePath);
			return file;
		}

		/// <summary>
		/// 关闭文件并释放相应资源
		/// </summary>
		public virtual void Close ()
		{
		}
	}

	/// <summary>
	/// 数据包打包文件访问对象
	/// </summary>
	public class BundleFile : ResourceFile
	{
		private Bundle bundle;
		private FileMeta metadata;

		internal BundleFile (string relativePath, Bundle bundle)
		{
			this.bundle = bundle;
			metadata = bundle.GetMetadata (relativePath);
		}

		public override long Size {
			get{ return metadata.size; }
		}

		public override string RelativePath {
			get{ return metadata.relativePath; }
		}

		public override void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			bundle.Read (metadata, dst, dstStartIndex, readFilePos, readSize);
		}
	}

	/// <summary>
	/// 磁盘文件访问对象
	/// </summary>
	public class DiskFile : ResourceFile
	{
		#region Nesting

		private struct FileCounterItem
		{
			internal DiskFile file;
			internal int counter;
		}

		#endregion

		/// <summary>
		/// 文件计数器
		/// </summary>
		private static Dictionary<string, FileCounterItem> fileCounters = new Dictionary<string, FileCounterItem> ();

		private FileInfo fileInfo;
		private string relativePath;
		private FileStream fs;

		public override long Size {
			get {
				return fileInfo.Length;
			}
		}

		public override string RelativePath {
			get {
				return relativePath;
			}
		}

		private string AbsolutePath {
			get {
				return Path.Combine (ResourceFile.DiskFileRoot, RelativePath);
			}
		}

		internal static DiskFile OpenFile (string relativePath)
		{
			// 文件尚未被创建时，创建访问文件的数据流对象
			// 之后每打开一次这个文件，文件引用数+1
			lock (fileCounters) {
				if (fileCounters.ContainsKey (relativePath)) {
					var item = fileCounters [relativePath];
					item.counter++;
					return item.file;
				}

				var file = new DiskFile (relativePath);
				fileCounters [relativePath] = new FileCounterItem () {
					file = file,
					counter = 1
				};
				return file;
			}
		}

		private DiskFile (string relativePath)
		{
			this.relativePath = relativePath;

			if (string.IsNullOrWhiteSpace (ResourceFile.DiskFileRoot))
				throw new InvalidOperationException ("Disk file not available: ResourceFile.DiskFileRoot is not set");

			if (!File.Exists (AbsolutePath))
				throw new FileNotFoundException (string.Format ("Disk file '{0}' is not found", AbsolutePath));

			fileInfo = new FileInfo (AbsolutePath);
			fs = fileInfo.Open (FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public override void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			Utils.CheckReadParameters (dst, dstStartIndex, readFilePos, readSize, fs.Length);

			lock (fs) {
				fs.Position = readFilePos;
				fs.Read (dst, dstStartIndex, readSize);
			}
		}

		public override void Close ()
		{
			// 关闭文件时文件引用数-1
			// 当引用数<=0时，释放文件流
			bool closeFs = false;
			lock (fileCounters) {
				if (fileCounters.ContainsKey (RelativePath)) {
					var item = fileCounters [relativePath];
					item.counter--;
					if (item.counter <= 0) {
						closeFs = true;
						fileCounters.Remove (relativePath);
					}
				} else {
					closeFs = true;
				}
			}
			if (closeFs) {
				lock (fs) {
					fs.Close ();
				}
			}
		}
	}
}

