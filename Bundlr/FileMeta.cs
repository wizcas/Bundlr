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

		/// <summary>
		/// 文件所属数据包的唯一ID，内部使用
		/// 用于在读文件数据时，判断该文件是否属于指定的数据包
		/// </summary>
		internal string bundleUid;

		internal FileMeta (string relPath, long pos, long length)
		{
			this.relativePath = relPath;
			this.pos = pos;
			this.size = length;
		}

		/// <summary>
		/// 将元数据序列化到指定流
		/// </summary>
		/// <param name="stream">元数据写入的流</param>
		internal void Serialize(Stream stream)
		{
			stream.Write (relativePath);
			stream.Write (pos);
			stream.Write (size);
			stream.Flush ();
		}

		/// <summary>
		/// 从指定流中反序列化元数据，并返回一个新的元数据对象
		/// </summary>
		/// <param name="stream">从中反序列化元数据的流</param>
		internal static FileMeta Deserialize(Stream stream)
		{
			string relPath = stream.ReadString ();
			long pos = stream.ReadInt64 ();
			long length = stream.ReadInt64 ();
			return new FileMeta (relPath, pos, length);
		}
	}
}

