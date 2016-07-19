using System;
using System.IO;
using System.Text;

namespace Bundlr
{
	/// <summary>
	/// 流操作扩展类
	/// </summary>
	public static class StreamExtension
	{
		/// <summary>
		/// 判断系统是否使用的是小端序
		/// </summary>
		private readonly static bool IsLittleEndian = BitConverter.IsLittleEndian;

		private static byte[] UnifyBytesOrder (byte[] bytes)
		{
			if (!IsLittleEndian) {
				Array.Reverse (bytes);
			} 
			return bytes;
		}

		#region Write

		public static Stream WriteFromStream(this Stream s, Stream source, long size)
		{
			byte[] buffer = new byte[size];
			int numBytesRead = 0;
			int numBytesLeft = (int)size;
			// Read file and write bytes into the output stream
			while (numBytesLeft > 0) {
				int n = source.Read (buffer, numBytesRead, numBytesLeft);
				if (n == 0)
					break;
				s.Write (buffer, numBytesRead, n);
				numBytesRead += n;
				numBytesLeft -= n;
			}
			s.Flush ();
			return s;
		}

		public static Stream Write (this Stream s, UInt16 value)
		{
			var bytes = UnifyBytesOrder (BitConverter.GetBytes (value));
			s.Write (bytes, 0, bytes.Length);
			return s;
		}

		public static Stream Write (this Stream s, int value)
		{
			var bytes = UnifyBytesOrder (BitConverter.GetBytes (value));
			s.Write (bytes, 0, bytes.Length);
			return s;
		}

		public static Stream Write (this Stream s, long value)
		{
			var bytes = UnifyBytesOrder (BitConverter.GetBytes (value));
			s.Write (bytes, 0, bytes.Length);
			return s;
		}

		public static Stream Write (this Stream s, string value)
		{
			var bytes = Encoding.UTF8.GetBytes (value);
			if (bytes.Length > UInt16.MaxValue) {
				throw new ArgumentException (string.Format ("String is too long: the length must less or equal than {0}", UInt16.MaxValue));
			}
			UInt16 len = (UInt16)bytes.Length;

			s.Write (len)
				.Write (bytes, 0, bytes.Length);
			return s;
		}

		#endregion

		#region Read

		public static UInt16 ReadUInt16 (this Stream s)
		{
			var bytes = new byte[sizeof(UInt16)];
			s.Read (bytes, 0, bytes.Length);
			return BitConverter.ToUInt16 (bytes, 0);
		}

		public static int ReadInt32 (this Stream s)
		{
			var bytes = new byte[sizeof(int)];
			s.Read (bytes, 0, bytes.Length);
			return BitConverter.ToInt32 (bytes, 0);
		}

		public static long ReadInt64 (this Stream s)
		{
			var bytes = new byte[sizeof(long)];
			s.Read (bytes, 0, bytes.Length);
			return BitConverter.ToInt64 (bytes, 0);
		}

		public static string ReadString (this Stream s)
		{
			UInt16 len = s.ReadUInt16 ();
			var bytes = new byte[len];
			s.Read (bytes, 0, bytes.Length);
			return Encoding.UTF8.GetString (bytes);
		}

		#endregion
	}
}

