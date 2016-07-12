using System;
using Bundlr;

namespace BundlrTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if (args.Length < 1) {
				Console.WriteLine ("See help");
				return;
			}

			string filePath = args [0];

			BundleManager.Instance.Load ("test", filePath);
			BundleManager.Instance ["test"].ShowAll ();
		}
	}
}
