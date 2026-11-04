namespace CLIHub.Core.Plugins;

/// <summary>
///   Installs agent plugin packages from local <c> .clihub-plugin.zip </c> files. Packages are
///   untrusted input: the installer validates archive structure and security constraints,
///   stages extraction in a temporary directory, installs atomically with backup and rollback,
///   records lifecycle state, and reloads the catalog. Only descriptor and logo files are ever
///   written; no plugin code is loaded or executed in-process.
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
}
