using System;
using System.Collections.Generic;
using System.Linq;

namespace Bundlr
{
	public class Bundles
	{
		#region Nesting

		#endregion

		private static Bundles instance = new Bundles ();
		private static bool isDisposingAll = false;

		private Dictionary<string, Bundle> relpath2Bundle = new Dictionary<string, Bundle> ();
		private List<Bundle> bundles = new List<Bundle>();
		private Dictionary<string, int> bundleCounters = new Dictionary<string, int> ();


		~Bundles ()
		{
			DisposeAll ();
		}

		#region Public API

		public static string[] FileList {
			get {
				return instance.relpath2Bundle.Keys.ToArray ();
			}
		}

		public static void DisposeAll ()
		{
			lock (instance) {
				isDisposingAll = true;
				foreach (var bundle in instance.bundles) {
					bundle.Dispose ();
				}
				instance.relpath2Bundle.Clear ();
				instance.bundles.Clear ();
				instance.bundleCounters.Clear ();
				isDisposingAll = false;
			}
		}

		public static void Load (string filePath)
		{
			lock (instance) {
				instance.LoadBundle (filePath);
			}
		}

		public static BundleFile Open(string relativePath)
		{
			if (!instance.relpath2Bundle.ContainsKey (relativePath)) {
				return null;
			}

			return new BundleFile (instance.relpath2Bundle [relativePath], relativePath);
		}

		#endregion

		#region Bundle Organizing
		private void LoadBundle (string filePath)
		{
			try {
				var bundle = Bundle.Load (filePath);

				foreach (var file in bundle.FileList) {
					RegisterFile (file, bundle);
				}

				bundle.onDisposed += OnBundleDisposed;
				bundles.Add(bundle);
			} catch (ArgumentException e) {
				Console.WriteLine ("Failed to load bundle: {0}", e);
			}
		}

		private void RegisterFile (string relPath, Bundle bundle)
		{
			lock (relpath2Bundle) {
				// 检查是否已存在相同的相对路径
				if (relpath2Bundle.ContainsKey (relPath)) {
					var oldBundle = relpath2Bundle [relPath];
					// 若该相对路径被替换为访问新包里的文件，则减少一个对旧包的引用
					if (oldBundle != bundle) {
						MinusOneBundleUsage (oldBundle);
					}
				}
				relpath2Bundle [relPath] = bundle;
				AddOneBundleUsage (bundle);
			}
		}

		private void AddOneBundleUsage (Bundle bundle)
		{
			lock (bundleCounters) {
				if (!bundleCounters.ContainsKey (bundle.Uid))
					return;
			
				bundleCounters [bundle.Uid]++;
			}
		}

		private void MinusOneBundleUsage (Bundle bundle)
		{
			lock (bundleCounters) {
				if (!bundleCounters.ContainsKey (bundle.Uid))
					return;
			
				bundleCounters [bundle.Uid]--;
				// 这个包中再也没有文件被引用了，释放该包
				if (bundleCounters [bundle.Uid] <= 0)
					bundle.Dispose ();
			}
		}

		private void OnBundleDisposed(Bundle bundle)
		{
			if (isDisposingAll)
				return;

			bundles.Remove (bundle);
		}
		#endregion
	}
}

