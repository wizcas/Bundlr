using System;

namespace Bundlr
{
	public static class Constants
	{
		/// <summary>
		/// 数据包的字节对齐数
		/// </summary>
		public const int NumOfBytesAlignment = 8;

		/// <summary>
		/// 数据包中相对路径使用的路径分隔符
		/// </summary>
		public const string PathSeparator = "/";

		/// <summary>
		/// 当前运行的系统平台
		/// </summary>
		/// <value>The OS platform.</value>
		public static PlatformID OSPlatform {
			get {
				return Environment.OSVersion.Platform;
			}
		}

		/// <summary>
		/// 路径分隔符
		/// </summary>
		/// <value>The path separator.</value>
		public static string OSPathSeparator {
			get {
				if (OSPlatform == PlatformID.MacOSX || OSPlatform == PlatformID.Unix)
					return "/";

				return "\\";
			}
		}

	}
}

