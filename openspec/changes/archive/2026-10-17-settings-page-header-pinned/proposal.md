## Why

Some Settings pages are taller than the available window area — for example "CLI Agents" with its per-agent cards and the "Projects & Paths" and "Logs" pages — so the content area shows a vertical scrollbar. The page header currently lives inside the same `ScrollViewer` as the page content, so the page title, its one-line description, and the divider scroll out of view, leaving the user without a page identity while editing lower fields.

## What Changes

- Move the active page header out of the scrolling region: the window renders the page title, description, and divider in a fixed row above the content scroller.
- Carry the page description in navigation metadata (`SettingsPage.Description`) so the fixed header can render it for the active page, and remove the duplicated per-page header blocks.
- Keep page-specific status lines (agent metrics, current version) at the top of the scrollable content beneath the fixed header.
- Keep the sidebar, drawn window chrome, validation message, and footer fixed as they are today; only the content below the page header scrolls.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme`: Adds the requirement that the page header stays visible while the content scrolls. The page title and description remain part of the page presentation, but they are rendered once in the window's fixed header instead of inside each page's scrolling content.

## Impact

- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — page metadata gains a description used by the header.
- `src/CLIHub/Views/SettingsWindow.xaml` — fixed header row and a content scroll region beneath it.
- `src/CLIHub/Views/Settings*Page.xaml` — per-page header blocks removed; status lines retained at the top of the content.
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs` — catalog assertions for page descriptions.
- No configuration, persistence, or user-facing preference changes.
