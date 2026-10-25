using System.Diagnostics;
using BelmontsCurseUltrawide;

public class FrameIntervalsTests
{
	[Fact]
	public void Session_hitches_survive_after_recent_window_has_rolled_over()
	{
		var samples = new FrameIntervals(2);
		long start = Stopwatch.Frequency;
		samples.Record(start, 0);
		samples.Record(start * 2, 0);
		samples.Record(start * 2 + 1, 0);
		samples.Record(start * 2 + 2, 0);
		Assert.True(samples.Snapshot(0).Max() < 25);
		Assert.Equal(1000d, samples.LifetimeWorst(0), 6);
		Assert.Equal(1, samples.LifetimeOver25(0));
	}

	[Fact]
	public void Session_hitch_totals_exclude_focus_gaps_and_mode_switches()
	{
		var samples = new FrameIntervals();
		samples.Record(Stopwatch.Frequency, 0);
		samples.ResetClock();
		samples.Record(Stopwatch.Frequency * 10, 0);
		samples.Record(Stopwatch.Frequency * 20, 1);
		Assert.Equal(0d, samples.LifetimeWorst(0));
		Assert.Equal(0d, samples.LifetimeWorst(1));
		Assert.Equal(0, samples.LifetimeOver25(0));
		Assert.Equal(0, samples.LifetimeOver25(1));
	}

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

	[Fact]
	public void Record_returns_the_interval_or_minus_one_when_there_is_no_valid_sample()
	{
		var samples = new FrameIntervals(4);
		long second = Stopwatch.Frequency;
		Assert.Equal(-1, samples.Record(second, 0));          // first frame
		Assert.Equal(1000, samples.Record(second * 2, 0), 3);  // 1 s later
		Assert.Equal(-1, samples.Record(second * 3, 1));       // mode change
		samples.ResetClock();
		Assert.Equal(-1, samples.Record(second * 9, 1));       // after a focus gap
	}
}
