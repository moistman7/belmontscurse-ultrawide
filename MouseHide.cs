using UnityEngine;
using UnityEngine.InputSystem;

namespace BelmontsCurseUltrawide;

// Hides the mouse cursor after a few idle seconds and shows it again on movement, a click or the wheel.
internal static class MouseHide
{
	private static float _lastActive;
	private static bool _hidden;

	internal static void Tick()
	{
		// When the game locks the cursor it is already managing it.
		if (!Plugin.HideMouse.Value || Cursor.lockState != CursorLockMode.None) { Restore(); return; }
		float now = Time.unscaledTime;
		var mouse = Mouse.current;
		if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.01f || mouse.leftButton.isPressed || mouse.rightButton.isPressed ||
			mouse.middleButton.isPressed || mouse.scroll.ReadValue().sqrMagnitude > 0.01f))
		{
			_lastActive = now;
			Restore();
			return;
		}
		if (now - _lastActive < Mathf.Max(0.5f, Plugin.MouseIdleSeconds.Value)) return;
		// Hide, and keep it hidden if the game shows it again while the mouse is still idle.
		if (Cursor.visible) { Cursor.visible = false; _hidden = true; }
	}

	private static void Restore()
	{
		if (!_hidden) return;
		Cursor.visible = true;
		_hidden = false;
	}
}
