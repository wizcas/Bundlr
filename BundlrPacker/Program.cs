using System;
using System.IO;
using System.Collections.Generic;

namespace BundlrPacker
{
	class MainClass
	{
		private static string rootPath;

		public static void Main (string[] args)
		{
			var pathArg = Repath("~/test");

			rootPath = pathArg;

			var di = new DirectoryInfo (rootPath);

			List<PackingFile> files = new List<PackingFile> ();
			WalkDir (di, files);

			Packer packer = new Packer (Repath ("~/test.blr"));
			packer.Pack (files);
		}

		public static void WalkDir (DirectoryInfo di, List<PackingFile> files)
		{
			if (!di.Exists)
				return;
			foreach (var fsi in di.GetFileSystemInfos()) {
				if (fsi is DirectoryInfo)
					WalkDir (fsi as DirectoryInfo, files);
				else if (fsi is FileInfo) {
					FileInfo fi = fsi as FileInfo;
					string relativePath = fi.FullName.Replace (rootPath, string.Empty).Replace ("\\", Constants.PATH_SEPARATER);
					if (relativePath.StartsWith (Constants.PATH_SEPARATER))
						relativePath = relativePath.Substring (1);
					files.Add (new PackingFile (fi, relativePath));
				}
			}
		}

		public static string Repath(string path){
			var env = Environment.OSVersion.Platform;
			if (env == PlatformID.Unix || env == PlatformID.MacOSX) {
				// Unix-based系统下将开头的~转换成用户目录
				if ((path.Length == 1 && path == "~")
					||
					path.Length > 1 && path.StartsWith ("~/")) {
					path = Environment.GetEnvironmentVariable ("HOME") +
						(path.Length > 1 
							? path.Substring (1) : string.Empty);
				}
			}
			return path;
		}
	}
}
