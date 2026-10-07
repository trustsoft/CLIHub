namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;
using CLIHub.Hotkeys;

public class PreferenceApplierTests
{
    [Fact]
    public void ApplyRuntime_ForwardsToRuntimeTarget()
    {
        var runtime = new Mock<IRuntimePreferenceTarget>(MockBehavior.Strict);
        runtime.Setup(x => x.SetRuntime(RuntimeKind.PowerShell));
        var applier = CreateApplier(runtime: runtime.Object);

        applier.ApplyRuntime(RuntimeKind.PowerShell);

        runtime.Verify(x => x.SetRuntime(RuntimeKind.PowerShell), Times.Once);
    }

    [Fact]
    public void ApplyPathDisplayStyle_ForwardsToDisplayTarget()
    {
        var display = new Mock<IPathDisplayStyleTarget>(MockBehavior.Strict);
        display.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.MiddleEllipsis));
        var applier = CreateApplier(display: display.Object);

        applier.ApplyPathDisplayStyle(PathDisplayStyle.MiddleEllipsis);

        display.Verify(x => x.ApplyPathDisplayStyle(PathDisplayStyle.MiddleEllipsis), Times.Once);
    }

    [Fact]
    public void ApplyHotkey_ForwardsResultWithoutOwningRollback()
    {
        var hotkey = new Mock<IGlobalHotkeyService>(MockBehavior.Strict);
        hotkey.Setup(x => x.ReRegister(HotkeyParser.Default)).Returns(false);
        var applier = CreateApplier(hotkey: hotkey.Object);

        var result = applier.ApplyHotkey(HotkeyParser.Default);

        Assert.False(result);
        hotkey.Verify(x => x.ReRegister(HotkeyParser.Default), Times.Once);
    }

    [Fact]
    public void ApplyStartWithWindows_ForwardsResult()
    {
        var startup = new Mock<IStartupService>(MockBehavior.Strict);
        startup.Setup(x => x.SetEnabled(true)).Returns(true);
        var applier = CreateApplier(startup: startup.Object);

        Assert.True(applier.ApplyStartWithWindows(true));
        startup.Verify(x => x.SetEnabled(true), Times.Once);
    }

    [Fact]
    public void AddClIHubServices_RegistersNarrowRuntimeTarget()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        var registration = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IRuntimePreferenceTarget));

        Assert.Equal(typeof(ProcessRuntimePreferenceTarget), registration.ImplementationType);
    }

    [Fact]
    public void AddClIHubServices_MapsDisplayTargetToLaunchWindowViewModel()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        var registration = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IPathDisplayStyleTarget));

        Assert.NotNull(registration.ImplementationFactory);
    }

    private static PreferenceApplier CreateApplier(
        IRuntimePreferenceTarget? runtime = null,
        IGlobalHotkeyService? hotkey = null,
        IStartupService? startup = null,
        IPathDisplayStyleTarget? display = null) =>
        new(
            runtime ?? new Mock<IRuntimePreferenceTarget>().Object,
            hotkey ?? new Mock<IGlobalHotkeyService>().Object,
            startup ?? new Mock<IStartupService>().Object,
            display ?? new Mock<IPathDisplayStyleTarget>().Object,
            NullLogger<PreferenceApplier>.Instance);
}
