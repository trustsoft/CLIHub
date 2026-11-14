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
        var stateRepository = new InMemoryPluginRegistry();
        var versionProvider = new Mock<IUpdateVersionProvider>();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("new-plugin", "New Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null),
                new("current-plugin", "Current Plugin", "Desc", "2.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg2.zip", "def456", null, null, ">=1.0.0", null, null, null),
                new("update-plugin", "Update Plugin", "Desc", "3.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg3.zip", "ghi789", null, null, ">=1.0.0", null, null, null),
                new("downgrade-plugin", "Downgrade Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg4.zip", "jkl012", null, null, ">=1.0.0", null, null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        var doc = stateRepository.Document;
        doc.Plugins["current-plugin"] = new PluginRegistryEntry { InstalledVersion = "2.0.0" };
        doc.Plugins["update-plugin"] = new PluginRegistryEntry { InstalledVersion = "1.5.0" };
        doc.Plugins["downgrade-plugin"] = new PluginRegistryEntry { InstalledVersion = "2.0.0" };

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
        var stateRepository = new InMemoryPluginRegistry();
        var versionProvider = new Mock<IUpdateVersionProvider>();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("unknown-version-plugin", "Unknown Version Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        var doc = stateRepository.Document;
        doc.Plugins["unknown-version-plugin"] = new PluginRegistryEntry { InstalledVersion = null };

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
                new("opencode", "Built-in OpenCode", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null),
                new("custom-plugin", "Custom", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg2.zip", "def456", null, null, ">=1.0.0", null, null, null)
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
                new("future-plugin", "Future Plugin", "Desc", "1.0.0", 1, "99.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=99.0.0", null, null, null)
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
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
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
        var stateRepository = new InMemoryPluginRegistry();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
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
        feedClient.Verify(x => x.GetFeedAsync(It.IsAny<CancellationToken>()), Times.Once);
        feedClient.Verify(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        installer.Verify(x => x.Inspect(It.IsAny<string>()), Times.Once);
        installer.Verify(x => x.Install(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        Assert.Single(dialogService.ConfirmationPreviews);
        Assert.Single(dialogService.Outcomes);
        Assert.Equal(PluginUpdateStatus.Current, item.Status);
        Assert.Equal("1.0.0", item.InstalledVersion);
        Assert.False(item.ShowActionButton);
    }

    [Fact]
    public async Task InstallCommand_UpdateAvailable_InstallsWithReplaceExistingTrue()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService { ConfirmInstallation = true };
        var stateRepository = new InMemoryPluginRegistry();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("update-plugin", "Update Plugin", "Desc", "2.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginDownloadResult(true, "temp.zip", []));

        var doc = stateRepository.Document;
        doc.Plugins["update-plugin"] = new PluginRegistryEntry { InstalledVersion = "1.0.0" };

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
    public async Task InstallCommand_UpdateAvailable_Success_MarksCardCurrentWithoutRefetchingFeed()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService { ConfirmInstallation = true };
        var stateRepository = new InMemoryPluginRegistry();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("update-plugin", "Update Plugin", "Desc", "2.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginDownloadResult(true, "temp.zip", []));

        var doc = stateRepository.Document;
        doc.Plugins["update-plugin"] = new PluginRegistryEntry { InstalledVersion = "1.0.0" };

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
        feedClient.Verify(x => x.GetFeedAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(PluginUpdateStatus.Current, item.Status);
        Assert.Equal("2.0.0", item.InstalledVersion);
        Assert.False(item.IsDowngrade);
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
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
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
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
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
                new("plugin1", "Plugin 1", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
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

    [Fact]
    public async Task LoadAsync_V2DescriptorOnlyEntry_CanInstallFromRangeAndShowsBlockedRange()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var versionProvider = new Mock<IUpdateVersionProvider>();

        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 2,
            LastUpdated: null,
            Plugins: [
                new("future-v2", "Future V2", "Desc", "1.0.0", 1, null, "Pub", "https://example.com", null, null, null, null, ">=2.0.0", "stable", """{"id":"future-v2","name":"Future V2","commands":{"launch":{"executable":"fv2"}}}""", null)
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        versionProvider.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");

        var vm = CreateViewModel(feedClient.Object, versionProvider: versionProvider.Object);

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.Single(vm.Plugins);
        Assert.True(vm.Plugins[0].UsesDescriptorInstall);
        Assert.False(vm.Plugins[0].CanInstall);
        Assert.Equal("Requires CLIHub >=2.0.0. Update CLIHub to install this plugin.", vm.Plugins[0].BlockedReason);
    }

    [Fact]
    public async Task InstallCommand_DescriptorOnly_InspectsConfirmsInstallsWithoutPackageDownload()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService { ConfirmInstallation = true };
        var stateRepository = new InMemoryPluginRegistry();

        const string descriptorJson = """{"id":"desc-plugin","name":"Desc Plugin","version":"2.0.0","commands":{"launch":{"executable":"dp"}}}""";
        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 2,
            LastUpdated: null,
            Plugins: [
                new("desc-plugin", "Desc Plugin", "Desc", "2.0.0", 1, null, "Pub", "https://example.com", null, null, null, null, ">=1.0.0", "stable", descriptorJson, "https://example.com/logo.png")
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadLogoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginLogoDownloadResult(true, [1, 2, 3], []));

        var preview = new PluginInstallPreview(true, "desc-plugin", "Desc Plugin", "2.0.0", "Pub", "https://example.com", [], false, null, []);
        installer.Setup(x => x.InspectDescriptor(It.IsAny<PluginDescriptorInstallSource>())).Returns(preview);
        installer.Setup(x => x.InstallDescriptor(It.IsAny<PluginDescriptorInstallSource>(), It.IsAny<bool>()))
            .Returns(PluginInstallResult.Succeeded("desc-plugin"));

        var vm = CreateViewModel(feedClient.Object, installer.Object, dialogService, stateRepository);
        await vm.LoadAsync();

        // Act
        var item = vm.Plugins[0];
        vm.InstallCommand.Execute(item);

        // Assert
        feedClient.Verify(x => x.GetFeedAsync(It.IsAny<CancellationToken>()), Times.Once);
        feedClient.Verify(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        feedClient.Verify(x => x.DownloadLogoAsync("https://example.com/logo.png", It.IsAny<CancellationToken>()), Times.Once);
        installer.Verify(x => x.InspectDescriptor(It.IsAny<PluginDescriptorInstallSource>()), Times.Once);
        installer.Verify(x => x.InstallDescriptor(It.IsAny<PluginDescriptorInstallSource>(), It.IsAny<bool>()), Times.Once);
        installer.Verify(x => x.Install(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        Assert.Single(dialogService.ConfirmationPreviews);
        Assert.Single(dialogService.Outcomes);
        Assert.Equal(PluginUpdateStatus.Current, item.Status);
        Assert.Equal("2.0.0", item.InstalledVersion);
        Assert.False(item.ShowActionButton);
    }

    [Fact]
    public async Task InstallCommand_DescriptorOnly_LogoDownloadFailure_InstallsWithoutLogo()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService { ConfirmInstallation = true };

        const string descriptorJson = """{"id":"desc-plugin","name":"Desc Plugin","version":"2.0.0","commands":{"launch":{"executable":"dp"}}}""";
        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 2,
            LastUpdated: null,
            Plugins: [
                new("desc-plugin", "Desc Plugin", "Desc", "2.0.0", 1, null, "Pub", "https://example.com", null, null, null, null, ">=1.0.0", "stable", descriptorJson, "https://example.com/logo.png")
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        feedClient.Setup(x => x.DownloadLogoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginLogoDownloadResult(false, null, ["Logo download failed"]));

        var preview = new PluginInstallPreview(true, "desc-plugin", "Desc Plugin", "2.0.0", "Pub", "https://example.com", [], false, null, []);
        installer.Setup(x => x.InspectDescriptor(It.IsAny<PluginDescriptorInstallSource>())).Returns(preview);
        installer.Setup(x => x.InstallDescriptor(It.IsAny<PluginDescriptorInstallSource>(), It.IsAny<bool>()))
            .Returns(PluginInstallResult.Succeeded("desc-plugin"));

        var vm = CreateViewModel(feedClient.Object, installer.Object, dialogService);
        await vm.LoadAsync();

        // Act
        var item = vm.Plugins[0];
        vm.InstallCommand.Execute(item);

        // Assert: the failed logo fetch is tolerated and the install proceeds without logo bytes.
        feedClient.Verify(x => x.DownloadLogoAsync("https://example.com/logo.png", It.IsAny<CancellationToken>()), Times.Once);
        installer.Verify(x => x.InstallDescriptor(It.Is<PluginDescriptorInstallSource>(s => s.LogoBytes == null), It.IsAny<bool>()), Times.Once);
        Assert.Single(dialogService.Outcomes);
        Assert.Equal(PluginUpdateStatus.Current, item.Status);
    }

    [Fact]
    public async Task InstallCommand_DescriptorOnly_UserCancels_NoInstall()
    {
        // Arrange
        var feedClient = new Mock<IPluginFeedClient>();
        var installer = new Mock<IPluginInstaller>();
        var dialogService = new FakePluginDialogService { ConfirmInstallation = false };

        const string descriptorJson = """{"id":"desc-plugin","name":"Desc Plugin","version":"2.0.0","commands":{"launch":{"executable":"dp"}}}""";
        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 2,
            LastUpdated: null,
            Plugins: [
                new("desc-plugin", "Desc Plugin", "Desc", "2.0.0", 1, null, "Pub", "https://example.com", null, null, null, null, ">=1.0.0", "stable", descriptorJson, "https://example.com/logo.png")
            ]);

        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));

        var preview = new PluginInstallPreview(true, "desc-plugin", "Desc Plugin", "2.0.0", "Pub", "https://example.com", [], false, null, []);
        installer.Setup(x => x.InspectDescriptor(It.IsAny<PluginDescriptorInstallSource>())).Returns(preview);

        var vm = CreateViewModel(feedClient.Object, installer.Object, dialogService);
        await vm.LoadAsync();

        // Act
        var item = vm.Plugins[0];
        vm.InstallCommand.Execute(item);

        // Assert: no install and no logo fetch — the logo is fetched only after confirmation.
        installer.Verify(x => x.InstallDescriptor(It.IsAny<PluginDescriptorInstallSource>(), It.IsAny<bool>()), Times.Never);
        feedClient.Verify(x => x.DownloadLogoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(dialogService.ConfirmationPreviews);
        Assert.Empty(dialogService.Outcomes);
    }

    [Fact]
    public async Task LoadAsync_WhenTheFeedComesFromTheCache_SetsTheStaleCacheMessage()
    {
        var feedClient = new Mock<IPluginFeedClient>();
        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("new-plugin", "New Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
            ]);
        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, [])
            {
                FromCache = true,
                CachedAt = DateTimeOffset.Now.AddHours(-2).AddMinutes(-5)
            });
        var vm = CreateViewModel(feedClient.Object);

        await vm.LoadAsync();

        Assert.True(vm.HasCacheStatusMessage);
        Assert.Equal(
            "Using cached plugin list (last updated 2 hours ago). Check your internet connection to refresh.",
            vm.CacheStatusMessage);
        Assert.Single(vm.Plugins);
    }

    [Fact]
    public async Task LoadAsync_WhenTheFeedIsFresh_DoesNotSetTheStaleCacheMessage()
    {
        var feedClient = new Mock<IPluginFeedClient>();
        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("new-plugin", "New Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
            ]);
        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        var vm = CreateViewModel(feedClient.Object);

        await vm.LoadAsync();

        Assert.False(vm.HasCacheStatusMessage);
        Assert.Null(vm.CacheStatusMessage);
        Assert.Single(vm.Plugins);
    }

    [Fact]
    public async Task LoadAsync_WhenTheFeedFails_ShowsTheExistingErrorWithoutACacheMessage()
    {
        var feedClient = new Mock<IPluginFeedClient>();
        feedClient.Setup(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(false, null, ["Could not load plugin feed. Check your network connection."]));
        var vm = CreateViewModel(feedClient.Object);

        await vm.LoadAsync();

        // The hard-error path keeps its existing string; the stale-cache note is cache-only.
        Assert.True(vm.HasError);
        Assert.Equal("Could not load plugin feed. Check your network connection.", vm.ErrorMessage);
        Assert.False(vm.HasCacheStatusMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenARefreshReturnsAFreshFeed_ClearsTheStaleCacheMessage()
    {
        var feedClient = new Mock<IPluginFeedClient>();
        var feedIndex = new PluginFeedIndex(
            SchemaVersion: 1,
            LastUpdated: null,
            Plugins: [
                new("new-plugin", "New Plugin", "Desc", "1.0.0", 1, "1.0.0", "Pub", "https://example.com", "https://example.com/pkg.zip", "abc123", null, null, ">=1.0.0", null, null, null)
            ]);
        feedClient.SetupSequence(x => x.GetFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, [])
            {
                FromCache = true,
                CachedAt = DateTimeOffset.Now.AddMinutes(-30)
            })
            .ReturnsAsync(new PluginFeedResult(true, feedIndex, []));
        var vm = CreateViewModel(feedClient.Object);

        await vm.LoadAsync();
        Assert.True(vm.HasCacheStatusMessage);

        await vm.LoadAsync();

        Assert.False(vm.HasCacheStatusMessage);
        Assert.Null(vm.CacheStatusMessage);
    }

    [Theory]
    [InlineData(20, "20 minutes ago")]
    [InlineData(60, "1 hour ago")]
    [InlineData(150, "2 hours ago")]
    [InlineData(2880, "2 days ago")]
    public void FormatCacheAge_ForARecentCache_ReturnsAReadableAge(int minutesAgo, string expected)
    {
        var now = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

        var text = BrowsePluginsDialogViewModel.FormatCacheAge(now.AddMinutes(-minutesAgo), now);

        Assert.Equal(expected, text);
    }

    [Fact]
    public void FormatCacheAge_ForACacheOlderThanAWeek_ReturnsTheLocalTimestamp()
    {
        var now = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
        var cachedAt = now.AddDays(-10);

        var text = BrowsePluginsDialogViewModel.FormatCacheAge(cachedAt, now);

        Assert.DoesNotContain("ago", text, StringComparison.Ordinal);
        Assert.Contains(cachedAt.ToLocalTime().Day.ToString(), text, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatCacheAge_WhenTheTimestampIsUnknown_ReturnsRecently()
    {
        var text = BrowsePluginsDialogViewModel.FormatCacheAge(null, DateTimeOffset.Now);

        Assert.Equal("recently", text);
    }

    private static BrowsePluginsDialogViewModel CreateViewModel(
        IPluginFeedClient? feedClient = null,
        IPluginInstaller? installer = null,
        IPluginDialogService? dialogService = null,
        IPluginRegistry? stateRepository = null,
        IUpdateVersionProvider? versionProvider = null)
    {
        return new BrowsePluginsDialogViewModel(
            feedClient ?? Mock.Of<IPluginFeedClient>(),
            installer ?? Mock.Of<IPluginInstaller>(),
            stateRepository ?? new InMemoryPluginRegistry(),
            dialogService ?? new FakePluginDialogService(),
            versionProvider ?? Mock.Of<IUpdateVersionProvider>(v => v.GetCurrentVersion() == "1.0.0"),
            new InlineOperationLifetime(),
            NullLogger<BrowsePluginsDialogViewModel>.Instance);
    }
}
