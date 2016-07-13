using System;
using System.IO;
using System.Collections.Generic;

namespace Bundlr
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
			Console.WriteLine (string.Format ("Successfully packed to '{0}'.", bundlePath));
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