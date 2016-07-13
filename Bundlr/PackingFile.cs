using System;
using System.IO;
using Bundlr;

namespace Bundlr
{
	public class PackingFile
	{
		public FileInfo fileInfo;
		public string relativePath;

		public PackingFile (FileInfo fileInfo, string relativePath)
		{
			this.fileInfo = fileInfo;
			this.relativePath = relativePath;
		}

		public long GetMetadata (Stream output, BinaryWriter wtr, long pos)
		{
			wtr.Write (relativePath);
			wtr.Write (pos);
			wtr.Write (fileInfo.Length);
			wtr.Flush ();

			return pos + fileInfo.Length;
		}

		public void Pack (Stream output)
		{
			if (!fileInfo.Exists) {
				Console.WriteLine (string.Format ("File '{0}' not exists", relativePath));
				return;
			}
				
			using (FileStream fs = fileInfo.Open (FileMode.Open)) {
				Utils.Stream2Stream (fs, output, fs.Length);
			}
		}

		public override string ToString ()
		{
			return relativePath;
		}
	}
}

