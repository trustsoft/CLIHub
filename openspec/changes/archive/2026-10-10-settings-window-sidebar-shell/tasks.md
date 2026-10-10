## 1. Navigation model

- [x] 1.1 Add a static page catalog (group label, page key, page title, implemented flag) for the three groups and eight pages, and verify with a unit test that it enumerates exactly 3 groups and 8 pages in the specified order.
- [x] 1.2 Add selected-page state and the default page to `SettingsViewModel` (opens on "General & Startup") and verify with a unit test that the default is "General & Startup" and that changing the selection updates the active page.

## 2. Window shell

- [x] 2.1 Rework `SettingsWindow.xaml` into a shell (sidebar `ListBox` + content host + existing footer) and verify the app builds and the window shows the sidebar with three groups and eight items.
- [x] 2.2 Bind the active page and its accent highlight to the sidebar selection and verify the active item is highlighted while others stay neutral.
- [x] 2.3 Widen the default window and raise `MinWidth` to fit the sidebar and verify the window opens at the new size and remains resizable.
- [x] 2.4 Add a shared empty-state view for pages without implemented settings and verify opening such a page shows the empty state instead of controls.

## 3. Re-home existing settings into page views

- [x] 3.1 Create the General & Startup page view with the "Start with Windows" and "Show window on startup" toggles and verify they bind and persist as before (existing startup tests pass).
- [x] 3.2 Create the Hotkeys & Launchers page view with the global-hotkey capture field and verify capture still works (existing hotkey tests pass).
- [x] 3.3 Create the Terminal Profiles page view with the default-runtime selector and verify it binds and persists.
- [x] 3.4 Create the CLI Agents page view with the agents-probe `TTL, minutes` and `Timeout, seconds` fields and verify blank-uses-default and invalid-number behavior is unchanged (existing probe tests pass).
- [x] 3.5 Create the Appearance page view with the path display selector and verify it binds and persists.
- [x] 3.6 Create the Updates page view with the "Check for updates on startup" toggle, the manual update check, and the status line and verify the manual check still reports its outcome.
- [x] 3.7 Remove the old flat section markup from `SettingsWindow.xaml` and verify no duplicate controls remain and Save/Cancel still work.

## 4. Theme styles

- [x] 4.1 Add sidebar navigation styles (group label, page item, accent active-page highlight, page header) to `SettingsStyles.xaml`, reusing the shared palette, and verify they render correctly on the dark surface.

## 5. Verification

- [x] 5.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass, including the new navigation tests.
- [x] 5.3 Run `openspec validate settings-window-sidebar-shell --strict` and verify the change is valid.
- [x] 5.4 Smoke-test the window: open Settings, switch across all eight pages, save a change, and cancel; verify preference behavior is unchanged.
