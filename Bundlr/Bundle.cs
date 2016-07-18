using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Bundlr
{
	internal class Bundle : IDisposable
	{
		private Dictionary<string, FileMeta> dictMetadata = new Dictionary<string, FileMeta> ();
		private FileStream fs;
		private int headerLen;
		private long dataStartOffset;

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
			fs = new FileStream (FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
			LoadMetadata ();
		}

		private void LoadMetadata ()
		{
			lock (fs) {
				fs.Seek (0, SeekOrigin.Begin);
				headerLen = fs.ReadInt32 () + sizeof(int);

				Version = Version.Deserialize (fs);
				dataStartOffset = fs.ReadInt64 ();

				while (fs.Position < headerLen) {
					var fm = FileMeta.Deserialize (fs);
					dictMetadata [fm.relativePath] = fm;
				}
			}
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

			lock (fs) {
				// 计算新的读取位置
				long newPos = dataStartOffset + meta.pos + readFilePos;
				// 移动指针到读取位置
				long offset2Current = newPos - fs.Position;
				if (offset2Current != 0) {
					fs.Seek (offset2Current, SeekOrigin.Current);
				}

				fs.Read (dst, dstStartIndex, readSize);
			}
		}

		public void Dispose ()
		{
			lock (fs) {
				if (fs != null) {
					fs.Dispose ();
					fs = null;
				}
				dictMetadata.Clear ();
				if (onDisposed != null) {
					onDisposed (this);
					onDisposed = null;
				}
			}
		}
	}
}

