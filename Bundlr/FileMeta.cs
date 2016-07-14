using System;
using System.IO;

namespace Bundlr
{
	/// <summary>
	/// 文件元数据
	/// </summary>
	public class FileMeta
	{
		/// <summary>
		/// 相对路径
		/// </summary>
		/// <value>The relative path.</value>
		public string relativePath{ get; internal set; }

		/// <summary>
		/// 数据起始位置
		/// </summary>
		/// <value>The position.</value>
		public long pos{ get; internal set; }

		/// <summary>
		/// 数据大小
		/// </summary>
		/// <value>The length.</value>
		public long size{ get; internal set; }

		public FileMeta (string relPath, long size)
		{
			relativePath = relPath;
			pos = -1;
			this.size = size;
		}

		public FileMeta (string relPath, long pos, long length)
		{
			this.relativePath = relPath;
			this.pos = pos;
			this.size = length;
		}

		public void Serialize(Stream stream)
		{
			stream.Write (relativePath);
			stream.Write (pos);
			stream.Write (size);
			stream.Flush ();
		}

		public static FileMeta Deserialize(Stream stream)
		{
			string relPath = stream.ReadString ();
			long pos = stream.ReadInt64 ();
			long length = stream.ReadInt64 ();
			return new FileMeta (relPath, pos, length);
		}
	}
}

