using UnityEngine;

namespace BelmontsCurseUltrawide;

internal class StoredAnchors
{
	public RectTransform Rt;

	public AnchorState Orig;

	// Where the mod last placed this panel; a different position later means the game moved it.
	public AnchorState Applied;

	public int CanvasId;
}
