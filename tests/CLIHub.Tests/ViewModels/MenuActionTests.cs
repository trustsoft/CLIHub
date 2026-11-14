namespace CLIHub.Tests.ViewModels;

using CLIHub.ViewModels;
using Xunit;

public sealed class MenuActionTests
{
    [Fact]
    public void EffectiveAccessibleName_NullAccessibleName_ReturnsLabel()
    {
        var action = new MenuAction
        {
            Label = "Test Label",
            AccessibleName = null
        };

        Assert.Equal("Test Label", action.EffectiveAccessibleName);
    }

    [Fact]
    public void EffectiveAccessibleName_EmptyAccessibleName_ReturnsLabel()
    {
        var action = new MenuAction
        {
            Label = "Test Label",
            AccessibleName = string.Empty
        };

        Assert.Equal("Test Label", action.EffectiveAccessibleName);
    }

    [Fact]
    public void EffectiveAccessibleName_ProvidedAccessibleName_ReturnsAccessibleName()
    {
        var action = new MenuAction
        {
            Label = "Test Label",
            AccessibleName = "Custom Accessible Name"
        };

        Assert.Equal("Custom Accessible Name", action.EffectiveAccessibleName);
    }
}
