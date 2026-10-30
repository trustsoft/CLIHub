namespace CLIHub.ViewModels;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Tests.Builders;

public class LaunchWindowViewModelPolicyTests
{
    [Fact]
    public void PrepareForShow_RefreshesProjectsThenAgentsThenSelectsDefault()
    {
        var order = new List<string>();
        var projectService = new Mock<IProjectService>();
        projectService.Setup(service => service.GetAllProjects())
            .Callback(() => order.Add("projects"))
            .Returns(Array.Empty<Project>());
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(service => service.IsInstalledInSystem(It.IsAny<Plugin>()))
            .Callback(() => order.Add("agents"))
            .Returns(true);
        var preferencesStore = new Mock<IPreferencesStore>();
        preferencesStore.Setup(store => store.Load())
            .Callback(() => order.Add("default"))
            .Returns(new AppPreferences { DefaultAgentId = "b" });

        var viewModel = new LaunchWindowViewModelBuilder()
            .WithPlugins(Launchable("a"), Launchable("b"))
            .WithProjectService(projectService)
            .WithAgentDetectionService(detection)
            .WithPreferencesStore(preferencesStore)
            .Build();

        order.Clear();

        viewModel.PrepareForShow();

        Assert.NotEmpty(order);
        Assert.Equal("projects", order[0]);
        Assert.True(order.IndexOf("agents") < order.IndexOf("default"));
        Assert.Equal("b", viewModel.SelectedAgent?.Plugin.Id);
    }

    [Fact]
    public void PrepareForActivation_RefreshesProjectsAndAgentsWithoutDefaultSelection()
    {
        var (viewModel, mocks) = new LaunchWindowViewModelBuilder()
            .WithPlugins(Launchable("a"), Launchable("b"))
            .WithPreferences(new AppPreferences { DefaultAgentId = "b" })
            .WithAgentDetectionService(InstalledDetection())
            .BuildWithMocks();

        viewModel.ApplyDefaultAgentSelection();
        var selectedAgent = viewModel.SelectedAgent;
        mocks.ProjectService.Invocations.Clear();
        mocks.AgentDetectionService.Invocations.Clear();
        mocks.PreferencesStore.Invocations.Clear();

        viewModel.PrepareForActivation();

        Assert.NotNull(selectedAgent);
        Assert.Contains(
            mocks.ProjectService.Invocations,
            invocation => invocation.Method.Name == nameof(IProjectService.GetAllProjects));
        Assert.Contains(
            mocks.AgentDetectionService.Invocations,
            invocation => invocation.Method.Name == nameof(IAgentDetectionService.IsInstalledInSystem));
        mocks.PreferencesStore.Verify(store => store.Load(), Times.Never);
        Assert.Same(selectedAgent, viewModel.SelectedAgent);
    }

    [Fact]
    public void ShouldHideOnDeactivate_PinnedReturnsFalse()
    {
        var now = 1000L;
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithTickCount(() => now)
            .Build();

        viewModel.IsPinned = true;
        viewModel.MarkShown();
        now = 2000;

        Assert.False(viewModel.ShouldHideOnDeactivate(false));
    }

    [Fact]
    public void ShouldHideOnDeactivate_PromptOpenReturnsFalse()
    {
        var now = 1000L;
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithTickCount(() => now)
            .Build();

        viewModel.MarkShown();
        now = 2000;

        Assert.False(viewModel.ShouldHideOnDeactivate(true));
    }

    [Fact]
    public void ShouldHideOnDeactivate_WithinGraceReturnsFalse()
    {
        var now = 1000L;
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithTickCount(() => now)
            .Build();

        viewModel.MarkShown();
        now = 1300;

        Assert.False(viewModel.ShouldHideOnDeactivate(false));
    }

    [Fact]
    public void ShouldHideOnDeactivate_AfterGraceReturnsTrue()
    {
        var now = 1000L;
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithTickCount(() => now)
            .Build();

        viewModel.MarkShown();
        now = 1401;

        Assert.True(viewModel.ShouldHideOnDeactivate(false));
    }

    [Fact]
    public void MarkShown_ResetsGraceWindow()
    {
        var now = 1000L;
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithTickCount(() => now)
            .Build();

        viewModel.MarkShown();
        now = 1300;
        viewModel.MarkShown();
        now = 1600;

        Assert.False(viewModel.ShouldHideOnDeactivate(false));
    }

    private static Plugin Launchable(string id) => new()
    {
        Id = id,
        Name = id,
        Commands = new AgentCommands { Launch = new PluginCommand { Executable = id } }
    };

    private static Mock<IAgentDetectionService> InstalledDetection()
    {
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(service => service.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        return detection;
    }
}
