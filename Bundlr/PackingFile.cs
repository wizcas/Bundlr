using System;
using System.IO;
using Bundlr;

namespace Bundlr
{
	public class PackingFile
	{
		public FileInfo fileInfo;
		public FileMeta metadata;

		public PackingFile (FileInfo fileInfo, string relativePath)
		{
			this.fileInfo = fileInfo;
			this.metadata = new FileMeta(relativePath, fileInfo.Length);
		}

		/// <summary>
		/// 生成元数据并通过制定的BinaryWriter写入到流中
		/// </summary>
		/// <returns>下一个文件的起始数据位置，用于写到下一个文件的元数据中</returns>
		/// <param name="wtr">写入到指定流的BinaryWriter</param>
		/// <param name="pos">该文件在元数据中记录的数据起始位置</param>
		public long GenerateMetadata (BinaryWriter wtr, long pos)
		{
			metadata.pos = pos;
			metadata.Serialize (wtr);
			return pos + metadata.length;
		}

		/// <summary>
		/// 打包文件数据到输出流
		/// </summary>
		/// <param name="output">要写入的输出流</param>
		public void Pack (Stream output)
		{
			if (!fileInfo.Exists) {
				Console.WriteLine (string.Format ("File '{0}' not exists", metadata.relativePath));
				return;
			}
				
			using (FileStream fs = fileInfo.Open (FileMode.Open)) {
				Utils.Stream2Stream (fs, output, fs.Length);
			}
		}

		public override string ToString ()
		{
			return metadata.relativePath;
		}
	}
}

