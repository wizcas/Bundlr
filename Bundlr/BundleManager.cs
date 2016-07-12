using System;
using System.Collections.Generic;

namespace Bundlr
{
	public class BundleManager
	{
		private static BundleManager instance;

		public static BundleManager Instance {
			get {
				if (instance == null)
					instance = new BundleManager ();
				return instance;
			}
		}

		private Dictionary<string, Bundle> dictBundles = new Dictionary<string, Bundle> ();

		public Bundle this [string id] {
			get {
				if (dictBundles.ContainsKey (id))
					return dictBundles [id];
				return null;
			}
		}


		private BundleManager ()
		{
			
		}

		public Bundle Load (string id, string filePath)
		{
			var b = Bundle.Load (filePath);
			dictBundles [id] = b;
			return b;
		}

		public void DisposeAll ()
		{
			foreach (var kv in dictBundles) {
				kv.Value.Dispose ();
			}
			dictBundles.Clear ();
		}
	}
}

