namespace CLIHub.Tests.Services;

using CLIHub.Core.Projects;

public class ProjectPathPolicyTests
{
    [Fact]
    public void AreEqual_IgnoresTrailingSeparatorsAndCase()
    {
        var root = Path.Combine(Path.GetTempPath(), "CLIHub", "Project");

        Assert.True(ProjectPathPolicy.AreEqual(root, root.ToUpperInvariant() + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void Normalize_ReturnsFullPathWithoutTrailingSeparator()
    {
        var relative = Path.Combine(".", "project", "..");

        var normalized = ProjectPathPolicy.Normalize(relative);

        Assert.Equal(Path.GetFullPath(relative).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), normalized);
    }
}
