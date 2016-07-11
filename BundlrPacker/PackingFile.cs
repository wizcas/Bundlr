using System;
using System.IO;

namespace BundlrPacker
{
	public class PackingFile
	{
		public FileInfo fileInfo;
		public string relativePath;

		public PackingFile (FileInfo file, string relativePath)
		{
			this.fileInfo = fileInfo;
			this.relativePath = relativePath;
		}

		public int GetMetadata (Stream output, int pos)
		{
			using (BinaryWriter wtr = new BinaryWriter (output, System.Text.Encoding.UTF8)) {
				wtr.Write (relativePath);
				wtr.Write (pos);
				wtr.Write (fileInfo.Length);
			}
			return pos + fileInfo.Length;
		}

		public void Pack (Stream output)
		{
			if (!fileInfo.Exists) {
				Console.WriteLine (string.Format ("File '{0}' not exists", relativePath));
				return;
			}
				
			using (FileStream fs = fileInfo.Open (FileMode.Open)) {
				byte[] buffer = new byte[1024];
				int numBytesRead = 0;
				while (true) {
					int n = fs.Read (buffer, numBytesRead, 1024);
					if (n == 0)
						break;
					output.Write (buffer, 0, buffer.Length);
					numBytesRead += n;
				}

			}
		}

		public override string ToString ()
		{
			return relativePath;
		}
	}
}

