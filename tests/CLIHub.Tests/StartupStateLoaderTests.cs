namespace CLIHub.Tests;

using Moq;

using CLIHub;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

public sealed class StartupStateLoaderTests
{
    [Fact]
    public void Load_InitializesPluginsLoadsAndAppliesPreferences()
    {
        // Arrange
        var sequence = new MockSequence();
        var pluginInitialization = new Mock<IPluginInitializationService>(MockBehavior.Strict);
        var preferencesStore = new Mock<IPreferencesStore>(MockBehavior.Strict);
        var startupPreferences = new Mock<IStartupPreferencesApplier>(MockBehavior.Strict);

        var preferences = new AppPreferences
        {
            ShowWindowOnStartup = true,
            CheckForUpdatesOnStartup = false
        };

        pluginInitialization.InSequence(sequence).Setup(x => x.Initialize());
        preferencesStore.InSequence(sequence).Setup(x => x.Load()).Returns(preferences);
        startupPreferences.InSequence(sequence).Setup(x => x.Apply(preferences));

        var loader = new StartupStateLoader(
            pluginInitialization.Object,
            preferencesStore.Object,
            startupPreferences.Object);

        // Act
        var result = loader.Load();

        // Assert
        Assert.NotNull(result);
        Assert.Same(preferences, result.Preferences);
        pluginInitialization.Verify(x => x.Initialize(), Times.Once);
        preferencesStore.Verify(x => x.Load(), Times.Once);
        startupPreferences.Verify(x => x.Apply(preferences), Times.Once);
    }

    [Fact]
    public void Load_ReturnsImmutableStartupState()
    {
        // Arrange
        var pluginInitialization = new Mock<IPluginInitializationService>();
        var preferencesStore = new Mock<IPreferencesStore>();
        var startupPreferences = new Mock<IStartupPreferencesApplier>();

        var preferences = new AppPreferences();

        pluginInitialization.Setup(x => x.Initialize());
        preferencesStore.Setup(x => x.Load()).Returns(preferences);
        startupPreferences.Setup(x => x.Apply(preferences));

        var loader = new StartupStateLoader(
            pluginInitialization.Object,
            preferencesStore.Object,
            startupPreferences.Object);

        // Act
        var result = loader.Load();

        // Assert
        Assert.IsType<StartupState>(result);
        Assert.Equal(preferences, result.Preferences);
    }

    [Fact]
    public void Constructor_WithNullPluginInitialization_ThrowsArgumentNullException()
    {
        // Arrange
        var preferencesStore = new Mock<IPreferencesStore>();
        var startupPreferences = new Mock<IStartupPreferencesApplier>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new StartupStateLoader(null!, preferencesStore.Object, startupPreferences.Object));

        Assert.Equal("pluginInitialization", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullPreferencesStore_ThrowsArgumentNullException()
    {
        // Arrange
        var pluginInitialization = new Mock<IPluginInitializationService>();
        var startupPreferences = new Mock<IStartupPreferencesApplier>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new StartupStateLoader(pluginInitialization.Object, null!, startupPreferences.Object));

        Assert.Equal("preferencesStore", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullStartupPreferences_ThrowsArgumentNullException()
    {
        // Arrange
        var pluginInitialization = new Mock<IPluginInitializationService>();
        var preferencesStore = new Mock<IPreferencesStore>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new StartupStateLoader(pluginInitialization.Object, preferencesStore.Object, null!));

        Assert.Equal("startupPreferences", ex.ParamName);
    }
}
