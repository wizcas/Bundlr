using System;
using System.IO;
using Bundlr;
using System.Collections.Generic;

namespace Bundlr
{
	/// <summary>
	/// 打包文件信息集合类，在基础的集合功能上提供了相同相对路径的冲突处理
	/// </summary>
	internal class PackingFileCollection : IEnumerable<PackingFile>
	{
		/// <summary>
		/// 打包文件信息列表，用于保证打包文件的顺序
		/// </summary>
		private List<PackingFile> files = new List<PackingFile> ();
		/// <summary>
		/// 相对路径验证字典，用于处理相对路径冲突的问题，以及通过相对路径获取打包文件信息
		/// </summary>
		private Dictionary<string, PackingFile> validationDict = new Dictionary<string, PackingFile> ();

		/// <summary>
		/// 返回已添加的打包文件数量
		/// </summary>
		/// <value>The count.</value>
		internal int Count {
			get {
				return files.Count;
			}
		}

		/// <summary>
		/// 添加一个打包文件
		/// </summary>
		/// <param name="relativePath">相对路径，不区分大小写</param>
		/// <param name="fileInfo">文件的系统信息</param>
		/// <exception cref="FileNotFoundException">当在磁盘上找不到文件时会抛出该异常</exception>
		/// <exception cref="ArgumentException">当使用了一个已注册的相对路径时会抛出此异常</exception>
		internal void Add (string relativePath, FileInfo fileInfo)
		{
			Add (new PackingFile (relativePath, fileInfo));
		}

		/// <summary>
		/// 添加一个打包文件
		/// </summary>
		/// <param name="file">打包文件信息对象</param>
		/// <exception cref="FileNotFoundException">当在磁盘上找不到文件时会抛出该异常</exception>
		/// <exception cref="ArgumentException">当使用了一个已注册的相对路径时会抛出此异常</exception>
		internal void Add (PackingFile file)
		{
			if (!file.fileInfo.Exists) {
				throw new FileNotFoundException ("Failed to add a packing file", file.fileInfo.FullName);
			}
			if (validationDict.ContainsKey (file.relativePath)) {
				throw new ArgumentException (string.Format ("Conflict: the relative path '{0}' is registered in packing list",
					file.relativePath));
			}

			files.Add (file);
			validationDict [file.relativePath] = file;
		}

		/// <summary>
		/// 通过相对路径删除一个打包文件信息
		/// </summary>
		/// <param name="relativePath">要删除的相对路径，不区分大小写</param>
		/// <returns>成功删除时返回<c>true</c>，否则返回<c>false</c></returns>
		internal bool Remove (string relativePath)
		{
			if (!validationDict.ContainsKey (relativePath)) {
				return false;
			}

			var file = validationDict [relativePath];
			validationDict.Remove (relativePath);
			return files.Remove (file);
		}

		/// <summary>
		/// 清空打包文件列表
		/// </summary>
		internal void Clear ()
		{
			files.Clear ();
			validationDict.Clear ();
		}

		#region IEnumerable implementation

		public IEnumerator<PackingFile> GetEnumerator ()
		{
			return files.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return files.GetEnumerator ();
		}

		#endregion
	}

	/// <summary>
	/// 打包文件信息类
	/// </summary>
	internal class PackingFile
	{
		/// <summary>
		/// 相对路径
		/// </summary>
		internal string relativePath;
		/// <summary>
		/// 文件的系统信息
		/// </summary>
		internal FileInfo fileInfo;
		/// <summary>
		/// 通过打包程序生成的文件元数据
		/// </summary>
		internal FileMeta metadata;

		internal PackingFile (string relativePath, FileInfo fileInfo)
		{
			this.relativePath = relativePath.ToLower ();
			this.fileInfo = fileInfo;
			this.metadata = null;
		}
	}
}

