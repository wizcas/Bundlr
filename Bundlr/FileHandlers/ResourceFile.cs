using System;
using System.IO;
using System.Collections.Generic;

namespace Bundlr
{
	/// <summary>
	/// 资源文件访问对象抽象基类
	/// </summary>
	public abstract class ResourceFile : IDisposable
	{
		/// <summary>
		/// 全局设定，指定本地磁盘文件的相对路径搜索位置
		/// </summary>
		public static string DiskFileRoot = "";

		/// <summary>
		/// 获取文件大小
		/// </summary>
		/// <value>The size.</value>
		public abstract long Size { get; }

		/// <summary>
		/// 获取文件相对路径
		/// </summary>
		/// <value>The relative path.</value>
		public abstract string RelativePath { get; }

		/// <summary>
		/// 将文件指定位置、长度的数据读取到输出数组中
		/// </summary>
		/// <param name="dst">读取数据的输出数组</param>
		/// <param name="dstStartIndex">输出数组的起始写入位置</param>
		/// <param name="readFilePos">文件的起始读取位置</param>
		/// <param name="readSize">要读取的字节长度</param>
		public abstract void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize);

		/// <summary>
		/// 通过相对路径打开文件，自动判断文件的获取位置
		/// </summary>
		/// <param name="relativePath">文件的相对路径</param>
		public static ResourceFile Open (string relativePath)
		{
			ResourceFile file = Bundles.File (relativePath);
			if (file == null)
				file = DiskFile.OpenFile (relativePath);
			return file;
		}

		/// <summary>
		/// 关闭文件并释放相应资源
		/// </summary>
		public virtual void Close ()
		{
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			Close ();
		}

		#endregion
	}
}

