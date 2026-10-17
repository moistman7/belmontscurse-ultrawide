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

[BepInPlugin("local.belmontscurse.ultrawide", "Belmont's Curse Ultrawide", "0.8.4")]
public class Plugin : BasePlugin
{
	internal static Plugin Instance;

	internal static bool On;
	// Menu layout switches are read once at startup: changing them while menus exist leaves their scaling half-applied.
	internal static bool MenusOn, NarrowOn;

	internal static bool CamOn;

	internal static ConfigEntry<bool> Enabled;

	internal static ConfigEntry<float> MatchValue;
	internal static ConfigEntry<bool> FixNarrow;
	internal static ConfigEntry<bool> PadShortcuts;
	internal static ConfigEntry<bool> HideMouse;
	internal static ConfigEntry<bool> HideMouseAlways;
	internal static ConfigEntry<float> MouseIdleSeconds;
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
	internal static ConfigEntry<float> HudSafeHorizontal;
	internal static ConfigEntry<float> HudSafeVertical;
	internal static ConfigEntry<bool> HistoryEnabled;
	internal static ConfigEntry<bool> SaveHistory;
	internal static ConfigEntry<bool> EnableTaaOption;
	internal static ConfigEntry<int> HistoryLimit;
	internal static ConfigEntry<Key> KeyHistory;
	internal static ConfigEntry<GamepadButton> PadHistory;
	internal static ConfigEntry<Key> KeyMenu;
	internal static ConfigEntry<GamepadButton> PadMenu;
	internal static ConfigEntry<Key> KeyReplay;
	internal static ConfigEntry<GamepadButton> PadReplay;

	internal static ConfigEntry<string> HudScaleCanvases;

	internal static ConfigEntry<bool> AutoAdvance;

	internal static ConfigEntry<float> AutoAdvanceDelay;

	internal static ConfigEntry<bool> AutoAdvanceUnvoiced;

	internal static ConfigEntry<float> UnvoicedBaseSeconds;

	internal static ConfigEntry<float> UnvoicedSecondsPerCharacter;

	internal static ConfigEntry<bool> ShowToasts;

	internal static ConfigEntry<float> DialogueDimOverscan;

	internal static ConfigEntry<Key> KeyCenterHud;

	internal static ConfigEntry<Key> KeyBigHud;

	internal static ConfigEntry<Key> KeyAutoAdvance;

	internal static ConfigEntry<Key> KeyAutoAdvanceUnvoiced;

	internal static ConfigEntry<Key> KeyDump;

	internal static ConfigEntry<GamepadButton> PadModifier;

	internal static ConfigEntry<GamepadButton> PadCenterHud;

	internal static ConfigEntry<GamepadButton> PadAutoAdvance;

