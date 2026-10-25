using System;

namespace BelmontsCurseUltrawide;

internal class Job
{
	public string Name;

	public float Interval;

	public float Next;

	// Steady-state interval (0 = none): used once the scene has been stable for a while, for scans whose
	// results also arrive through a game hook or a scene-load kick.
	public float Relaxed;

	public Action Run;

	public double MaxMs;

	public double TotalMs;

	public int Runs;

	public bool ErrorLogged;

	public int Errors;

	public bool Disabled;
}
