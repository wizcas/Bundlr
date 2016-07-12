using System;

namespace Bundlr
{
	public class Utils
	{
		public static string Repath (string path)
		{
			var env = Environment.OSVersion.Platform;
			if (env == PlatformID.Unix || env == PlatformID.MacOSX) {
				// Unix-based系统下将开头的~转换成用户目录
				if ((path.Length == 1 && path == "~")
				    ||
				    path.Length > 1 && path.StartsWith ("~/")) {
					path = Environment.GetEnvironmentVariable ("HOME") +
					(path.Length > 1 
							? path.Substring (1) : string.Empty);
				}
			}
			return path;
		}
	}
}

