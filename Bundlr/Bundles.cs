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

		private static Bundles instance = new Bundles ();
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
		public static string[] FileList {
			get {
				return instance.relpath2Bundle.Keys.ToArray ();
			}
		}
		/// <summary>
		/// 从指定位置加载数据包。若新加载的数据包中有相对路径与已加载数据包冲突，则会用新包中的数据覆盖旧包的。
		/// </summary>
		/// <param name="filePath">数据包文件的磁盘路径</param>
		public static void Load (string filePath)
		{
			lock (instance) {
				instance.LoadBundle (filePath);
			}
		}
		/// <summary>
		/// 通过相对路径从已加载的数据包中加载文件
		/// </summary>
		/// <param name="relativePath">文件访问对象</param>
		internal static BundleFile File (string relativePath)
		{
			relativePath = relativePath.ToLower ();
			if (!instance.relpath2Bundle.ContainsKey (relativePath)) {
				return null;
			}

			var bundle = instance.relpath2Bundle [relativePath];
			return new BundleFile (relativePath, bundle);
		}
		/// <summary>
		/// 释放所有已加载的数据包
		/// </summary>
		public static void DisposeAll ()
		{
			lock (instance) {
				isDisposingAll = true;
				foreach (var bundle in instance.loadedBundles) {
					bundle.Dispose ();
				}
				instance.relpath2Bundle.Clear ();
				instance.loadedBundles.Clear ();
				instance.bundleCounters.Clear ();
				isDisposingAll = false;
			}
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
				lock (loadedBundles) {
					loadedBundles.Add (bundle);
				}
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
				foreach (string file in bundle.FileList) {
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