using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;

namespace Bundlr
{
	internal class Bundle : IDisposable
	{
		private Dictionary<string, FileMeta> dictMetadata = new Dictionary<string, FileMeta> ();
		private FileStream fs;
		private int headerLen;
		private long dataStartOffset;

		private Mutex fsMutex = new Mutex (false);

		internal Action<Bundle> onDisposed;

		internal string Uid{ get; private set; }

		internal Version Version { get; private set; }

		internal string FilePath{ get; private set; }


		internal string[] FileList {
			get {
				return dictMetadata.Keys.ToArray ();
			}
		}

		internal static Bundle Load (string filePath)
		{
			filePath = Utils.Repath (filePath);
			if (!File.Exists (filePath))
				throw new ArgumentException ("Cannot file file: " + filePath);
			var ret = new Bundle (filePath);
			ret.Uid = Guid.NewGuid ().ToString ();
			return ret;
		}

		private Bundle (string filePath)
		{
			FilePath = filePath;

			fsMutex.WaitOne ();

			LoadMetadata ();
			if (Bundles.Caching == BundleCaching.AlwaysCached)
				OpenFileStream ();

			fsMutex.ReleaseMutex ();
		}

		private void LoadMetadata ()
		{
			OpenFileStream ();
			fs.Seek (0, SeekOrigin.Begin);
			headerLen = fs.ReadInt32 () + sizeof(int);

			Version = Version.Deserialize (fs);
			dataStartOffset = fs.ReadInt64 ();

			while (fs.Position < headerLen) {
				var fm = FileMeta.Deserialize (fs);
				dictMetadata [fm.relativePath] = fm;
			}
			CloseFileStream ();
		}

		internal bool Has (string relativePath)
		{
			return dictMetadata.ContainsKey (relativePath);
		}

		internal FileMeta GetMetadata (string relativePath)
		{
			if (!Has (relativePath))
				return null;

			return dictMetadata [relativePath];
		}

		internal void Read (FileMeta meta, byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			if (meta == null)
				throw new ArgumentNullException ("meta");

			Utils.CheckReadParameters (dst, dstStartIndex, readFilePos, readSize, meta.size);

			Profiler.StartSample ("wait one read");
			fsMutex.WaitOne ();
			Profiler.EndSample ("wait one read");

			if (Bundles.Caching == BundleCaching.None)
				OpenFileStream ();
				
			// 计算新的读取位置
			long newPos = dataStartOffset + meta.pos + readFilePos;
			// 移动指针到读取位置
			long offset2Current = newPos - fs.Position;
			if (offset2Current != 0) {
				Profiler.StartSample ("seek");
				fs.Seek (offset2Current, SeekOrigin.Current);
				Profiler.EndSample ("seek");
			}

			fs.Read (dst, dstStartIndex, readSize);

			if (Bundles.Caching == BundleCaching.None)
				CloseFileStream ();

			Profiler.StartSample ("release read");
			fsMutex.ReleaseMutex ();
			Profiler.EndSample ("release read");
		}

		public void Dispose ()
		{
			fsMutex.WaitOne ();
			CloseFileStream ();
			dictMetadata.Clear ();
			fsMutex.ReleaseMutex ();
			if (onDisposed != null) {
				onDisposed (this);
				onDisposed = null;
			}
		}

		private void OpenFileStream ()
		{
			Profiler.StartSample ("open");
			fs = new FileStream (FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
			Profiler.EndSample ("open");
		}

		private void CloseFileStream ()
		{
			Profiler.StartSample ("close");
			if (fs == null)
				return;
			fs.Close ();
			fs = null;
			Profiler.EndSample ("close");
		}
	}
}

