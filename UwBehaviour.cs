using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Cinemachine;
using Il2CppInterop.Runtime;
using Il2CppSystem.Collections.Generic;
using MotherBase.CameraConfiner;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BelmontsCurseUltrawide;

public class UwBehaviour : MonoBehaviour
{
	private static readonly ScreenSideBars MainMenuBars = new("UwMainMenuBars");
	private const float Aspect16x9 = 1.7777778f;

	private static readonly System.Collections.Generic.List<ScalerEntry> Scalers = new System.Collections.Generic.List<ScalerEntry>();

	private static readonly System.Collections.Generic.List<Graphic> LetterboxGraphics = new System.Collections.Generic.List<Graphic>();

	private static readonly System.Collections.Generic.List<VideoPlayer> VideoPlayers = new System.Collections.Generic.List<VideoPlayer>();

	private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Camera> CameraBuffer;

	private static int CameraBufferCount;

	private static readonly System.Collections.Generic.Dictionary<int, RawImage> VideoImages = new System.Collections.Generic.Dictionary<int, RawImage>();

	private static readonly System.Collections.Generic.Dictionary<int, float> VideoImageNextSearch = new System.Collections.Generic.Dictionary<int, float>();

	private static readonly System.Collections.Generic.Dictionary<int, RawImage> RtImages = new System.Collections.Generic.Dictionary<int, RawImage>();

	private static readonly System.Collections.Generic.Dictionary<int, float> RtNextSearch = new System.Collections.Generic.Dictionary<int, float>();

	private static readonly System.Collections.Generic.Dictionary<int, Camera> RtAspectSet = new System.Collections.Generic.Dictionary<int, Camera>();

	private static readonly System.Collections.Generic.Dictionary<int, float> OrigMatch = new System.Collections.Generic.Dictionary<int, float>();

	private static readonly System.Collections.Generic.Dictionary<int, Vector2> OrigRef = new System.Collections.Generic.Dictionary<int, Vector2>();

	private static readonly HashSet<int> Hidden = new HashSet<int>();

	private static readonly System.Collections.Generic.Dictionary<int, int> OrigFit = new System.Collections.Generic.Dictionary<int, int>();

	private static readonly System.Collections.Generic.Dictionary<int, RiState> RiOrig = new System.Collections.Generic.Dictionary<int, RiState>();

	private static readonly System.Collections.Generic.Dictionary<int, StoredAnchors> UiOrig = new System.Collections.Generic.Dictionary<int, StoredAnchors>();

	private static readonly System.Collections.Generic.Dictionary<int, StoredAnchors> BackdropOrig = new System.Collections.Generic.Dictionary<int, StoredAnchors>();

	private static readonly HashSet<int> ConfinersOff = new HashSet<int>();

	private static readonly System.Collections.Generic.Dictionary<int, int> OrigVideoAspect = new System.Collections.Generic.Dictionary<int, int>();

	private static readonly System.Collections.Generic.Dictionary<int, ScreenSizeState> ScreenSizeOrig = new System.Collections.Generic.Dictionary<int, ScreenSizeState>();

	private static readonly System.Collections.Generic.List<CameraScreenSize> ScreenSizes = new System.Collections.Generic.List<CameraScreenSize>();

	private static readonly HashSet<string> LoggedScreenSizes = new HashSet<string>();

	private static readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<RectTransform, int>> TreeQueue = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<RectTransform, int>>();

	private static readonly System.Collections.Generic.List<int> TmpIds = new System.Collections.Generic.List<int>();

	private static readonly System.Collections.Generic.List<Job> Jobs = new System.Collections.Generic.List<Job>();

	private static int _jobIndex;

	private static readonly Stopwatch JobWatch = new Stopwatch();
	// Slowest single run of each per-frame feature since the last dump; shows what a frame spike came from.
	private static readonly System.Collections.Generic.Dictionary<string, double> ActionMaxMs = new System.Collections.Generic.Dictionary<string, double>();

	private static readonly Stopwatch FrameWatch = new Stopwatch();
	private static readonly FrameIntervals FrameSamples = new();
	private static int _dumpSerial;

	private static double _updateMaxMs;

	private static double _updateTotalMs;

	private static double _renderMaxMs;

	private static double _renderTotalMs;

	private static long _updateRuns;

	private static long _renderRuns;

	private static int _uiPass;

	private static IntPtr _kbPtr;


	private static KeyControl _kF7;

	private static KeyControl _kF10;


	private static KeyControl _kF6;

	private static KeyControl _kUnvoiced;

	private static KeyControl _kF9;

	private static float _nextForce;

	private static float _nextCamSearch;

	private static Camera _cam;
	private static CinemachineBrain _camBrain;

	private static bool _camSpoofed;
	private static Camera _menuViewportCamera;
	private static Rect _menuOriginalRect;
	private static bool _menuViewportApplied;

	private static bool _projectionErrorLogged;

	private static UnityAction<Scene, LoadSceneMode> _sceneLoaded;

	private static Il2CppSystem.Action<UnityEngine.Rendering.ScriptableRenderContext, Camera> _beginCamera;

	private static Il2CppSystem.Action<UnityEngine.Rendering.ScriptableRenderContext, Camera> _endCamera;

	private static bool _renderAspectSwapped;

	private static float _logicAspect;

	private static Matrix4x4 _savedProjection;

	private static bool _renderAspectErrorLogged;

	private static bool _wide;

	private static readonly Action KeysAction = ReadKeys;

	private static readonly Action ResAction = ForceRes;

	private static readonly Action CamAction = CameraLogic;

	private static readonly Action SizeAction = ScreenSizeFrame;

	private static readonly Action ProjAction = UpdateProjection;

	private static readonly Action IntroAction = SkipIntroFrame;

	private static readonly Action CapAction = StartupCapFrame;

	private static readonly System.Collections.Generic.Dictionary<string, int> FeatureErrors = new System.Collections.Generic.Dictionary<string, int>();

	private static readonly HashSet<string> DisabledFeatures = new HashSet<string>();

	private static bool _capDone;

	private static bool _capStarted;

	private static bool _capSplashSeen;

	private static int _capWantedFps = -1;

	private static int _capWantedVsync = -1;

	private static float _capSplashGoneAt = -1f;

	private static float _nextCapCheck;

	private static float _nextCapSplashCheck;

	private const float CapHoldAfterIntro = 5f;

	private const float CapMaxSeconds = 90f;

	private const float CapNoIntroSeconds = 30f;

	private static bool _introDone;

	private static bool _introSeen;

	private static float _introFirstSeen = -1f;

	private static float _nextIntroCheck;

	private static readonly HashSet<int> SkippedIntros = new HashSet<int>();

	public UwBehaviour(IntPtr ptr)
		: base(ptr)
	{
	}

	internal static void Init()
	{
		AddJob("scan canvases", 4f, ScanCanvases);
		AddJob("ui", 0.25f, UiJob);
		AddJob("scan cutscene", 5f, ScanCutscene);
		AddJob("letterbox", 0.2f, LetterboxJob);
		AddJob("scan save-room masks", 2f, SaveRoomMasks.Scan);
		AddJob("scan loading cameras", 2f, LoadingScreenPresentation.Scan);
		AddJob("save-room masks", 0.2f, SaveRoomMasks.Apply);
		AddJob("scan video", 5f, ScanVideo);
		AddJob("video", 0.25f, VideoJob);
		AddJob("scan camera size", 2f, ScanScreenSizes);
		AddJob("misc", 1f, MiscJob);
		AddJob("render cameras", 0.1f, RenderTextureCameraJob);
		try
		{
			_sceneLoaded = DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>((Action<Scene, LoadSceneMode>)((Scene s, LoadSceneMode m) =>
			{
				Kick();
				_nextIntroCheck = 0f;
				Guard("skip intros", IntroAction);
			}));
			SceneManager.add_sceneLoaded(_sceneLoaded);
		}
		catch (Exception ex)
		{
			Plugin.Instance.Log.LogWarning("Could not hook sceneLoaded (fixes still apply, slightly later): " + ex.Message);
		}
		try
		{
			// Hooked at load, so this runs before any game effect that subscribes later
			// (fullscreen light quads, planar reflections) and they see the real aspect.
			_beginCamera = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>>((Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>)OnBeginCameraRendering);
			_endCamera = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>>((Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>)OnEndCameraRendering);
			UnityEngine.Rendering.RenderPipelineManager.add_beginCameraRendering(_beginCamera);
			UnityEngine.Rendering.RenderPipelineManager.add_endCameraRendering(_endCamera);
		}
		catch (Exception ex2)
		{
			Plugin.Instance.Log.LogWarning("Could not hook camera rendering (door/fullscreen lights may look squashed): " + ex2.Message);
		}
	}

