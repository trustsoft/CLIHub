namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core;
using CLIHub.Core.Hotkeys;
using CLIHub.Core.Models;
using CLIHub.Hotkeys;

public class HotkeyStartupRegistrarTests
{
    [Fact]
    public void Register_ValidPreference_RegistersParsedDefinition()
    {
        var hotkey = new Mock<IGlobalHotkeyService>(MockBehavior.Strict);
        hotkey.Setup(x => x.Register(It.IsAny<HotkeyDefinition>())).Returns(true);
        var registrar = new HotkeyStartupRegistrar(
            hotkey.Object,
            NullLogger<HotkeyStartupRegistrar>.Instance);

        registrar.Register(new AppPreferences { Hotkey = "Alt+F4" });

        hotkey.Verify(
            x => x.Register(new HotkeyDefinition(HotkeyModifiers.Alt, 0x73)),
            Times.Once);
    }

    [Fact]
    public void Register_InvalidPreference_RegistersDefaultDefinition()
    {
        var hotkey = new Mock<IGlobalHotkeyService>(MockBehavior.Strict);
        hotkey.Setup(x => x.Register(It.IsAny<HotkeyDefinition>())).Returns(true);
        var registrar = new HotkeyStartupRegistrar(
            hotkey.Object,
            NullLogger<HotkeyStartupRegistrar>.Instance);

        registrar.Register(new AppPreferences { Hotkey = "invalid" });

        hotkey.Verify(x => x.Register(HotkeyParser.Default), Times.Once);
    }

    [Fact]
    public void AddClIHubServices_RegistersHotkeyStartupServices()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IGlobalHotkeyService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHotkeyStartupRegistrar));
    }
}
