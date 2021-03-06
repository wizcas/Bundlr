﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Bundlr
{
	/// <summary>
	/// 磁盘文件访问对象
	/// </summary>
	public class DiskFile : ResourceFile
	{
		#region Nesting

		private struct FileCounterItem
		{
			internal DiskFile file;
			internal int counter;
		}

		#endregion

		/// <summary>
		/// 文件计数器
		/// </summary>
		private static Dictionary<string, FileCounterItem> fileCounters = new Dictionary<string, FileCounterItem> ();

		private FileInfo fileInfo;
		private string relativePath;
		private FileStream fs;

		public override long Size {
			get {
				return fileInfo.Length;
			}
		}

		public override string RelativePath {
			get {
				return relativePath;
			}
		}

		private string AbsolutePath {
			get {
				return Path.Combine (ResourceFile.DiskFileRoot, RelativePath);
			}
		}

		internal static DiskFile OpenFile (string relativePath)
		{
			// 文件尚未被创建时，创建访问文件的数据流对象
			// 之后每打开一次这个文件，文件引用数+1
			lock (fileCounters) {
				if (fileCounters.ContainsKey (relativePath)) {
					var item = fileCounters [relativePath];
					item.counter++;
					return item.file;
				}

				var file = new DiskFile (relativePath);
				fileCounters [relativePath] = new FileCounterItem () {
					file = file,
					counter = 1
				};
				return file;
			}
		}

		private DiskFile (string relativePath)
		{
			this.relativePath = relativePath;

			if (string.IsNullOrWhiteSpace (ResourceFile.DiskFileRoot))
				throw new InvalidOperationException ("Disk file not available: ResourceFile.DiskFileRoot is not set");

			if (!File.Exists (AbsolutePath))
				throw new FileNotFoundException (string.Format ("Disk file '{0}' is not found", AbsolutePath));

			fileInfo = new FileInfo (AbsolutePath);
			fs = fileInfo.Open (FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public override void Read (byte[] dst, int dstStartIndex, int readFilePos, int readSize)
		{
			Utils.CheckReadParameters (dst, dstStartIndex, readFilePos, readSize, fs.Length);

			lock (fs) {
				fs.Position = readFilePos;
				fs.Read (dst, dstStartIndex, readSize);
			}
		}

		public override void Close ()
		{
			// 关闭文件时文件引用数-1
			// 当引用数<=0时，释放文件流
			bool closeFs = false;
			lock (fileCounters) {
				if (fileCounters.ContainsKey (RelativePath)) {
					var item = fileCounters [relativePath];
					item.counter--;
					if (item.counter <= 0) {
						closeFs = true;
						fileCounters.Remove (relativePath);
					}
				} else {
					closeFs = true;
				}
			}
			if (closeFs) {
				lock (fs) {
					fs.Close ();
				}
			}
		}
	}
}

