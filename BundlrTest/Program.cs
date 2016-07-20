using System;
using Bundlr;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BundlrTest
{
	class MainClass
	{
		public static string ActualDirRoot = Utils.Repath ("~/test/");

		private static string[] files;
		private static TestTask[] tasks;
		private static int progress;
		private static bool isMultiThreads;
		private static int runTimes = 1;
		private static float totalBundleTime;
		private static float totalFileSystemTime;
		private static float totalFileSizeInMB;
		private static float totalBundleAccSpeed;
		private static float totalFileSystemAccSpeed;

		private static Mutex progressMutex;

		public static void Main (string[] args)
		{
			if (args.Length < 3) {
				Console.WriteLine ("Usage: BundlrTest <PACK_FILE> <ACTUAL_DIR>> <RUN_TIMES> [c] [t] [r]");
				return;
			}

			string filePath = args [0];
			ActualDirRoot = args [1];
			runTimes = int.Parse (args [2]);

			isMultiThreads = args.Contains ("t") ? true : false;
			bool isRandomFiles = args.Contains ("r") ? true : false;

			if (args.Contains ("c"))
				Bundles.Caching = BundleCaching.AlwaysCached;
			else
				Bundles.Caching = BundleCaching.None;

//			string filePath = "~/test.blr";
//			bool isRandomFiles = true;

			Console.WriteLine (
				"\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>" +
				"\nBundlr is running with settings: ");
			Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine (
				"\n> Bundles caching mode : {0}" +
				"\n> File access sequence: {1}" +
				"\n> Threading: {2}",
				Bundles.Caching,
				isRandomFiles ? "Randomly" : "Sequential",
				isMultiThreads ? "Multi-threads" : "Single thread"
			);
			Console.ResetColor ();
			Console.WriteLine (
				"\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n");

			Console.WriteLine (string.Format ("Loading bundle file '{0}'", filePath));
			Bundles.Load (filePath);

			progressMutex = new Mutex (false, "progress");

			files = Bundles.FileList;

			if (isRandomFiles)
				files = ShuffleArray (files);

			foreach (var file in files) {
				var f = ResourceFile.Open (file);
				totalFileSizeInMB += f.Size;
				f.Close ();
			}
			totalFileSizeInMB /= 1024 * 1024;
			Console.WriteLine ("Total file size: {0}MB", totalFileSizeInMB);

			totalBundleAccSpeed = 0;
			totalBundleTime = 0;
			totalFileSystemAccSpeed = 0;
			totalFileSystemTime = 0;

			tasks = new TestTask[files.Length];

			for (int i = 0; i < runTimes; i++) {
				Console.WriteLine (">>> Running benchmark #" + (i + 1));
				RunOne ();
			}

			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine ("\n\n\n=============== Performance Report ==============");
			Console.WriteLine ("[Final Avg. File Access Speed]");
			Console.WriteLine ("Bundlr: {0:N3}MB/s, FileSystem: {1:N3}MB/s", totalBundleAccSpeed / runTimes, totalFileSystemAccSpeed / runTimes);
			Console.WriteLine ("[Final Avg. File Process Time]");
			Console.WriteLine ("Bundlr: {0:N3}μs, FileSystem: {1:N3}μs", totalBundleTime / runTimes, totalFileSystemTime / runTimes);
			Console.ResetColor ();

			// Output to file
			using (var s = new StreamWriter (Utils.Repath ("~/profile.txt"), true, Encoding.UTF8)) {
				s.WriteLine (string.Format ("{0:N4}\t{1:N4}", totalBundleAccSpeed / runTimes, totalFileSystemAccSpeed / runTimes));
			}

			TestDuplicatedPath ();

			Bundles.DisposeAll ();

			OutputProfiler ();
		}

		private static string[] ShuffleArray (string[] files)
		{
			Random rnd = new Random (DateTime.Now.Millisecond);
			List<string> ret = new List<string> ();
			List<string> shuffled = new List<string> (files);
			while (shuffled.Count > 0) {
				int i = rnd.Next (0, shuffled.Count);
				ret.Add (shuffled [i]);
				shuffled.RemoveAt (i);
			}
			return ret.ToArray ();
		}

		private static void RunOne ()
		{
			progress = 0;

			List<Task> threadTasks = new List<Task> ();

			for (int i = 0; i < files.Length; i++) {
				var testPath = files [i];
				var t = new TestTask (testPath);
				tasks [i] = t;
				if (!isMultiThreads)
					t.Run ();
				else {
					var tt = new Task (t.Run);
					tt.Start ();
					threadTasks.Add (tt);

				}
			}
				
			if (isMultiThreads)
				Task.WaitAll (threadTasks.ToArray (), new TimeSpan (0, 1, 0));

			OutputRunStatistics ();
		}

		public static void UpdateProgress ()
		{
			progressMutex.WaitOne ();
			progress++;
			Console.CursorLeft = 0;
			Console.Write (string.Format ("Processing file {0} / {1}", progress, files.Length));

			progressMutex.ReleaseMutex ();
		}

		private static void OutputRunStatistics ()
		{
			long runBundleTicks = 0;
			long runFileSystemTicks = 0;
			float runBundleAccSpeed = 0;
			float runFileSystemAccSpeed = 0;

			foreach (var t in tasks) {
				runBundleTicks += t.bundleTicks;
				runFileSystemTicks += t.fileSystemTicks;
				runBundleAccSpeed += t.bundleAccSpeed;
				runFileSystemAccSpeed += t.fileSystemAccSpeed;
			}

			float avgBundleAccSpeed = runBundleAccSpeed / files.Length;
			float avgFileSystemAccSpeed = runFileSystemAccSpeed / files.Length;

			totalBundleAccSpeed += avgBundleAccSpeed;
			totalFileSystemAccSpeed += avgFileSystemAccSpeed;

			Console.WriteLine ("\r\n[Avg. File Access Speed] Bundlr: {0:N3}MB/s, FileSystem: {1:N3}MB/s",
				avgBundleAccSpeed, avgFileSystemAccSpeed);

			float runBundleTime = runBundleTicks * Profiler.usPerTick;
			float runFileSystemTime = runFileSystemTicks * Profiler.usPerTick;
			float avgBundleTime = runBundleTime / files.Length;
			float avgFileSystemTime = runFileSystemTime / files.Length;

			totalBundleTime += avgBundleTime;
			totalFileSystemTime += avgFileSystemTime;

			Console.WriteLine ("[Avg. File Process Time] Bundlr: {0:N3}μs, FileSystem: {1:N3}μs", 
				avgBundleTime, avgFileSystemTime);	
		}

		private static void TestDuplicatedPath ()
		{
			Console.WriteLine ("\n========= Duplicated Relative Path Test =========");
			Console.WriteLine (">> In currently loaded Bundle");

			string s = ReadTestTxtString ();
			Console.WriteLine ("  text.txt is now : {0}", s);

			Console.WriteLine (">> Loading Bundle ~/test.blr");
			Bundles.Load (Utils.Repath ("~/test.blr"));

			s = ReadTestTxtString ();
			Console.WriteLine ("  text.txt is now : {0}", s);
		}

		private static string ReadTestTxtString ()
		{
			var f = ResourceFile.Open ("teSt.txt");
			var data = new byte[(int)f.Size];
			f.Read (data, 0, 0, (int)f.Size);
			return Encoding.UTF8.GetString (data);
		}

		private static void OutputProfiler ()
		{
			Console.WriteLine ("\n========= Detailed Profiler Report ===========");
			Console.WriteLine (Profiler.OutputAll ());
		}
	}
}