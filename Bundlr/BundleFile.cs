using System;

namespace Bundlr
{
	public class BundleFile
	{
		private Bundle bundle;
		private FileMeta metadata;

		public BundleFile (Bundle bundle, string relativePath)
		{
			this.bundle = bundle;
			metadata = bundle.GetMetadata(relativePath);
		}

		public long Size {
			get{ return metadata.size; }
		}

		public void Read(byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			bundle.Read (metadata, dst, dstStartIndex, readFilePos, readSize);
		}
	}
}

