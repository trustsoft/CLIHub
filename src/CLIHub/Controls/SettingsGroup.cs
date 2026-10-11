namespace CLIHub.Controls;

using System.Windows;
using System.Windows.Controls;

/// <summary>
///   Hosts several grouped <see cref="SettingCard"/> rows on one shared bordered surface, with
///   hairline separators between rows (none on the first).
/// </summary>
public sealed class SettingsGroup : ItemsControl
{
    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => false;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new ContentControl();
}
