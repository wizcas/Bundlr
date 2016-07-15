using System;
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

		public void AddFile (FileInfo fileInfo, string relativePath)
		{
			packingFiles.Add (relativePath, fileInfo);
		}

		public void RemoveFile (string relativePath)
		{
			packingFiles.Remove (relativePath);
		}

		public void Pack ()
		{
			using (FileStream fs = new FileStream (bundlePathWithName, FileMode.Create, FileAccess.Write)) {
				// All files' metadata
				var metaBytes = GenerateMetadata ();

				// Calculate the size of bundle's header and the start position of file data
				int headerSize = CalculateHeaderSize (metaBytes.Length);
				long dataStartOffset = Utils.GetByteAlignedPos (headerSize);

				// Bundle's own info
				var infoBytes = GenerateInfo (dataStartOffset);

				fs.Write (headerSize - sizeof(int)); // Writes (info size + files' metadata size)
				fs.Write (infoBytes, 0, infoBytes.Length); // Writes bundle's info
				fs.Write (metaBytes, 0, metaBytes.Length); // Writes all files' metadata

				// Write all files
				WriteFileBytes (fs, dataStartOffset);

				fs.Flush ();
			}

			Console.WriteLine (string.Format ("Successfully packed to '{0}'.", bundlePathWithName));
		}

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

		private int CalculateHeaderSize (int metaSize)
		{
			// 文件头总长度 = 包大小长度（int32) + 版本号长度（Version类定义）+ 数据起始位置长度（int64）+ 文件元数据长度（int32）
			return sizeof(int) + CurrentVersion.Size + sizeof(long) + metaSize;
		}

		private byte[] GenerateInfo (long dataStartOffset)
		{
			Console.WriteLine ("Generating file info...");
			using (MemoryStream s = new MemoryStream ()) {
				//写入数据包版本号
				CurrentVersion.Serialize (s);
				//写入数据包起始位置
				s.Write (dataStartOffset);
				s.Flush ();
				return s.ToArray ();
			}
		}

		private void WriteFileBytes (Stream s, long startOffset)
		{
			foreach (var file in packingFiles) {
				// 移动指针到指定文件数据的起始位置
				long pos = file.metadata.pos + startOffset;
				long offset = pos - s.Position;
				s.Seek (offset, SeekOrigin.Current);
							
				using (FileStream fs = file.fileInfo.Open (FileMode.Open)) {
					s.WriteFromStream (fs, file.metadata.size);
				}
			}
		}
	}
}