using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BelmontsCurseUltrawide;

// The game's own controller button sprites (GamepadDescriptor assets), picked to match the connected pad.
internal static class GamepadGlyphs
{
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
		if (layout == _layout) return _current;
		_layout = layout;
		_current = null;
		// Load only the set that matches this pad instead of every glyph set under SpriteAssets.
		try
		{
			foreach (string name in Candidates(layout.ToLowerInvariant()))
			{
				_current = Resources.Load<GamepadDescriptor>("SpriteAssets/" + name);
				if (_current != null) break;
			}
			if (_current == null && !_scanned)
			{
				_scanned = true;
				foreach (var d in Resources.LoadAll<GamepadDescriptor>("SpriteAssets")) if (d != null) { _current = d; break; }
			}
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("Gamepad glyph load failed: " + ex.Message); }
		Plugin.Instance.Log.LogInfo("Gamepad glyphs: " + (_current != null ? _current.name : "none found") + " (pad layout " + layout + ")");
		return _current;
	}

	private static string[] Candidates(string layout)
	{
		if (layout.Contains("dualsense") || layout.Contains("ps5")) return new[] { "GamepadPS5", "GamepadPS4", "GamepadXbox" };
		if (layout.Contains("dual") || layout.Contains("playstation") || layout.Contains("ps4")) return new[] { "GamepadPS4", "GamepadXbox" };
		if (layout.Contains("switch") || layout.Contains("nintendo")) return new[] { "GamepadSwitch", "GamepadXbox" };
		if (layout.Contains("steam")) return new[] { "GamepadSteamdeck", "GamepadXbox" };
		return new[] { "GamepadXbox" };
	}
}
