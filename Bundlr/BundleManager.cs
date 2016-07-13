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
		private bool isDisposingAll = false;

		public Bundle this [string id] {
			get {
				lock (this) {
					if (Has (id))
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
			return dictBundles.ContainsKey (id);
		}

		public Bundle Load (string id, string filePath)
		{
			lock (this) {
				Bundle ret;
				if (!Has (id)) {
					ret = Bundle.Load (id, filePath);
					ret.onDisposed += OnBundleDisposed; // Callback after bundle disposed, for unregistering the bundle in manager
					dictBundles [id] = ret;
				} else {
					ret = dictBundles [id];
				}
				return ret;
			}
		}

		public Bundle Reload(string id)
		{
			var b = this [id];
			if (b == null) {
				throw new ArgumentException (string.Format ("Bundle '{0}' has never been loaded"));
			}

			string path = b.FilePath;
			b.Dispose ();
			return Load (id, path);
		}

		public void DisposeAll ()
		{
			lock (this) {
				isDisposingAll = true;
				foreach (var kv in dictBundles) {
					kv.Value.Dispose ();
				}
				dictBundles.Clear ();
				isDisposingAll = false;
			}
		}

		private void OnBundleDisposed(string id)
		{
			if (isDisposingAll)
				return;

			dictBundles.Remove (id);
		}
	}
}

