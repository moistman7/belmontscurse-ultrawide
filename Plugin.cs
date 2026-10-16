using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BelmontsCurseUltrawide;

[BepInPlugin("local.belmontscurse.ultrawide", "Belmont's Curse Ultrawide", "0.6.0")]
public class Plugin : BasePlugin
{
	internal static Plugin Instance;

	internal static bool On;

	internal static bool CamOn;

	internal static ConfigEntry<bool> Enabled;

	internal static ConfigEntry<float> MatchValue;
	internal static ConfigEntry<bool> FixNarrow;
	internal static ConfigEntry<bool> PadShortcuts;
	internal static ConfigEntry<bool> AutoBigHud;
	internal static ConfigEntry<bool> AutoBigHudDone;
	internal static ConfigEntry<string> LastGameVersion;

	internal static ConfigEntry<bool> FixCameraRect;

	internal static ConfigEntry<bool> RemoveLetterbox;

	internal static ConfigEntry<bool> FixVideos;

	internal static ConfigEntry<bool> Camera16x9Logic;

	internal static ConfigEntry<string> CameraMaxAspect;

	internal static ConfigEntry<bool> FixScreenSize;

	internal static ConfigEntry<bool> DisableConfiners;

	internal static ConfigEntry<bool> CenterMenus;

	internal static ConfigEntry<string> MenuCanvases;

	internal static ConfigEntry<bool> CenterHud;

	internal static ConfigEntry<string> HudCanvases;

	internal static ConfigEntry<bool> BigHud;

	internal static ConfigEntry<float> HudScale;

	internal static ConfigEntry<string> HudScaleCanvases;

	internal static ConfigEntry<bool> AutoAdvance;

	internal static ConfigEntry<float> AutoAdvanceDelay;

	internal static ConfigEntry<Key> KeyCenterHud;

	internal static ConfigEntry<Key> KeyBigHud;

	internal static ConfigEntry<Key> KeyAutoAdvance;

	internal static ConfigEntry<Key> KeyDump;

	internal static ConfigEntry<GamepadButton> PadModifier;

	internal static ConfigEntry<GamepadButton> PadCenterHud;

	internal static ConfigEntry<GamepadButton> PadAutoAdvance;

	internal static ConfigEntry<GamepadButton> PadDump;

	internal static ConfigEntry<bool> SkipIntros;

	internal static ConfigEntry<int> StartupFpsCap;

	internal static ConfigEntry<bool> ForceResolution;

	internal static ConfigEntry<int> ResWidth;

	internal static ConfigEntry<int> ResHeight;

	internal static ConfigEntry<string> ScreenMode;

	internal static ConfigEntry<float> ForceDelay;

	internal static ConfigEntry<float> ForceWindow;

