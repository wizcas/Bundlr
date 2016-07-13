using System;
using System.IO;
using System.Collections.Generic;

namespace Bundlr
{
	/// <summary>
	/// 数据打包处理类
	/// </summary>
	public class Packer
	{
		public string bundlePathWithName;
		public Dictionary<string, FileInfo> packingFiles = new Dictionary<string, FileInfo> ();

		public Packer (string bundlePathWithName)
		{
			this.bundlePathWithName = bundlePathWithName;
		}

		public void AddFile(FileInfo fileInfo, string relativePath)
		{
			if (packingFiles.ContainsKey (relativePath)) {
				Console.WriteLine (string.Format ("Conflict: '{0}' is overwritten with '{1}'", 
					relativePath, fileInfo.FullName));
			}

			packingFiles [relativePath] = fileInfo;
		}

		public void Pack (List<PackingFile> files)
		{
			using (FileStream fs = new FileStream (bundlePathWithName, FileMode.Create, FileAccess.Write)) {
				using (BinaryWriter wtr = new BinaryWriter (fs, System.Text.Encoding.UTF8)) {
					byte[] metadata = GenerateMetadata (files);
					int metaLen = metadata.Length;

					wtr.Write (metaLen);

					// Write metadata into file
					fs.Write (metadata, 0, metaLen);

					// Write file bytes
					foreach (var file in files) {
						Console.WriteLine (string.Format ("Packing file '{0}'...", file.metadata.relativePath));
						file.Pack (fs);
					}
				}
			}
			Console.WriteLine (string.Format ("Successfully packed to '{0}'.", bundlePathWithName));
		}

		public void Pack(List<FileInfo> fileInfos, List<string> relativePaths)
		{
			
		}

		private byte[] GenerateMetadata (List<PackingFile> files)
		{
			Console.WriteLine ("Generating metadata...");
			using (MemoryStream s = new MemoryStream ()) {
				using (BinaryWriter wtr = new BinaryWriter (s)) {
					long pos = 0;
					foreach (var file in files) {
						pos = file.GenerateMetadata (wtr, pos);
					}
					return s.ToArray ();
				}
			}
		}
	}
}