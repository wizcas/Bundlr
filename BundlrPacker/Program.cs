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
		private static List<string> ignoreFileList = new List<string>(){
			".DS_Store"
		};

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
			// 规范输入路径，必须以‘/‘结尾
			if (!inPath.EndsWith (Constants.OSPathSeparator))
				inPath += Constants.OSPathSeparator;
			rootPath = inPath;

			var outPath = Utils.Repath (args [1]);
			if (!outPath.EndsWith (".blr"))
				outPath += ".blr";

			Console.WriteLine(string.Format("Packing '{0}' --> '{1}'", inPath, outPath));

			var di = new DirectoryInfo (rootPath);

			Stopwatch timer = new Stopwatch ();
			timer.Reset ();
			timer.Start ();

			Packer packer = new Packer (Utils.Repath (outPath));

			WalkDir (di, packer);
			packer.Pack ();

			timer.Stop ();
			Console.WriteLine ("{0} files are packed. ({1}ms)", packer.FilesCount, timer.ElapsedMilliseconds);
		}

		public static void WalkDir (DirectoryInfo di, Packer packer)
		{
			if (!di.Exists)
				return;
			foreach (var fsi in di.GetFileSystemInfos()) {
				if (fsi is DirectoryInfo)
					WalkDir (fsi as DirectoryInfo, packer);
				else if (fsi is FileInfo) {
					FileInfo fi = fsi as FileInfo;
					if (ignoreFileList.Contains (fi.Name))
						continue;
					Console.WriteLine (fi.FullName);
					packer.AddFile(fi, FileFullPath2RelativePath(fi));
				}
			}
		}

		public static string FileFullPath2RelativePath(FileInfo fileInfo)
		{
			return fileInfo.FullName.Replace (rootPath, string.Empty).Replace ("\\", Constants.PathSeparator);
		}
	}
}
