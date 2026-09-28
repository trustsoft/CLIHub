namespace CLIHub.Tests.Hotkeys;

using CLIHub.Core.Hotkeys;

public class HotkeyParserTests
{
    [Fact]
    public void ModifierValues_MatchWin32Constants()
    {
        Assert.Equal(1, (int)HotkeyModifiers.Alt);
        Assert.Equal(2, (int)HotkeyModifiers.Control);
        Assert.Equal(4, (int)HotkeyModifiers.Shift);
        Assert.Equal(8, (int)HotkeyModifiers.Win);
    }

    [Fact]
    public void TryParse_CtrlShiftA_ReturnsControlShiftAndVk()
    {
        var ok = HotkeyParser.TryParse("Ctrl+Shift+A", out var definition);

        Assert.True(ok);
        Assert.NotNull(definition);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, definition!.Modifiers);
        Assert.Equal(0x41, definition.VirtualKey);
    }

    [Theory]
    [InlineData("ctrl+shift+a")]
    [InlineData("Shift+Ctrl+A")]
    [InlineData("  Ctrl + Shift + A  ")]
    public void TryParse_IsCaseInsensitiveAndOrderIndependent(string input)
    {
        var ok = HotkeyParser.TryParse(input, out var definition);

        Assert.True(ok);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, definition!.Modifiers);
        Assert.Equal(0x41, definition.VirtualKey);
    }

    [Fact]
    public void TryParse_AltF4_ReturnsAltAndVk()
    {
        Assert.True(HotkeyParser.TryParse("Alt+F4", out var definition));
        Assert.Equal(HotkeyModifiers.Alt, definition!.Modifiers);
        Assert.Equal(0x73, definition.VirtualKey);
    }

    [Fact]
    public void TryParse_WinD_ReturnsWinAndVk()
    {
        Assert.True(HotkeyParser.TryParse("Win+D", out var definition));
        Assert.Equal(HotkeyModifiers.Win, definition!.Modifiers);
        Assert.Equal(0x44, definition.VirtualKey);
    }

    [Fact]
    public void TryParse_DigitKey_ReturnsDigitVk()
    {
        Assert.True(HotkeyParser.TryParse("Ctrl+1", out var definition));
        Assert.Equal(0x31, definition!.VirtualKey);
    }

    [Theory]
    [InlineData("Ctrl+F1", 0x70)]
    [InlineData("Ctrl+F12", 0x7B)]
    [InlineData("Alt+F24", 0x87)]
    public void TryParse_FunctionKeys(string input, int expectedVk)
    {
        Assert.True(HotkeyParser.TryParse(input, out var definition));
        Assert.Equal(expectedVk, definition!.VirtualKey);
    }

    [Theory]
    [InlineData("A")]          // no modifier
    [InlineData("Ctrl")]       // no key
    [InlineData("Ctrl+Foo")]   // unknown key
    [InlineData("Ctrl+Shift")] // no key
    [InlineData("Win+Space")]  // unsupported key
    [InlineData("Ctrl+A+B")]   // two keys
    [InlineData("Alt+F25")]    // out of F-key range
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_Invalid_ReturnsFalse(string? input)
    {
        Assert.False(HotkeyParser.TryParse(input, out var definition));
        Assert.Null(definition);
    }

    [Theory]
    [InlineData("Ctrl+Shift+A")]
    [InlineData("Alt+F4")]
    [InlineData("Ctrl+1")]
    [InlineData("Win+D")]
    [InlineData("Ctrl+Alt+F12")]
    public void Format_RoundTripsThroughTryParse(string input)
    {
        Assert.True(HotkeyParser.TryParse(input, out var definition));

        var formatted = HotkeyParser.Format(definition!);

        Assert.True(HotkeyParser.TryParse(formatted, out var reparsed));
        Assert.Equal(definition, reparsed);
    }

    [Fact]
    public void Format_Default_IsCanonical()
    {
        Assert.Equal("Ctrl+Shift+A", HotkeyParser.Format(HotkeyParser.Default));
    }

    [Fact]
    public void ParseOrDefault_InvalidInput_ReturnsFallback()
    {
        var result = HotkeyParser.ParseOrDefault("nonsense", HotkeyParser.Default);
        Assert.Equal(HotkeyParser.Default, result);
    }

    [Fact]
    public void ParseOrDefault_ValidInput_ReturnsParsed()
    {
        var result = HotkeyParser.ParseOrDefault("Alt+F1", HotkeyParser.Default);
        Assert.Equal(0x70, result.VirtualKey);
        Assert.Equal(HotkeyModifiers.Alt, result.Modifiers);
    }
}
