namespace CLIHub.Core.Plugins;

/// <summary>
///   Installs agent plugins from local <c> .clihub-plugin.zip </c> packages or, for schema
///   version 2 feed entries without a package, directly from an inline descriptor. Inspection
///   validates the source entirely in memory (no writes); installation stages the payload under
///   a temporary <c> .staging-&lt;guid&gt; </c> directory, moves it into place atomically with a
///   backup of any replaced plugin, rolls back on failure, records lifecycle state, and reloads
///   the catalog. Only <c> plugin.json </c> and <c> logo.png </c> may ever be written: no DLLs,
///   no code execution.
/// </summary>
public interface IPluginInstaller
{
    /// <summary>
    ///   Inspects a plugin package without writing files.
    /// </summary>
    /// <param name="packagePath"> Path to the .clihub-plugin.zip file. </param>
    /// <returns> Preview with metadata, validation errors, or replacement info. </returns>
    PluginInstallPreview Inspect(string packagePath);

    /// <summary>
    ///   Installs a plugin package after confirmation.
    /// </summary>
    /// <param name="packagePath"> Path to the .clihub-plugin.zip file. </param>
    /// <param name="replaceExisting"> Whether to replace an existing installed plugin with the same id. </param>
    /// <returns> Result with installed plugin id or errors. </returns>
    PluginInstallResult Install(string packagePath, bool replaceExisting);

    /// <summary>
    ///   Inspects an inline descriptor without writing files. The descriptor-only path writes
    ///   only <c> plugin.json </c> (plus an optional <c> logo.png </c>) with no zip and no
    ///   checksum step: the feed index is the integrity anchor.
    /// </summary>
    /// <param name="source"> Descriptor-only install source. </param>
    /// <returns> Preview with metadata, validation errors, or replacement info. </returns>
    PluginInstallPreview InspectDescriptor(PluginDescriptorInstallSource source);

    /// <summary>
    ///   Installs a plugin from an inline descriptor after confirmation, running the same
    ///   staging, backup, atomic-move, rollback, state, and catalog-reload path as a package
    ///   install.
    /// </summary>
    /// <param name="source"> Descriptor-only install source. </param>
    /// <param name="replaceExisting"> Whether to replace an existing installed plugin with the same id. </param>
    /// <returns> Result with installed plugin id or errors. </returns>
    PluginInstallResult InstallDescriptor(PluginDescriptorInstallSource source, bool replaceExisting);
}
