using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bundlr
{
	public class Bundle : IDisposable
	{
		private Dictionary<string, FileMeta> dictMetadata = new Dictionary<string, FileMeta> ();
		private FileStream fs;
		private BinaryReader rdr;
		private MemoryStream ms;

		public int MetaLen { get; private set; }
		private int HeaderLen {
			get {
				return MetaLen + sizeof(int);
			}
		}

		private Bundle (string filePath)
		{
			fs = new FileStream (filePath, FileMode.Open, FileAccess.Read);
			rdr = new BinaryReader (fs, Encoding.UTF8);
			ms = new MemoryStream ();
		}

		private void LoadMetadata ()
		{
			fs.Seek (0, SeekOrigin.Begin);
			MetaLen = rdr.ReadInt32 ();

			int headerLen = MetaLen + sizeof(int);


			while (fs.Position < headerLen) {
				string relPath = rdr.ReadString ();
				long pos = rdr.ReadInt64 ();
				long length = rdr.ReadInt64 ();
				FileMeta fm = new FileMeta (relPath, pos, length);
				dictMetadata [relPath] = fm;
			}

			foreach (var kv in dictMetadata) {
				Console.WriteLine (string.Format ("{0}: {1}, {2}", kv.Key, kv.Value.Pos, kv.Value.Length));
			}
		}

		internal static Bundle Load (string filePath)
		{
			filePath = Utils.Repath (filePath);
			if (!File.Exists (filePath))
				throw new ArgumentException ("Cannot file file: " + filePath);
			var bundle = new Bundle (filePath);
			bundle.LoadMetadata ();
			return bundle;
		}

		public bool Has(string relativePath){
			return dictMetadata.ContainsKey (relativePath);
		}

		public byte[] Get(string relativePath)
		{
			if (!Has (relativePath))
				return null;

			var meta = dictMetadata [relativePath];
			byte[] data = new byte[meta.Length];

			// 计算新起始位置和当前流指针位置的偏差值
			long newPos = HeaderLen + meta.Pos;
			long offset2Current = newPos - fs.Position;
			// 如果新起始位置在别处，则根据与当前指针位置的偏差值移动指针
			if (offset2Current != 0)
				fs.Seek (offset2Current, SeekOrigin.Current);
			fs.Read(data, 0, meta.Length);
			return data;
		}

		public void Dispose ()
		{
			if (fs != null) {
				fs.Dispose ();
				fs = null;
			}
			if (rdr != null) {
				rdr.Dispose ();
				rdr = null;
			}
			if (ms != null) {
				ms.Dispose ();
				ms = null;
			}
		}
	}
}

