namespace BelmontsCurseUltrawide.Tests;

public class SchedulingTests
{
	[Fact]
	public void Cheap_scan_keeps_its_normal_interval()
		=> Assert.Equal(2f, Scheduling.NextGap(2f, 0f, stable: false, isScan: true, scale: 1f, lastRunMs: 1.0));

	[Fact]
	public void Stable_scene_uses_the_relaxed_interval_only_when_longer()
	{
		Assert.Equal(10f, Scheduling.NextGap(2f, 10f, stable: true, isScan: true, scale: 1f, lastRunMs: 0.5));
		Assert.Equal(2f, Scheduling.NextGap(2f, 10f, stable: false, isScan: true, scale: 1f, lastRunMs: 0.5));
		Assert.Equal(5f, Scheduling.NextGap(5f, 3f, stable: true, isScan: true, scale: 1f, lastRunMs: 0.5));
		Assert.Equal(2f, Scheduling.NextGap(2f, 0f, stable: true, isScan: true, scale: 1f, lastRunMs: 0.5));
	}

	[Fact]
	public void Slow_scan_waits_long_enough_to_stay_within_the_budget()
	{
		// 20 ms at 0.5% budget = 4 s, longer than the 2 s base.
		Assert.Equal(4f, Scheduling.NextGap(2f, 0f, stable: false, isScan: true, scale: 1f, lastRunMs: 20.0), 3);
		// 200 ms spike = 40 s, capped at 30 s.
		Assert.Equal(Scheduling.MaxBudgetGap, Scheduling.NextGap(2f, 0f, stable: false, isScan: true, scale: 1f, lastRunMs: 200.0));
	}

	[Fact]
	public void Budget_never_shortens_an_interval()
		=> Assert.Equal(10f, Scheduling.NextGap(2f, 10f, stable: true, isScan: true, scale: 1f, lastRunMs: 20.0));

	[Fact]
	public void Slowdown_setting_scales_scans_only()
	{
		Assert.Equal(6f, Scheduling.NextGap(2f, 0f, stable: false, isScan: true, scale: 3f, lastRunMs: 1.0));
		Assert.Equal(0.25f, Scheduling.NextGap(0.25f, 0f, stable: false, isScan: false, scale: 3f, lastRunMs: 1.0));
		// A scale below 1 (bad config) never speeds scans up.
		Assert.Equal(2f, Scheduling.NextGap(2f, 0f, stable: false, isScan: true, scale: 0.2f, lastRunMs: 1.0));
	}

	[Fact]
	public void Non_scan_jobs_ignore_the_budget()
		=> Assert.Equal(0.25f, Scheduling.NextGap(0.25f, 0f, stable: false, isScan: false, scale: 1f, lastRunMs: 50.0));

	[Fact]
	public void Bad_run_times_are_ignored()
	{
		Assert.Equal(2f, Scheduling.NextGap(2f, 0f, stable: false, isScan: true, scale: 1f, lastRunMs: double.NaN));
		Assert.Equal(2f, Scheduling.NextGap(2f, 0f, stable: false, isScan: true, scale: 1f, lastRunMs: -3.0));
	}

	[Fact]
	public void Hitch_gate_ignores_normal_and_invalid_frames()
	{
		var gate = new HitchGate();
		Assert.False(gate.ShouldLog(-1, 0));
		Assert.False(gate.ShouldLog(16.7, 1));
		Assert.False(gate.ShouldLog(79.9, 2));
		Assert.False(gate.ShouldLog(double.NaN, 3));
		Assert.Equal(0, gate.Total);
	}

	[Fact]
	public void Hitch_gate_logs_a_stall_then_rate_limits()
	{
		var gate = new HitchGate();
		Assert.True(gate.ShouldLog(120, 10));
		Assert.False(gate.ShouldLog(300, 11));   // within 2 s of the last log
		Assert.True(gate.ShouldLog(95, 12.5));
		Assert.Equal(3, gate.Total);
		Assert.Equal(2, gate.Logged);
		Assert.Equal(1, gate.Suppressed);
		Assert.Equal(300, gate.Worst);
	}

	[Fact]
	public void Hitch_gate_stops_logging_after_the_session_cap()
	{
		var gate = new HitchGate();
		int logged = 0;
		for (int i = 0; i < 100; i++) if (gate.ShouldLog(100, i * 3.0)) logged++;
		Assert.Equal(HitchGate.MaxLogged, logged);
		Assert.Equal(100, gate.Total);
		Assert.Equal(100 - HitchGate.MaxLogged, gate.Suppressed);
	}
}
