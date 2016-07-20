using System;

namespace Bundlr
{

	/// <summary>
	/// 数据包打包文件访问对象
	/// </summary>
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

}

