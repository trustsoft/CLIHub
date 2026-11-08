namespace CLIHub.Tests.ViewModels.Agents;

using CLIHub.Core.Plugins;
using CLIHub.Core.Updates;
using CLIHub.Launching;
using CLIHub.Tests.Support;
using CLIHub.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using static CLIHub.Tests.Support.TestHelpers;

public class BrowsePluginsDialogViewModelTests
{
    [Fact]
    public async Task LoadAsync_FeedSuccessful_PopulatesPluginsWithCorrectStatuses()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var stateRepository = new InMemoryPluginStateRepository();
        var versionProvider = new Mock<IUpdateVersionProvider>();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("new-plugin", "New Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null),
                new("current-plugin", "Current Plugin", "Desc", "2.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg2.zip", "def456", null, null),
                new("update-plugin", "Update Plugin", "Desc", "3.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg3.zip", "ghi789", null, null),
                new("downgrade-plugin", "Downgrade Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg4.zip", "jkl012", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        var doc = stateRepository.Document;
        doc.Plugins["current-plugin"] = new PluginStateEntry { InstalledVersion = "2.0.0" };
        doc.Plugins["update-plugin"] = new PluginStateEntry { InstalledVersion = "1.5.0" };
        doc.Plugins["downgrade-plugin"] = new PluginStateEntry { InstalledVersion = "2.0.0" };

        versionProvider.Setup(x => x.GetCurrentVersion()).Returns("2.0.0");

        var vm = CreateViewModel(feedClient.Object, stateRepository: stateRepository, versionProvider: versionProvider.Object);

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.Equal(4, vm.Plugins.Count);
        Assert.Contains(vm.Plugins, p => p.Id == "new-plugin" && p.Status == PluginUpdateStatus.Available);
        Assert.Contains(vm.Plugins, p => p.Id == "current-plugin" && p.Status == PluginUpdateStatus.Current);
        Assert.Contains(vm.Plugins, p => p.Id == "update-plugin" && p.Status == PluginUpdateStatus.UpdateAvailable);
        Assert.Contains(vm.Plugins, p => p.Id == "downgrade-plugin" && p.Status == PluginUpdateStatus.DifferentVersionAvailable);
    }

    [Fact]
    public async Task LoadAsync_InstalledWithNullVersion_ShowsUpdateAvailable()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var stateRepository = new InMemoryPluginStateRepository();
        var versionProvider = new Mock<IUpdateVersionProvider>();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("unknown-version-plugin", "Unknown Version Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        var doc = stateRepository.Document;
        doc.Plugins["unknown-version-plugin"] = new PluginStateEntry { InstalledVersion = null };

        versionProvider.Setup(x => x.GetCurrentVersion()).Returns("2.0.0");

        var vm = CreateViewModel(feedClient.Object, stateRepository: stateRepository, versionProvider: versionProvider.Object);

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.Single(vm.Plugins);
        Assert.Equal("unknown-version-plugin", vm.Plugins[0].Id);
        Assert.Equal(PluginUpdateStatus.UpdateAvailable, vm.Plugins[0].Status);
    }

    [Fact]
    public async Task LoadAsync_ReservedBuiltInId_FiltersOut()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("opencode", "Built-in OpenCode", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null),
                new("custom-plugin", "Custom", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg2.zip", "def456", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        var vm = CreateViewModel(feedClient.Object);

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.Single(vm.Plugins);
        Assert.Equal("custom-plugin", vm.Plugins[0].Id);
        Assert.DoesNotContain(vm.Plugins, p => p.Id == "opencode");
    }

    [Fact]
    public async Task LoadAsync_MinAppVersionUnsatisfied_CanInstallFalse()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var versionProvider = new Mock<IUpdateVersionProvider>();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("future-plugin", "Future Plugin", "Desc", "1.0.0", 1, "99.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        versionProvider.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");

        var vm = CreateViewModel(feedClient.Object, versionProvider: versionProvider.Object);

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.Single(vm.Plugins);
        Assert.False(vm.Plugins[0].CanInstall);
        Assert.Equal("Requires CLIHub 99.0.0 or newer. Update CLIHub to install this plugin.", vm.Plugins[0].BlockedReason);
    }

