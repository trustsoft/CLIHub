namespace CLIHub.Tests.Services;

using System.Windows;

using Microsoft.Extensions.Logging;

using Moq;

using CLIHub.Core.Models;
using CLIHub.Settings;
using CLIHub.Themes;

/// <summary>
///   Covers the WPF theme preference target without an application object. The test process
///   intentionally creates no <see cref="Application" /> (asserted by
///   <c> ResourceKeyToTemplateConverterTests </c>), so only the null-guard path is reachable here; the
///   applied and failed palette logging needs a real application and is covered by manual smoke.
/// </summary>
public sealed class WpfThemePreferenceTargetTests
{
    [Fact]
    public void SetTheme_NoApplicationInstance_DoesNotLoadAPaletteOrLog()
    {
        Assert.Null(Application.Current);
        var logger = new Mock<ILogger<WpfThemePreferenceTarget>>();
        var switcher = new PaletteSwitcher(
            _ => throw new InvalidOperationException("No palette may be loaded without an application."));

        new WpfThemePreferenceTarget(switcher, logger.Object).SetTheme(AppTheme.Light);

        logger.VerifyNoOtherCalls();
    }
}
