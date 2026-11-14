namespace CLIHub.Core.Plugins;

/// <summary>
///   Feed-side source of a descriptor-only plugin install: the verbatim descriptor JSON from a
///   schema version 2 feed entry's inline <c> descriptor </c>, plus optional logo bytes
///   downloaded from the entry's <c> logoUrl </c>. The feed index in the git repository is the
///   integrity anchor, so there is no package download and no checksum step.
/// </summary>
/// <param name="DescriptorJson"> Verbatim raw JSON text of the inline descriptor. </param>
/// <param name="LogoBytes"> Optional logo bytes written as the installed plugin's <c> logo.png </c>. </param>
public sealed record PluginDescriptorInstallSource(string DescriptorJson, byte[]? LogoBytes);
