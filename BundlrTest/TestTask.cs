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
		private string relPath;
		private Stopwatch timer;

		public long bundleTicks;
		public long fileSystemTicks;
		public float bundleAccSpeed;
		public float fileSystemAccSpeed;

		public TestTask (string relPath)
		{
			this.relPath = relPath;
			timer = new Stopwatch ();
		}

		private void DoRun()
		{
			timer.Restart ();
			var bf = ResourceFile.Open (relPath);
			var size = bf.Size / (float)(1024 * 1024); // unit in MB

			var data1 = new byte[(int)bf.Size];
			bf.Read (data1, 0, 0, (int)bf.Size);
			bf.Close ();
			timer.Stop ();
			bundleTicks = timer.ElapsedTicks;
			bundleAccSpeed = size / (bundleTicks * MainClass.usPerTick / 1000f); // unit in MB/s

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
			fileSystemTicks = timer.ElapsedTicks;
			fileSystemAccSpeed = size / (fileSystemTicks * MainClass.usPerTick / 1000f); // unit in MB/s

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
		}

		public void Run()
		{
			DoRun ();
			MainClass.UpdateProgress ();
		}
	}
}

