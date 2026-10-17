using System;
using UnityEngine;

namespace BelmontsCurseUltrawide;

// The dark gradient along the bottom of full-screen dialogue is exactly screen-wide and its image fades out
// at the sides, which leaves a lighter strip at the screen edge on ultrawide. Stretch it past the edges.
internal static class DialogueDim
{
	private const string CanvasName = "DialogueBoxFullScreen";

	private const string DimName = "Gradient";

	private static float _nextSearch;

	private static RectTransform _dim;

	internal static void Tick()
	{
		float overscan = Plugin.DialogueDimOverscan.Value;
		if (overscan <= 0f || !Plugin.On)
		{
			return;
		}
		float now = Time.unscaledTime;
		if (_dim == null)
		{
			if (now < _nextSearch)
			{
				return;
			}
			_nextSearch = now + 1f;
			GameObject canvas = GameObject.Find(CanvasName);
			Transform child = (canvas == null) ? null : canvas.transform.Find(DimName);
			_dim = (child == null) ? null : child.GetComponent<RectTransform>();
			if (_dim == null)
			{
				return;
			}
			Plugin.Instance.Log.LogInfo("Dialogue dim gradient widened by " + overscan + " on each side");
		}
		Vector2 min = _dim.offsetMin;
		Vector2 max = _dim.offsetMax;
		if (Math.Abs(min.x + overscan) > 0.01f || Math.Abs(max.x - overscan) > 0.01f)
		{
			_dim.offsetMin = new Vector2(-overscan, min.y);
			_dim.offsetMax = new Vector2(overscan, max.y);
		}
	}
}
