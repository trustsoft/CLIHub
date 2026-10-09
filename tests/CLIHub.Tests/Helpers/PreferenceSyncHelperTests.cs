namespace CLIHub.Tests.Helpers;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Helpers;

using Moq;

using Xunit;

public sealed class PreferenceSyncHelperTests
{
    [Fact]
    public void SyncPreference_NullStore_ThrowsArgumentNullException()
    {
        // Arrange
        IPreferencesStore? store = null;
        Action<AppPreferences> update = p => { };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PreferenceSyncHelper.SyncPreference(store!, update));
    }

    [Fact]
    public void SyncPreference_NullUpdateAction_ThrowsArgumentNullException()
    {
        // Arrange
        var mockStore = new Mock<IPreferencesStore>();
        Action<AppPreferences>? update = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PreferenceSyncHelper.SyncPreference(mockStore.Object, update!));
    }

    [Fact]
    public void SyncPreference_ValidArguments_CallsUpdate()
    {
        // Arrange
        var mockStore = new Mock<IPreferencesStore>();
        var updateCalled = false;
        Action<AppPreferences> update = p => { updateCalled = true; };
        
        // Setup mock to actually invoke the callback
        mockStore.Setup(s => s.Update(It.IsAny<Action<AppPreferences>>()))
            .Callback<Action<AppPreferences>>(action => action(new AppPreferences()));

        // Act
        PreferenceSyncHelper.SyncPreference(mockStore.Object, update);

        // Assert
        mockStore.Verify(s => s.Update(It.IsAny<Action<AppPreferences>>()), Times.Once);
        Assert.True(updateCalled);
    }

    [Fact]
    public void SyncPreference_WithCallback_InvokesCallback()
    {
        // Arrange
        var mockStore = new Mock<IPreferencesStore>();
        Action<AppPreferences> update = p => { };
        var callbackInvoked = false;
        Action callback = () => { callbackInvoked = true; };

        // Act
        PreferenceSyncHelper.SyncPreference(mockStore.Object, update, callback);

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void SyncPreference_WithoutCallback_DoesNotThrow()
    {
        // Arrange
        var mockStore = new Mock<IPreferencesStore>();
        Action<AppPreferences> update = p => { };

        // Act & Assert (no exception expected)
        var exception = Record.Exception(() =>
            PreferenceSyncHelper.SyncPreference(mockStore.Object, update, onChanged: null));

        Assert.Null(exception);
    }

    [Fact]
    public void SyncPreference_CallbackThrows_PropagatesException()
    {
        // Arrange
        var mockStore = new Mock<IPreferencesStore>();
        Action<AppPreferences> update = p => { };
        Action callback = () => throw new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PreferenceSyncHelper.SyncPreference(mockStore.Object, update, callback));

        Assert.Equal("Test exception", exception.Message);
    }
}
