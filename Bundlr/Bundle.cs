using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Bundlr
{
	public class Bundle : IDisposable
	{
		private Dictionary<string, FileMeta> dictMetadata = new Dictionary<string, FileMeta> ();
		private FileStream fs;

		public Action<Bundle> onDisposed;

		public string Uid{ get; private set; }

		public Version Version { get; private set; }

		public long DataStartOffset{ get; private set; }

		internal string FilePath{ get; private set; }

		private int headerLen;

		public string[] FileList {
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
				DataStartOffset = fs.ReadInt64 ();

				while (fs.Position < headerLen) {
					var fm = FileMeta.Deserialize (fs);
					dictMetadata [fm.relativePath] = fm;
				}
			}
		}

		public void ShowAll ()
		{
			Console.WriteLine ("[Files in bundle]");
			foreach (var kv in dictMetadata) {
				Console.WriteLine (string.Format ("\t{0}: {1}, {2}", kv.Key, kv.Value.pos, kv.Value.pos));
			}
		}

		public bool Has (string relativePath)
		{
			return dictMetadata.ContainsKey (relativePath);
		}

		public FileMeta GetMetadata (string relativePath)
		{
			if (!Has (relativePath))
				return null;

			return dictMetadata [relativePath];
		}

		public void Read (FileMeta meta, byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			if (meta == null)
				throw new ArgumentNullException ("meta");

			if (dst == null)
				throw new ArgumentNullException ("dst");

			if (dstStartIndex < 0)
				throw new ArgumentException ("Start writing position of the target array must >= 0");

			if (readFilePos < 0)
				throw new ArgumentException ("Start reading position of the file must >= 0");

			if (readSize < 0)
				throw new ArgumentException ("The reading size must >= 0");

			if (dst.Length - dstStartIndex < readSize) {
				throw new ArgumentException ("Not enough space in target byte array for readSize " + readSize);
			}

			if (readFilePos + readSize > meta.size) {
				throw new ArgumentException ("Trying to read beyond the EOF");
			}

			lock (fs) {
				// 计算新的读取位置
				long newPos = DataStartOffset + meta.pos + readFilePos;
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

