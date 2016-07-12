using System;
using System.IO;
using System.Collections.Generic;
using Bundlr;

namespace BundlrPacker
{
	class MainClass
	{
		private static string rootPath;

		public static void Main (string[] args)
		{
			var pathArg = Utils.Repath("~/test");

			rootPath = pathArg;

			var di = new DirectoryInfo (rootPath);

			List<PackingFile> files = new List<PackingFile> ();
			WalkDir (di, files);

			Packer packer = new Packer (Utils.Repath ("~/test.blr"));
			packer.Pack (files);

			TestLoad ();
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

		public static void TestLoad()
		{
			BundleManager.Instance.Load ("test", "~/test.blr");
			Console.WriteLine ("Has img.jpg? " + BundleManager.Instance["test"].Has ("img.jpg"));
			using(FileStream fs = new FileStream(Utils.Repath("~/img-extract.jpg"), FileMode.Create, FileAccess.Write)){
				var b = BundleManager.Instance["test"].Get ("img.jpg");
				fs.Write (b, 0, b.Length);
				fs.Flush ();
				Console.WriteLine ("Extracted img.jpg to ~/img-extract.jpg");
			}
		}
	}
}
