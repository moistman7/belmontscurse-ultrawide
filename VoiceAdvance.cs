using System;
using UnityEngine;

namespace BelmontsCurseUltrawide;

// Advances voiced dialogue lines once the voice clip has finished and the text is fully shown.
internal static class VoiceAdvance
{
	private static bool _voiceWasActive;

	private static bool _waiting;

	private static float _readyAt;

	internal static void Tick()
	{
		if (!Plugin.AutoAdvance.Value)
		{
			_voiceWasActive = false;
			_waiting = false;
			return;
		}
		bool voiceActive = AnyVoiceActive();
		if (_voiceWasActive && !voiceActive)
		{
			_waiting = true;
			_readyAt = 0f;
		}
		else if (voiceActive)
		{
			_waiting = false;
		}
		_voiceWasActive = voiceActive;
		if (!_waiting)
		{
			return;
		}
		CutsceneLineReader reader = FindActiveReader();
		if (reader == null)
		{
			// The line was already dismissed (or the voice was not part of a dialogue box).
			_waiting = false;
			return;
		}
		if (reader._isReading)
		{
			_readyAt = 0f;
			return;
		}
		float now = Time.unscaledTime;
		if (_readyAt == 0f)
		{
			_readyAt = now + Math.Max(0f, Plugin.AutoAdvanceDelay.Value);
			return;
		}
		if (now < _readyAt)
		{
			return;
		}
		_waiting = false;
		Plugin.Instance.Log.LogInfo("Auto-advance: voice finished, ending line on " + reader.gameObject.name);
		reader.End();
	}

	private static bool AnyVoiceActive()
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
