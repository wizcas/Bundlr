using System;
using System.IO;

namespace Bundlr
{
	public class Utils
	{
		public static string Repath (string path)
		{
			var env = Environment.OSVersion.Platform;
			if (env == PlatformID.Unix || env == PlatformID.MacOSX) {
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
	}
}