	internal static ConfigEntry<GamepadButton> PadAutoAdvanceUnvoiced;

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
		EnableTaaOption = Config.Bind("Graphics", "EnableTAAOption", true, "Add Temporal Anti-Aliasing to the Video menu's anti-aliasing choices (the game's own TAA mode).");
		HistoryEnabled = Config.Bind("Dialogue", "HistoryEnabled", true, "Keep dialogue text in memory for this game session. F10 opens history; nothing is written to disk.");
		SaveHistory = Config.Bind("Dialogue", "SaveHistoryToDisk", false, "Keep dialogue history between game sessions in BepInEx/BelmontsCurseUltrawide_history.txt (text and speaker names only; portraits and voice are not saved). Read at startup.");
		HistoryLimit = Config.Bind("Dialogue", "HistoryLimit", 200, new ConfigDescription("Maximum dialogue lines kept for this session.", new AcceptableValueRange<int>(20, 1000)));
		KeyHistory = Config.Bind("Hotkeys", "DialogueHistory", Key.F10, "Open/close dialogue history. PageUp/PageDown browse; Escape closes.");
		PadHistory = Config.Bind("Controller", "DialogueHistory", GamepadButton.North, "Modifier + this button opens/closes dialogue history. D-pad navigates the whole history menu; A/Cross selects, shoulders change pages.");
		KeyMenu = Config.Bind("Hotkeys", "ModMenu", Key.F1, "Open/close the mod settings menu from anywhere (the same panel as Settings > Graphics > Mod settings).");
		PadMenu = Config.Bind("Controller", "ModMenu", GamepadButton.LeftShoulder, "Modifier + this button opens/closes the mod settings menu.");
		KeyReplay = Config.Bind("Hotkeys", "ReplayHistoryVoice", Key.R, "Replay/stop the selected history line's voice while history is open. Arrows navigate the history menu; Enter selects.");
		PadReplay = Config.Bind("Controller", "ReplayHistoryVoice", GamepadButton.West, "Replay/stop selected history voice (no modifier) while history is open. D-pad navigates the history menu; A/Cross selects; shoulders change pages.");
		HudSafeHorizontal = Config.Bind("UI", "HUDSafeAreaHorizontal", 0f, new ConfigDescription("Inset each horizontal edge of HUD_Canvas by this percent of the available HUD width. Applied inside the centered 16:9 area when CenterHUD is on. 0 = original positions.", new AcceptableValueRange<float>(0f, 20f)));
		HudSafeVertical = Config.Bind("UI", "HUDSafeAreaVertical", 0f, new ConfigDescription("Inset each vertical edge of HUD_Canvas by this percent of screen height. 0 = original positions. Does not move menus or full-screen dialogue.", new AcceptableValueRange<float>(0f, 20f)));
		Enabled = Config.Bind("General", "EnableFix", defaultValue: true, "Apply the ultrawide fix.");
		SkipIntros = Config.Bind("General", "SkipIntros", defaultValue: true, "Skip the company logos at startup (first-launch settings screens like brightness and language are still shown).");
		StartupFpsCap = Config.Bind("General", "StartupFpsCap", 60, "Limit the frame rate to this while the game starts up (logos, loading, until the title screen), which avoids the occasional black screen at startup. After that your in-game frame rate and V-Sync settings are put back. 0 = off.");
		AutoAdvance = Config.Bind("Dialogue", "AutoAdvanceVoiced", defaultValue: false, "Advance voiced dialogue automatically once the voice line has finished and the text is fully shown. Lines without voice still wait for you. F5 toggles it in game (saved).");
		AutoAdvanceDelay = Config.Bind("Dialogue", "AutoAdvanceDelay", 0.4f, "Seconds to wait after the voice and text are done before advancing.");
		AutoAdvanceUnvoiced = Config.Bind("Dialogue", "AutoAdvanceUnvoiced", defaultValue: false, "Also advance lines that have no voice, after a reading-time delay. F6 toggles it in game (saved) (UnvoicedBaseSeconds + UnvoicedSecondsPerCharacter per character).");
		UnvoicedBaseSeconds = Config.Bind("Dialogue", "UnvoicedBaseSeconds", 1.0f, "Unvoiced lines: seconds to wait after the text is fully shown, before the per-character time.");
		UnvoicedSecondsPerCharacter = Config.Bind("Dialogue", "UnvoicedSecondsPerCharacter", 0.05f, "Unvoiced lines: extra seconds per character of the line (0.05 is roughly 20 characters per second).");
		ShowToasts = Config.Bind("UI", "ShowMessages", defaultValue: true, "Show a short on-screen message when you toggle auto-advance, the bigger HUD or the centered HUD.");
		DialogueDimOverscan = Config.Bind("UI", "DialogueDimOverscan", 300f, "Full-screen dialogue: how far (in UI units) the dark gradient along the bottom extends past each screen edge, so it reaches the sides on ultrawide. 0 = leave as the game made it.");
		KeyCenterHud = Config.Bind("Hotkeys", "CenterHUD", Key.F8, "Key that toggles the HUD between the centered 16:9 area and the screen edges. Use a key name such as F10, Home, Backquote or Numpad5.");
		KeyBigHud = Config.Bind("Hotkeys", "BigHUD", Key.F7, "Key that toggles the bigger HUD.");
		KeyAutoAdvance = Config.Bind("Hotkeys", "AutoAdvanceVoiced", Key.F5, "Key that toggles auto-advance for voiced dialogue.");
		KeyAutoAdvanceUnvoiced = Config.Bind("Hotkeys", "AutoAdvanceUnvoiced", Key.F6, "Key that toggles auto-advance for lines without voice.");
		KeyDump = Config.Bind("Hotkeys", "DiagnosticDump", Key.F9, "Key that writes the diagnostic dump to the log.");
		PadModifier = Config.Bind("Controller", "Modifier", GamepadButton.Select, "Hold this controller button and press one of the buttons below. (L3 + R3 together always toggles the bigger HUD.) Needs Input.GamepadShortcuts. Button names: Select, Start, North, South, East, West, LeftShoulder, RightShoulder, LeftStick, RightStick, DpadUp, DpadDown, DpadLeft, DpadRight.");
		PadCenterHud = Config.Bind("Controller", "CenterHUD", GamepadButton.LeftStick, "Modifier + this button toggles the centered HUD.");
		PadAutoAdvance = Config.Bind("Controller", "AutoAdvanceVoiced", GamepadButton.RightStick, "Modifier + this button toggles auto-advance for voiced dialogue.");
		PadAutoAdvanceUnvoiced = Config.Bind("Controller", "AutoAdvanceUnvoiced", GamepadButton.RightShoulder, "Modifier + this button toggles auto-advance for lines without voice.");
		PadDump = Config.Bind("Controller", "DiagnosticDump", GamepadButton.Start, "Modifier + this button writes the diagnostic dump to the log.");
		if (KeyAutoAdvance.Value == Key.F5 && KeyBigHud.Value == Key.F6 && KeyCenterHud.Value == Key.F7 && KeyDump.Value == Key.F9)
		{
			// Settings saved with the 0.7.0 default keys: move to the grouped layout (F6 is now unvoiced auto-advance).
			KeyBigHud.Value = Key.F7;
			KeyCenterHud.Value = Key.F8;
		}
		MatchValue = Config.Bind("General", "CanvasMatch", 1f, "CanvasScaler match value used on ultrawide. 0 = match width (zoomed), 1 = match height (no zoom).");
		FixVideos = Config.Bind("General", "FixVideos", defaultValue: true, "Stop pre-rendered videos from being stretched/zoomed on ultrawide (fits them inside the screen instead).");
		FixNarrow = Config.Bind("UI", "FixNarrowScreens", defaultValue: true, "On screens narrower than 16:9 (16:10 such as the Steam Deck), match the UI to the screen width so menus are not cropped/zoomed.");
		HideMouse = Config.Bind("Input", "HideMouseOnIdle", false, "Hide the mouse cursor after it has been still for MouseIdleSeconds, and show it again when you move or click it.");
		MouseIdleSeconds = Config.Bind("Input", "MouseIdleSeconds", 3f, "Seconds of mouse stillness before the cursor is hidden (0.5 or more).");
		HideMouseAlways = Config.Bind("Input", "HideMouseAlways", false, "Keep the cursor hidden all the time (works without HideMouseOnIdle; it is still shown in the history window and the mod menu). Game menus can then only be used with the keyboard or a controller.");
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
		CenterHud = Config.Bind("UI", "CenterHUD", defaultValue: false, "Keep the HUD and full-screen dialogue inside a centered 16:9 area instead of the screen edges. F8 toggles it in game (saved).");
		HudCanvases = Config.Bind("UI", "HudCanvases", "HUD_Canvas,DialogueBoxFullScreen", "Canvas names CenterHUD applies to.");
		BigHud = Config.Bind("UI", "BigHUD", defaultValue: false, "Draw the HUD (health, magic, notifications) bigger. F7 toggles it in game (saved).");
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
		MenusOn = CenterMenus.Value;
		NarrowOn = FixNarrow.Value;
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
		DialogueHistory.Init();
		AntiAliasing.Init();
		ModSettingsRow.Init();
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
		Log.LogInfo("Belmont's Curse Ultrawide 0.8.4 loaded. F5 = auto-advance voiced dialogue, F6 = auto-advance unvoiced dialogue, F7 = big HUD (controller: L3 + R3), F8 = HUD centered / at screen edges, F9 = diagnostic dump, F10 = dialogue history (controller: Select + North). Controller: hold Select, then R3 / Right Shoulder / L3 / Start for the other functions in that order.");
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
