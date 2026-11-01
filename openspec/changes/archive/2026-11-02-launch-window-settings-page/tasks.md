# Tasks: Launch Window Settings Page

Implementation tasks for splitting the Settings Appearance page, creating a new Launch Window page under General, and updating the tests and documentation.

## Stage 1: Catalog and Tests

- [x] 1.1 Update `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` catalog: add the new `launch-window` page entry between `hotkeys` and `projects-registry`, update the `hotkeys` title to "Global Hotkey" and description to "The global hotkey used to show the launcher.", and update the `appearance` title to "Theme & Accent" and description to "Theme and accent color."
- [x] 1.2 Update `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs` assertions: change the page count from 10 to 11, add `launch-window` to the key order assertion between `hotkeys` and `projects-registry`, update the hotkeys title assertion to "Global Hotkey", and add a slot-keys tuple row for `launch-window` with all-null slots between `hotkeys` and `projects-registry`

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 2: New Page View + Routing + XAML Moves

- [x] 2.1 Create `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` with a `UserControl` containing a `StackPanel` with two sections: `LAUNCHER POSITIONING` section header + the "Launcher window position" card (copied verbatim from `SettingsHotkeysPage.xaml:37-52`), followed by `PATHS` section header + the "Path display" card (copied verbatim from `SettingsAppearancePage.xaml:209-224`)
- [x] 2.2 Create `src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs` with the thin code-behind pattern: a `partial class` inheriting `UserControl` with a parameterless constructor that calls `this.InitializeComponent()`, and an XML doc comment
- [x] 2.3 Add the `views:SettingsLaunchWindowPage` entry to the page stack in `src/CLIHub/Views/SettingsWindow.xaml` (in the `Grid` at lines 502-522) with `Visibility="{Binding SelectedPage.Key, Converter={StaticResource PageKeyVisibility}, ConverterParameter=launch-window}"`, inserted after the `SettingsHotkeysPage` line
- [x] 2.4 Remove the `LAUNCHER POSITIONING` section (lines 37-52) from `src/CLIHub/Views/SettingsHotkeysPage.xaml`, keeping only the `ACTIVATION` section
- [x] 2.5 Remove the `PATHS` section (lines 209-224) from `src/CLIHub/Views/SettingsAppearancePage.xaml`, keeping only the `THEME` section

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 3: Docs + Spec Delta

- [x] 3.1 Update `README.md:36` to describe the Appearance page as showing theme and accent color only, and mention the new Launch Window page for path display and launcher positioning
- [x] 3.2 Update `docs/images/README.md:12` to describe `settings-appearance.png` as "Settings → Appearance (theme and accent color)." and add a note that the Launch Window page is where path display and launcher position are now located
- [x] 3.3 Confirm `openspec/changes/launch-window-settings-page/specs/preferences-ui/spec.md` (drafted by the plan stage) covers MODIFIED requirements for "Path display preference" (the control now lives on "Launch Window"), "Popup screen position" (the section now lives on "Launch Window", page renamed to "Global Hotkey"), "Application theme selector" (page now "Theme & Accent"), "Accent color selector" (page now "Theme & Accent"), and "Settings sections and pages" (eleven pages, new order, launch-window page under General, and which preference is on which page); update it only if the implemented titles or section names differ
- [x] 3.4 Complete the page rename in the remaining docs that name the pages: `docs/images/README.md` capture description, `docs/releasing.md` capture description, and the `popupScreenPosition` note in `docs/architecture/configuration.md`
- [x] 3.5 File hygiene: normalize the changed and new files to the repository's CRLF working-tree convention, drop the now-unused `xmlns:behaviors` declarations from the two source pages, and refresh the stale `SettingsAppearancePage` XML doc comment

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
graphify update .
```

## Verification

After all tasks are complete:

- [x] Build passes: `dotnet build CLIHub.sln`
- [x] Tests pass: `dotnet test CLIHub.sln`
- [x] OpenSpec validation passes: `openspec validate --all`
- [x] Graphify updated: `graphify update .`
- [x] Smoke: Open the Settings window, navigate to "Launch Window" under General, verify both the launcher positioning and path display controls are visible and bound correctly, verify the "Global Hotkey" page shows only the hotkey capture field, verify "Theme & Accent" shows only theme and accent controls, and verify the sidebar shows 11 pages in the correct order