	// The 16:9 aspect is only meant for the game's camera logic. While the camera renders, effects
	// such as the door light shafts build a "fullscreen" quad from camera.aspect; with the 16:9 value
	// that quad covers only the middle of an ultrawide screen and the light gets squashed.
	private static void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera cam)
	{
		try
		{
			if (!_camSpoofed || cam == null || _cam == null || cam.Pointer != _cam.Pointer)
			{
				return;
			}
			int pixelHeight = cam.pixelHeight;
			if (pixelHeight <= 0)
			{
				return;
			}
			float num = (float)cam.pixelWidth / (float)pixelHeight;
			float aspect = cam.aspect;
			if (Math.Abs(aspect - num) > 0.0001f)
			{
				_logicAspect = aspect;
				_savedProjection = cam.projectionMatrix;
				cam.aspect = num;
				cam.projectionMatrix = _savedProjection;
				_renderAspectSwapped = true;
			}
		}
		catch (Exception ex)
		{
			if (!_renderAspectErrorLogged)
			{
				Plugin.Instance.Log.LogError("render aspect: " + ex);
			}
			_renderAspectErrorLogged = true;
		}
	}

	private static void OnEndCameraRendering(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera cam)
	{
		try
		{
			if (!_renderAspectSwapped || cam == null || _cam == null || cam.Pointer != _cam.Pointer)
			{
				return;
			}
			_renderAspectSwapped = false;
			if (_camSpoofed)
			{
				cam.aspect = _logicAspect;
				cam.projectionMatrix = _savedProjection;
			}
		}
		catch (Exception ex)
		{
			if (!_renderAspectErrorLogged)
			{
				Plugin.Instance.Log.LogError("render aspect: " + ex);
			}
			_renderAspectErrorLogged = true;
		}
	}

	private static void AddJob(string name, float interval, Action run)
	{
		Jobs.Add(new Job
		{
			Name = name,
			Interval = interval,
			Run = run
		});
	}

	// Called after the mod menu flips a setting: refresh the cached switches the fixes read, and re-run the scans.
	internal static void ApplyFixToggles()
	{

		Kick();
	}

	private static void Kick()
	{
		foreach (Job job in Jobs)
		{
			job.Next = 0f;
		}
		_jobIndex = 0;
	}

	private void LateUpdate()
	{
		Guard("camera picture", ProjAction);
		Guard("mouse hide", MouseHide.Tick);
	}

	private static void Guard(string name, Action a)
	{
		if (DisabledFeatures.Count > 0 && DisabledFeatures.Contains(name))
		{
			return;
		}
		try
		{
			long started = Stopwatch.GetTimestamp();
			int dumpBefore = _dumpSerial;
			a();
			if (dumpBefore != _dumpSerial) return;
			double took = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
			ActionMaxMs.TryGetValue(name, out var worst);
			if (took > worst) ActionMaxMs[name] = took;
		}
		catch (Exception t)
		{
			FeatureErrors.TryGetValue(name, out var value);
			value = (FeatureErrors[name] = value + 1);
			bool isEnabled;
			if (value == 1)
			{
				ManualLogSource log = Plugin.Instance.Log;
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(2, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(name);
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral(": ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				log.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
			if (value >= 20)
			{
				DisabledFeatures.Add(name);
				ManualLogSource log2 = Plugin.Instance.Log;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(95, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("'");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(name);
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("' kept failing (maybe after a game update) and was switched off; the other fixes keep running.");
				}
				log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				if (name == "camera" || name == "camera picture")
				{
					ResetCamera();
				}
			}
		}
	}

	private static void ResetCamera()
	{
		try
		{
			if (_cam != null && _camSpoofed)
			{
				_cam.ResetAspect();
				_cam.ResetProjectionMatrix();
			}
		}
		catch
		{
		}
		_camSpoofed = false;
	}

	private void Update()
	{
		if (Application.isFocused) FrameSamples.Record(Stopwatch.GetTimestamp(), ModMenu.IsOpen ? 2 : DialogueHistory.IsOpen ? 1 : 0);
		else FrameSamples.ResetClock();
		int dumpBefore = _dumpSerial;
		FrameWatch.Restart();
		try
		{
			_wide = Screen.height > 0 && (float)Screen.width / (float)Screen.height > 1.7877778f;
			Guard("keys", KeysAction);
			Guard("dialogue history", DialogueHistory.Tick);
			Guard("mod menu", ModMenu.Tick);
			Guard("resolution", ResAction);
			Guard("loading presentation", LoadingScreenPresentation.Apply);
			Guard("camera", CamAction);
			Guard("zone camera size", SizeAction);
			Guard("skip intros", IntroAction);
			Guard("startup fps cap", CapAction);
			Guard("world ui camera", WorldUiAction);
			Guard("gamepad shortcut", PadAction);
			Guard("auto big hud", AutoHudAction);
				Guard("auto advance", VoiceAdvance.Tick);
				Guard("toast", Toast.Tick);
				Guard("dialogue dim", DialogueDim.Tick);
			RunOneJob();
		}
		catch (Exception ex)
		{
			Plugin.Instance.Log.LogError(ex.ToString());
		}
		FrameWatch.Stop();
		if (dumpBefore != _dumpSerial) return;
		double totalMilliseconds = FrameWatch.Elapsed.TotalMilliseconds;
		_updateTotalMs += totalMilliseconds;
		_updateRuns++;
		if (totalMilliseconds > _updateMaxMs)
		{
			_updateMaxMs = totalMilliseconds;
		}
	}

	private static void RunOneJob()
	{
		float unscaledTime = Time.unscaledTime;
		int count = Jobs.Count;
		for (int i = 0; i < count; i++)
		{
			int num = (_jobIndex + i) % count;
			Job job = Jobs[num];
			if (job.Disabled || unscaledTime < job.Next)
			{
				continue;
			}
			job.Next = unscaledTime + job.Interval;
			_jobIndex = (num + 1) % count;
			JobWatch.Restart();
			try
			{
				job.Run();
			}
			catch (Exception t)
			{
				bool isEnabled;
				if (!job.ErrorLogged)
				{
					ManualLogSource log = Plugin.Instance.Log;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(8, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("job '");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted(job.Name);
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("': ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
				job.ErrorLogged = true;
				if (++job.Errors >= 20)
				{
					job.Disabled = true;
					ManualLogSource log2 = Plugin.Instance.Log;
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(95, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("'");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(job.Name);
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("' kept failing (maybe after a game update) and was switched off; the other fixes keep running.");
					}
					log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
			JobWatch.Stop();
			double totalMilliseconds = JobWatch.Elapsed.TotalMilliseconds;
			job.TotalMs += totalMilliseconds;
			job.Runs++;
			if (totalMilliseconds > job.MaxMs)
			{
				job.MaxMs = totalMilliseconds;
			}
			break;
		}
	}

	private static bool _autoHudDone;
	private static IntPtr _padPtr;
	private static readonly GamepadButton[] PadButtons = new GamepadButton[9];
	private static readonly ButtonControl[] PadControls = new ButtonControl[9];

	private static ButtonControl PadCtl(Gamepad pad, int slot, GamepadButton button)
	{
		if (PadControls[slot] == null || PadButtons[slot] != button)
		{
			PadControls[slot] = pad[button];
			PadButtons[slot] = button;
		}
		return PadControls[slot];
	}

	// Controller shortcut for players without a keyboard: both stick buttons (L3 + R3) toggle the big HUD.
	private static void PadAction()
	{
		if (!Plugin.PadShortcuts.Value)
		{
			return;
		}
		Gamepad current = Gamepad.current;
		if (current == null)
		{
			return;
		}
		if (_padPtr != current.Pointer)
		{
			_padPtr = current.Pointer;
			Array.Clear(PadControls, 0, PadControls.Length);
		}
		if (PadCtl(current, 0, Plugin.PadModifier.Value).isPressed)
		{
			if (PadCtl(current, 1, Plugin.PadMenu.Value).wasPressedThisFrame)
				ModMenu.Toggle();
			if (PadCtl(current, 2, Plugin.PadHistory.Value).wasPressedThisFrame)
				DialogueHistory.Toggle();
			// Modifier held: each feature has its own button.
			if (PadCtl(current, 3, Plugin.PadCenterHud.Value).wasPressedThisFrame)
			{
				Plugin.CenterHud.Value = !Plugin.CenterHud.Value;
				Plugin.Instance.Log.LogInfo("HUD centered = " + Plugin.CenterHud.Value + " (controller shortcut)");
				Toast.Show("Centered HUD: " + OnOff(Plugin.CenterHud.Value));
				Kick();
			}
			if (PadCtl(current, 4, Plugin.PadAutoAdvance.Value).wasPressedThisFrame)
			{
				Plugin.AutoAdvance.Value = !Plugin.AutoAdvance.Value;
				Plugin.Instance.Log.LogInfo("Auto-advance voiced dialogue = " + Plugin.AutoAdvance.Value + " (controller shortcut)");
				Toast.Show("Auto-advance (voiced): " + OnOff(Plugin.AutoAdvance.Value));
			}
			if (PadCtl(current, 5, Plugin.PadAutoAdvanceUnvoiced.Value).wasPressedThisFrame)
			{
				Plugin.AutoAdvanceUnvoiced.Value = !Plugin.AutoAdvanceUnvoiced.Value;
				Plugin.Instance.Log.LogInfo("Auto-advance unvoiced dialogue = " + Plugin.AutoAdvanceUnvoiced.Value + " (controller shortcut)");
				Toast.Show("Auto-advance (no voice): " + OnOff(Plugin.AutoAdvanceUnvoiced.Value));
			}
			if (PadCtl(current, 6, Plugin.PadDump.Value).wasPressedThisFrame)
			{
				Dump();
				Toast.Show("Diagnostic dump written");
			}
			return;
		}
		var leftStick = PadCtl(current, 7, GamepadButton.LeftStick);
		var rightStick = PadCtl(current, 8, GamepadButton.RightStick);
		if (leftStick.isPressed && rightStick.isPressed && (leftStick.wasPressedThisFrame || rightStick.wasPressedThisFrame))
		{
			Plugin.BigHud.Value = !Plugin.BigHud.Value;
			Plugin.Instance.Log.LogInfo("Big HUD = " + Plugin.BigHud.Value + " (controller shortcut, x" + Plugin.HudScale.Value + ")");
			Toast.Show("Bigger HUD: " + OnOff(Plugin.BigHud.Value));
			Kick();
		}
	}

	// Small screens (Steam Deck): turn the big HUD on once, then leave the choice to the player.
	private static void AutoHudAction()
	{
		if (_autoHudDone || Time.unscaledTime < Plugin.ForceDelay.Value + 3f)
		{
			return;
		}
		_autoHudDone = true;
		if (Plugin.AutoBigHud.Value && !Plugin.AutoBigHudDone.Value && Screen.height > 0 && Screen.height <= 900)
		{
			if (!Plugin.BigHud.Value)
			{
				Plugin.BigHud.Value = true;
				Plugin.Instance.Log.LogInfo("Small screen (" + Screen.width + "x" + Screen.height + "): big HUD turned on (UI.AutoBigHUD). Toggle it with F6 or L3+R3.");
			}
			Plugin.AutoBigHudDone.Value = true;
			Kick();
		}
	}

	private static string OnOff(bool value)
	{
		return value ? "ON" : "OFF";
	}

	private static readonly Key[] _keyOf = new Key[7];

	private static readonly KeyControl[] _keyCtl = new KeyControl[7];

	private static KeyControl KeyCtl(Keyboard keyboard, int slot, Key key)
	{
		KeyControl control = _keyCtl[slot];
		if (control == null || _keyOf[slot] != key)
		{
			control = _keyCtl[slot] = keyboard[key];
			_keyOf[slot] = key;
		}
		return control;
	}

	private static void ReadKeys()
	{
		Keyboard current = Keyboard.current;
		if (current == null)
		{
			_kbPtr = IntPtr.Zero;
			return;
		}
		// Controls are looked up again only when the keyboard changes or a key setting is edited, not every frame.
		if (_kbPtr != current.Pointer)
		{
			_kbPtr = current.Pointer;
			Array.Clear(_keyCtl, 0, _keyCtl.Length);
		}
		if (KeyCtl(current, 0, Plugin.KeyMenu.Value).wasPressedThisFrame)
			ModMenu.Toggle();
		if (KeyCtl(current, 1, Plugin.KeyHistory.Value).wasPressedThisFrame)
			DialogueHistory.Toggle();
		_kF7 = KeyCtl(current, 2, Plugin.KeyDump.Value);
		_kF10 = KeyCtl(current, 3, Plugin.KeyCenterHud.Value);
		_kF6 = KeyCtl(current, 4, Plugin.KeyBigHud.Value);
		_kF9 = KeyCtl(current, 5, Plugin.KeyAutoAdvance.Value);
		_kUnvoiced = KeyCtl(current, 6, Plugin.KeyAutoAdvanceUnvoiced.Value);
		if (_kF9.wasPressedThisFrame)
		{
			Plugin.AutoAdvance.Value = !Plugin.AutoAdvance.Value;
			Plugin.Instance.Log.LogInfo("Auto-advance voiced dialogue = " + Plugin.AutoAdvance.Value);
			Toast.Show("Auto-advance (voiced): " + OnOff(Plugin.AutoAdvance.Value));
		}
		if (_kUnvoiced.wasPressedThisFrame)
		{
			Plugin.AutoAdvanceUnvoiced.Value = !Plugin.AutoAdvanceUnvoiced.Value;
			Plugin.Instance.Log.LogInfo("Auto-advance unvoiced dialogue = " + Plugin.AutoAdvanceUnvoiced.Value);
			Toast.Show("Auto-advance (no voice): " + OnOff(Plugin.AutoAdvanceUnvoiced.Value));
		}
		if (_kF7.wasPressedThisFrame)
		{
			Dump();
			Toast.Show("Diagnostic dump written");
		}
		bool isEnabled;
		if (_kF10.wasPressedThisFrame)
		{
			Plugin.CenterHud.Value = !Plugin.CenterHud.Value;
			ManualLogSource log = Plugin.Instance.Log;
			isEnabled = Plugin.CenterHud.Value;
			log.LogInfo("HUD centered = " + isEnabled);
			Toast.Show("Centered HUD: " + OnOff(Plugin.CenterHud.Value));
			Kick();
		}
		if (_kF6.wasPressedThisFrame)
		{
			Plugin.BigHud.Value = !Plugin.BigHud.Value;
			ManualLogSource log2 = Plugin.Instance.Log;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(14, 2, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Big HUD = ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(Plugin.BigHud.Value);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" (x");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(Plugin.HudScale.Value);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(")");
			}
			log2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			Toast.Show("Bigger HUD: " + OnOff(Plugin.BigHud.Value));
			Kick();
		}
	}

	private static void ForceRes()
	{
		if (!Plugin.ForceResolution.Value)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime < Plugin.ForceDelay.Value || unscaledTime > Plugin.ForceDelay.Value + Plugin.ForceWindow.Value || unscaledTime < _nextForce)
		{
			return;
		}
		_nextForce = unscaledTime + 1f;
		int num = Plugin.ResWidth.Value;
		int num2 = Plugin.ResHeight.Value;
		if (num <= 0 || num2 <= 0)
		{
			num = Display.main.systemWidth;
			num2 = Display.main.systemHeight;
		}
		if (Screen.width != num || Screen.height != num2)
		{
			if (!Enum.TryParse<FullScreenMode>(Plugin.ScreenMode.Value, ignoreCase: true, out var result))
			{
				result = FullScreenMode.FullScreenWindow;
			}
			ManualLogSource log = Plugin.Instance.Log;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(29, 5, out var isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Forcing resolution ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("x");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num2);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(result);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" (was ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(Screen.width);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("x");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(Screen.height);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(")");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			Screen.SetResolution(num, num2, result);
			Kick();
		}
	}

	private static Camera GameplayCamera()
	{
		float unscaledTime = Time.unscaledTime;
		if (_cam != null && unscaledTime < _nextCamSearch)
		{
			return _cam;
		}
		_nextCamSearch = unscaledTime + ((_cam != null) ? 5f : 1f);
		Camera camera = null;
		foreach (CinemachineBrain item in UnityEngine.Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None))
		{
			if (!(item == null))
			{
				Camera component = item.GetComponent<Camera>();
				if (component != null && component.targetTexture == null && component.isActiveAndEnabled)
				{
					camera = component;
					break;
				}
			}
		}
		if (camera == null && _cam != null)
		{
			return _cam;
		}
		if (camera == null || _cam == null || camera.Pointer != _cam.Pointer)
		{
			if (_cam != null && _camSpoofed)
			{
				_cam.ResetAspect();
				_cam.ResetProjectionMatrix();
			}
			_camSpoofed = false;
			_cam = camera;
			_camBrain = camera == null ? null : camera.GetComponent<CinemachineBrain>();
		}
		return _cam;
	}

	private static float LogicAspectCap(float real)
	{
		if (float.TryParse((Plugin.CameraMaxAspect.Value ?? "auto").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result > 0f)
		{
			return Math.Max(1.7777778f, result);
		}
		return 1.7777778f;
	}

	private static void CameraLogic()
	{
		Camera camera = GameplayCamera();
		MainMenuViewport(camera);
		if (camera == null)
		{
			return;
		}
		float num = ((Screen.height > 0) ? ((float)Screen.width / (float)Screen.height) : 1.7777778f);
		float num2 = LogicAspectCap(num);
		if (Plugin.On && num > num2 + 0.01f)
		{
			if (!_camSpoofed)
			{
				ManualLogSource log = Plugin.Instance.Log;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(51, 3, out var isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Camera logic limited to ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num2, "F3");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" aspect for '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(camera.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' (screen is ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num, "F3");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(")");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				_camSpoofed = true;
			}
			if (Math.Abs(camera.aspect - num2) > 0.0001f)
			{
				camera.aspect = num2;
			}
		}
		else if (_camSpoofed)
		{
			camera.ResetAspect();
			camera.ResetProjectionMatrix();
			_camSpoofed = false;
			Plugin.Instance.Log.LogInfo("Camera logic aspect limit off");
		}
	}

	private static void MainMenuViewport(Camera camera)
	{
		var brain = _camBrain;
		var live = brain == null ? null : brain.ActiveVirtualCamera;
		bool apply = Plugin.On && Screen.height > 0 && (float)Screen.width / Screen.height > Aspect16x9 + 0.01f
			&& live != null && live.Name == "CamMainMenu";
		if (_menuViewportApplied && (!apply || camera == null || _menuViewportCamera == null || camera.Pointer != _menuViewportCamera.Pointer))
		{
			if (_menuViewportCamera != null) _menuViewportCamera.rect = _menuOriginalRect;
			_menuViewportApplied = false;
			_menuViewportCamera = null;
		}
		if (!apply)
		{
			MainMenuBars.Set(false);
			return;
		}
		if (!_menuViewportApplied)
		{
			_menuViewportCamera = camera;
			_menuOriginalRect = camera.rect;
			_menuViewportApplied = true;
		}
		float width = Aspect16x9 / ((float)Screen.width / Screen.height);
		var viewport = new Rect((1f - width) * 0.5f, 0f, width, 1f);
		if (camera.rect != viewport) camera.rect = viewport;
		MainMenuBars.Set(true, width);
	}

	private static void UpdateProjection()
	{
		if (!_camSpoofed)
		{
			return;
		}
		long timestamp = Stopwatch.GetTimestamp();
		try
		{
			Camera cam = _cam;
			if (cam == null)
			{
				return;
			}
			int pixelHeight = cam.pixelHeight;
			if (pixelHeight > 0)
			{
				float num = (float)cam.pixelWidth / (float)pixelHeight;
				float nearClipPlane = cam.nearClipPlane;
				float farClipPlane = cam.farClipPlane;
				bool orthographic = cam.orthographic;
				float num2 = (orthographic ? cam.orthographicSize : cam.fieldOfView);
				Matrix4x4 projectionMatrix = default;
				if (orthographic)
				{
					float num3 = num2;
					projectionMatrix.m00 = 1f / (num3 * num);
					projectionMatrix.m11 = 1f / num3;
					projectionMatrix.m22 = -2f / (farClipPlane - nearClipPlane);
					projectionMatrix.m23 = (0f - (farClipPlane + nearClipPlane)) / (farClipPlane - nearClipPlane);
					projectionMatrix.m33 = 1f;
				}
				else
				{
					float num4 = 1f / (float)Math.Tan((double)num2 * Math.PI / 360.0);
					projectionMatrix.m00 = num4 / num;
					projectionMatrix.m11 = num4;
					projectionMatrix.m22 = (farClipPlane + nearClipPlane) / (nearClipPlane - farClipPlane);
					projectionMatrix.m23 = 2f * farClipPlane * nearClipPlane / (nearClipPlane - farClipPlane);
					projectionMatrix.m32 = -1f;
				}
				cam.projectionMatrix = projectionMatrix;
			}
		}
		catch (Exception ex)
		{
			if (!_projectionErrorLogged)
			{
				Plugin.Instance.Log.LogError("projection: " + ex);
			}
			_projectionErrorLogged = true;
		}
		finally
		{
			double num5 = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
			_renderTotalMs += num5;
			_renderRuns++;
			if (num5 > _renderMaxMs)
			{
				_renderMaxMs = num5;
			}
		}
	}

	private static void ScanScreenSizes()
	{
		ScreenSizes.Clear();
		foreach (CameraScreenSize item in UnityEngine.Object.FindObjectsByType<CameraScreenSize>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (item != null)
			{
				ScreenSizes.Add(item);
			}
		}
	}

	private static void ScreenSizeFrame()
	{
		bool flag = Plugin.On && Plugin.FixScreenSize.Value && _wide;
		if (!flag && ScreenSizeOrig.Count == 0)
		{
			return;
		}
		foreach (CameraScreenSize screenSize in ScreenSizes)
		{
			if (screenSize == null)
			{
				continue;
			}
			if (flag)
			{
				if (screenSize.screenSizeType != CameraScreenSize.ScreenSizeType.Width)
				{
					continue;
				}
				int instanceID = screenSize.GetInstanceID();
				float horizontalScreenUnits = screenSize.horizontalScreenUnits;
				if (!ScreenSizeOrig.ContainsKey(instanceID))
				{
					ScreenSizeOrig[instanceID] = new ScreenSizeState
					{
						Type = screenSize.screenSizeType,
						Vertical = screenSize.verticalScreenUnits
					};
				}
				float num = horizontalScreenUnits / 1.7777778f;
				if (LoggedScreenSizes.Add(instanceID + ":" + horizontalScreenUnits))
				{
					ManualLogSource log = Plugin.Instance.Log;
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(45, 3, out var isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Width-based camera size on '");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(screenSize.gameObject.name);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("': ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(horizontalScreenUnits);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" wide -> ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" high");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
				screenSize.verticalScreenUnits = num;
				screenSize.screenSizeType = CameraScreenSize.ScreenSizeType.Height;
			}
			else
			{
				int instanceID2 = screenSize.GetInstanceID();
				if (ScreenSizeOrig.TryGetValue(instanceID2, out var value))
				{
					screenSize.screenSizeType = value.Type;
					screenSize.verticalScreenUnits = value.Vertical;
					ScreenSizeOrig.Remove(instanceID2);
				}
			}
		}
	}

	private static void SetConfiner(Behaviour b, bool off, string kind)
	{
		if (b == null)
		{
			return;
		}
		int instanceID = b.GetInstanceID();
		if (off)
		{
			if (!b.enabled)
			{
				return;
			}
			b.enabled = false;
			if (ConfinersOff.Add(instanceID))
			{
				ManualLogSource log = Plugin.Instance.Log;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(15, 2, out var isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Disabled ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(kind);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" on '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(b.gameObject.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		else if (ConfinersOff.Remove(instanceID))
		{
			b.enabled = true;
		}
	}

	private static void MiscJob()
	{
		bool flag = Plugin.On && Plugin.DisableConfiners.Value && _wide;
		if (flag || ConfinersOff.Count > 0)
		{
			foreach (CinemachineConfiner2D item in UnityEngine.Object.FindObjectsByType<CinemachineConfiner2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				SetConfiner(item, flag, "CinemachineConfiner2D");
			}
			foreach (CinemachineConfiner2DSimplified item2 in UnityEngine.Object.FindObjectsByType<CinemachineConfiner2DSimplified>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				SetConfiner(item2, flag, "CinemachineConfiner2DSimplified");
			}
		}
		if (!Plugin.On || !Plugin.FixCameraRect.Value)
		{
			return;
		}
		int cameraCount = FillCameraBuffer();
		for (int i = 0; i < cameraCount; i++)
		{
			Camera allCamera = CameraBuffer[i];
			if (LoadingScreenPresentation.Owns(allCamera)) continue;
			if (_menuViewportApplied && _menuViewportCamera != null && allCamera != null && allCamera.Pointer == _menuViewportCamera.Pointer) continue;
			if (!(allCamera == null) && !(allCamera.targetTexture != null))
			{
				Rect rect = allCamera.rect;
				if (rect.height > 0.999f && rect.width < 0.999f)
				{
					allCamera.rect = new Rect(0f, rect.y, 1f, rect.height);
				}
			}
		}
	}

	internal static void StartupCapFrame()
	{
		if (_capDone)
		{
			return;
		}
		int value = Plugin.StartupFpsCap.Value;
		if (value <= 0)
		{
			if (_capStarted)
			{
				ReleaseStartupCap("turned off");
			}
			else
			{
				_capDone = true;
			}
			return;
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup < _nextCapCheck)
		{
			return;
		}
		_nextCapCheck = realtimeSinceStartup + 0.05f;
		if (!_capStarted)
		{
			_capStarted = true;
			_capWantedFps = Application.targetFrameRate;
			_capWantedVsync = QualitySettings.vSyncCount;
			ManualLogSource log = Plugin.Instance.Log;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(78, 3, out var isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Startup: frame rate capped at ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(value);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" until the title screen (game had fps=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_capWantedFps);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", vsync=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_capWantedVsync);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(").");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
		int targetFrameRate = Application.targetFrameRate;
		if (targetFrameRate != value)
		{
			_capWantedFps = targetFrameRate;
		}
		int vSyncCount = QualitySettings.vSyncCount;
		if (vSyncCount != 0)
		{
			_capWantedVsync = vSyncCount;
		}
		if (targetFrameRate != value)
		{
			Application.targetFrameRate = value;
		}
		if (vSyncCount != 0)
		{
			QualitySettings.vSyncCount = 0;
		}
		if (realtimeSinceStartup >= _nextCapSplashCheck)
		{
			_nextCapSplashCheck = realtimeSinceStartup + 0.25f;
			if (UnityEngine.Object.FindObjectsByType<SplashScreenManager>(FindObjectsSortMode.None).Length > 0)
			{
				_capSplashSeen = true;
				_capSplashGoneAt = -1f;
			}
			else if (_capSplashSeen && _capSplashGoneAt < 0f)
			{
				_capSplashGoneAt = realtimeSinceStartup;
			}
		}
		if (_capSplashGoneAt >= 0f && realtimeSinceStartup - _capSplashGoneAt >= 5f)
		{
			ReleaseStartupCap("title screen reached");
		}
		else if (!_capSplashSeen && realtimeSinceStartup > 30f)
		{
			ReleaseStartupCap("no logo sequence seen");
		}
		else if (realtimeSinceStartup > 90f)
		{
			ReleaseStartupCap("time limit");
		}
	}

	private static void ReleaseStartupCap(string why)
	{
		_capDone = true;
		if (!_capStarted)
		{
			return;
		}
		try
		{
			if (QualitySettings.vSyncCount == 0 && _capWantedVsync > 0)
			{
				QualitySettings.vSyncCount = _capWantedVsync;
			}
			if (Application.targetFrameRate == Plugin.StartupFpsCap.Value)
			{
				Application.targetFrameRate = _capWantedFps;
			}
		}
		finally
		{
			ManualLogSource log = Plugin.Instance.Log;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(48, 3, out var isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Startup frame rate cap removed (");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(why);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("): fps=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(Application.targetFrameRate);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", vsync=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(QualitySettings.vSyncCount);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(".");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
	}

	private static void SkipIntroFrame()
	{
		if (_introDone || !Plugin.SkipIntros.Value)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime < _nextIntroCheck)
		{
			return;
		}
		_nextIntroCheck = unscaledTime + 0.02f;
		bool isEnabled;
		foreach (SplashScreenManager item in UnityEngine.Object.FindObjectsByType<SplashScreenManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			Il2CppSystem.Collections.Generic.List<SplashScreenElement> list = ((item == null) ? null : item.splashScreenElements);
			if (list == null)
			{
				continue;
			}
			for (int i = 1; i < list.Count; i++)
			{
				SplashScreenElement splashScreenElement = list[i];
				if (splashScreenElement == null)
				{
					continue;
				}
				BaseSplashScreenElement baseSplashScreenElement = splashScreenElement.TryCast<BaseSplashScreenElement>();
				if (!(baseSplashScreenElement == null) && (!(baseSplashScreenElement.TryCast<SplashScreenElementFades>() == null) || !(baseSplashScreenElement.TryCast<SplashScreenElementAnimation>() == null)) && (!baseSplashScreenElement.showOnlyOnce || !baseSplashScreenElement.showOnlyInJapaneseRegion))
				{
					baseSplashScreenElement.showOnlyOnce = true;
					baseSplashScreenElement.showOnlyInJapaneseRegion = true;
					ManualLogSource log = Plugin.Instance.Log;
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(24, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Intro '");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(baseSplashScreenElement.gameObject.name);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' will be skipped");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
			}
		}
		int num = 0;
		foreach (SplashScreenElementFades item2 in UnityEngine.Object.FindObjectsByType<SplashScreenElementFades>(FindObjectsSortMode.None))
		{
			if (item2 == null)
			{
				continue;
			}
			num++;
			if (!item2._animationStarted)
			{
				continue;
			}
			if (_introFirstSeen < 0f)
			{
				_introFirstSeen = unscaledTime;
			}
			if (!(unscaledTime - _introFirstSeen < 0.5f) && SkippedIntros.Add(item2.GetInstanceID()))
			{
				ManualLogSource log2 = Plugin.Instance.Log;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(17, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Skipping intro '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(item2.gameObject.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'");
				}
				log2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				item2.CloseSplashScreen();
			}
		}
		foreach (SplashScreenElementAnimation item3 in UnityEngine.Object.FindObjectsByType<SplashScreenElementAnimation>(FindObjectsSortMode.None))
		{
			if (item3 == null)
			{
				continue;
			}
			num++;
			Animation animationToPlay = item3.animationToPlay;
			if (animationToPlay == null || !animationToPlay.isPlaying)
			{
				continue;
			}
			if (_introFirstSeen < 0f)
			{
				_introFirstSeen = unscaledTime;
			}
			if (!(unscaledTime - _introFirstSeen < 0.5f) && SkippedIntros.Add(item3.GetInstanceID()))
			{
				ManualLogSource log3 = Plugin.Instance.Log;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(17, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Skipping intro '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(item3.gameObject.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'");
				}
				log3.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				item3.TrySkipSplashScreenElement();
			}
		}
		bool flag = UnityEngine.Object.FindObjectsByType<SplashScreenManager>(FindObjectsSortMode.None).Length > 0;
		if (flag || num > 0)
		{
			_introSeen = true;
		}
		if ((_introSeen && !flag && num == 0) || unscaledTime > 120f)
		{
			_introDone = true;
		}
	}

	private static RawImage FindImageFor(Camera cam, Texture tex)
	{
		int instanceID = cam.GetInstanceID();
		if (RtImages.TryGetValue(instanceID, out var value) && value != null && value.texture != null && value.texture.Pointer == tex.Pointer)
		{
			return value;
		}
		float unscaledTime = Time.unscaledTime;
		if (RtNextSearch.TryGetValue(instanceID, out var value2) && unscaledTime < value2)
		{
			return null;
		}
		RtNextSearch[instanceID] = unscaledTime + 2f;
		foreach (RawImage item in UnityEngine.Object.FindObjectsByType<RawImage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
		{
			if (!(item == null) && !(item.texture == null) && !(item.texture.Pointer != tex.Pointer))
			{
				RtImages[instanceID] = item;
				ManualLogSource log = Plugin.Instance.Log;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(36, 4, out var isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Camera '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(cam.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' draws into UI image '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(item.gameObject.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' (");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(tex.width);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("x");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(tex.height);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(")");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				return item;
			}
		}
		return null;
	}

	// Keep enumeration current without allocating Camera.allCameras on every poll.
	// Both callers run synchronously on the Unity main thread.
	private static int FillCameraBuffer()
	{
		int needed = Camera.allCamerasCount;
		if (CameraBuffer == null || CameraBuffer.Length < needed)
		{
			CameraBuffer = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Camera>(Math.Max(4, needed));
			CameraBufferCount = 0;
		}
		int count = needed == 0 ? 0 : Camera.GetAllCameras(CameraBuffer);
		// Release references to cameras that disappeared since the previous poll.
		for (int i = count; i < CameraBufferCount; i++) CameraBuffer[i] = null;
		CameraBufferCount = count;
		return count;
	}

	private static void RenderTextureCameraJob()
	{
		if (!Plugin.On || (!Plugin.MenusOn && !Plugin.CenterHud.Value) || !_wide)
		{
			if (RtAspectSet.Count <= 0)
			{
				return;
			}
			foreach (System.Collections.Generic.KeyValuePair<int, Camera> item in RtAspectSet)
			{
				if (item.Value != null)
				{
					item.Value.ResetAspect();
				}
			}
			RtAspectSet.Clear();
			return;
		}
		int cameraCount = FillCameraBuffer();
		for (int i = 0; i < cameraCount; i++)
		{
			Camera allCamera = CameraBuffer[i];
			if (allCamera == null)
			{
				continue;
			}
			RenderTexture targetTexture = allCamera.targetTexture;
			if (targetTexture == null || targetTexture.height <= 0)
			{
				continue;
			}
			RawImage rawImage = FindImageFor(allCamera, targetTexture);
			if (rawImage == null)
			{
				continue;
			}
			Rect rect = rawImage.rectTransform.rect;
			Rect uvRect = rawImage.uvRect;
			if (rect.height <= 0f || uvRect.width <= 0f || uvRect.height <= 0f)
			{
				continue;
			}
			float num = rect.width / rect.height;
			float num2 = (float)targetTexture.width / (float)targetTexture.height;
			float num3 = num * (uvRect.height / uvRect.width);
			int instanceID = allCamera.GetInstanceID();
			if (Math.Abs(num3 - num2) < 0.01f)
			{
				if (RtAspectSet.Remove(instanceID))
				{
					allCamera.ResetAspect();
				}
			}
			else
			{
				if (!(Math.Abs(allCamera.aspect - num3) > 0.001f))
				{
					continue;
				}
				if (!RtAspectSet.ContainsKey(instanceID))
				{
					ManualLogSource log = Plugin.Instance.Log;
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(55, 3, out var isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Camera '");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(allCamera.name);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("': rendering at ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num3, "F3");
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" to match its panel (image is ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num2, "F3");
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(")");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
				allCamera.aspect = num3;
				RtAspectSet[instanceID] = allCamera;
			}
		}
	}

	private static bool InList(string list, string name)
	{
		string[] array = list.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			if (string.Equals(array[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	// Menus such as the pause/map/journal are world-space canvases seen by an orthographic camera whose
	// size is fixed for a 16:9 height. On screens narrower than 16:9 the canvas is taller than that, so
	// the camera only sees the middle of it (zoomed in, tabs and footer cut off). Size the camera to
	// the canvas instead.
	private struct WorldCanvasEntry
	{
		internal Canvas Canvas;
		internal RectTransform Root;
	}

	private static readonly System.Collections.Generic.List<WorldCanvasEntry> WorldCanvases = new();

	private static readonly System.Collections.Generic.Dictionary<int, float> OrigOrthoSize = new System.Collections.Generic.Dictionary<int, float>();

	private static bool NarrowScreen()
	{
		if (Plugin.On && Plugin.NarrowOn && Screen.height > 0)
		{
			return (float)Screen.width / (float)Screen.height < 1.7677778f;
		}
		return false;
	}

	private static void WorldUiScan()
	{
		WorldCanvases.Clear();
		if (!NarrowScreen())
		{
			return;
		}
		foreach (ScalerEntry scaler in Scalers)
		{
			if (scaler.Scaler == null)
			{
				continue;
			}
			Canvas component = scaler.Scaler.GetComponent<Canvas>();
			if (component != null && component.renderMode == RenderMode.WorldSpace && component.worldCamera != null && component.worldCamera.orthographic)
			{
				WorldCanvases.Add(new WorldCanvasEntry { Canvas = component, Root = scaler.Root });
			}
		}
	}

	private static void WorldUiAction()
	{
		if (!NarrowScreen())
		{
			if (OrigOrthoSize.Count > 0)
			{
				foreach (var entry in WorldCanvases)
				{
					var camera = entry.Canvas == null ? null : entry.Canvas.worldCamera;
					if (camera != null && OrigOrthoSize.TryGetValue(camera.GetInstanceID(), out var value))
					{
						camera.orthographicSize = value;
					}
				}
				OrigOrthoSize.Clear();
			}
			return;
		}
		foreach (var entry in WorldCanvases)
		{
			var worldCamera = entry.Canvas == null ? null : entry.Canvas.worldCamera;
			if (worldCamera == null || entry.Root == null)
			{
				continue;
			}
			RectTransform rectTransform = entry.Root;
			float num = rectTransform.rect.height * rectTransform.lossyScale.y * 0.5f;
			if (num <= 1f)
			{
				continue;
			}
			int instanceID = worldCamera.GetInstanceID();
			if (!OrigOrthoSize.ContainsKey(instanceID))
			{
				OrigOrthoSize[instanceID] = worldCamera.orthographicSize;
			}
			if (Math.Abs(worldCamera.orthographicSize - num) > 0.01f)
			{
				worldCamera.orthographicSize = num;
			}
		}
	}

	private static void ScanCanvases()
	{
		HashSet<int> seen = new HashSet<int>();
		foreach (CanvasScaler item in UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (item == null)
			{
				continue;
			}
			int instanceID = item.GetInstanceID();
			seen.Add(instanceID);
			bool flag = false;
			foreach (ScalerEntry scaler in Scalers)
			{
				if (scaler.Id == instanceID)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				string text = item.gameObject.name;
				Scalers.Add(new ScalerEntry
				{
					Scaler = item,
					Root = item.GetComponent<RectTransform>(),
					Id = instanceID,
					Name = text,
					IsMenu = InList(Plugin.MenuCanvases.Value, text),
					IsHud = InList(Plugin.HudCanvases.Value, text),
					IsHudScale = InList(Plugin.HudScaleCanvases.Value, text)
				});
			}
		}
		Scalers.RemoveAll((ScalerEntry e) => !seen.Contains(e.Id));
	}

	private static void UiJob()
	{
		WorldUiScan();
		_uiPass++;
		bool fullPass = _uiPass % 8 == 0;
		float num = ((Screen.height > 0) ? ((float)Screen.width / (float)Screen.height) : 1.7777778f);
		foreach (ScalerEntry scaler2 in Scalers)
		{
			CanvasScaler scaler = scaler2.Scaler;
			if (scaler == null)
			{
				continue;
			}
			if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
			{
				Vector2 referenceResolution = scaler.referenceResolution;
				float num2 = ((referenceResolution.y > 0f) ? (referenceResolution.x / referenceResolution.y) : 1.7777778f);
				bool num3 = Plugin.On && num > num2 + 0.01f;
				float matchWidthOrHeight = scaler.matchWidthOrHeight;
				float value;
				bool narrow = Plugin.On && Plugin.NarrowOn && num < num2 - 0.01f;
				if (num3 || narrow)
				{
					if (!OrigMatch.ContainsKey(scaler2.Id))
					{
						OrigMatch[scaler2.Id] = matchWidthOrHeight;
					}
					float target = (narrow ? 0f : Plugin.MatchValue.Value);
					if (Math.Abs(matchWidthOrHeight - target) > 0.001f)
					{
						scaler.matchWidthOrHeight = target;
					}
				}
				else if (OrigMatch.TryGetValue(scaler2.Id, out value))
				{
					scaler.matchWidthOrHeight = value;
					OrigMatch.Remove(scaler2.Id);
				}
			}
			if (scaler2.IsHudScale && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
			{
				float num4 = Math.Max(0.5f, Math.Min(3f, Plugin.HudScale.Value));
				bool num5 = Plugin.On && Plugin.BigHud.Value && Math.Abs(num4 - 1f) > 0.001f;
				Vector2 referenceResolution2 = scaler.referenceResolution;
				Vector2 value2;
				if (num5)
				{
					if (!OrigRef.ContainsKey(scaler2.Id))
					{
						OrigRef[scaler2.Id] = referenceResolution2;
					}
					Vector2 referenceResolution3 = OrigRef[scaler2.Id] / num4;
					if (Math.Abs(referenceResolution2.x - referenceResolution3.x) > 0.5f || Math.Abs(referenceResolution2.y - referenceResolution3.y) > 0.5f)
					{
						scaler.referenceResolution = referenceResolution3;
					}
				}
				else if (OrigRef.TryGetValue(scaler2.Id, out value2))
				{
					scaler.referenceResolution = value2;
					OrigRef.Remove(scaler2.Id);
				}
			}
			if (scaler2.IsMenu || scaler2.IsHud || scaler2.Name == "HUD_Canvas")
			{
				bool flag = Plugin.On && (scaler2.IsMenu ? Plugin.MenusOn : Plugin.CenterHud.Value);
				CenterCanvas(scaler2, flag, fullPass);
			}
		}
	}

	private static AnchorState OrigAnchors(System.Collections.Generic.Dictionary<int, StoredAnchors> store, RectTransform rt, int id)
	{
		if (!store.TryGetValue(id, out var value))
		{
			return new AnchorState
			{
				Min = rt.anchorMin,
				Max = rt.anchorMax
			};
		}
		// The game moved this panel since the mod placed it (an animation, a layout change): what it has now is the real original.
		if (value.Applied != null && rt != null && MovedByGame(value, rt))
		{
			value.Orig = new AnchorState { Min = rt.anchorMin, Max = rt.anchorMax };
			value.Applied = null;
		}
		return value.Orig;
	}

	private static bool MovedByGame(StoredAnchors stored, RectTransform rt)
	{
		var a = stored.Applied;
		return Logic.Drifted(rt.anchorMin.x, rt.anchorMax.x, rt.anchorMin.y, rt.anchorMax.y, a.Min.x, a.Max.x, a.Min.y, a.Max.y);
	}

	private static bool SpansWidth(AnchorState a, RectTransform rt)
	{
		if (a.Min.x > 0.001f || a.Max.x < 0.999f)
		{
			return false;
		}
		if (Math.Abs(rt.sizeDelta.x) < 2f)
		{
			return Math.Abs(rt.anchoredPosition.x) < 2f;
		}
		return false;
	}

	private static bool IsLeafGraphic(RectTransform rt)
	{
		if (rt.childCount == 0)
		{
			return rt.GetComponent<Graphic>() != null;
		}
		return false;
	}

	private static bool SetAnchorsX(RectTransform rt, float minX, float maxX, AnchorState o)
	{
		Vector2 anchorMin = rt.anchorMin;
		Vector2 anchorMax = rt.anchorMax;
		if (Math.Abs(anchorMin.x - minX) <= 0.0005f && Math.Abs(anchorMax.x - maxX) <= 0.0005f)
		{
			return false;
		}
		rt.anchorMin = new Vector2(minX, o.Min.y);
		rt.anchorMax = new Vector2(maxX, o.Max.y);
		return true;
	}

	private static void CenterCanvas(ScalerEntry e, bool on, bool fullPass)
	{
		RectTransform root = e.Root;
		if (root == null)
		{
			return;
		}
		Rect rect = root.rect;
		if (rect.height <= 0f)
		{
			return;
		}
		float num = rect.width / rect.height;
		float horizontal = Plugin.On && e.Name == "HUD_Canvas" ? Plugin.HudSafeHorizontal.Value / 100f : 0f;
		float vertical = Plugin.On && e.Name == "HUD_Canvas" ? Plugin.HudSafeVertical.Value / 100f : 0f;
		float centered = on && num > 1.7877778f ? (1f - Aspect16x9 / num) * 0.5f : 0f;
		if (centered == 0f && horizontal == 0f && vertical == 0f)
		{
			RestoreAll(UiOrig, e.Id);
			RestoreAll(BackdropOrig, e.Id);
			e.Extended.Clear();
			return;
		}
		float num2 = centered + horizontal * (1f - 2f * centered);
		float k = num2 / (1f - 2f * num2);
		int childCount = root.childCount;
		for (int i = 0; i < childCount; i++)
		{
			RectTransform rectTransform = root.GetChild(i).TryCast<RectTransform>();
			if (rectTransform == null)
			{
				continue;
			}
			int instanceID = rectTransform.GetInstanceID();
			bool activeInHierarchy = rectTransform.gameObject.activeInHierarchy;
			e.ChildWasActive.TryGetValue(instanceID, out var value);
			e.ChildWasActive[instanceID] = activeInHierarchy;
			AnchorState anchorState = OrigAnchors(UiOrig, rectTransform, instanceID);
			if (!IsLeafGraphic(rectTransform) || !SpansWidth(anchorState, rectTransform))
			{
				if (!UiOrig.ContainsKey(instanceID))
				{
					UiOrig[instanceID] = new StoredAnchors
					{
						Rt = rectTransform,
						Orig = anchorState,
						CanvasId = e.Id
					};
				}
				SetAnchorsX(rectTransform, num2 + anchorState.Min.x * (1f - 2f * num2), num2 + anchorState.Max.x * (1f - 2f * num2), anchorState);
				rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, vertical + anchorState.Min.y * (1f - 2f * vertical));
				rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, vertical + anchorState.Max.y * (1f - 2f * vertical));
				if (UiOrig.TryGetValue(instanceID, out var placed)) placed.Applied = new AnchorState { Min = rectTransform.anchorMin, Max = rectTransform.anchorMax };
				if (e.Extended.Add(instanceID) || (activeInHierarchy && (!value | fullPass)))
				{
					ExtendBackdrops(rectTransform, k, e.Id);
				}
			}
		}
	}

	private static void RestoreAll(System.Collections.Generic.Dictionary<int, StoredAnchors> store, int canvasId)
	{
		if (store.Count == 0)
		{
			return;
		}
		TmpIds.Clear();
		foreach (System.Collections.Generic.KeyValuePair<int, StoredAnchors> item in store)
		{
			if (item.Value.CanvasId == canvasId)
			{
				TmpIds.Add(item.Key);
			}
		}
		foreach (int tmpId in TmpIds)
		{
			StoredAnchors storedAnchors = store[tmpId];
			// If the game has moved it since the mod placed it, the game's position stands; do not overwrite it.
			if (storedAnchors.Rt != null && !(storedAnchors.Applied != null && MovedByGame(storedAnchors, storedAnchors.Rt)))
			{
				storedAnchors.Rt.anchorMin = storedAnchors.Orig.Min;
				storedAnchors.Rt.anchorMax = storedAnchors.Orig.Max;
			}
			store.Remove(tmpId);
		}
	}

	private static void ExtendBackdrops(RectTransform top, float k, int canvasId)
	{
		TreeQueue.Clear();
		for (int i = 0; i < top.childCount; i++)
		{
			RectTransform rectTransform = top.GetChild(i).TryCast<RectTransform>();
			if (rectTransform != null)
			{
				TreeQueue.Add(new System.Collections.Generic.KeyValuePair<RectTransform, int>(rectTransform, 1));
			}
		}
		for (int j = 0; j < TreeQueue.Count; j++)
		{
			RectTransform key = TreeQueue[j].Key;
			int value = TreeQueue[j].Value;
			int instanceID = key.GetInstanceID();
			AnchorState anchorState = OrigAnchors(BackdropOrig, key, instanceID);
			if (!SpansWidth(anchorState, key))
			{
				continue;
			}
			if (IsLeafGraphic(key))
			{
				if (!BackdropOrig.ContainsKey(instanceID))
				{
					BackdropOrig[instanceID] = new StoredAnchors
					{
						Rt = key,
						Orig = anchorState,
						CanvasId = canvasId
					};
				}
				SetAnchorsX(key, 0f - k, 1f + k, anchorState);
			}
			else
			{
				if (value >= 4)
				{
					continue;
				}
				for (int l = 0; l < key.childCount; l++)
				{
					RectTransform rectTransform2 = key.GetChild(l).TryCast<RectTransform>();
					if (rectTransform2 != null)
					{
						TreeQueue.Add(new System.Collections.Generic.KeyValuePair<RectTransform, int>(rectTransform2, value + 1));
					}
				}
			}
		}
		TreeQueue.Clear();
	}

	private static void ScanCutscene()
	{
		LetterboxGraphics.Clear();
		foreach (CutsceneLetterBoxing item in UnityEngine.Object.FindObjectsByType<CutsceneLetterBoxing>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (item == null)
			{
				continue;
			}
			CacheLetterboxGraphics(item);
			// The controller can reference a view outside its own transform hierarchy.
			if (item._uiView != null) CacheLetterboxGraphics(item._uiView);
		}
	}

	private static void CacheLetterboxGraphics(Component root)
	{
		foreach (Graphic componentsInChild in root.GetComponentsInChildren<Graphic>(includeInactive: true))
		{
			if (componentsInChild != null && !LetterboxGraphics.Contains(componentsInChild))
			{
				LetterboxGraphics.Add(componentsInChild);
			}
		}
	}

	private static void LetterboxJob()
	{
		bool flag = Plugin.On && Plugin.RemoveLetterbox.Value;
		foreach (Graphic letterboxGraphic in LetterboxGraphics)
		{
			if (letterboxGraphic == null)
			{
				continue;
			}
			int instanceID = letterboxGraphic.GetInstanceID();
			if (flag)
			{
				if (!letterboxGraphic.enabled)
				{
					continue;
				}
				Color color = letterboxGraphic.color;
				if (!(color.r < 0.1f) || !(color.g < 0.1f) || !(color.b < 0.1f) || !(letterboxGraphic.rectTransform.rect.width > 1000f))
				{
					continue;
				}
				letterboxGraphic.enabled = false;
				if (Hidden.Add(instanceID))
				{
					ManualLogSource log = Plugin.Instance.Log;
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(30, 2, out var isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Hid letterbox graphic '");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(letterboxGraphic.gameObject.name);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' size=");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(letterboxGraphic.rectTransform.rect.size);
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
			}
			else if (Hidden.Remove(instanceID))
			{
				letterboxGraphic.enabled = true;
			}
		}
	}

	private static void ScanVideo()
	{
		VideoPlayers.Clear();
		bool flag = Plugin.On && Plugin.FixVideos.Value;
		bool isEnabled;
		foreach (VideoPlayer item in UnityEngine.Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (item == null)
			{
				continue;
			}
			VideoPlayers.Add(item);
			int instanceID = item.GetInstanceID();
			bool flag2 = item.renderMode == VideoRenderMode.CameraFarPlane || item.renderMode == VideoRenderMode.CameraNearPlane;
			int value;
			if (flag)
			{
				if (flag2 && (item.aspectRatio == VideoAspectRatio.FitOutside || item.aspectRatio == VideoAspectRatio.FitHorizontally))
				{
					OrigVideoAspect[instanceID] = (int)item.aspectRatio;
					ManualLogSource log = Plugin.Instance.Log;
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(29, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("VideoPlayer '");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(item.gameObject.name);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("': ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(item.aspectRatio);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" -> FitInside");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
					item.aspectRatio = VideoAspectRatio.FitInside;
				}
			}
			else if (OrigVideoAspect.TryGetValue(instanceID, out value))
			{
				item.aspectRatio = (VideoAspectRatio)value;
				OrigVideoAspect.Remove(instanceID);
			}
		}
		foreach (AspectRatioFitter item2 in UnityEngine.Object.FindObjectsByType<AspectRatioFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (item2 == null)
			{
				continue;
			}
			int instanceID2 = item2.GetInstanceID();
			int value2;
			if (flag)
			{
				if (item2.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent && item2.GetComponent<RawImage>() != null)
				{
					OrigFit[instanceID2] = (int)item2.aspectMode;
					item2.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
					ManualLogSource log2 = Plugin.Instance.Log;
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(48, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Video RawImage '");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(item2.gameObject.name);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("': EnvelopeParent -> FitInParent");
					}
					log2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
			}
			else if (OrigFit.TryGetValue(instanceID2, out value2))
			{
				item2.aspectMode = (AspectRatioFitter.AspectMode)value2;
				OrigFit.Remove(instanceID2);
			}
		}
	}

	private static RawImage FindVideoImage(VideoPlayer vp, Texture tex)
	{
		int instanceID = vp.GetInstanceID();
		if (VideoImages.TryGetValue(instanceID, out var value) && value != null && value.texture != null && value.texture.Pointer == tex.Pointer)
		{
			return value;
		}
		float unscaledTime = Time.unscaledTime;
		if (VideoImageNextSearch.TryGetValue(instanceID, out var value2) && unscaledTime < value2)
		{
			return null;
		}
		VideoImageNextSearch[instanceID] = unscaledTime + 2f;
		foreach (RawImage item in UnityEngine.Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (!(item == null) && !(item.texture == null) && !(item.texture.Pointer != tex.Pointer))
			{
				VideoImages[instanceID] = item;
				return item;
			}
		}
		return null;
	}

	private static void VideoJob()
	{
		bool flag = Plugin.On && Plugin.FixVideos.Value;
		foreach (VideoPlayer videoPlayer in VideoPlayers)
		{
			if (videoPlayer == null)
			{
				continue;
			}
			RenderTexture targetTexture = videoPlayer.targetTexture;
			if (!(targetTexture == null))
			{
				RawImage rawImage = FindVideoImage(videoPlayer, targetTexture);
				if (!(rawImage == null))
				{
					FitVideo(rawImage, targetTexture, flag);
				}
			}
		}
	}

	private static void FitVideo(RawImage ri, Texture tex, bool on)
	{
		int instanceID = ri.GetInstanceID();
		RectTransform rectTransform = ri.rectTransform;
		Canvas canvas = ri.canvas;
		if (canvas == null)
		{
			return;
		}
		Rect rect = canvas.rootCanvas.GetComponent<RectTransform>().rect;
		float width = rect.width;
		float height = rect.height;
		if (width <= 0f || height <= 0f || tex.height <= 0)
		{
			return;
		}
		float num = (float)tex.width / (float)tex.height;
		RiState value;
		if (on && Math.Abs(width / height - num) > 0.01f)
		{
			if (!RiOrig.ContainsKey(instanceID))
			{
				RiOrig[instanceID] = new RiState
				{
					AnchorMin = rectTransform.anchorMin,
					AnchorMax = rectTransform.anchorMax,
					Pivot = rectTransform.pivot,
					SizeDelta = rectTransform.sizeDelta,
					AnchoredPos = rectTransform.anchoredPosition,
					Uv = ri.uvRect
				};
				ManualLogSource log = Plugin.Instance.Log;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(49, 4, out var isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Fitting video image '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(ri.gameObject.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' to ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num, "F3");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" aspect inside canvas ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(width);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("x");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(height);
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
			float num2;
			float num3;
			if (width / height > num)
			{
				num2 = height;
				num3 = height * num;
			}
			else
			{
				num3 = width;
				num2 = width / num;
			}
			Vector2 vector = new Vector2(0.5f, 0.5f);
			Vector2 sizeDelta = new Vector2(num3, num2);
			if (rectTransform.anchorMin != vector)
			{
				rectTransform.anchorMin = vector;
			}
			if (rectTransform.anchorMax != vector)
			{
				rectTransform.anchorMax = vector;
			}
			if (rectTransform.pivot != vector)
			{
				rectTransform.pivot = vector;
			}
			if (rectTransform.anchoredPosition != Vector2.zero)
			{
				rectTransform.anchoredPosition = Vector2.zero;
			}
			Vector2 sizeDelta2 = rectTransform.sizeDelta;
			if (Math.Abs(sizeDelta2.x - num3) > 0.5f || Math.Abs(sizeDelta2.y - num2) > 0.5f)
			{
				rectTransform.sizeDelta = sizeDelta;
			}
			Rect uvRect = ri.uvRect;
			if (uvRect.x != 0f || uvRect.y != 0f || uvRect.width != 1f || uvRect.height != 1f)
			{
				ri.uvRect = new Rect(0f, 0f, 1f, 1f);
			}
		}
		else if (RiOrig.TryGetValue(instanceID, out value))
		{
			rectTransform.anchorMin = value.AnchorMin;
			rectTransform.anchorMax = value.AnchorMax;
			rectTransform.pivot = value.Pivot;
			rectTransform.sizeDelta = value.SizeDelta;
			rectTransform.anchoredPosition = value.AnchoredPos;
			ri.uvRect = value.Uv;
			RiOrig.Remove(instanceID);
		}
	}

	private static void DumpTree(StringBuilder sb, RectTransform rt, int depth, int maxDepth)
	{
		if (rt == null || !rt.gameObject.activeInHierarchy)
		{
			return;
		}
		Graphic component = rt.GetComponent<Graphic>();
		string value = ((component == null) ? "" : $" {component.GetIl2CppType().Name} enabled={component.enabled} color={component.color}");
		CanvasGroup component2 = rt.GetComponent<CanvasGroup>();
		string value2 = ((component2 == null) ? "" : $" group.alpha={component2.alpha}");
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(34, 9, sb);
		handler.AppendFormatted(new string(' ', 4 + depth * 2));
		handler.AppendLiteral("'");
		handler.AppendFormatted(rt.gameObject.name);
		handler.AppendLiteral("' anchors=");
		handler.AppendFormatted(rt.anchorMin);
		handler.AppendLiteral("-");
		handler.AppendFormatted(rt.anchorMax);
		handler.AppendLiteral(" pos=");
		handler.AppendFormatted(rt.anchoredPosition);
		handler.AppendLiteral(" sizeDelta=");
		handler.AppendFormatted(rt.sizeDelta);
		handler.AppendLiteral(" rect=");
		handler.AppendFormatted(rt.rect.size);
		handler.AppendFormatted(value);
		handler.AppendFormatted(value2);
		sb.AppendLine(ref handler);
		if (depth < maxDepth)
		{
			for (int i = 0; i < rt.childCount && i < 30; i++)
			{
				DumpTree(sb, rt.GetChild(i).TryCast<RectTransform>(), depth + 1, maxDepth);
			}
		}
	}

	private static void Dump()
	{
		_dumpSerial++;
		FrameSamples.ResetClock();
		ManualLogSource log = Plugin.Instance.Log;
		StringBuilder stringBuilder = new StringBuilder();
		try
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(105, 9, stringBuilder2);
			handler.AppendLiteral("=== DUMP 0.8.10 === enabled=");
			handler.AppendFormatted(Plugin.On);
			handler.AppendLiteral(" screen ");
			handler.AppendFormatted(Screen.width);
			handler.AppendLiteral("x");
			handler.AppendFormatted(Screen.height);
			handler.AppendLiteral(" aspect ");
			handler.AppendFormatted((float)Screen.width / (float)Screen.height, "F3");
			handler.AppendLiteral(" fullscreen=");
			handler.AppendFormatted(Screen.fullScreenMode);
			handler.AppendLiteral(" ");
			handler.AppendLiteral("cameraLimit=");
			handler.AppendFormatted(_camSpoofed);
			handler.AppendLiteral(" capSetting=");
			handler.AppendFormatted(Plugin.CameraMaxAspect.Value);
			handler.AppendLiteral(" centerMenus=");
			handler.AppendFormatted(Plugin.CenterMenus.Value);
			handler.AppendLiteral(" centerHud=");
			handler.AppendFormatted(Plugin.CenterHud.Value);
			stringBuilder3.AppendLine(ref handler);
			SaveRoomMasks.Dump(stringBuilder);
			LoadingScreenPresentation.Dump(stringBuilder);
			stringBuilder.AppendLine("Cutscene letterbox: enabled=" + Plugin.RemoveLetterbox.Value + " cached=" + LetterboxGraphics.Count);
			foreach (var graphic in LetterboxGraphics)
			{
				if (graphic == null) continue;
				stringBuilder.AppendLine("  letterbox '" + graphic.name + "' parent='" + (graphic.transform.parent != null ? graphic.transform.parent.name : "")
					+ "' active=" + graphic.gameObject.activeInHierarchy + " enabled=" + graphic.enabled + " owned=" + Hidden.Contains(graphic.GetInstanceID())
					+ " size=" + graphic.rectTransform.rect.size + " color=" + graphic.color);
			}
			stringBuilder.AppendLine("Main-menu 16:9 viewport=" + _menuViewportApplied);
			stringBuilder.AppendLine("Main-menu pure-black bars=" + MainMenuBars.Visible);
			stringBuilder.AppendLine("HUD safe area: horizontal=" + Plugin.HudSafeHorizontal.Value + "% vertical=" + Plugin.HudSafeVertical.Value + "%");
			stringBuilder.AppendLine("Dialogue history: hooks=" + DialogueHistory.Available + " enabled=" + Plugin.HistoryEnabled.Value + " lines=" + DialogueHistory.Count + " open=" + DialogueHistory.IsOpen);
			stringBuilder.AppendLine("History voices: recorded=" + DialogueHistory.VoicedCount + " replaying=" + HistoryVoice.Playing);
			GamepadGlyphs.Dump(stringBuilder);
			HistoryView.Dump(stringBuilder);
			FrameSamples.Dump(stringBuilder);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(61, 4, stringBuilder2);
			handler.AppendLiteral("Timing: plugin Update avg=");
			handler.AppendFormatted((_updateRuns > 0) ? (_updateTotalMs / (double)_updateRuns) : 0.0, "F3");
			handler.AppendLiteral("ms max=");
			handler.AppendFormatted(_updateMaxMs, "F3");
			handler.AppendLiteral("ms; ");
			handler.AppendLiteral("projection avg=");
			handler.AppendFormatted((_renderRuns > 0) ? (_renderTotalMs / (double)_renderRuns) : 0.0, "F3");
			handler.AppendLiteral("ms max=");
			handler.AppendFormatted(_renderMaxMs, "F3");
			handler.AppendLiteral("ms");
			stringBuilder4.AppendLine(ref handler);
			foreach (Job job in Jobs)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(38, 5, stringBuilder2);
				handler.AppendLiteral("    job '");
				handler.AppendFormatted(job.Name);
				handler.AppendLiteral("' every ");
				handler.AppendFormatted(job.Interval);
				handler.AppendLiteral("s runs=");
				handler.AppendFormatted(job.Runs);
				handler.AppendLiteral(" avg=");
				handler.AppendFormatted((job.Runs > 0) ? (job.TotalMs / (double)job.Runs) : 0.0, "F3");
				handler.AppendLiteral("ms max=");
				handler.AppendFormatted(job.MaxMs, "F3");
				handler.AppendLiteral("ms");
				stringBuilder5.AppendLine(ref handler);
			}
			foreach (var pair in ActionMaxMs) if (pair.Value >= 1.0) stringBuilder.AppendLine("    feature '" + pair.Key + "' slowest run " + pair.Value.ToString("F1") + "ms");
			ActionMaxMs.Clear();
			_updateMaxMs = 0.0;
			_renderMaxMs = 0.0;
			foreach (Job job2 in Jobs)
			{
				job2.MaxMs = 0.0;
			}
			foreach (ScalerEntry scaler in Scalers)
			{
				if (scaler.Scaler == null || scaler.Root == null)
				{
					continue;
				}
				RectTransform root = scaler.Root;
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(37, 5, stringBuilder2);
				handler.AppendLiteral("Canvas '");
				handler.AppendFormatted(scaler.Name);
				handler.AppendLiteral("' ref=");
				handler.AppendFormatted(scaler.Scaler.referenceResolution);
				handler.AppendLiteral(" match=");
				handler.AppendFormatted(scaler.Scaler.matchWidthOrHeight);
				handler.AppendLiteral(" rect=");
				handler.AppendFormatted(root.rect.size);
				handler.AppendLiteral(" children=");
				handler.AppendFormatted(root.childCount);
				stringBuilder6.AppendLine(ref handler);
				try
				{
					Canvas cv = scaler.Scaler.GetComponent<Canvas>();
					if (cv != null)
					{
						stringBuilder.AppendLine("    renderMode=" + cv.renderMode + " scaleFactor=" + cv.scaleFactor.ToString("F4") + " planeDistance=" + cv.planeDistance + " worldCamera=" + ((cv.worldCamera != null) ? (cv.worldCamera.name + " ortho=" + cv.worldCamera.orthographic + " size=" + cv.worldCamera.orthographicSize.ToString("F2") + " fov=" + cv.worldCamera.fieldOfView + " aspect=" + cv.worldCamera.aspect.ToString("F3") + " rect=" + cv.worldCamera.rect) : "none") + " lossyScale=" + root.lossyScale + " pos=" + root.position);
					}
				}
				catch (Exception)
				{
				}
				for (int i = 0; i < root.childCount; i++)
				{
					RectTransform rectTransform = root.GetChild(i).TryCast<RectTransform>();
					if (!(rectTransform == null))
					{
						if ((scaler.IsMenu || scaler.IsHud) && rectTransform.gameObject.activeInHierarchy)
						{
							DumpTree(stringBuilder, rectTransform, 0, 3);
							continue;
						}
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder7 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(30, 5, stringBuilder2);
						handler.AppendLiteral("    '");
						handler.AppendFormatted(rectTransform.gameObject.name);
						handler.AppendLiteral("' active=");
						handler.AppendFormatted(rectTransform.gameObject.activeSelf);
						handler.AppendLiteral(" anchors=");
						handler.AppendFormatted(rectTransform.anchorMin);
						handler.AppendLiteral("-");
						handler.AppendFormatted(rectTransform.anchorMax);
						handler.AppendLiteral(" size=");
						handler.AppendFormatted(rectTransform.rect.size);
						stringBuilder7.AppendLine(ref handler);
					}
				}
			}
			foreach (CameraScreenSize item in UnityEngine.Object.FindObjectsByType<CameraScreenSize>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (!(item == null))
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder8 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(47, 6, stringBuilder2);
					handler.AppendLiteral("CameraScreenSize '");
					handler.AppendFormatted(item.gameObject.name);
					handler.AppendLiteral("' active=");
					handler.AppendFormatted(item.gameObject.activeInHierarchy);
					handler.AppendLiteral(" type=");
					handler.AppendFormatted(item.screenSizeType);
					handler.AppendLiteral(" ");
					handler.AppendLiteral("h=");
					handler.AppendFormatted(item.horizontalScreenUnits);
					handler.AppendLiteral(" v=");
					handler.AppendFormatted(item.verticalScreenUnits);
					handler.AppendLiteral(" height=");
					handler.AppendFormatted(item.GetCameraScreenHeight());
					stringBuilder8.AppendLine(ref handler);
				}
			}
			foreach (CameraTarget item2 in UnityEngine.Object.FindObjectsByType<CameraTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (!(item2 == null) && item2.gameObject.activeInHierarchy)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder9 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(72, 9, stringBuilder2);
					handler.AppendLiteral("CameraTarget '");
					handler.AppendFormatted(item2.gameObject.name);
					handler.AppendLiteral("' main=");
					handler.AppendFormatted(item2.isMainTarget);
					handler.AppendLiteral(" boss=");
					handler.AppendFormatted(item2.isBossFightDynamicCamera);
					handler.AppendLiteral(" zoomIn=");
					handler.AppendFormatted(item2.maxZoomIn);
					handler.AppendLiteral(" zoomOut=");
					handler.AppendFormatted(item2.maxZoomOut);
					handler.AppendLiteral(" ");
					handler.AppendLiteral("dx=");
					handler.AppendFormatted(item2.distanceXDynamicZoom);
					handler.AppendLiteral(" dy=");
					handler.AppendFormatted(item2.distanceYDynamicZoom);
					handler.AppendLiteral(" customRect=");
					handler.AppendFormatted(item2.customRectConfiner);
					handler.AppendLiteral(" bounds=");
					handler.AppendFormatted(item2.cameraConfinerBounds);
					stringBuilder9.AppendLine(ref handler);
				}
			}
			foreach (CinemachineBrain item3 in UnityEngine.Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None))
			{
				if (!(item3 == null))
				{
					ICinemachineCamera activeVirtualCamera = item3.ActiveVirtualCamera;
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder10 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(35, 2, stringBuilder2);
					handler.AppendLiteral("CinemachineBrain on '");
					handler.AppendFormatted(item3.gameObject.name);
					handler.AppendLiteral("' live camera=");
					handler.AppendFormatted((activeVirtualCamera == null) ? "none" : activeVirtualCamera.Name);
					Camera brainCam = item3.GetComponent<Camera>();
					handler.AppendLiteral(" physical=");
					handler.AppendFormatted(brainCam != null && brainCam.usePhysicalProperties);
					handler.AppendLiteral(" tracked=");
					handler.AppendFormatted(brainCam != null && _cam != null && brainCam.Pointer == _cam.Pointer);
					stringBuilder10.AppendLine(ref handler);
				}
			}
			foreach (RedWineCameraConfiner item4 in UnityEngine.Object.FindObjectsByType<RedWineCameraConfiner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (item4 != null)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder11 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(52, 3, stringBuilder2);
					handler.AppendLiteral("RedWineCameraConfiner on '");
					handler.AppendFormatted(item4.gameObject.name);
					handler.AppendLiteral("' enabled=");
					handler.AppendFormatted(item4.enabled);
					handler.AppendLiteral(" zoomMultiplier=");
					handler.AppendFormatted(item4.zoomMultiplier);
					stringBuilder11.AppendLine(ref handler);
				}
			}
			foreach (Camera allCamera in Camera.allCameras)
			{
				if (!(allCamera == null))
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder12 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(64, 9, stringBuilder2);
					handler.AppendLiteral("Camera '");
					handler.AppendFormatted(allCamera.name);
					handler.AppendLiteral("' ortho=");
					handler.AppendFormatted(allCamera.orthographic);
					handler.AppendLiteral(" fov=");
					handler.AppendFormatted(allCamera.fieldOfView);
					handler.AppendLiteral(" aspect=");
					handler.AppendFormatted(allCamera.aspect, "F3");
					handler.AppendLiteral(" pixels=");
					handler.AppendFormatted(allCamera.pixelWidth);
					handler.AppendLiteral("x");
					handler.AppendFormatted(allCamera.pixelHeight);
					handler.AppendLiteral(" ");
					handler.AppendLiteral("pos=");
					handler.AppendFormatted(allCamera.transform.position);
					handler.AppendLiteral(" rect=");
					handler.AppendFormatted(allCamera.rect);
					handler.AppendLiteral(" targetTexture=");
					handler.AppendFormatted((allCamera.targetTexture == null) ? "none" : (allCamera.targetTexture.width + "x" + allCamera.targetTexture.height));
					stringBuilder12.AppendLine(ref handler);
				}
			}
			DumpLights(stringBuilder);
		}
		catch (Exception ex)
		{
			stringBuilder.AppendLine("dump error: " + ex);
		}
		log.LogInfo(stringBuilder.ToString());
		try
		{
			string path = System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "BelmontsCurseUltrawide_dump.txt");
			System.IO.File.WriteAllText(path, stringBuilder.ToString());
			log.LogInfo("Dump also written to " + path);
		}
		catch (Exception ex2)
		{
			log.LogWarning("Could not write dump file: " + ex2.Message);
		}
	}

	// Lights and light-like renderers around the gameplay camera, with where they land on screen
	// (vx 0..1 across the full width; the 16:9 logic area is the middle part).
	private static void DumpLights(StringBuilder sb)
	{
		try
		{
			UnityEngine.Rendering.RenderPipelineAsset rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
			UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp = (rp == null) ? null : rp.TryCast<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>();
			if (urp != null)
			{
				sb.AppendLine("URP '" + urp.name + "' additionalLights=" + urp.additionalLightsRenderingMode + " perObjectLimit=" + urp.maxAdditionalLightsCount + " shadows=" + urp.supportsAdditionalLightShadows);
			}
			else
			{
				sb.AppendLine("Render pipeline: " + ((rp == null) ? "built-in" : rp.name));
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine("URP info error: " + ex.Message);
		}
		Camera cam = _cam;
		if (cam == null)
		{
			return;
		}
		float half = (float)cam.pixelWidth / (float)Math.Max(1, cam.pixelHeight) / 1.7777778f;
		float lo = 0.5f - 0.5f / half;
		float hi = 0.5f + 0.5f / half;
		sb.AppendLine("Lights (16:9 area is vx " + lo.ToString("F2", CultureInfo.InvariantCulture) + ".." + hi.ToString("F2", CultureInfo.InvariantCulture) + "):");
		System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<float, string>> rows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<float, string>>();
		foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (light == null)
			{
				continue;
			}
			Vector3 v = cam.WorldToViewportPoint(light.transform.position);
			float reach = light.range / Math.Max(0.01f, cam.orthographic ? cam.orthographicSize * 2f : (2f * Math.Abs(v.z) * (float)Math.Tan(cam.fieldOfView * Math.PI / 360.0)));
			if (v.x < -reach || v.x > 1f + reach || v.y < -reach || v.y > 1f + reach)
			{
				continue;
			}
			Transform parent = light.transform.parent;
			rows.Add(new System.Collections.Generic.KeyValuePair<float, string>(v.x, "  light '" + light.name + "' parent='" + ((parent == null) ? "" : parent.name) + "' active=" + light.isActiveAndEnabled + " type=" + light.type + " vx=" + v.x.ToString("F2", CultureInfo.InvariantCulture) + " vy=" + v.y.ToString("F2", CultureInfo.InvariantCulture) + " range=" + light.range.ToString("F1", CultureInfo.InvariantCulture) + " intensity=" + light.intensity.ToString("F2", CultureInfo.InvariantCulture) + " color=" + light.color + " spot=" + light.spotAngle.ToString("F0", CultureInfo.InvariantCulture) + " mode=" + light.renderMode + " mask=" + light.cullingMask));
		}
		foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
		{
			if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
			{
				continue;
			}
			string n = r.gameObject.name;
			string lower = n.ToLowerInvariant();
			string mat = (r.sharedMaterial == null) ? "" : r.sharedMaterial.name;
			string lowerMat = mat.ToLowerInvariant();
			if (!(lower.Contains("black") || lowerMat.Contains("black") || lower.Contains("light") || lower.Contains("vignet") || lower.Contains("spot") || lower.Contains("dark") || lower.Contains("fog") || lower.Contains("glow") || lower.Contains("shaft") || lowerMat.Contains("light") || lowerMat.Contains("vignet") || lowerMat.Contains("dark") || lowerMat.Contains("fog") || lowerMat.Contains("glow")))
			{
				continue;
			}
			Bounds b = r.bounds;
			Vector3 a = cam.WorldToViewportPoint(b.min);
			Vector3 c = cam.WorldToViewportPoint(b.max);
			float x0 = Math.Min(a.x, c.x);
			float x1 = Math.Max(a.x, c.x);
			float y0 = Math.Min(a.y, c.y);
			float y1 = Math.Max(a.y, c.y);
			if (x1 < 0f || x0 > 1f || y1 < 0f || y0 > 1f)
			{
				continue;
			}
			Transform parent2 = r.transform.parent;
			rows.Add(new System.Collections.Generic.KeyValuePair<float, string>((x0 + x1) * 0.5f, "  renderer '" + n + "' parent='" + ((parent2 == null) ? "" : parent2.name) + "' " + r.GetIl2CppType().Name + " mat='" + mat + "' vx=" + x0.ToString("F2", CultureInfo.InvariantCulture) + ".." + x1.ToString("F2", CultureInfo.InvariantCulture) + " vy=" + y0.ToString("F2", CultureInfo.InvariantCulture) + ".." + y1.ToString("F2", CultureInfo.InvariantCulture) + " sorting=" + r.sortingLayerName + "/" + r.sortingOrder));
		}
		rows.Sort((x, y) => x.Key.CompareTo(y.Key));
		foreach (System.Collections.Generic.KeyValuePair<float, string> row in rows)
		{
			sb.AppendLine(row.Value);
		}
		sb.AppendLine("  (" + rows.Count + " entries)");
		DumpProbe(sb, cam, 0.06f, 0.47f);
		DumpProbe(sb, cam, 0.12f, 0.47f);
		DumpProbe(sb, cam, 0.12f, 0.20f);
		DumpProbe(sb, cam, 0.50f, 0.47f);
		DumpConcealment(sb);
	}

	private static void DumpConcealment(StringBuilder sb)
	{
		// The game's darkening of what lies outside the current room (a circle sized for 16:9).
		sb.AppendLine("Concealment:");
		foreach (ConcealmentController c in UnityEngine.Object.FindObjectsByType<ConcealmentController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (c == null)
			{
				continue;
			}
			sb.AppendLine("  ConcealmentController '" + c.gameObject.name + "' active=" + c.isActiveAndEnabled + " enable=" + c.enable + " screenR=" + c.screenSpaceRadius + " worldR=" + c.worldSpaceRadius + " currentR=" + c.currentRadius + " targetR=" + c.targetRadius + " roundness=" + c.roundness + " intensity=" + c.intensity + " noise=" + c.noiseScale + "/" + c.noiseStrength + " gamepathZ=" + c.gamepathZPosition + " switchToWS=" + ConcealmentController.currentSwitchScreenToWS + "->" + ConcealmentController.targetSwitchScreenToWS);
		}
		try
		{
			sb.AppendLine("  globals: center=" + Shader.GetGlobalVector(ConcealmentController._ConcealmentCenter) + " switchScreenToMC=" + Shader.GetGlobalFloat(ConcealmentController._ConcealmentCenterSwitchScreenToMC) + " screenR=" + Shader.GetGlobalFloat(ConcealmentController._ConcealmentScreenSpaceRadius) + " worldR=" + Shader.GetGlobalFloat(ConcealmentController._ConcealmentWorldSpaceRadius) + " roundness=" + Shader.GetGlobalFloat(ConcealmentController._ConcealmentRoundness) + " fov=" + Shader.GetGlobalFloat(ConcealmentController._ConcealmentCameraFOV) + " gamepathZ=" + Shader.GetGlobalFloat(ConcealmentController._ConcealmentGamepathZPosition) + " gamepath=" + Shader.GetGlobalFloat(ConcealmentController._ConcealmentGamepath) + " intensity=" + Shader.GetGlobalFloat(ConcealmentController._ConcealmentIntensity) + " mcPos=" + Shader.GetGlobalVector(McConcealmentSystem._Concealment_MCPosition));
		}
		catch (Exception ex)
		{
			sb.AppendLine("  globals error: " + ex.Message);
		}
	}

	// Every visible renderer whose screen rectangle covers one point, any name, nearest first.
	private static void DumpProbe(StringBuilder sb, Camera cam, float px, float py)
	{
		sb.AppendLine("Probe vx=" + px.ToString("F2", CultureInfo.InvariantCulture) + " vy=" + py.ToString("F2", CultureInfo.InvariantCulture) + ":");
		System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<float, string>> rows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<float, string>>();
		Vector3[] corners = new Vector3[8];
		foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
		{
			if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || !r.isVisible)
			{
				continue;
			}
			Bounds b = r.bounds;
			Vector3 mn = b.min;
			Vector3 mx = b.max;
			corners[0] = new Vector3(mn.x, mn.y, mn.z);
			corners[1] = new Vector3(mx.x, mn.y, mn.z);
			corners[2] = new Vector3(mn.x, mx.y, mn.z);
			corners[3] = new Vector3(mx.x, mx.y, mn.z);
			corners[4] = new Vector3(mn.x, mn.y, mx.z);
			corners[5] = new Vector3(mx.x, mn.y, mx.z);
			corners[6] = new Vector3(mn.x, mx.y, mx.z);
			corners[7] = new Vector3(mx.x, mx.y, mx.z);
			float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue, z = float.MaxValue;
			for (int i = 0; i < 8; i++)
			{
				Vector3 v = cam.WorldToViewportPoint(corners[i]);
				x0 = Math.Min(x0, v.x);
				x1 = Math.Max(x1, v.x);
				y0 = Math.Min(y0, v.y);
				y1 = Math.Max(y1, v.y);
				z = Math.Min(z, v.z);
			}
			if (px < x0 || px > x1 || py < y0 || py > y1 || z <= 0f)
			{
				continue;
			}
			Material m = r.sharedMaterial;
			Transform p = r.transform.parent;
			Transform gp = (p == null) ? null : p.parent;
			rows.Add(new System.Collections.Generic.KeyValuePair<float, string>(z, "  z=" + z.ToString("F1", CultureInfo.InvariantCulture) + " '" + r.gameObject.name + "' in '" + ((p == null) ? "" : p.name) + "' < '" + ((gp == null) ? "" : gp.name) + "' " + r.GetIl2CppType().Name + " mat='" + ((m == null) ? "" : m.name) + "' shader='" + ((m == null || m.shader == null) ? "" : m.shader.name) + "' layer=" + LayerMask.LayerToName(r.gameObject.layer) + " sorting=" + r.sortingLayerName + "/" + r.sortingOrder + " vx=" + x0.ToString("F2", CultureInfo.InvariantCulture) + ".." + x1.ToString("F2", CultureInfo.InvariantCulture) + " vy=" + y0.ToString("F2", CultureInfo.InvariantCulture) + ".." + y1.ToString("F2", CultureInfo.InvariantCulture)));
		}
		rows.Sort((a, c) => a.Key.CompareTo(c.Key));
		int n = 0;
		foreach (System.Collections.Generic.KeyValuePair<float, string> row in rows)
		{
			if (++n > 60)
			{
				sb.AppendLine("  ... " + (rows.Count - 60) + " more");
				break;
			}
			sb.AppendLine(row.Value);
		}
		sb.AppendLine("  (" + rows.Count + " renderers)");
	}
}
