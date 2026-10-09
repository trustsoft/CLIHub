# Phase 14: Theme Selection UI (Saved for Later)

## Status
**DEFERRED** - Saved for implementation after current refactoring cycle

## Goal
Add UI to Preferences window for selecting launch window theme (Dark/Light/System).

## Current State
- Theme infrastructure exists (`LaunchWindowTheme` enum with Dark/Light/System values)
- `AppPreferences.LaunchWindowTheme` property exists
- `ThemeManager` applies themes at startup
- No UI exposed in Preferences window

## Implementation Plan

### Task 1: Add Theme Picker to PreferencesWindow
**File:** `src/CLIHub/Views/PreferencesWindow.xaml`

Add theme selection ComboBox in the "Launch Window" section:

```xaml
<TextBlock Text="Theme:" 
           Margin="0,16,0,4" />
<ComboBox SelectedItem="{Binding LaunchWindowTheme}"
          ItemsSource="{Binding AvailableThemes}"
          MinWidth="200"
          HorizontalAlignment="Left">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Converter={StaticResource ThemeDisplayNameConverter}}" />
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

### Task 2: Update PreferencesViewModel
**File:** `src/CLIHub/ViewModels/PreferencesViewModel.cs`

Add properties:
```csharp
public LaunchWindowTheme LaunchWindowTheme
{
    get => _launchWindowTheme;
    set
    {
        if (!SetProperty(ref _launchWindowTheme, value))
        {
            return;
        }

        PreferenceSyncHelper.SyncPreference(
            _preferencesStore,
            preferences => preferences.LaunchWindowTheme = value,
            ApplyThemeChange);
    }
}

public IReadOnlyList<LaunchWindowTheme> AvailableThemes { get; } = 
    Enum.GetValues<LaunchWindowTheme>();

private void ApplyThemeChange()
{
    // Notify ThemeManager or reload window
}
```

### Task 3: Add ThemeDisplayNameConverter
**File:** `src/CLIHub/Converters/ThemeDisplayNameConverter.cs`

Create value converter for friendly display names:
```csharp
namespace CLIHub.Converters;

using System.Globalization;
using System.Windows.Data;
using CLIHub.Core.Models;

public sealed class ThemeDisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            LaunchWindowTheme.Dark => "Dark",
            LaunchWindowTheme.Light => "Light",
            LaunchWindowTheme.System => "System Default",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

### Task 4: Register Converter in App.xaml
**File:** `src/CLIHub/App.xaml`

```xaml
<Application.Resources>
    <ResourceDictionary>
        <converters:ThemeDisplayNameConverter x:Key="ThemeDisplayNameConverter"/>
        <!-- other converters -->
    </ResourceDictionary>
</Application.Resources>
```

### Task 5: Wire Up Theme Change Handler
Options:
- **A)** Restart launch window when theme changes
- **B)** Live reload theme resources
- **C)** Show "Restart required" message

Recommended: **Option C** (safest, clearest UX)

## Testing
- Manual verification in Preferences window
- Verify preference persists across restarts
- Check all three theme options render correctly

## Notes
- Deferred because it requires UI design decisions
- Low priority compared to structural refactoring
- Can be implemented independently later
