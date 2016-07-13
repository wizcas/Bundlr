﻿using System;
using System.Diagnostics;
using Bundlr;
using System.Linq;
using System.IO;
using System.Threading;

namespace BundlrTest
{
	public class TestTask
	{
		private string bundleId;
		private string relPath;
		private Stopwatch timer;

		public long bundleTicks;
		public long fileSystemTicks;

		public TestTask (string bundleId, string relPath)
		{
			this.bundleId = bundleId;
			this.relPath = relPath;
			timer = new Stopwatch ();
		}

		public void Run()
		{
			timer.Restart ();
			var data1 = BundleManager.Instance [bundleId].Get (relPath);
			timer.Stop ();
			bundleTicks += timer.ElapsedTicks;

			var f = new FileInfo (Path.Combine(MainClass.ActualDirRoot, relPath));
			timer.Restart ();
			byte[] data2;
			using (var ms = new MemoryStream ()) {
				using (var fs = f.OpenRead ()) {
					Utils.Stream2Stream (fs, ms, fs.Length);
				}
				data2 = ms.ToArray ();
			}
			timer.Stop ();
			fileSystemTicks += timer.ElapsedTicks;

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
			if (!isDataSame) {
				Console.WriteLine (string.Format ("\r\nData mismatch: {0}\r\n", relPath));
			}
		}

		public void ThreadCallback(object context)
		{
			Run ();
			MainClass.UpdateProgress ();
		}
	}
}
