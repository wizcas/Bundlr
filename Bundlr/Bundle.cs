﻿using System;
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
		private BinaryReader rdr;

		public string Id { get ; private set; }

		public int MetaLen { get; private set; }

		private int HeaderLen {
			get {
				return MetaLen + sizeof(int);
			}
		}

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
			fs = new FileStream (filePath, FileMode.Open, FileAccess.Read);
			rdr = new BinaryReader (fs, Encoding.UTF8);
			LoadMetadata ();
		}

		private void LoadMetadata ()
		{
			fs.Seek (0, SeekOrigin.Begin);
			MetaLen = rdr.ReadInt32 ();

			while (fs.Position < HeaderLen) {
				string relPath = rdr.ReadString ();
				long pos = rdr.ReadInt64 ();
				long length = rdr.ReadInt64 ();
				FileMeta fm = new FileMeta (relPath, pos, length);
				dictMetadata [relPath] = fm;
			}
		}

		public void ShowAll ()
		{
			Console.WriteLine ("[Files in bundle]");
			foreach (var kv in dictMetadata) {
				Console.WriteLine (string.Format ("\t{0}: {1}, {2}", kv.Key, kv.Value.Pos, kv.Value.Length));
			}
		}

		public bool Has (string relativePath)
		{
			lock (dictMetadata) {
				return dictMetadata.ContainsKey (relativePath);
			}
		}

		public byte[] Get (string relativePath)
		{
			if (!Has (relativePath))
				return null;

			FileMeta meta = null;
			lock (dictMetadata) {
				meta = dictMetadata [relativePath];
			}

			if (meta == null)
				return null;

			lock (fs) {
				// 计算新起始位置和当前流指针位置的偏差值
				long newPos = HeaderLen + meta.Pos;
				long offset2Current = newPos - fs.Position;
				// 如果新起始位置在别处，则根据与当前指针位置的偏差值移动指针
				if (offset2Current != 0)
					fs.Seek (offset2Current, SeekOrigin.Current);
				//fs.Read(data, 0, meta.Length);
				using (MemoryStream ms = new MemoryStream ()) {
					Utils.Stream2Stream (fs, ms, meta.Length);
					return ms.ToArray ();
				}
			}
		}

		public void Dispose ()
		{
			lock (this) {
				if (fs != null) {
					fs.Dispose ();
					fs = null;
				}
				if (rdr != null) {
					rdr.Dispose ();
					rdr = null;
				}
				dictMetadata.Clear ();
			}
		}
	}
}

