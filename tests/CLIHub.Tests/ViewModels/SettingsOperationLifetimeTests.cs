namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Updates;
using CLIHub.ViewModels;

public class SettingsOperationLifetimeTests
{
    [Fact]
    public void CheckForUpdatesCommand_WhenApplicationCancels_ReportsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var updates = new Mock<IUpdateService>();
        updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");
        updates
            .Setup(x => x.CheckForUpdatesAsync(cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var lifetime = new Mock<IApplicationOperationLifetime>(MockBehavior.Strict);
        lifetime
            .Setup(x => x.RunAsync("Settings update check", It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((string _, Func<CancellationToken, Task> operation) => operation(cancellation.Token));

        var viewModel = new SettingsViewModel(
            new SettingsApplicationService(
                new Mock<IPreferencesStore>().Object,
                new Mock<IStartupService>().Object,
                new Mock<IPreferenceApplier>().Object,
                NullLogger<SettingsApplicationService>.Instance),
            updates.Object,
            lifetime.Object,
            NullLogger<SettingsViewModel>.Instance);

        viewModel.CheckForUpdatesCommand.Execute(null);

        Assert.Equal("Update check cancelled.", viewModel.UpdateMessage);
    }
}
