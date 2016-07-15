using System;

namespace Bundlr
{
	public class BundleFile
	{
		private Bundle bundle;
		private FileMeta metadata;

		internal BundleFile (string relativePath, Bundle bundle)
		{
			this.bundle = bundle;
			metadata = bundle.GetMetadata(relativePath);
		}

		public long Size {
			get{ return metadata.size; }
		}

		public string RelativePath
		{
			get{ return metadata.relativePath; }
		}

		public void Read(byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			bundle.Read (metadata, dst, dstStartIndex, readFilePos, readSize);
		}
	}
}

