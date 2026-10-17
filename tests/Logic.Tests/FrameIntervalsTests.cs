using System.Diagnostics;
using BelmontsCurseUltrawide;

public class FrameIntervalsTests
{
	[Fact]
	public void Converts_clock_ticks_to_milliseconds()
	{
		var samples = new FrameIntervals();
		samples.Record(Stopwatch.Frequency, 0);
		samples.Record(Stopwatch.Frequency * 2, 0);
		Assert.Equal(1000d, Assert.Single(samples.Snapshot(0)), 6);
	}

	[Fact]
	public void Excludes_diagnostic_gaps_and_mode_transitions()
	{
		var samples = new FrameIntervals();
		samples.Record(1, 0);
		samples.Record(2, 1);
		samples.ResetClock();
		samples.Record(100000, 1);
		Assert.Empty(samples.Snapshot(0));
		Assert.Empty(samples.Snapshot(1));
		samples.Record(100001, 1);
		Assert.Single(samples.Snapshot(1));
	}

	[Fact]
	public void Keeps_only_recent_samples_and_sorts_without_mutating_the_ring()
	{
		var samples = new FrameIntervals(2);
		samples.Record(1, 0);
		samples.Record(11, 0);
		samples.Record(14, 0);
		samples.Record(16, 0);
		var sorted = samples.Snapshot(0);
		Assert.Equal(2, sorted.Length);
		Assert.Equal(2 * 1000d / Stopwatch.Frequency, sorted[0], 10);
		Assert.Equal(3 * 1000d / Stopwatch.Frequency, sorted[1], 10);
		Assert.Equal(sorted, samples.Snapshot(0));
	}
}
