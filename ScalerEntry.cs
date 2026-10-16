using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

internal class ScalerEntry
{
	public CanvasScaler Scaler;

	public RectTransform Root;

	public int Id;

	public string Name;

	public bool IsMenu;

	public bool IsHud;

	public bool IsHudScale;

	public readonly Dictionary<int, bool> ChildWasActive = new Dictionary<int, bool>();

	public readonly HashSet<int> Extended = new HashSet<int>();
}
