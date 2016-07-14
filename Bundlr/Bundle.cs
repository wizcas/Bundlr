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

		public Action<string> onDisposed;

		public string Id { get ; private set; }

		public Version Version { get; private set; }

		public long DataStartOffset{ get; private set; }

		internal string FilePath{ get; private set; }

		private int headerLen;

		public string[] FileList {
			get {
				return dictMetadata.Keys.ToArray ();
			}
		}

		internal static Bundle Load (string id, string filePath)
		{
			filePath = Utils.Repath (filePath);
			if (!File.Exists (filePath))
				throw new ArgumentException ("Cannot file file: " + filePath);
			var ret = new Bundle (filePath);
			ret.Id = id;
			return ret;
		}

		private Bundle (string filePath)
		{
			FilePath = filePath;
			fs = new FileStream (FilePath, FileMode.Open, FileAccess.Read);
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

		public byte[] Get (string relativePath)
		{
			if (!Has (relativePath))
				return null;

			FileMeta meta = dictMetadata [relativePath];

			if (meta == null)
				return null;

			lock (fs) {
				try {
					// 计算新起始位置和当前流指针位置的偏差值
					long newPos = DataStartOffset + meta.pos;
					long offset2Current = newPos - fs.Position;
					// 如果新起始位置在别处，则根据与当前指针位置的偏差值移动指针
					if (offset2Current != 0)
						fs.Seek (offset2Current, SeekOrigin.Current);
				
					using (MemoryStream ms = new MemoryStream ()) {
//						Utils.Stream2Stream (fs, ms, meta.length);
						ms.WriteFromStream (fs, meta.size);
						return ms.ToArray ();
					}
				} catch (Exception e) {
					Console.WriteLine ("Extracting data error: " + e);
					return null;
				}
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
					onDisposed (Id);
					onDisposed = null;
				}
			}
		}
	}
}

