using System;
using System.Collections.Generic;
using System.Linq;

namespace Bundlr
{
	public class Bundles
	{
		/// <summary>
		/// 数据包缓存机制，默认为<see cref="BundleCaching.AlwaysCached"/>
		/// </summary>
		public static BundleCaching Caching = BundleCaching.AlwaysCached;

		private static readonly object syncRoot = new object();
		private static Bundles instance;
		public static Bundles Instance {
			get {
				// Double-check locking
				if (instance == null) {
					lock (syncRoot) {
						if (instance == null)
							instance = new Bundles ();
					}
				}
				return instance;
			}
		}

		private static bool isDisposingAll = false;

		/// <summary>
		/// 相对路径 -> 数据包
		/// </summary>
		private Dictionary<string, Bundle> relpath2Bundle = new Dictionary<string, Bundle> ();
		/// <summary>
		/// 所有已加载的数据包列表
		/// </summary>
		private List<Bundle> loadedBundles = new List<Bundle> ();
		/// <summary>
		/// 数据包引用计数器
		/// </summary>
		private Dictionary<string, int> bundleCounters = new Dictionary<string, int> ();

		~Bundles ()
		{
			DisposeAll ();
		}

		#region Public API

		/// <summary>
		/// 已加载数据包中所有文件的相对路径列表
		/// </summary>
		/// <value>The file list.</value>
		public static string[] RelativePaths {
			get {
				return Instance.relpath2Bundle.Keys.ToArray ();
			}
		}

		/// <summary>
		/// 从指定位置加载数据包。若新加载的数据包中有相对路径与已加载数据包冲突，则会用新包中的数据覆盖旧包的。
		/// </summary>
		/// <param name="filePath">数据包文件的磁盘路径</param>
		public static void Load (string filePath)
		{
			lock (syncRoot) {
				Instance.LoadBundle (filePath);
			}
		}

		/// <summary>
		/// 通过相对路径从已加载的数据包中加载文件
		/// </summary>
		/// <param name="relativePath">文件访问对象</param>
		internal static BundleFile File (string relativePath)
		{
			relativePath = relativePath.ToLower ();
			if (!Instance.relpath2Bundle.ContainsKey (relativePath)) {
				return null;
			}

			var bundle = Instance.relpath2Bundle [relativePath];

			if (bundle == null)
				return null;

			return new BundleFile (relativePath, bundle);
		}

		/// <summary>
		/// 释放所有已加载的数据包
		/// </summary>
		public static void DisposeAll ()
		{
			lock (syncRoot) {
				isDisposingAll = true;
				foreach (var bundle in Instance.loadedBundles) {
					bundle.Dispose ();
				}
				Instance.relpath2Bundle.Clear ();
				Instance.loadedBundles.Clear ();
				Instance.bundleCounters.Clear ();
				isDisposingAll = false;
			}
		}

		#endregion

		#region Bundle Organizing

		private void LoadBundle (string filePath)
		{
			try {

				var bundle = new Bundle(filePath);

				foreach (var relPath in bundle.RelativePaths) {
					RegisterRelPath (relPath, bundle);
				}

				bundle.onDisposed += OnBundleDisposed;

				lock (loadedBundles) {
					loadedBundles.Add (bundle);
				}
			} catch (ArgumentException e) {
				Console.WriteLine ("Failed to load bundle: {0}", e);
			}
		}

		private void RegisterRelPath (string relPath, Bundle bundle)
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

		private void OnBundleDisposed (Bundle bundle)
		{
			// 当一次性释放所有数据包时，不再进行下面的单独处理
			if (isDisposingAll)
				return;

			// 数据包被释放时，也从各种记录中删掉相关信息
			lock (loadedBundles) {
				loadedBundles.Remove (bundle);
			}
			lock (bundleCounters) {
				bundleCounters.Remove (bundle.Uid);
			}
			lock (relpath2Bundle) {
				foreach (string file in bundle.RelativePaths) {
					if (!relpath2Bundle.ContainsKey (file))
						continue;

					var b = relpath2Bundle [file];
					if (b == bundle) {
						relpath2Bundle.Remove (file);
					}
				}
			}
		}

		#endregion
	}
}

/// <summary>
/// 数据包缓存方式
/// </summary>
public enum BundleCaching
{
	/// <summary>
	/// 不缓存文件流，每次取数据时都创建新的文件流，用后立即销毁
	/// </summary>
	None,
	/// <summary>
	/// 始终保存用于访问包数据的文件流对象，直到数据包被销毁
	/// </summary>
	AlwaysCached
}