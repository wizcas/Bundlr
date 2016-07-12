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
				lock (dictBundles) {
					if (dictBundles.ContainsKey (id))
						return dictBundles [id];
					return null;
				}
			}
		}

		private BundleManager ()
		{
			
		}

		public bool Has (string id)
		{
			lock (dictBundles) {
				return dictBundles.ContainsKey (id);
			}
		}

		public Bundle Load (string id, string filePath)
		{
			Bundle ret;
			if (!Has (id)) {
				lock (dictBundles) {
					ret = Bundle.Load (id, filePath);
					dictBundles [id] = ret;
				}
			} else {
				lock (dictBundles) {
					ret = dictBundles [id];
				}
			}
			return ret;
		}

		public void DisposeAll ()
		{
			lock (dictBundles) {
				foreach (var kv in dictBundles) {
					kv.Value.Dispose ();
				}
				dictBundles.Clear ();
			}
		}
	}
}

