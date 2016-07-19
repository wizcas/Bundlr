using System;
using System.IO;
using System.Collections.Generic;

namespace Bundlr
{
	public abstract class ResourceFile
	{
		public static string DiskFileRoot = "";

		public abstract long Size { get; }

		public abstract string RelativePath { get; }

		public abstract void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize);

		public static ResourceFile Open (string relativePath)
		{
			ResourceFile file = Bundles.File (relativePath);
			if (file == null)
				file = DiskFile.OpenFile (relativePath);
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
		#region Nesting

		private struct FileCounterItem
		{
			internal DiskFile file;
			internal int counter;
		}

		#endregion

		private static Dictionary<string, FileCounterItem> fileCounters = new Dictionary<string, FileCounterItem> ();

		private FileInfo fileInfo;
		private string relativePath;
		private FileStream fs;

		public override long Size {
			get {
				return fileInfo.Length;
			}
		}

		public override string RelativePath {
			get {
				return relativePath;
			}
		}

		private string AbsolutePath {
			get {
				return Path.Combine (ResourceFile.DiskFileRoot, RelativePath);
			}
		}

		internal static DiskFile OpenFile (string relativePath)
		{
			lock (fileCounters) {
				if (fileCounters.ContainsKey (relativePath)) {
					var item = fileCounters [relativePath];
					item.counter++;
					return item.file;
				}

				var file = new DiskFile (relativePath);
				fileCounters [relativePath] = new FileCounterItem () {
					file = file,
					counter = 1
				};
				return file;
			}
		}

		private DiskFile (string relativePath)
		{
			this.relativePath = relativePath;

			if (string.IsNullOrWhiteSpace (ResourceFile.DiskFileRoot))
				throw new InvalidOperationException ("Disk file not available: ResourceFile.DiskFileRoot is not set");

			if (!File.Exists (AbsolutePath))
				throw new FileNotFoundException (string.Format ("Disk file '{0}' is not found", AbsolutePath));

			fileInfo = new FileInfo (AbsolutePath);
			fs = fileInfo.Open (FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public override void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			Utils.CheckReadParameters (dst, dstStartIndex, readFilePos, readSize, fs.Length);

			lock (fs) {
				fs.Position = readFilePos;
				fs.Read (dst, dstStartIndex, readSize);
			}
		}

		public override void Close ()
		{
			bool closeFs = false;
			lock (fileCounters) {
				if (fileCounters.ContainsKey (RelativePath)) {
					var item = fileCounters [relativePath];
					item.counter--;
					if (item.counter <= 0) {
						closeFs = true;
						fileCounters.Remove (relativePath);
					}
				} else {
					closeFs = true;
				}
			}
			if (closeFs) {
				lock (fs) {
					fs.Close ();
				}
			}
		}
	}
}

