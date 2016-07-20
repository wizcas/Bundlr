﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Bundlr
{
	/// <summary>
	/// 数据打包处理类
	/// </summary>
	public class Packer
	{
		public static readonly Version CurrentVersion = new Version (1, 0, 0);

		public string bundlePathWithName;
		private PackingFileCollection packingFiles = new PackingFileCollection ();

		public int FilesCount {
			get{ return packingFiles.Count; }
		}

		public Packer (string bundlePathWithName)
		{
			this.bundlePathWithName = bundlePathWithName;
		}

		/// <summary>
		/// 添加一个打包文件
		/// </summary>
		/// <param name="fileInfo">文件信息</param>
		/// <param name="relativePath">文件相对路径</param>
		public void AddFile (FileInfo fileInfo, string relativePath)
		{
			packingFiles.Add (relativePath, fileInfo);
		}

		/// <summary>
		/// 清除所有已添加的打包文件
		/// </summary>
		public void Clear()
		{
			packingFiles.Clear ();
		}

		/// <summary>
		/// 执行打包
		/// </summary>
		public void Pack ()
		{
			using (FileStream fs = new FileStream (bundlePathWithName, FileMode.Create, FileAccess.Write)) {
				// All files' metadata
				byte[] metaBytes = GenerateMetadata ();

				// Calculate the size of bundle's header and the start position of bundle's body (data part)
				int headerSize = CalculateHeaderSize (metaBytes.Length);
				long bodyBeginPos = Utils.GetByteAlignedPos (headerSize);

				// Bundle's own info
				byte[] infoBytes = GenerateInfo (bodyBeginPos);

				fs.Write (headerSize - sizeof(int)); // Writes (info size + files' metadata size)
				fs.Write (infoBytes, 0, infoBytes.Length); // Writes bundle's info
				fs.Write (metaBytes, 0, metaBytes.Length); // Writes all files' metadata

				// Write all files
				WriteFileBytes (fs, bodyBeginPos);

				fs.Flush ();
			}

			Console.WriteLine (string.Format ("Successfully packed to '{0}'.", bundlePathWithName));
		}

		/// <summary>
		/// 生成打包文件的元数据
		/// </summary>
		/// <returns>打包文件元数据</returns>
		private byte[] GenerateMetadata ()
		{
			Console.WriteLine ("Generating metadata...");
			using (MemoryStream s = new MemoryStream ()) {
				//遍历所有文件，计算每个文件的起始位置并写入元数据
				long pos = 0;
				foreach (var file in packingFiles) {
					var relPath = file.relativePath;
					var fileInfo = file.fileInfo;
					var fileSize = fileInfo.Length;

					var meta = new FileMeta (relPath, pos, fileSize);
					file.metadata = meta;

					meta.Serialize (s);

					pos += fileSize;
					// 下个文件数据的起始位置字节对齐
					pos = Utils.GetByteAlignedPos (pos);
				}
				s.Flush ();
				return s.ToArray ();
			}
		}

		/// <summary>
		/// 计算包头长度
		/// </summary>
		/// <returns>包头长度</returns>
		/// <param name="metaSize">打包文件元数据长度</param>
		private int CalculateHeaderSize (int metaSize)
		{
			// 文件头总长度 = 包大小长度（int32) + 版本号长度（Version类定义）+ 数据起始位置长度（int64）+ 文件元数据长度（int32）
			return sizeof(int) + CurrentVersion.Size + sizeof(long) + metaSize;
		}

		/// <summary>
		/// 生成数据包信息
		/// </summary>
		/// <returns>数据包信息数据</returns>
		/// <param name="dataStartOffset">数据包体部分起始位置</param>
		private byte[] GenerateInfo (long bodyBeginPos)
		{
			Console.WriteLine ("Generating file info...");
			using (MemoryStream s = new MemoryStream ()) {
				//写入数据包版本号
				CurrentVersion.Serialize (s);
				//写入数据包起始位置
				s.Write (bodyBeginPos);
				s.Flush ();
				return s.ToArray ();
			}
		}
		/// <summary>
		/// 依次将打包文件的文件数据写入数据包
		/// </summary>
		/// <param name="s">要写入的流</param>
		/// <param name="startOffset">包体数据的起始写入位置</param>
		private void WriteFileBytes (Stream s, long bodyBeginPos)
		{
			foreach (var file in packingFiles) {
				// 移动指针到指定文件数据的起始位置
				long pos = file.metadata.pos + bodyBeginPos;
				long offset = pos - s.Position;
				s.Seek (offset, SeekOrigin.Current);
							
				using (FileStream fs = file.fileInfo.Open (FileMode.Open)) {
					s.WriteFromStream (fs, file.metadata.size);
				}
			}
		}
	}
}