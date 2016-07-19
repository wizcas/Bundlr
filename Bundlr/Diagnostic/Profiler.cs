using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Bundlr
{
	public static class Profiler
	{
		public static readonly float nsPerTick = (1000L * 1000L * 1000L) / (float)Stopwatch.Frequency;
		public static readonly float usPerTick = (1000L * 1000L) / (float)Stopwatch.Frequency;
		public static readonly float msPerTick = (1000L) / (float)Stopwatch.Frequency;
		public static readonly float secPerTick = 1L / (float)Stopwatch.Frequency;

		private static Dictionary<string, Stopwatch> stopwatches = new Dictionary<string, Stopwatch> ();

		private static Stopwatch CreateOrGetTimer (string key)
		{
			Stopwatch timer;
			if (!stopwatches.ContainsKey (key)) {
				timer = new Stopwatch ();
				stopwatches [key] = timer;
			} else {
				timer = stopwatches [key];
			}
			return timer;
		}

		public static void StartSample (string key)
		{
			var timer = CreateOrGetTimer (key);
			if (!timer.IsRunning)
				timer.Start ();
		}

		public static void RestartSample (string key)
		{
			CreateOrGetTimer (key).Restart ();
		}

		public static void EndSample (string key)
		{
			if (!stopwatches.ContainsKey (key))
				return;

			stopwatches [key].Stop ();
		}

		public static float GetTotalTime (string key)
		{
			if (!stopwatches.ContainsKey (key))
				return 0f;

			var timer = stopwatches [key];
			return timer.ElapsedTicks * usPerTick;
		}

		public static string OutputAll()
		{
			StringBuilder sb = new StringBuilder ();
			foreach (var k in stopwatches.Keys) {
				sb.AppendLine (string.Format ("{0}\t->\t{1}μs", k, GetTotalTime (k)));
			}
			return sb.ToString ();
		}
	}
}

