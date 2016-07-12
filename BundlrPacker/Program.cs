using System;
using System.IO;
using System.Collections.Generic;
using Bundlr;
using System.Diagnostics;

namespace BundlrPacker
{
	class MainClass
	{
		private static string rootPath;

		public static void PrintHelp ()
		{
			Console.WriteLine (
				"Usage:\tBundlrPacker.exe <ROOT_DIR> <OUTPUT_FILE>\r\n" +
				"<ROOT_DIR>\tThe root directory of packing files." +
				"<OUTPUT_FILE>\tThe path of output file."
			);
		}

		public static void Main (string[] args)
		{
			if (args.Length < 2) {
				PrintHelp ();
				return;
			}
							
			var	inPath = Utils.Repath (args [0]);
			var outPath = Utils.Repath (args [1]);

			rootPath = inPath;

			if (!outPath.EndsWith (".blr"))
				outPath += ".blr";

			var di = new DirectoryInfo (rootPath);

			Stopwatch timer = new Stopwatch ();
			timer.Reset ();
			timer.Start ();

			List<PackingFile> files = new List<PackingFile> ();
			WalkDir (di, files);

			Packer packer = new Packer (Utils.Repath (outPath));
			packer.Pack (files);

			timer.Stop ();
			Console.WriteLine ("{0} files are packed. ({1}ms)", files.Count, timer.ElapsedMilliseconds);
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
	}
}
