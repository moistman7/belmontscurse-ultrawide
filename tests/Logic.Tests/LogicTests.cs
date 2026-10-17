using BelmontsCurseUltrawide;

public class LogicTests
{
	[Theory]
	[InlineData(1.25f, 1, 1.0f, 1.6f, 0.05f, 1.3f)]
	[InlineData(1.25f, -1, 1.0f, 1.6f, 0.05f, 1.2f)]
	[InlineData(1.6f, 1, 1.0f, 1.6f, 0.05f, 1.6f)]
	[InlineData(1.0f, -1, 1.0f, 1.6f, 0.05f, 1.0f)]
	[InlineData(3f, 1, 0.5f, 10f, 0.5f, 3.5f)]
	public void Step_moves_on_the_grid_and_clamps(float value, int dir, float min, float max, float step, float expected)
		=> Assert.Equal(expected, Logic.Step(value, dir, min, max, step), 3);

	[Fact]
	public void Step_snaps_an_off_grid_value()
		=> Assert.Equal(1.3f, Logic.Step(1.27f, 1, 1.0f, 1.6f, 0.05f), 3);

	[Theory]
	[InlineData(1.25f, 0.05f, "1.25")]
	[InlineData(3f, 0.5f, "3.0")]
	[InlineData(3f, 1f, "3")]
	[InlineData(0.4f, 0.1f, "0.4")]
	public void FormatValue_shows_only_needed_digits(float value, float step, string expected)
		=> Assert.Equal(expected, Logic.FormatValue(value, step));

	[Fact]
	public void FitSize_keeps_short_text_at_full_size()
		=> Assert.Equal(32f, Logic.FitSize("Hello.", 800, 110));

	[Fact]
	public void FitSize_shrinks_long_text_and_stops_at_the_minimum()
	{
		string longText = new string('x', 400);
		float size = Logic.FitSize(longText, 800, 110);
		Assert.True(size < 32f);
		Assert.True(size >= 16f);
		Assert.Equal(16f, Logic.FitSize(new string('x', 20000), 800, 110));
	}

	[Fact]
	public void FitSize_counts_line_breaks()
		=> Assert.True(Logic.FitSize("a\nb\nc\nd\ne", 800, 110) < 32f);

	[Theory]
	[InlineData("Hello<br>world", "Hello\nworld")]
	[InlineData("<b>Bold</b> text", "Bold text")]
	[InlineData("  padded  ", "padded")]
	[InlineData("a<BR/>b", "a\nb")]
	[InlineData(null, "")]
	public void CleanText_strips_tags_and_converts_breaks(string? raw, string expected)
		=> Assert.Equal(expected, Logic.CleanText(raw!));

	[Fact]
	public void Drifted_ignores_tiny_float_noise_and_notices_real_moves()
	{
		Assert.False(Logic.Drifted(0.1f, 0.9f, 0f, 1f, 0.1001f, 0.9f, 0f, 1f));
		Assert.True(Logic.Drifted(0.1f, 0.9f, 0f, 1f, 0.2f, 0.9f, 0f, 1f));
		Assert.True(Logic.Drifted(0.1f, 0.9f, 0f, 1f, 0.1f, 0.9f, 0.1f, 1f));
	}

	[Theory]
	[InlineData("Trevor", "We should go.")]
	[InlineData("", "No speaker line")]
	[InlineData("Rose", "Two\nlines with\ttab and \\ backslash and trailing \\")]
	public void Entry_survives_a_save_and_load(string speaker, string text)
	{
		string line = Logic.EncodeEntry(speaker, text);
		Assert.DoesNotContain("\n", line);
		Assert.True(Logic.TryDecodeEntry(line, out var s, out var t));
		Assert.Equal(speaker, s);
		Assert.Equal(text, t);
	}

	[Theory]
	[InlineData("")]
	[InlineData("no tab here")]
	[InlineData("Speaker\t")]
	public void Bad_save_lines_are_rejected(string line)
		=> Assert.False(Logic.TryDecodeEntry(line, out _, out _));
}
