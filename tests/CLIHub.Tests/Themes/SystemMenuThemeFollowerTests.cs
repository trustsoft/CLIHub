namespace CLIHub.Tests.Themes;

using System.Windows;

using CLIHub.Theming;

using Microsoft.Win32;

/// <summary>
///   Covers the decision logic of <see cref="SystemMenuThemeFollower" /> through its internal seam:
///   the subscribe/unsubscribe delegates are captured, the Windows light mode is controllable, and
///   events are raised by invoking the captured handler.
/// </summary>
/// <remarks>
///   The test process deliberately creates no <see cref="System.Windows.Application" />, so the
///   dispatched apply step returns at its <c> Application.Current </c> guard before any palette
///   work; the real menu-palette swap under a live application stays manual smoke per the precedent
///   in <c> SystemThemeFollowerTests </c>. There is no stored theme in play anywhere: the point of
///   this follower is that the menu palette follows Windows in every stored theme.
/// </remarks>
public sealed class SystemMenuThemeFollowerTests
{
    [Fact]
    public void UserPreferenceChanged_ModeFlip_DispatchesOneApply()
    {
        var fixture = new FollowerFixture { SystemLight = false };
        fixture.Follower.Start(fixture.Dispatch);

        fixture.SystemLight = true;
        fixture.RaisePreferenceChanged();

        var apply = Assert.Single(fixture.Dispatched);
        Assert.Null(Record.Exception(apply));
    }

    [Fact]
    public void UserPreferenceChanged_ModeUnchanged_DoesNotDispatch()
    {
        var fixture = new FollowerFixture { SystemLight = false };
        fixture.Follower.Start(fixture.Dispatch);

        fixture.RaisePreferenceChanged();
        fixture.RaisePreferenceChanged();

        Assert.Empty(fixture.Dispatched);
    }

    [Fact]
    public void UserPreferenceChanged_TwoFlips_DispatchTwice()
    {
        var fixture = new FollowerFixture { SystemLight = false };
        fixture.Follower.Start(fixture.Dispatch);

        fixture.SystemLight = true;
        fixture.RaisePreferenceChanged();
        fixture.SystemLight = false;
        fixture.RaisePreferenceChanged();

        Assert.Equal(2, fixture.Dispatched.Count);
    }

    [Fact]
    public void Dispose_Unsubscribes_AndLaterEventsDispatchNothing()
    {
        var fixture = new FollowerFixture { SystemLight = false };
        fixture.Follower.Start(fixture.Dispatch);
        var handler = Assert.Single(fixture.Subscribed);

        fixture.Follower.Dispose();

        Assert.Same(handler, Assert.Single(fixture.Unsubscribed));
        Assert.Empty(fixture.Dispatched);

        fixture.SystemLight = true;
        fixture.RaisePreferenceChanged();

        Assert.Empty(fixture.Dispatched);

        fixture.Follower.Dispose();

        Assert.Single(fixture.Unsubscribed);
    }

    [Fact]
    public void Start_SecondCall_IsANoOp()
    {
        var fixture = new FollowerFixture();
        fixture.Follower.Start(fixture.Dispatch);

        fixture.Follower.Start(fixture.Dispatch);

        Assert.Single(fixture.Subscribed);
        Assert.Empty(fixture.Unsubscribed);
    }

    private sealed class FollowerFixture
    {
        public List<UserPreferenceChangedEventHandler> Subscribed { get; } = [];

        public List<UserPreferenceChangedEventHandler> Unsubscribed { get; } = [];

        public List<Action> Dispatched { get; } = [];

        public bool SystemLight { get; set; }

        public SystemMenuThemeFollower Follower { get; }

        private UserPreferenceChangedEventHandler? subscribedHandler;

        public FollowerFixture()
        {
            Assert.Null(Application.Current);

            this.Follower = new SystemMenuThemeFollower(
                new SystemMenuTheme(_ => new ResourceDictionary()),
                () => this.SystemLight,
                handler =>
                {
                    this.Subscribed.Add(handler);
                    this.subscribedHandler = handler;
                },
                handler =>
                {
                    this.Unsubscribed.Add(handler);
                    this.subscribedHandler = null;
                });
        }

        public void Dispatch(Action action) => this.Dispatched.Add(action);

        /// <summary>
        ///   Raises the preference-change broadcast like the static system event would: only the
        ///   currently subscribed handler is invoked, none after an unsubscribe.
        /// </summary>
        public void RaisePreferenceChanged() =>
            this.subscribedHandler?.Invoke(this, new UserPreferenceChangedEventArgs(UserPreferenceCategory.General));
    }
}