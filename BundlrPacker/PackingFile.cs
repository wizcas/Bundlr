using System;
using System.IO;

namespace BundlrPacker
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
				byte[] buffer = new byte[fs.Length];
				int numBytesRead = 0;
				int numBytesLeft = (int)fs.Length;
				// Read file and write bytes into the output stream
				while (numBytesLeft > 0) {
					int n = fs.Read (buffer, numBytesRead, numBytesLeft);
					if (n == 0)
						break;
					output.Write (buffer, numBytesRead, n);
					numBytesRead += n;
					numBytesLeft -= n;
				}
			}
		}

		public override string ToString ()
		{
			return relativePath;
		}
	}
}

