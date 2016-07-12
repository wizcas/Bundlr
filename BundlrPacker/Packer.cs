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

				fs.WriteByte(Convert.ToByte (metaLen));

				// Write metadata into file
				fs.Write (metadata, 0, metaLen);

				// Write file bytes
				foreach (var file in files) {
					Console.WriteLine (string.Format ("Packing file '{0}'...", file.relativePath));
					file.Pack (fs);
				}
			}
		}

		private byte[] GenerateMetadata (List<PackingFile> files)
		{
			Console.WriteLine ("Generating metadata...");
			using (MemoryStream s = new MemoryStream ()) {
				long pos = 0;
				foreach (var file in files) {
					pos = file.GetMetadata (s, pos);
				}
				return s.ToArray ();
			}
		}
	}
}