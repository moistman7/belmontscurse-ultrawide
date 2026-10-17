using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BelmontsCurseUltrawide;

// The game's own controller button sprites (GamepadDescriptor assets), picked to match the connected pad.
internal static class GamepadGlyphs
{
	private static bool _scanned, _fullLoad;
	private static string _layout = "";
	private static GamepadDescriptor _current;
	private static readonly Dictionary<GamepadButton, Sprite> Sprites = new();
	private static Sprite _dpad;

	internal static void Dump(StringBuilder sb)
	{
		try
		{
			var pad = Gamepad.current;
			var descriptor = pad != null ? Descriptor(pad) : null;
			sb.AppendLine("Gamepad glyph diagnostics: shortcuts=" + Plugin.PadShortcuts.Value + " pad=" + (pad != null ? pad.name + " layout=" + pad.layout + " id=" + pad.deviceId : "none") + " descriptor=" + (descriptor != null ? descriptor.name : "none") + " replay=" + Plugin.PadReplay.Value);
			if (descriptor == null) return;
			var icons = descriptor.icons;
			foreach (var button in new[] { GamepadButton.South, GamepadButton.East, GamepadButton.LeftShoulder, GamepadButton.RightShoulder, Plugin.PadReplay.Value })
			{
				try
				{
					sb.AppendLine("  " + button + " field=" + Describe(FromField(icons, button)) + " backing=" + Describe(FromField(descriptor._icons, button)));
					foreach (var path in Paths(button))
					{
						try { sb.AppendLine("    GetSprite(" + path + ")=" + Describe(icons.GetSprite(path))); }
						catch (Exception ex) { sb.AppendLine("    GetSprite(" + path + ") error=" + ex.Message); }
					}
				}
				catch (Exception ex) { sb.AppendLine("  " + button + " error=" + ex.Message); }
			}
			sb.AppendLine("  Dpad field=" + Describe(icons.dpad) + " backing=" + Describe(descriptor._icons.dpad));
		}
		catch (Exception ex) { sb.AppendLine("Gamepad glyph diagnostics failed: " + ex.Message); }
	}

	internal static string Describe(Sprite sprite) => sprite != null ? sprite.name + " id=" + sprite.GetInstanceID() : "null";

	internal static Sprite Get(GamepadButton button)
	{
		var sprite = Lookup(button);
		if (sprite == null && !_fullLoad && Gamepad.current != null && _current != null)
		{
			// Loading only the matching descriptor can leave its button sprites unresolved; load the whole folder once, as the game does.
			_fullLoad = true;
			try { Resources.LoadAll<GamepadDescriptor>("SpriteAssets"); } catch { }
			sprite = Lookup(button);
			Plugin.Instance.Log.LogInfo("Gamepad glyphs: " + button + " was empty after loading the descriptor alone; after loading the folder it is " + (sprite != null ? "available" : "still missing"));
		}
		if (sprite == null && Gamepad.current != null && _current != null && _reported.Add(button))
			Plugin.Instance.Log.LogWarning("Gamepad glyphs: no sprite for " + button + " in " + _current.name + " (field read and GetSprite both empty)");
		return sprite;
	}

	private static readonly HashSet<GamepadButton> _reported = new();

	private static readonly HashSet<GamepadButton> _viaGame = new();

	private static Sprite Lookup(GamepadButton button)
	{
		var pad = Gamepad.current;
		if (pad == null) return null;
		var descriptor = Descriptor(pad);
		if (descriptor == null) return null;
		if (Sprites.TryGetValue(button, out var cached) && cached != null) return cached;
		var icons = descriptor.icons;
		var sprite = FromField(icons, button);
		sprite = Retain(sprite != null ? sprite : FromGame(icons, button));
		if (sprite != null) Sprites[button] = sprite;
		return sprite;
	}

	private static Sprite Retain(Sprite sprite)
	{
		if (sprite == null) return null;
		// Managed references alone do not keep IL2CPP assets alive through UnloadUnusedAssets.
		sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
		if (sprite.texture != null) sprite.texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
		return sprite;
	}

	// The interop reads the struct's fields by memory offset, which can come back empty; the game's own GetSprite reads them itself.
	private static Sprite FromGame(GamepadDescriptor.GamepadIcons icons, GamepadButton button)
	{
		foreach (string path in Paths(button))
		{
			try
			{
				var sprite = icons.GetSprite(path);
				if (sprite != null)
				{
					if (_viaGame.Add(button)) Plugin.Instance.Log.LogInfo("Gamepad glyphs: " + button + " found through GetSprite(\"" + path + "\")");
					return sprite;
				}
			}
			catch (Exception ex) { if (_viaGame.Add(button)) Plugin.Instance.Log.LogWarning("Gamepad glyphs: GetSprite(\"" + path + "\") failed: " + ex.Message); }
		}
		return null;
	}

	private static string[] Paths(GamepadButton button) => button switch
	{
		GamepadButton.South => new[] { "buttonSouth" },
		GamepadButton.North => new[] { "buttonNorth" },
		GamepadButton.East => new[] { "buttonEast" },
		GamepadButton.West => new[] { "buttonWest" },
		GamepadButton.Start => new[] { "start", "startButton" },
		GamepadButton.Select => new[] { "select", "selectButton" },
		GamepadButton.LeftTrigger => new[] { "leftTrigger" },
		GamepadButton.RightTrigger => new[] { "rightTrigger" },
		GamepadButton.LeftShoulder => new[] { "leftShoulder" },
		GamepadButton.RightShoulder => new[] { "rightShoulder" },
		GamepadButton.DpadUp => new[] { "dpad/up", "dpadUp" },
		GamepadButton.DpadDown => new[] { "dpad/down", "dpadDown" },
		GamepadButton.DpadLeft => new[] { "dpad/left", "dpadLeft" },
		GamepadButton.DpadRight => new[] { "dpad/right", "dpadRight" },
		GamepadButton.LeftStick => new[] { "leftStickPress", "leftStick" },
		GamepadButton.RightStick => new[] { "rightStickPress", "rightStick" },
		_ => Array.Empty<string>(),
	};

	private static Sprite FromField(GamepadDescriptor.GamepadIcons icons, GamepadButton button)
	{
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
		if (descriptor == null) return null;
		if (_dpad == null) _dpad = Retain(descriptor.icons.dpad);
		return _dpad;
	}

	private static GamepadDescriptor Descriptor(Gamepad pad)
	{
		string layout = pad.layout ?? "";
		if (layout == _layout && _current != null) return _current;
		Sprites.Clear();
		_dpad = null;
		_fullLoad = false;
		_reported.Clear();
		_viaGame.Clear();
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
		if (_current != null) _current.hideFlags |= HideFlags.DontUnloadUnusedAsset;
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
