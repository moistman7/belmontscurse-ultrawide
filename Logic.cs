using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BelmontsCurseUltrawide;

// Pure helpers with no game types, so they can be unit-tested (tests/Logic.Tests links this file).
internal static class Logic
{
	// Centered 16:9 presentation; narrower and invalid screen sizes need no side bars.
	internal static float PresentationWidth(int width, int height)
		=> width > 0 && height > 0 ? Math.Min(1f, (16f / 9f) * height / width) : 1f;

	// One step of a slider: moves by `direction` steps on a grid that starts at `min`, never leaving [min, max].
	internal static float Step(float value, int direction, float min, float max, float step)
	{
		if (step <= 0) return Math.Clamp(value, min, max);
		float next = value + direction * step;
		float snapped = min + (float)Math.Round((next - min) / step) * step;
		return Math.Clamp((float)Math.Round(snapped, 4), min, max);
	}

	// "1.25" for a step of 0.05, "3" for a whole step, so a value shows only the digits it can have.
	internal static string FormatValue(float value, float step)
	{
		int decimals = 0;
		for (float s = step; decimals < 4 && Math.Abs(s - Math.Round(s)) > 0.0001f; s *= 10) decimals++;
		return value.ToString("F" + decimals, System.Globalization.CultureInfo.InvariantCulture);
	}

	// Cheap size estimate (no text layout pass): the largest size whose estimated line count fits the card body.
	internal static float FitSize(string text, float width, float height, float max = 32, float min = 16)
	{
		int breaks = 0;
		foreach (char c in text) if (c == '\n') breaks++;
		for (float size = max; size > min; size -= 2)
		{
			float perLine = Math.Max(1, width / (size * 0.5f));
			int lines = (int)Math.Ceiling(text.Length / perLine) + breaks;
			if (lines * size * 1.2f <= height) return size;
		}
		return min;
	}

	// Game text carries <br> and rich-text tags; history keeps plain text with real line breaks.
	internal static string CleanText(string raw)
	{
		if (raw == null) return "";
		return Regex.Replace(Regex.Replace(raw, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase), "<[^>]*>", "").Trim();
	}

	internal static string StripTags(string raw) => raw == null ? "" : Regex.Replace(raw, "<[^>]*>", "").Trim();

	// True when something other than the mod moved a stored panel since the mod last placed it,
	// so the stored "original" position is stale and the current one should become the new original.
	internal static bool Drifted(float minX, float maxX, float minY, float maxY, float appliedMinX, float appliedMaxX, float appliedMinY, float appliedMaxY, float tolerance = 0.0005f)
		=> Math.Abs(minX - appliedMinX) > tolerance || Math.Abs(maxX - appliedMaxX) > tolerance ||
		   Math.Abs(minY - appliedMinY) > tolerance || Math.Abs(maxY - appliedMaxY) > tolerance;

	// Saved history: one line per entry, "speaker<TAB>text", with tab, newline and backslash escaped.
	internal static string EncodeEntry(string speaker, string text) => Escape(speaker ?? "") + "\t" + Escape(text ?? "");

	internal static bool TryDecodeEntry(string line, out string speaker, out string text)
	{
		speaker = text = "";
		if (string.IsNullOrEmpty(line)) return false;
		int tab = line.IndexOf('\t');
		if (tab < 0) return false;
		speaker = Unescape(line.Substring(0, tab));
		text = Unescape(line.Substring(tab + 1));
		return text.Length > 0;
	}

	private static string Escape(string s)
	{
		var sb = new StringBuilder(s.Length + 8);
		foreach (char c in s)
		{
			if (c == '\\') sb.Append("\\\\");
			else if (c == '\t') sb.Append("\\t");
			else if (c == '\n') sb.Append("\\n");
			else if (c != '\r') sb.Append(c);
		}
		return sb.ToString();
	}

	private static string Unescape(string s)
	{
		var sb = new StringBuilder(s.Length);
		for (int i = 0; i < s.Length; i++)
		{
			if (s[i] != '\\' || i == s.Length - 1) { sb.Append(s[i]); continue; }
			char next = s[++i];
			sb.Append(next == 't' ? '\t' : next == 'n' ? '\n' : next);
		}
		return sb.ToString();
	}
}
