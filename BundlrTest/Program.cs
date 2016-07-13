using System;
using Bundlr;
using System.Diagnostics;
using System.IO;

namespace BundlrTest
{
	class MainClass
	{
		private static string dirRoot = Utils.Repath ("~/HJD/");

		public static void Main (string[] args)
		{
			if (args.Length < 1) {
				Console.WriteLine ("See help");
				return;
			}

			string filePath = args [0];

			string id = "test";

			BundleManager.Instance.Load (id, filePath);
			Console.WriteLine (string.Format ("Has Bundle {0}? -->\t{1}", id, BundleManager.Instance.Has (id)));
//			BundleManager.Instance [id].ShowAll ();

			var fileList = BundleManager.Instance [id].FileList;
			long totalBundleTicks = 0;
			long totalFileSystemTicks = 0;
			for (int i = 0; i < fileList.Length; i++) {
				var testPath = fileList [i];
				Console.CursorLeft = 0;
				Console.Write (string.Format ("Processing file {0} / {1}", i, fileList.Length));
				TestData (id, testPath, ref totalBundleTicks, ref totalFileSystemTicks);
			}
			long usPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
			Console.WriteLine (string.Format ("\r\nAvg. Bundlr: {0}μs, Avg. FileSystem: {1}μs", 
				totalBundleTicks * usPerTick / 1000f / fileList.Length, 
				totalFileSystemTicks * usPerTick / 1000f / fileList.Length));
		}

		private static void TestData (string bundleId, string testPath, 
		                              ref long totalBundleTicks, 
		                              ref long totalFileSystemTicks)
		{
			Stopwatch timer = new Stopwatch ();
			timer.Restart ();
			var data1 = BundleManager.Instance [bundleId].Get (testPath);
			timer.Stop ();
			totalBundleTicks += timer.ElapsedTicks;

			var f = new FileInfo (dirRoot + testPath);
			timer.Restart ();
			byte[] data2;
			using (var ms = new MemoryStream ()) {
				using (var fs = f.OpenRead ()) {
					Utils.Stream2Stream (fs, ms, fs.Length);
				}
				data2 = ms.ToArray ();
			}
			timer.Stop ();
			totalFileSystemTicks += timer.ElapsedTicks;

			bool isDataSame = true;
			if (data1.Length != data2.Length) {
				isDataSame = false;
			} else {
				for (int i = 0; i < data1.Length; i++) {
					if (data1 [i] != data2 [i]) {
						isDataSame = false;
						break;
					}
				}
			}
//			Console.WriteLine ("data1 == data2? " + isDataSame);
			if (!isDataSame) {
				Console.WriteLine (string.Format ("Data mismatch: {0}", testPath));
			}
		}
	}
}
