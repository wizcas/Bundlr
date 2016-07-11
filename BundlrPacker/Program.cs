using System;
using System.IO;
using System.Collections.Generic;

namespace BundlrPacker
{
	class MainClass
	{
		public const string PATH_SEPARATER = "/";

		public static void Main (string[] args)
		{
			var pathArg = "~/Documents";

			var env = Environment.OSVersion.Platform;
			if (env == PlatformID.Unix || env == PlatformID.MacOSX) {
				// Unix-based系统下将开头的~转换成用户目录
				if ((pathArg.Length == 1 && pathArg == "~")
				    ||
				    pathArg.Length > 1 && pathArg.StartsWith ("~/")) {
					pathArg = Environment.GetEnvironmentVariable ("HOME") +
					(pathArg.Length > 1 
							? pathArg.Substring (1) : string.Empty);
				}
			}

			var di = new DirectoryInfo (pathArg);

			List<FileInfo> files = new List<FileInfo> ();
			WalkDir (di, files);

			foreach (var fi in files) {
				string relativePath = fi.FullName.Replace (pathArg, string.Empty).Replace ("\\", PATH_SEPARATER);
				if (relativePath.StartsWith (PATH_SEPARATER))
					relativePath = relativePath.Substring (1);

				PackingFile pf = new PackingFile (fi, relativePath);

				Console.WriteLine (pf);
			}
		}

		public static void WalkDir (DirectoryInfo di, List<FileInfo> files)
		{
			if (!di.Exists)
				return;
			foreach (var fsi in di.GetFileSystemInfos()) {
				if (fsi is DirectoryInfo)
					WalkDir (fsi as DirectoryInfo, files);
				else if (fsi is FileInfo)
					files.Add (fsi as FileInfo);
			}
		}
	}
}
