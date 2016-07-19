using System;
using System.IO;

namespace Bundlr
{
	public class Utils
	{
		/// <summary>
		/// 解析并重新生成完整路径（主要是针对~用户目录）
		/// </summary>
		/// <param name="path">Path.</param>
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

		/// <summary>
		/// 将数据做流到流的拷贝
		/// </summary>
		/// <param name="input">输入流</param>
		/// <param name="output">输出流</param>
		/// <param name="numBytesTotal">要拷贝的数据大小（字节长度）</param>
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

		/// <summary>
		/// 将某字节位置按照<see cref="Constants.NumOfBytesAlignment"/>进行字节对齐
		/// </summary>
		/// <returns>字节对齐后的新位置</returns>
		/// <param name="pos">要进行对齐的位置</param>
		public static long GetByteAlignedPos(long pos)
		{
			return (long)Math.Ceiling ((double)pos / Constants.NumOfBytesAlignment) * (long)Constants.NumOfBytesAlignment;
		}

		/// <summary>
		/// 检查读取数据的参数是否合法
		/// </summary>
		/// <param name="dst">输出数组</param>
		/// <param name="dstStartIndex">输出数组起始写入位置</param>
		/// <param name="readFilePos">文件起始读取位置.</param>
		/// <param name="readSize">读取长度</param>
		/// <param name="fileSize"></param>
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

