## 1. Page metadata

- [x] 1.1 Add a `Description` value to `SettingsPage` and supply a one-line description for every catalog page; keep the XML documentation for the record parameters
- [x] 1.2 Extend `SettingsNavigationTests` to assert that every page has a non-empty description

## 2. Fixed page header

- [x] 2.1 In `SettingsWindow.xaml`, add an `Auto` row above the content `ScrollViewer` that renders `SelectedPage.Title`, `SelectedPage.Description`, and the hairline divider; move the scroller to the row below
- [x] 2.2 Collapse the description line when the active page has no description, and verify the window still lays out at minimum size

## 3. Page content cleanup

- [x] 3.1 Remove the page title/description/divider block from every `Settings*Page.xaml` view
- [x] 3.2 Keep the "CLI Agents" metrics line and the "Updates" current-version line at the top of the scrollable content; verify their top spacing against the fixed divider
- [x] 3.3 Verify the placeholder page still renders its empty state under the fixed header

## 4. Documentation and verification

- [x] 4.1 Update `docs/architecture/ui.md` with the fixed settings page header and the scrolling content region
- [x] 4.2 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`
- [x] 4.3 Run `openspec validate --all` and confirm the change and specs validate
