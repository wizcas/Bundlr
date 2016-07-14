using System;
using Bundlr;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace BundlrTest
{
	class MainClass
	{
		public static string ActualDirRoot = Utils.Repath ("~/test/");
		const string BundleId = "test";

		private static string[] testPaths = new string[] {
			"LogoPic/3DSSZLogoBig.png",
			"PHZ/Sound/advance.ogg",
			"dcef3/widevinecdmadapter.dll"
		};

		private static string[] files;
		private static TestTask[] tasks;
		private static int progress;
		private static bool isUseThreadPool;
		private static int runTimes = 1;
		private static float totalBundleTime;
		private static float totalFileSystemTime;

		private static Mutex progressMutex;
		private static ManualResetEvent doneSignal;

		public static void Main (string[] args)
		{
			if (args.Length < 3) {
				Console.WriteLine ("Usage: BundlrTest <PACK_FILE> <ACTUAL_DIR>> <RUN_TIMES> [t]");
				return;
			}

			string filePath = args [0];
			ActualDirRoot = args [1];
			runTimes = int.Parse (args [2]);
			isUseThreadPool = args.Length >= 4 && args[3] == "t" ? true : false;

//			string filePath = "~/test.blr";

			Console.WriteLine (string.Format ("Loading bundle '{0}' as '{1}'", filePath, BundleId));
			BundleManager.Instance.Load (BundleId, filePath);
			Console.WriteLine (string.Format ("Is bundle ‘{0}’ loaded? --> {1}", BundleId, BundleManager.Instance.Has (BundleId)));

			progressMutex = new Mutex (false, "progress");

			files = BundleManager.Instance [BundleId].FileList;
			tasks = new TestTask[files.Length];
			doneSignal = new ManualResetEvent (false);

			for (int i = 0; i < runTimes; i++) {
				Console.WriteLine (">>> Running benchmark #" + (i + 1));
				RunOne ();
			}

			Console.WriteLine ("============================");
			Console.WriteLine (string.Format ("Final Avg. Bundlr Time: {0}μs", totalBundleTime / runTimes));
			Console.WriteLine (string.Format ("Final Avg. FileSystem Time: {0}μs", totalFileSystemTime / runTimes));
		}

		private static void RunOne()
		{
			progress = 0;
			doneSignal.Reset ();
			for (int i = 0; i < files.Length; i++) {
				var testPath = files [i];
				var t = new TestTask (BundleId, testPath);
				tasks [i] = t;
				if (!isUseThreadPool)
					RunInSingleThread (t);
				else
					RunInThreadPool (t);
			}

			if (isUseThreadPool)
				doneSignal.WaitOne ();

			OutputRunStatistics ();
		}

		private static void RunInSingleThread (TestTask task)
		{
			task.Run ();
			UpdateProgress ();
		}

		private static void RunInThreadPool (TestTask task)
		{
			ThreadPool.QueueUserWorkItem (task.ThreadCallback);
		}

		public static void UpdateProgress ()
		{
			progressMutex.WaitOne ();
			progress++;
			Console.CursorLeft = 0;
			Console.Write (string.Format ("Processing file {0} / {1}", progress, files.Length));
			if (progress == files.Length)
				doneSignal.Set ();
			progressMutex.ReleaseMutex ();
		}

		private static void OutputRunStatistics ()
		{
			long runBundleTicks = 0;
			long runFileSystemTicks = 0;
			foreach (var t in tasks) {
				runBundleTicks += t.bundleTicks;
				runFileSystemTicks += t.fileSystemTicks;
			}

			long usPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;

			float runBundleTime = runBundleTicks * usPerTick / 1000f / files.Length;
			float runFileSystemTime = runFileSystemTicks * usPerTick / 1000f / files.Length;

			totalBundleTime += runBundleTime;
			totalFileSystemTime += runFileSystemTime;

			Console.WriteLine (string.Format ("\r\nAvg. Bundlr Time: {0}μs, Avg. FileSystem Time: {1}μs", 
				runBundleTime, runFileSystemTime));
		}
	};
}
