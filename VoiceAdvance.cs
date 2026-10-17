using System;
using UnityEngine;

namespace BelmontsCurseUltrawide;

// Advances dialogue lines automatically. Voiced lines advance once the voice clip has finished and the
// text is fully shown. Unvoiced lines (optional) advance after a reading-time delay.
internal static class VoiceAdvance
{
	private static CutsceneLineReader _reader;

	private static float _nextSearch;

	private static bool _lineOpen;

	private static bool _wasReading;

	private static bool _sawVoice;

	private static bool _handled;

	private static float _readyAt;

	// Called when the game starts a line, so the reader that is actually active is used at once (left/right boxes alternate).
	internal static void Track(CutsceneLineReader reader)
	{
		if (reader != null) { _reader = reader; _nextSearch = 0f; }
	}

	internal static void Tick()
	{
		if (DialogueHistory.IsOpen || ModMenu.IsOpen)
		{
			_readyAt = 0f;
			return;
		}
		bool voiced = Plugin.AutoAdvance.Value;
		bool unvoiced = Plugin.AutoAdvanceUnvoiced.Value;
		if (!voiced && !unvoiced)
		{
			Reset();
			return;
		}
		float now = Time.unscaledTime;
		if ((_reader == null || !_reader.isActiveAndEnabled || !_reader._isActive) && now >= _nextSearch)
		{
			_nextSearch = now + 0.1f;
			_reader = FindActiveReader();
		}
		if (_reader == null || !_reader.isActiveAndEnabled || !_reader._isActive)
		{
			Reset();
			return;
		}
		bool voiceActive = AnyVoiceActive();
		if (_reader._isReading)
		{
			// Text is still appearing: this is (the start of) a new line.
			if (!_wasReading)
			{
				_handled = false;
				_readyAt = 0f;
				_sawVoice = false;
			}
			_wasReading = true;
			_lineOpen = true;
			_sawVoice |= voiceActive;
			return;
		}
		_wasReading = false;
		if (!_lineOpen)
		{
			// Picked up mid-line: judge it from what we see from now on.
			_lineOpen = true;
			_sawVoice = voiceActive;
		}
		_sawVoice |= voiceActive;
		if (_handled)
		{
			return;
		}
		if (voiceActive)
		{
			_readyAt = 0f;
			return;
		}
		float delay;
		if (_sawVoice)
		{
			if (!voiced)
			{
				return;
			}
			delay = Math.Max(0f, Plugin.AutoAdvanceDelay.Value);
		}
		else
		{
			if (!unvoiced)
			{
				return;
			}
			delay = Math.Max(Plugin.AutoAdvanceDelay.Value, Plugin.UnvoicedBaseSeconds.Value + Math.Max(0, _reader._nbMaxVisibleCharacters) * Plugin.UnvoicedSecondsPerCharacter.Value);
		}
		if (_readyAt == 0f)
		{
			_readyAt = now + delay;
			return;
		}
		if (now < _readyAt)
		{
			return;
		}
		_handled = true;
		Plugin.Instance.Log.LogInfo("Auto-advance: ending " + (_sawVoice ? "voiced" : "unvoiced") + " line on " + _reader.gameObject.name);
		_reader.End();
	}

	private static void Reset()
	{
		_lineOpen = false;
		_wasReading = false;
		_sawVoice = false;
		_handled = false;
		_readyAt = 0f;
	}

	internal static bool AnyVoiceActive()
	{
		var players = VoiceOverManager._voiceOverPlayers;
		if (players == null)
		{
			return false;
		}
		for (int i = 0; i < players.Length; i++)
		{
			VoiceOverPlayer player = players[i];
			if (player != null && player.IsActive())
			{
				return true;
			}
		}
		return false;
	}

	private static CutsceneLineReader FindActiveReader()
	{
		var readers = UnityEngine.Object.FindObjectsOfType<CutsceneLineReader>();
		for (int i = 0; i < readers.Length; i++)
		{
			CutsceneLineReader reader = readers[i];
			if (reader != null && reader._isActive && reader.isActiveAndEnabled)
			{
				return reader;
			}
		}
		return null;
	}
}
