## 1. Page catalog

- [x] 1.1 Change the `projects` page title to "Project Discovery" and keep the key, icon, group, and description unchanged; verify with `SettingsNavigationTests`
- [x] 1.2 Update XML documentation, XAML comments, and `docs/architecture/scanning.md` page references

## 2. Specifications

- [x] 2.1 Update the `preferences-ui` "Settings sections" page list and the `settings-theme` "Cards on the data pages" text to "Project Discovery"
- [x] 2.2 Update the unarchived `projects-page-live-scanning` and `projects-summary-scan-coverage` deltas to the new page name so the specs stay consistent regardless of archive order

## 3. Validation

- [x] 3.1 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`
- [ ] 3.2 Launch the application and confirm the sidebar item and page header read "Project Discovery"
