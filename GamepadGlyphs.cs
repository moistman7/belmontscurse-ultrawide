using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BelmontsCurseUltrawide;

// The game's own controller button sprites (GamepadDescriptor assets), picked to match the connected pad.
internal static class GamepadGlyphs
{
	private static readonly List<GamepadDescriptor> All = new();
	private static bool _scanned;
	private static string _layout = "";
	private static GamepadDescriptor _current;

	internal static Sprite Get(GamepadButton button)
	{
		var pad = Gamepad.current;
		if (pad == null) return null;
		var descriptor = Descriptor(pad);
		if (descriptor == null) return null;
		var icons = descriptor.icons;
		return button switch
		{
			GamepadButton.South => icons.buttonSouth,
			GamepadButton.North => icons.buttonNorth,
			GamepadButton.East => icons.buttonEast,
			GamepadButton.West => icons.buttonWest,
			GamepadButton.Start => icons.startButton,
			GamepadButton.Select => icons.selectButton,
			GamepadButton.LeftTrigger => icons.leftTrigger,
			GamepadButton.RightTrigger => icons.rightTrigger,
			GamepadButton.LeftShoulder => icons.leftShoulder,
			GamepadButton.RightShoulder => icons.rightShoulder,
			GamepadButton.DpadUp => icons.dpadUp,
			GamepadButton.DpadDown => icons.dpadDown,
			GamepadButton.DpadLeft => icons.dpadLeft,
			GamepadButton.DpadRight => icons.dpadRight,
			GamepadButton.LeftStick => icons.leftStickPress,
			GamepadButton.RightStick => icons.rightStickPress,
			_ => null,
		};
	}

	internal static Sprite Dpad()
	{
		var pad = Gamepad.current;
		var descriptor = pad != null ? Descriptor(pad) : null;
		return descriptor?.icons.dpad;
	}

	private static GamepadDescriptor Descriptor(Gamepad pad)
	{
		string layout = pad.layout ?? "";
		if (_current != null && layout == _layout) return _current;
		_layout = layout;
		if (!_scanned)
		{
			// Only retry the scan while nothing has been found.
			try
			{
				foreach (var d in Resources.LoadAll<GamepadDescriptor>("SpriteAssets")) if (d != null) All.Add(d);
				if (All.Count == 0) foreach (var d in Resources.FindObjectsOfTypeAll<GamepadDescriptor>()) if (d != null) All.Add(d);
			}
			catch (Exception ex) { Plugin.Instance.Log.LogWarning("Gamepad glyph scan failed: " + ex.Message); }
			_scanned = All.Count > 0;
			var names = new List<string>();
			foreach (var d in All) names.Add(d.name + "/" + d.platformName);
			Plugin.Instance.Log.LogInfo("Gamepad glyph sets: " + string.Join(", ", names) + " (pad layout " + layout + ")");
		}
		string[] keys = Family(layout.ToLowerInvariant());
		_current = null;
		foreach (var key in keys)
		{
			foreach (var d in All)
				if ((d.name + " " + d.platformName).ToLowerInvariant().Contains(key)) { _current = d; break; }
			if (_current != null) break;
		}
		if (_current == null && All.Count > 0) _current = All[0];
		return _current;
	}

	private static string[] Family(string layout)
	{
		if (layout.Contains("dual") || layout.Contains("playstation") || layout.Contains("ps4") || layout.Contains("ps5")) return new[] { "dual", "playstation", "ps" };
		if (layout.Contains("switch") || layout.Contains("nintendo")) return new[] { "switch", "nintendo" };
		return new[] { "xbox", "xinput", "steam" };
	}
}
