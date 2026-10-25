using System;
using System.Diagnostics;
using System.Text;

namespace BelmontsCurseUltrawide;

// Bounded, allocation-free sampling. Sorting and formatting happen only when F9 is requested.
internal sealed class FrameIntervals
{
	private readonly double[][] _samples;
	private readonly int[] _counts = new int[3], _positions = new int[3];
	private readonly double[] _lifetimeWorst = new double[3];
	private readonly long[] _lifetimeOver25 = new long[3];
	private long _previous;
	private int _mode;

	internal FrameIntervals(int capacity = 2048)
	{
		_samples = new[] { new double[capacity], new double[capacity], new double[capacity] };
	}

	internal void ResetClock() => _previous = 0;

	// Returns the interval since the previous recorded frame in milliseconds, or -1 when there is no valid sample
	// (first frame, after a focus gap, or a mode change): callers use it to spot stalls without a second clock.
	internal double Record(long timestamp, int mode)
	{
		double result = -1;
		if (_previous != 0 && _mode == mode)
		{
			double ms = (timestamp - _previous) * 1000.0 / Stopwatch.Frequency;
			result = ms;
			_lifetimeWorst[mode] = Math.Max(_lifetimeWorst[mode], ms);
			if (ms > 25) _lifetimeOver25[mode]++;
			_samples[mode][_positions[mode]] = ms;
			_positions[mode] = (_positions[mode] + 1) % _samples[mode].Length;
			_counts[mode] = Math.Min(_counts[mode] + 1, _samples[mode].Length);
		}
		_previous = timestamp;
		_mode = mode;
		return result;
	}

	internal double[] Snapshot(int mode)
	{
		var copy = new double[_counts[mode]];
		Array.Copy(_samples[mode], copy, copy.Length);
		Array.Sort(copy);
		return copy;
	}

	internal double LifetimeWorst(int mode) => _lifetimeWorst[mode];
	internal long LifetimeOver25(int mode) => _lifetimeOver25[mode];

	internal void Dump(StringBuilder sb)
	{
		sb.AppendLine("Frame intervals: wall time between Unity Updates; recent 2048 samples per mode, excluding focus gaps, mode transitions and diagnostic dumps. Includes loading/startup; not GPU presentation timing.");
		string[] names = { "gameplay / game menus", "history", "mod settings" };
		for (int mode = 0; mode < names.Length; mode++)
		{
			var sorted = Snapshot(mode);
			sb.AppendLine(FormattableString.Invariant($"  {names[mode]} session history: worst={LifetimeWorst(mode):F3}ms >25ms={LifetimeOver25(mode)} (same focus/mode/dump exclusions)"));
			if (sorted.Length == 0) { sb.AppendLine("  " + names[mode] + ": no samples"); continue; }
			double sum = 0;
			int over25 = 0, over50 = 0;
			foreach (double ms in sorted) { sum += ms; if (ms > 25) over25++; if (ms > 50) over50++; }
			double Percentile(double fraction) => sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * fraction) - 1)];
			sb.AppendLine(FormattableString.Invariant($"  {names[mode]}: n={sorted.Length} span={sum / 1000:F1}s avg={sum / sorted.Length:F3}ms median={Percentile(0.5):F3}ms p95={Percentile(0.95):F3}ms p99={Percentile(0.99):F3}ms max={sorted[sorted.Length - 1]:F3}ms >25ms={over25} >50ms={over50}"));
		}
	}
}
