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
		/// 数据长度
		/// </summary>
		/// <value>The length.</value>
		public long length{ get; internal set; }

		public FileMeta (string relPath, long length)
		{
			relativePath = relPath;
			pos = -1;
			this.length = length;
		}

		public FileMeta (string relPath, long pos, long length)
		{
			this.relativePath = relPath;
			this.pos = pos;
			this.length = length;
		}

		public void Serialize (BinaryWriter wtr)
		{
			wtr.Write (relativePath);
			wtr.Write (pos);
			wtr.Write (length);
			wtr.Flush ();
		}

		public static FileMeta Deserialize (BinaryReader rdr)
		{
			string relPath = rdr.ReadString ();
			long pos = rdr.ReadInt64 ();
			long length = rdr.ReadInt64 ();
			return new FileMeta (relPath, pos, length);
		}
	}
}

