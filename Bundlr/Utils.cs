using System;
using System.IO;

namespace Bundlr
{
	public class Utils
	{
		public static string Repath (string path)
		{
			if (Constants.OSPlatform == PlatformID.Unix || Constants.OSPlatform == PlatformID.MacOSX) {
				// Unix-based系统下将开头的~转换成用户目录
				if ((path.Length == 1 && path == "~")
				    ||
				    path.Length > 1 && path.StartsWith ("~/")) {
					path = Environment.GetEnvironmentVariable ("HOME") +
					(path.Length > 1 
							? path.Substring (1) : string.Empty);
				}
			}
			return path;
		}

		public static void Stream2Stream (Stream input, Stream output, long numBytesTotal)
		{
			byte[] buffer = new byte[numBytesTotal];
			int numBytesRead = 0;
			int numBytesLeft = (int)numBytesTotal;
			// Read file and write bytes into the output stream
			while (numBytesLeft > 0) {
				int n = input.Read (buffer, numBytesRead, numBytesLeft);
				if (n == 0)
					break;
				output.Write (buffer, numBytesRead, n);
				numBytesRead += n;
				numBytesLeft -= n;
			}
			output.Flush ();
		}

		public static long GetByteAlignedPos(long pos)
		{
			return (long)Math.Ceiling ((double)pos / Constants.NumOfBytesAlignment) * (long)Constants.NumOfBytesAlignment;
		}

		public static void CheckReadParameters(byte[] dst, int dstStartIndex, int readFilePos, int readSize, long fileSize)
		{
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

			if (readFilePos + readSize > fileSize) {
				throw new ArgumentException ("Trying to read beyond the EOF");
			}
		}
	}
}

