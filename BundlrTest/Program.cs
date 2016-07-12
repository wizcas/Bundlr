using System;
using Bundlr;
using System.Diagnostics;
using System.IO;

namespace BundlrTest
{
	class MainClass
	{
		private static string[] testPaths = new string[]{
			"LogoPic/3DSSZLogoBig.png",
			"PHZ/Sound/advance.ogg",
			"dcef3/widevinecdmadapter.dll"
		};
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

			foreach (var testPath in testPaths) {
				TestData (id, testPath);
			}
		}

		private static void TestData(string bundleId, string testPath){
			long nsPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
			Stopwatch timer = new Stopwatch ();
			timer.Restart ();
			var data1 = BundleManager.Instance [bundleId].Get (testPath);
			timer.Stop ();
			long bundleTicks = timer.ElapsedTicks;

			var f = new FileInfo (dirRoot + testPath);
			timer.Restart ();
			byte[] data2;
			using (var ms = new MemoryStream ()) {
				using (var fs = f.OpenRead ()) {
					Utils.Stream2Stream (fs, ms, fs.Length);
				}
				data2 = ms.ToArray ();
			}

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
			Console.WriteLine ("data1 == data2? " + isDataSame);

			timer.Stop ();
			long fsTicks = timer.ElapsedMilliseconds;

			Console.WriteLine (string.Format ("Bundlr: {0}ns, FileSystem: {1}ns", bundleTicks * nsPerTick, fsTicks * nsPerTick));
		}
	}
}
