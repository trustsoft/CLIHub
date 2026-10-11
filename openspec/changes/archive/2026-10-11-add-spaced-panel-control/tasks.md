## 1. Implement the WPF panel

- [x] 1.1 Add `SpacedPanel` in `src/CLIHub/Controls/` with bindable orientation and spacing properties, documented defaults, and validation that prevents negative or non-finite spacing; verify the application project builds.
- [x] 1.2 Implement constraint-aware measure and arrange passes, including collapsed-child handling and one gap per adjacent visible pair; verify desired sizes and child rectangles with focused tests for both orientations and finite cross-axis constraints.

## 2. Verify control behavior

- [x] 2.1 Add deterministic layout tests for defaults, property changes, invalid spacing, empty/all-collapsed content, and overflow without child overlap; verify the focused `CLIHub.Tests` test filter passes using the repository's WPF threading setup.
- [x] 2.2 Run `dotnet test CLIHub.sln` and `dotnet build CLIHub.sln`; verify both complete successfully.

## 3. Adopt the control selectively

- [x] 3.1 Add shared spacing metrics to `Themes/Sizing.xaml` where repeated gaps have a common role, and adopt `SpacedPanel` in representative simple settings-page or toolbar groups; verify the updated XAML compiles and preserves existing outer margins and control alignment.
- [x] 3.2 Update `docs/architecture/ui.md` with the control's intended use and exclusions; verify the guidance identifies Grid-based pane/list layouts and virtualizing item panels as out of scope.
