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

		/// <summary>
		/// 以指定流作为源，写入指定长度到该流中
		/// </summary>
		/// <returns>当前操作的流对象</returns>
		/// <param name="s">当前操作的流对象</param>
		/// <param name="source">作为写入源的流对象</param>
		/// <param name="size">写入的字节长度</param>
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

		/// <summary>
		/// 向流中写入无符号16位整数
		/// </summary>
		/// <param name="s">当前操作的流对象</param>
		/// <param name="value">写入值</param>
		public static Stream Write (this Stream s, UInt16 value)
		{
			var bytes = UnifyBytesOrder (BitConverter.GetBytes (value));
			s.Write (bytes, 0, bytes.Length);
			return s;
		}

		/// <summary>
		/// 向流中写入32位整数
		/// </summary>
		/// <param name="s">当前操作的流对象</param>
		/// <param name="value">写入值</param>
		public static Stream Write (this Stream s, int value)
		{
			var bytes = UnifyBytesOrder (BitConverter.GetBytes (value));
			s.Write (bytes, 0, bytes.Length);
			return s;
		}

		/// <summary>
		/// 向流中写入64位整数
		/// </summary>
		/// <param name="s">当前操作的流对象</param>
		/// <param name="value">写入值</param>
		public static Stream Write (this Stream s, long value)
		{
			var bytes = UnifyBytesOrder (BitConverter.GetBytes (value));
			s.Write (bytes, 0, bytes.Length);
			return s;
		}

		/// <summary>
		/// 向流中写入字符串
		/// </summary>
		/// <param name="s">当前操作的流对象</param>
		/// <param name="value">写入值</param>
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

		/// <summary>
		/// 从流中读取无符号16位整数
		/// </summary>
		/// <returns>读取的值</returns>
		/// <param name="s">当前操作的流对象</param>
		public static UInt16 ReadUInt16 (this Stream s)
		{
			var bytes = new byte[sizeof(UInt16)];
			s.Read (bytes, 0, bytes.Length);
			return BitConverter.ToUInt16 (bytes, 0);
		}

		/// <summary>
		/// 从流中读取32位整数
		/// </summary>
		/// <returns>读取的值</returns>
		/// <param name="s">当前操作的流对象</param>
		public static int ReadInt32 (this Stream s)
		{
			var bytes = new byte[sizeof(int)];
			s.Read (bytes, 0, bytes.Length);
			return BitConverter.ToInt32 (bytes, 0);
		}

		/// <summary>
		/// 从流中读取64位整数
		/// </summary>
		/// <returns>读取的值</returns>
		/// <param name="s">当前操作的流对象</param>
		public static long ReadInt64 (this Stream s)
		{
			var bytes = new byte[sizeof(long)];
			s.Read (bytes, 0, bytes.Length);
			return BitConverter.ToInt64 (bytes, 0);
		}

		/// <summary>
		/// 从流中读取字符串
		/// </summary>
		/// <returns>读取的字符串</returns>
		/// <param name="s">当前操作的流对象</param>
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

