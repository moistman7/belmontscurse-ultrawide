using System;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BelmontsCurseUltrawide;

internal static class HistoryVoice
{
	private static VoiceOverPlayer _player;
	private static DialogueHistory.Entry _entry;
	private static int _scene;
	private static float _startedAt;
	internal static bool Playing => _player != null;

	internal static void Toggle(DialogueHistory.Entry entry)
	{
		if (ReferenceEquals(entry, _entry) && _player != null) { Stop(); return; }
		Stop();
		if (entry == null || !entry.HasVoice) { Toast.Show("No voice recorded for this line"); return; }
		if (VoiceAdvance.AnyVoiceActive()) { Toast.Show("Wait for the current voice line to finish"); return; }
		try
		{
			if (VoiceOverManager._dialogueCallback == null) { Toast.Show("Voice playback is not ready"); return; }
			// An independent player uses the game's localized FMOD callback without entering its live dialogue pool.
			_player = new VoiceOverPlayer();
			_player.Play(VoiceOverManager._dialogueCallback, entry.Character, entry.Voice);
			if (!_player._dialogueInstance.isValid()) { Stop(); Toast.Show("Voice line is unavailable"); return; }
			_entry = entry;
			_scene = SceneManager.GetActiveScene().handle;
			_startedAt = Time.unscaledTime;
			Plugin.Instance.Log.LogInfo("History voice replay: " + entry.Voice._id + " (" + entry.Speaker + ")");
		}
		catch (Exception ex)
		{
			Stop();
			Plugin.Instance.Log.LogWarning("History voice replay: " + ex.Message);
			Toast.Show("Voice line could not be replayed");
		}
	}

	internal static void Tick()
	{
		if (_player == null) return;
		try
		{
			if (SceneManager.GetActiveScene().handle != _scene || VoiceAdvance.AnyVoiceActive() ||
				(Time.unscaledTime - _startedAt > 0.25f && !_player.IsActive())) Stop();
		}
		catch (Exception ex) { Stop(); Plugin.Instance.Log.LogWarning("History voice playback ended: " + ex.Message); }
	}

	internal static void Stop()
	{
		var player = _player;
		_player = null; _entry = null;
		if (player == null) return;
		try
		{
			var instance = player._dialogueInstance;
			if (instance.isValid())
			{
				instance.stop(STOP_MODE.IMMEDIATE);
				instance.release();
			}
			player._dialogueInstance = default;
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("Stopping history voice replay: " + ex.Message); }
	}
}
