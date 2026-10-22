namespace CLIHub.Tests.Services;

using System.Windows;

using CLIHub.Settings;

/// <summary>
///   Covers the WPF accent preference target without an application object. The test process
///   intentionally creates no <see cref="Application" /> (asserted by
///   <c> ResourceKeyToTemplateConverterTests </c>), so only the null-guard path is reachable here; the
///   applied brushes need a real application and are covered by
///   <see cref="CLIHub.Tests.Settings.AccentApplierTests" /> plus manual smoke.
/// </summary>
public sealed class WpfAccentPreferenceTargetTests
{
    [Fact]
    public void SetAccent_NoApplicationInstance_DoesNotThrow()
    {
        Assert.Null(Application.Current);

        var exception = Record.Exception(() => new WpfAccentPreferenceTarget().SetAccent("#3B82F6"));

        Assert.Null(exception);
    }
}