    [Fact]
    public async Task LoadAsync_FeedLoadFailure_ShowsError()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(false, null, ["Network error"]));

        var vm = CreateViewModel(feedClient.Object);

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.True(vm.HasError);
        Assert.Equal("Network error", vm.ErrorMessage);
        Assert.Empty(vm.Plugins);
        Assert.False(vm.HasPlugins);
    }

    [Fact]
    public async Task LoadAsync_EmptyFeed_ShowsEmptyState()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var feedIndex = new PluginFeedIndex(1, null, []);
        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        var vm = CreateViewModel(feedClient.Object);

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.False(vm.HasError);
        Assert.Empty(vm.Plugins);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public async Task RefreshCommand_Executed_RefetchesFeed()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        var vm = CreateViewModel(feedClient.Object);
        await vm.LoadAsync();

        // Act
        vm.RefreshCommand.Execute(null);

        // Assert
        feedClient.Verify(x => x.GetFeedAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task InstallCommand_Success_DownloadsInspectsConfirmsInstalls()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService { ConfirmInstallation = true };
        var stateRepository = new InMemoryPluginStateRepository();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginDownloadResult(true, "temp.zip", []));

        var preview = new PluginInstallPreview(true, "plugin1", "Plugin 1", "1.0.0", "Pub", "https://example.com", [], false, null, []);
        installer.Setup(x => x.Inspect(It.IsAny<string>())).Returns(preview);
        installer.Setup(x => x.Install(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(PluginInstallResult.Succeeded("plugin1"));

        var vm = CreateViewModel(feedClient.Object, installer.Object, dialogService, stateRepository);
        await vm.LoadAsync();

        // Act
        var item = vm.Plugins[0];
        vm.InstallCommand.Execute(item);

        // Assert
        feedClient.Verify(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        installer.Verify(x => x.Inspect(It.IsAny<string>()), Times.Once);
        installer.Verify(x => x.Install(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        Assert.Single(dialogService.ConfirmationPreviews);
        Assert.Single(dialogService.Outcomes);
    }

    [Fact]
    public async Task InstallCommand_UpdateAvailable_InstallsWithReplaceExistingTrue()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService { ConfirmInstallation = true };
        var stateRepository = new InMemoryPluginStateRepository();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("update-plugin", "Update Plugin", "Desc", "2.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginDownloadResult(true, "temp.zip", []));

        var doc = stateRepository.Document;
        doc.Plugins["update-plugin"] = new PluginStateEntry { InstalledVersion = "1.0.0" };

        var preview = new PluginInstallPreview(true, "update-plugin", "Update Plugin", "2.0.0", "Pub", "https://example.com", [], false, null, []);
        installer.Setup(x => x.Inspect(It.IsAny<string>())).Returns(preview);
        installer.Setup(x => x.Install(It.IsAny<string>(), true)).Returns(PluginInstallResult.Succeeded("update-plugin"));

        var vm = CreateViewModel(feedClient.Object, installer.Object, dialogService, stateRepository);
        await vm.LoadAsync();

        // Act
        var item = vm.Plugins.Single(p => p.Id == "update-plugin");
        Assert.Equal(PluginUpdateStatus.UpdateAvailable, item.Status);
        vm.InstallCommand.Execute(item);

        // Assert
        installer.Verify(x => x.Install(It.IsAny<string>(), true), Times.Once);
        installer.Verify(x => x.Install(It.IsAny<string>(), false), Times.Never);
    }

    [Fact]
    public async Task InstallCommand_UserCancelsConfirmation_NoInstall()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService { ConfirmInstallation = false };

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginDownloadResult(true, "temp.zip", []));

        var preview = new PluginInstallPreview(true, "plugin1", "Plugin 1", "1.0.0", "Pub", "https://example.com", [], false, null, []);
        installer.Setup(x => x.Inspect(It.IsAny<string>())).Returns(preview);

        var vm = CreateViewModel(feedClient.Object, installer.Object, dialogService);
        await vm.LoadAsync();

        // Act
        var item = vm.Plugins[0];
        vm.InstallCommand.Execute(item);

        // Assert
        installer.Verify(x => x.Install(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        Assert.Single(dialogService.ConfirmationPreviews);
        Assert.Empty(dialogService.Outcomes);
    }

    [Fact]
    public async Task InstallCommand_DownloadFailure_ShowsError()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginDownloadResult(false, null, ["Download failed"]));

        var vm = CreateViewModel(feedClient.Object, installer.Object, dialogService);
        await vm.LoadAsync();

        // Act
        var item = vm.Plugins[0];
        vm.InstallCommand.Execute(item);

        // Assert
        installer.Verify(x => x.Inspect(It.IsAny<string>()), Times.Never);
        installer.Verify(x => x.Install(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        Assert.Single(dialogService.ValidationErrorsShown);
        Assert.Contains("Download failed", dialogService.ValidationErrorsShown[0]);
    }

    [Fact]
    public async Task InstallCommand_ChecksumFailure_ShowsError()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginDownloadResult(false, null, ["Checksum verification failed"]));

        var vm = CreateViewModel(feedClient.Object, installer.Object, dialogService);
        await vm.LoadAsync();

        // Act
        var item = vm.Plugins[0];
        vm.InstallCommand.Execute(item);

        // Assert
        installer.Verify(x => x.Inspect(It.IsAny<string>()), Times.Never);
        Assert.Single(dialogService.ValidationErrorsShown);
        Assert.Contains("Checksum verification failed", dialogService.ValidationErrorsShown[0]);
    }

    private static BrowsePluginsDialogViewModel CreateViewModel(
        IPluginFeedClient? feedClient = null,
        IPluginInstaller? installer = null,
        IPluginDialogService? dialogService = null,
        IPluginStateRepository? stateRepository = null,
        IUpdateVersionProvider? versionProvider = null)
    {
        return new BrowsePluginsDialogViewModel(
            feedClient ?? Mock.Of<IPluginFeedClient>(),
            installer ?? Mock.Of<IPluginInstaller>(),
            stateRepository ?? new InMemoryPluginStateRepository(),
            dialogService ?? new FakePluginDialogService(),
            versionProvider ?? Mock.Of<IUpdateVersionProvider>(v => v.GetCurrentVersion() == "1.0.0"),
            new InlineOperationLifetime(),
            NullLogger<BrowsePluginsDialogViewModel>.Instance);
    }
}
