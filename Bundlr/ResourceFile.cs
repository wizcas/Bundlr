using System;

namespace Bundlr
{
	public abstract class ResourceFile
	{
		public abstract long Size { get; }

		public abstract string RelativePath { get; }

		public abstract void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize);

		public static ResourceFile Open (string relativePath)
		{
			var file = Bundles.File (relativePath);
			return file;
		}

		public virtual void Close ()
		{
		}
	}

	public class BundleFile : ResourceFile
	{
		private Bundle bundle;
		private FileMeta metadata;

		internal BundleFile (string relativePath, Bundle bundle)
		{
			this.bundle = bundle;
			metadata = bundle.GetMetadata (relativePath);
		}

		public override long Size {
			get{ return metadata.size; }
		}

		public override string RelativePath {
			get{ return metadata.relativePath; }
		}

		public override void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			bundle.Read (metadata, dst, dstStartIndex, readFilePos, readSize);
		}
	}

	public class DiskFile : ResourceFile
	{
		public override long Size {
			get {
				throw new NotImplementedException ();
			}
		}

		public override string RelativePath {
			get {
				throw new NotImplementedException ();
			}
		}

		internal DiskFile (string relativePath)
		{

		}

		public override void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			throw new NotImplementedException ();
		}
	}
}