	public override void Load()
	{
		Instance = this;
		Enabled = Config.Bind("General", "EnableFix", defaultValue: true, "Apply the ultrawide fix.");
		SkipIntros = Config.Bind("General", "SkipIntros", defaultValue: true, "Skip the company logos at startup (first-launch settings screens like brightness and language are still shown).");
		StartupFpsCap = Config.Bind("General", "StartupFpsCap", 60, "Limit the frame rate to this while the game starts up (logos, loading, until the title screen), which avoids the occasional black screen at startup. After that your in-game frame rate and V-Sync settings are put back. 0 = off.");
		AutoAdvance = Config.Bind("Dialogue", "AutoAdvanceVoiced", defaultValue: false, "Advance voiced dialogue automatically once the voice line has finished and the text is fully shown. Lines without voice still wait for you. F5 toggles it in game (saved).");
		AutoAdvanceDelay = Config.Bind("Dialogue", "AutoAdvanceDelay", 0.4f, "Seconds to wait after the voice and text are done before advancing.");
		KeyCenterHud = Config.Bind("Hotkeys", "CenterHUD", Key.F7, "Key that toggles the HUD between the centered 16:9 area and the screen edges. Use a key name such as F10, Home, Backquote or Numpad5.");
		KeyBigHud = Config.Bind("Hotkeys", "BigHUD", Key.F6, "Key that toggles the bigger HUD.");
		KeyAutoAdvance = Config.Bind("Hotkeys", "AutoAdvanceVoiced", Key.F5, "Key that toggles auto-advance for voiced dialogue.");
		KeyDump = Config.Bind("Hotkeys", "DiagnosticDump", Key.F9, "Key that writes the diagnostic dump to the log.");
		PadModifier = Config.Bind("Controller", "Modifier", GamepadButton.Select, "Hold this controller button and press one of the buttons below. (L3 + R3 together always toggles the bigger HUD.) Needs Input.GamepadShortcuts. Button names: Select, Start, North, South, East, West, LeftShoulder, RightShoulder, LeftStick, RightStick, DpadUp, DpadDown, DpadLeft, DpadRight.");
		PadCenterHud = Config.Bind("Controller", "CenterHUD", GamepadButton.LeftStick, "Modifier + this button toggles the centered HUD.");
		PadAutoAdvance = Config.Bind("Controller", "AutoAdvanceVoiced", GamepadButton.RightStick, "Modifier + this button toggles auto-advance for voiced dialogue.");
		PadDump = Config.Bind("Controller", "DiagnosticDump", GamepadButton.Start, "Modifier + this button writes the diagnostic dump to the log.");
		MatchValue = Config.Bind("General", "CanvasMatch", 1f, "CanvasScaler match value used on ultrawide. 0 = match width (zoomed), 1 = match height (no zoom).");
		FixVideos = Config.Bind("General", "FixVideos", defaultValue: true, "Stop pre-rendered videos from being stretched/zoomed on ultrawide (fits them inside the screen instead).");
		FixNarrow = Config.Bind("UI", "FixNarrowScreens", defaultValue: true, "On screens narrower than 16:9 (16:10 such as the Steam Deck), match the UI to the screen width so menus are not cropped/zoomed.");
		PadShortcuts = Config.Bind("Input", "GamepadShortcuts", defaultValue: true, "Controller shortcut for players without a keyboard (Steam Deck): press the left and right stick buttons (L3 + R3) together to toggle the bigger HUD.");
		AutoBigHud = Config.Bind("UI", "AutoBigHUD", defaultValue: true, "On small screens (height 900 or less, such as the Steam Deck) turn the bigger HUD on once, the first time the game runs there. After that your own choice (F6 / L3+R3 / BigHUD) is kept.");
		AutoBigHudDone = Config.Bind("UI", "AutoBigHUDApplied", defaultValue: false, "Set automatically once AutoBigHUD has been applied. Set to false to apply it again.");
		LastGameVersion = Config.Bind("General", "LastGameVersion", "", "Filled in automatically: the game version seen on the last run, used to log a warning when the game gets updated. Leave alone.");
		RemoveLetterbox = Config.Bind("General", "RemoveCutsceneLetterbox", defaultValue: true, "Hide the black cinematic bars the game draws during cutscenes.");
		FixCameraRect = Config.Bind("General", "FixCameraRect", defaultValue: true, "Remove side bars caused by a camera viewport narrower than the screen.");
		Camera16x9Logic = Config.Bind("Camera", "LimitCameraLogicAspect", defaultValue: true, "On screens wider than CameraLogicMaxAspect, the game's camera logic (room limits, follow, zoom) is told the screen is CameraLogicMaxAspect wide, while the picture still renders at full width. Fixes the camera losing the player on 32:9.");
		CameraMaxAspect = Config.Bind("Camera", "CameraLogicAspect", "auto", "What the game's camera logic is told the screen shape is. auto = 16:9 on any wider screen, which is what the rooms are built for (fixes the camera swinging or stopping when entering areas). Or a number: the widest aspect the camera logic sees (2.4 = 21:9, 1.778 = 16:9).");
		FixScreenSize = Config.Bind("Camera", "FixWidthBasedZoom", defaultValue: true, "Rooms/fights that size the camera by width are converted to the height a 16:9 screen would show, so they are not zoomed in on ultrawide.");
		DisableConfiners = Config.Bind("Camera", "DisableConfiners", defaultValue: false, "Old workaround: switch off the Cinemachine room confiners entirely. Usually not needed with Camera16x9Logic on.");
		CenterMenus = Config.Bind("UI", "CenterMenus", defaultValue: true, "Keep the pause menu, journal and popups inside a centered 16:9 area (their backgrounds still fill the screen).");
		MenuCanvases = Config.Bind("UI", "MenuCanvases", "GameMenu,Canvas_ConfirmPopup,StartMenuView", "Canvas names CenterMenus applies to.");
		if (MenuCanvases.Value == "GameMenu,Canvas_ConfirmPopup")
		{
			// Old default: add the title and save screens.
			MenuCanvases.Value = "GameMenu,Canvas_ConfirmPopup,StartMenuView";
		}
		CenterHud = Config.Bind("UI", "CenterHUD", defaultValue: false, "Keep the HUD and full-screen dialogue inside a centered 16:9 area instead of the screen edges. F7 toggles it in game (saved).");
		HudCanvases = Config.Bind("UI", "HudCanvases", "HUD_Canvas,DialogueBoxFullScreen", "Canvas names CenterHUD applies to.");
		BigHud = Config.Bind("UI", "BigHUD", defaultValue: false, "Draw the HUD (health, magic, notifications) bigger. F6 toggles it in game (saved).");
		HudScale = Config.Bind("UI", "HUDScale", 1.25f, "How much bigger the HUD is when BigHUD is on. 1.25 = 25% bigger. Sensible range 1.1 to 1.6.");
		HudScaleCanvases = Config.Bind("UI", "HUDScaleCanvases", "HUD_Canvas", "Canvas names BigHUD applies to.");
		ForceResolution = Config.Bind("Resolution", "ForceResolution", defaultValue: true, "Set the resolution at startup (replaces the -screen-width/-screen-height launch options).");
		ResWidth = Config.Bind("Resolution", "Width", 0, "Target width in pixels. 0 = use the monitor's native width.");
		ResHeight = Config.Bind("Resolution", "Height", 0, "Target height in pixels. 0 = use the monitor's native height.");
		ScreenMode = Config.Bind("Resolution", "ScreenMode", "FullScreenWindow", "FullScreenWindow (borderless), ExclusiveFullScreen, MaximizedWindow or Windowed.");
		ForceDelay = Config.Bind("Resolution", "DelaySeconds", 2f, "Wait this long after launch before forcing the resolution, so the game's own setup runs first.");
		ForceWindow = Config.Bind("Resolution", "RetrySeconds", 15f, "Keep re-applying for this many seconds if the game changes it back. After that the in-game settings are left alone.");
		On = Enabled.Value;
		CamOn = Camera16x9Logic.Value;
		bool isEnabled;
		BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler;
		try
		{
			IDictionary dictionary = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(Config) as IDictionary;
			int num = dictionary?.Count ?? 0;
			if (num > 0)
			{
				dictionary.Clear();
				Config.Save();
				ManualLogSource log = Log;
				bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(45, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Removed ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" old setting(s) from the config file.");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (Exception ex)
		{
			Log.LogWarning("Could not tidy the config file: " + ex.Message);
		}
		ClassInjector.RegisterTypeInIl2Cpp<UwBehaviour>();
		AddComponent<UwBehaviour>();
		UwBehaviour.Init();
		try
		{
			UwBehaviour.StartupCapFrame();
		}
		catch (Exception ex2)
		{
			Log.LogWarning("Startup FPS cap: " + ex2.Message);
		}
		Log.LogInfo("Belmont's Curse Ultrawide 0.6.0 loaded. F7 = HUD centered / at screen edges, F6 = big HUD (controller: L3 + R3), F5 = auto-advance voiced dialogue (controller: Select + R3), F9 = diagnostic dump. Controller: hold Select + L3 for the centered HUD.");
		try
		{
			string gameVersion = UnityEngine.Application.version;
			Log.LogInfo("Game version " + gameVersion + " (Unity " + UnityEngine.Application.unityVersion + ")");
			string lastVersion = LastGameVersion.Value ?? "";
			if (lastVersion.Length > 0 && lastVersion != gameVersion)
			{
				Log.LogWarning("The game was updated (" + lastVersion + " -> " + gameVersion + "). If something looks wrong, check for a newer release of this mod.");
			}
			LastGameVersion.Value = gameVersion;
		}
		catch (Exception ex3)
		{
			Log.LogWarning("Game version check: " + ex3.Message);
		}
		ManualLogSource log2 = Log;
		bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(181, 11, out isEnabled);
		if (isEnabled)
		{
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Settings: fixes=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(On ? "on" : "OFF");
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", camera logic=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(CamOn ? CameraMaxAspect.Value : "OFF");
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", width-based zoom fix=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(FixScreenSize.Value);
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", ");
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral("menus centered=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(CenterMenus.Value);
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", HUD centered=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(CenterHud.Value);
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", big HUD=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(BigHud.Value ? ("x" + HudScale.Value) : "off");
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", cutscene bars removed=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(RemoveLetterbox.Value);
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", skip intros=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(SkipIntros.Value);
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", startup fps cap=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted((StartupFpsCap.Value > 0) ? StartupFpsCap.Value.ToString() : "off");
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", videos fitted=");
			bepInExInfoLogInterpolatedStringHandler.AppendFormatted(FixVideos.Value);
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", ");
			bepInExInfoLogInterpolatedStringHandler.AppendLiteral("resolution=");
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = bepInExInfoLogInterpolatedStringHandler;
			string t;
			if (ForceResolution.Value)
			{
				t = ((ResWidth.Value > 0 && ResHeight.Value > 0) ? (ResWidth.Value + "x" + ResHeight.Value) : "native");
			}
			else
			{
				t = "game's own";
			}
			bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(t);
		}
		log2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
	}
}
