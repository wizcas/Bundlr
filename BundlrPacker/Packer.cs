using System;
using System.IO;
using System.Collections.Generic;

namespace BundlrPacker
{
	public class Packer
	{
		public string bundlePath;

		public Packer (string bundlePath)
		{
			this.bundlePath = bundlePath;
		}

		public void Pack (List<PackingFile> files)
		{
			using (FileStream fs = new FileStream (bundlePath, FileMode.Create, FileAccess.Write)) {
				byte[] metadata = GenerateMetadata (files);
				int metaLen = metadata.Length;

				// Write metadata's length (int) into file
				using (BinaryWriter wtr = new BinaryWriter (fs)) {
					fs.Write (metaLen);
				}
				// Write metadata into file
				fs.Write (metadata, 0, metaLen);

				// Write file bytes
				foreach (var file in files) {
					file.Pack (fs);
				}
			}
		}

		private byte[] GenerateMetadata (List<PackingFile> files)
		{
			using (MemoryStream s = new MemoryStream ()) {
				int pos = 0;
				foreach (var file in files) {
					pos = file.GetMetadata (s, pos);
				}
				return s.ToArray ();
			}
		}
	}
}