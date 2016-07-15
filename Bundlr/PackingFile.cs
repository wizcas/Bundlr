using System;
using System.IO;
using Bundlr;
using System.Collections.Generic;

namespace Bundlr
{
	internal class PackingFileCollection : IEnumerable<PackingFile>
	{
		private List<PackingFile> files = new List<PackingFile> ();
		private Dictionary<string, PackingFile> validationDict = new Dictionary<string, PackingFile> ();

		internal int Count {
			get {
				return files.Count;
			}
		}

		internal void Add (string relativePath, FileInfo fileInfo)
		{
			Add (new PackingFile (relativePath, fileInfo));
		}

		internal void Add (PackingFile file)
		{
			if (!file.fileInfo.Exists) {
				throw new FileNotFoundException ("Failed to add a packing file", file.fileInfo.FullName);
			}
			if (validationDict.ContainsKey (file.relativePath)) {
				throw new ArgumentException (string.Format ("Conflict: the relative path '{0}' is registered in packing list",
					file.relativePath));
			}

			files.Add (file);
			validationDict [file.relativePath] = file;
		}

		internal bool Remove (string relativePath)
		{
			if (!validationDict.ContainsKey (relativePath)) {
				return false;
			}

			var file = validationDict [relativePath];
			validationDict.Remove (relativePath);
			return files.Remove (file);
		}

		internal void Clear ()
		{
			files.Clear ();
			validationDict.Clear ();
		}

		#region IEnumerable implementation

		public IEnumerator<PackingFile> GetEnumerator ()
		{
			return files.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return files.GetEnumerator ();
		}

		#endregion
	}

	internal class PackingFile
	{
		internal string relativePath;
		internal FileInfo fileInfo;
		internal FileMeta metadata;

		internal PackingFile (string relativePath, FileInfo fileInfo)
		{
			this.relativePath = relativePath.ToLower ();
			this.fileInfo = fileInfo;
			this.metadata = null;
		}
	}
}

