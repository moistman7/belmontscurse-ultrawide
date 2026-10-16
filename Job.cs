using System;

namespace BelmontsCurseUltrawide;

internal class Job
{
	public string Name;

	public float Interval;

	public float Next;

	public Action Run;

	public double MaxMs;

	public double TotalMs;

	public int Runs;

	public bool ErrorLogged;

	public int Errors;

	public bool Disabled;
}
