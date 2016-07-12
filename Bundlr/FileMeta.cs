using System;

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
		public string RelativePath{ get; private set; }

		/// <summary>
		/// 数据起始位置
		/// </summary>
		/// <value>The position.</value>
		public long Pos{ get; private set; }

		/// <summary>
		/// 数据长度
		/// </summary>
		/// <value>The length.</value>
		public long Length{ get; private set; }

		public FileMeta (string relPath, long pos, long length)
		{
			RelativePath = relPath;
			Pos = pos;
			Length = length;
		}
	}
}

