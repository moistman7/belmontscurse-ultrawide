using System;

namespace BelmontsCurseUltrawide;

// Pure helpers with no game types, so they can be unit-tested (tests/Logic.Tests links this file).
internal static class Scheduling
{
	// A scan may use at most this fraction of the time: a scan that took 20 ms waits at least 4 s, one that took 1 ms
	// is not held back beyond its normal interval. Keeps heavy scenes (where every search is slow) from hitching.
	internal const double BudgetFraction = 0.005;
	internal const float MaxBudgetGap = 30f;

	// Seconds until a job runs again.
	//  - baseInterval: its normal interval; relaxedInterval: the longer one used once the scene is stable (0 = none).
	//  - scale: the user's "scan slowdown" setting (1 = normal); only scans are scaled, never UI/presentation jobs.
	//  - lastRunMs: how long the run that just finished took.
	internal static float NextGap(float baseInterval, float relaxedInterval, bool stable, bool isScan, float scale, double lastRunMs)
	{
		float gap = baseInterval;
		if (stable && relaxedInterval > baseInterval) gap = relaxedInterval;
		if (!isScan) return gap;
		gap *= Math.Max(1f, scale);
		if (lastRunMs > 0 && !double.IsNaN(lastRunMs))
		{
			double budget = Math.Min(MaxBudgetGap, lastRunMs / 1000.0 / BudgetFraction);
			if (budget > gap) gap = (float)budget;
		}
		return gap;
	}
}

// Decides which long frames get a log line: only real stalls, rate limited, with a lifetime cap, so the log stays small.
internal sealed class HitchGate
{
	internal const double ThresholdMs = 80;
	internal const double MinGapSeconds = 2;
	internal const int MaxLogged = 40;

	private int _logged;
	private double _lastLoggedAt = double.NegativeInfinity;

	internal long Total { get; private set; }
	internal int Logged => _logged;
	internal long Suppressed { get; private set; }
	internal double Worst { get; private set; }

	// frameMs < 0 means "no valid sample this frame" (focus gap, mode change). Returns true if this stall should be logged.
	internal bool ShouldLog(double frameMs, double nowSeconds)
	{
		if (frameMs < ThresholdMs || double.IsNaN(frameMs)) return false;
		Total++;
		if (frameMs > Worst) Worst = frameMs;
		if (_logged >= MaxLogged || nowSeconds - _lastLoggedAt < MinGapSeconds)
		{
			Suppressed++;
			return false;
		}
		_logged++;
		_lastLoggedAt = nowSeconds;
		return true;
	}
}
