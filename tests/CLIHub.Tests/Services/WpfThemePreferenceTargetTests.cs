namespace CLIHub.Tests.Services;

using System.Windows;

using CLIHub.Core.Models;
using CLIHub.Settings;
using CLIHub.Themes;

/// <summary>
///   Covers the WPF theme preference target without an application object. The test process
///   intentionally creates no <see cref="Application" /> (asserted by
///   <c> ResourceKeyToTemplateConverterTests </c>), so only the null-guard path is reachable here; the
///   applied and kept-previous-palette reporting needs a real application and is covered by manual
///   smoke.
/// </summary>
public sealed class WpfThemePreferenceTargetTests
{
    [Fact]
    public void SetTheme_NoApplicationInstance_ReturnsFalseAndDoesNotLoadAPalette()
    {
        Assert.Null(Application.Current);
        var paletteLoads = 0;
        var switcher = new PaletteSwitcher(
            _ =>
            {
                paletteLoads++;
                throw new InvalidOperationException("No palette may be loaded without an application.");
            });
        var target = new WpfThemePreferenceTarget(switcher);

        var applied = target.SetTheme(AppTheme.Light);

        Assert.False(applied);
        Assert.Equal(0, paletteLoads);
    }
}
