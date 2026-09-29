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
    [InlineData("Ctrl+A+B")]   // two keys
    [InlineData("Alt+F25")]    // out of F-key range
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_Invalid_ReturnsFalse(string? input)
    {
        Assert.False(HotkeyParser.TryParse(input, out var definition));
        Assert.Null(definition);
    }

    [Fact]
    public void TryParse_Space_ReturnsSpaceVirtualKey()
    {
        Assert.True(HotkeyParser.TryParse("Ctrl+Alt+Space", out var definition));
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, definition!.Modifiers);
        Assert.Equal(0x20, definition.VirtualKey);
    }

    [Fact]
    public void Format_SpaceKey_IsNamed()
    {
        var definition = new HotkeyDefinition(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x20);

        Assert.Equal("Ctrl+Alt+Space", HotkeyParser.Format(definition));
    }

    [Theory]
    [InlineData("Enter", 0x0D)]
    [InlineData("Tab", 0x09)]
    [InlineData("Escape", 0x1B)]
    [InlineData("Backspace", 0x08)]
    [InlineData("Delete", 0x2E)]
    [InlineData("Insert", 0x2D)]
    [InlineData("Home", 0x24)]
    [InlineData("End", 0x23)]
    [InlineData("PageUp", 0x21)]
    [InlineData("PageDown", 0x22)]
    [InlineData("Up", 0x26)]
    [InlineData("Down", 0x28)]
    [InlineData("Left", 0x25)]
    [InlineData("Right", 0x27)]
    [InlineData("Space", 0x20)]
    public void TryParse_NamedKey_ReturnsExpectedVirtualKey(string key, int expectedVk)
    {
        Assert.True(HotkeyParser.TryParse($"Ctrl+{key}", out var definition));
        Assert.Equal(HotkeyModifiers.Control, definition!.Modifiers);
        Assert.Equal(expectedVk, definition.VirtualKey);
    }

    [Theory]
    [InlineData("cTrL+eNtEr", 0x0D)]
    [InlineData("CTRL+ESCAPE", 0x1B)]
    [InlineData("alt+PAGEUP", 0x21)]
    public void TryParse_NamedKey_IsCaseInsensitive(string input, int expectedVk)
    {
        Assert.True(HotkeyParser.TryParse(input, out var definition));
        Assert.Equal(expectedVk, definition!.VirtualKey);
    }

    [Theory]
    [InlineData("Return", 0x0D)]
    [InlineData("Esc", 0x1B)]
    [InlineData("Back", 0x08)]
    [InlineData("Del", 0x2E)]
    [InlineData("Ins", 0x2D)]
    [InlineData("PgUp", 0x21)]
    [InlineData("PgDn", 0x22)]
    public void TryParse_Alias_ResolvesToCanonicalVirtualKey(string alias, int expectedVk)
    {
        Assert.True(HotkeyParser.TryParse($"Ctrl+{alias}", out var definition));
        Assert.Equal(expectedVk, definition!.VirtualKey);
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData("Tab")]
    [InlineData("Escape")]
    [InlineData("Backspace")]
    [InlineData("Delete")]
    [InlineData("Insert")]
    [InlineData("Home")]
    [InlineData("End")]
    [InlineData("PageUp")]
    [InlineData("PageDown")]
    [InlineData("Up")]
    [InlineData("Down")]
    [InlineData("Left")]
    [InlineData("Right")]
    public void Format_NamedKey_UsesCanonicalName(string key)
    {
        Assert.True(HotkeyParser.TryParse($"Ctrl+{key}", out var definition));

        Assert.Equal($"Ctrl+{key}", HotkeyParser.Format(definition!));
    }

    [Theory]
    [InlineData("Return", "Enter")]
    [InlineData("Esc", "Escape")]
    [InlineData("Back", "Backspace")]
    [InlineData("Del", "Delete")]
    [InlineData("Ins", "Insert")]
    [InlineData("PgUp", "PageUp")]
    [InlineData("PgDn", "PageDown")]
    public void Format_AliasInput_UsesCanonicalName(string alias, string canonical)
    {
        Assert.True(HotkeyParser.TryParse($"Ctrl+{alias}", out var definition));

        var formatted = HotkeyParser.Format(definition!);

        Assert.Equal($"Ctrl+{canonical}", formatted);
        Assert.True(HotkeyParser.TryParse(formatted, out var reparsed));
        Assert.Equal(definition, reparsed);
    }

    [Theory]
    [InlineData("Ctrl+Shift+A")]
    [InlineData("Alt+F4")]
    [InlineData("Ctrl+1")]
    [InlineData("Win+D")]
    [InlineData("Ctrl+Alt+F12")]
    [InlineData("Ctrl+Alt+Space")]
    [InlineData("Ctrl+Enter")]
    [InlineData("Alt+Tab")]
    [InlineData("Ctrl+Left")]
    [InlineData("Ctrl+PageUp")]
    [InlineData("Alt+Backspace")]
    [InlineData("Ctrl+Delete")]
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
