## Context

See `proposal.md` - Why. Verified current state at HEAD `ccf4285` (working tree clean except `.opencode/pipeline.json`):

- `src/CLIHub/Launching/ProjectDialogService.cs:37-47`: private `Show(...)` takes `Application.Current?.MainWindow` as the MessageBox owner (`:42`) and falls back to ownerless `MessageBox.Show` when MainWindow is null (`:44-46`).
- `src/CLIHub/Launching/ProjectDialogService.cs:13-26`: `SelectProjectFolder()` and `SelectFolder(title)` pass `Application.Current?.MainWindow` directly to `OpenFolderDialog.ShowDialog(owner)` (`:23`).
- `src/CLIHub/Launching/IProjectDialogService.cs:21-26`: `bool ConfirmProjectRemoval(string projectName)` (interface method, no owner parameter).
- Callers:
  - `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs:243`: Settings Projects page calls `this.dialogs.ConfirmProjectRemoval(item.Name)` in the `RemoveProject(ProjectModel)` command handler.
  - `src/CLIHub/ViewModels/Projects/ProjectPaneController.cs:155`: Launch window calls `this.dialogs.ConfirmProjectRemoval(project.Name)` in `RemoveSelectedProject()`.
- `src/CLIHub/Views/SettingsWindow.xaml.cs:38-62`: `ShowSettings()` shows the Settings window non-modally via `Show()` (`:54`) and sets `Owner = Application.Current.MainWindow` (`:44-46`) so Settings stays above the launch window.
- `src/CLIHub/Settings/SettingsLauncher.cs:26-34`: shows/reactivates the single Settings window instance.
- The launch window (`MainWindow`) is `Application.Current.MainWindow` and is topmost; Settings is a separate window owned by MainWindow.

The bug: when Settings is open and `ConfirmProjectRemoval` is called from `ProjectRegistryViewModel`, the prompt is owned by MainWindow instead of the Settings window, so it can appear behind Settings, and Settings is not disabled while the prompt is open.

## Goals / Non-Goals

**Goals:**

- Prompt owned by the correct window: Settings when triggered from Settings, MainWindow when triggered from the launch window.
- No change to the interface signature, confirmation message, or removal logic.
- Fix applies to all three dialog methods for consistency (`ConfirmProjectRemoval`, `SelectFolder`, `SelectProjectFolder`).
- If practical, extract the resolution rule into a testable helper and add a test.

**Non-Goals:**

- Adding an explicit owner parameter to `IProjectDialogService` (avoid polluting the interface and callers).
- Requiring view models to pass a `Window` reference (view models should stay UI-agnostic).
- Changing the prompt text, buttons, or removal semantics.

## Decisions

### Decision 1: Resolve owner from currently active window

**Chosen:** Inside `ProjectDialogService`, resolve the owner as:

1. The currently active window from `Application.Current.Windows.OfType<Window>()` where `IsActive == true`, excluding the message box itself.
2. Fall back to `Application.Current?.MainWindow` if no window is active.
3. Fall back to ownerless `MessageBox.Show` if MainWindow is also null.

**Rationale:** WPF command execution happens on the UI thread after the command is invoked; at that moment, the triggering window is typically still active. `Window.IsActive` reflects keyboard focus and window activation state. This approach requires no interface change and naturally resolves the correct owner for both call sites.

**Alternatives considered:**

- Add an owner/accessor parameter to `IProjectDialogService` methods: Rejected — forces a `Window` reference into view models or adds ceremony for one call site.
- Use a thread-static or ambient context to carry the owner: Rejected — overly complex for a two-call-site service.
- Always use MainWindow: Rejected — that is the bug.

**Verification needed:** Confirm in manual testing that the active window at command execution time is indeed the triggering window in WPF. If the window loses activation before the command runs, the fallback to MainWindow still provides correct behavior for the launch window case.

### Decision 2: Extract owner resolution into a testable helper

**Chosen:** Define a static `DialogOwnerResolver.ResolveOwner()` method that takes `Application.Current.Windows` and `Application.Current.MainWindow` as parameters (or encapsulates them) and returns the resolved owner `Window?`. This helper applies the three-step rule from Decision 1.

**Rationale:** Extracting the rule makes it testable in isolation without showing real MessageBoxes. The helper can be tested on an STA thread (required for WPF `Window` instances) using the existing `TestHelpers.RunOnSta` helper from `tests/CLIHub.Tests/Support/TestHelpers.cs`.

**Alternatives considered:**

- Leave the resolution inline in `Show`: Accepted as a fallback if extraction proves too ceremony-heavy or the resolution is one-liner trivial.
- Mock `Application.Current`: Rejected — `Application` is a sealed singleton; mocking it for a unit test is impractical.

**Test strategy:** If extracted, the test creates two WPF `Window` instances on an STA thread, activates one, calls `ResolveOwner`, and asserts the active window is returned. Test the fallback cases (no active window → MainWindow; no MainWindow → null). Do not show real MessageBoxes.

**Implementation note:** If extraction adds more lines than it saves or complicates the service, inline the resolution in `Show` and document the rule in an XML comment instead. Prioritize simplicity over testability for this small helper.

### Decision 3: Apply the same fix to folder pickers

**Chosen:** `SelectFolder` and `SelectProjectFolder` use the same owner resolution (`:23` currently passes `Application.Current?.MainWindow` to `OpenFolderDialog.ShowDialog(owner)`).

**Rationale:** Folder pickers share the same owner path and face the same z-order issue if ever triggered from Settings (they are not today, but the fix is zero-cost and future-proof).

**Verification:** No folder picker is called from Settings today (only from launch window and tests), so this is a defensive consistency fix. Manual testing can skip it; the launch window case covers the path.

### Decision 4: No interface change

**Chosen:** `IProjectDialogService` stays unchanged; owner resolution is internal to `ProjectDialogService`.

**Rationale:** Callers do not need to know about owner resolution, and the interface is cleaner without a `Window` reference.

## Risks / Trade-offs

**[Risk] WPF commands may execute after the triggering window loses activation** → Mitigation: manual testing required to confirm `IsActive` is reliable at command execution time. If not, the fallback to MainWindow still provides correct behavior for the launch window, and Settings would need an explicit owner-passing path.

**[Risk] Extracting a helper adds ceremony for a small fix** → Accepted: if extraction is verbose, inline the resolution and document it in an XML comment. The test becomes optional.

**[Trade-off] No test for the actual MessageBox owner** → Accepted: testing real MessageBox ownership requires showing actual message boxes, which is brittle and interactive. The helper test (if extracted) covers the resolution logic; manual smoke testing verifies the end-to-end behavior.

**[Trade-off] Folder pickers are fixed preemptively** → Benefit: consistency and future-proof. Cost: none (same resolution path).

## Migration Plan

1. (Optional) Extract `DialogOwnerResolver.ResolveOwner()` and add a test in `tests/CLIHub.Tests/Launching/DialogOwnerResolverTests.cs` using `TestHelpers.RunOnSta`.
2. In `ProjectDialogService`, replace `Application.Current?.MainWindow` with the resolution rule (active window → MainWindow → null) in `Show` (`:42`) and folder picker calls (`:23`).
3. Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`.
4. **Manual smoke test (required):**
   - Open Settings → Projects, select a manual project, click "Remove Project ×"; verify the prompt appears above Settings and Settings is disabled while the prompt is open.
   - In the launch window, select a project, click "Remove Project"; verify the prompt appears above the launch window and works as before.
   - Add a manual project from the launch window using the folder picker; verify the picker is modal to the launch window.
5. Commit only after smoke test passes and user approval.

Rollback: revert code; no persisted state changes.

## Open Questions

- **Is `Window.IsActive` reliably true at command execution time for the triggering window?** → Manual testing will answer this. If not, an explicit owner-passing path may be needed.
- **Should folder pickers from Settings (if added later) also get the fix?** → Yes, they use the same owner path and are covered by this change.
- **If extraction proves verbose, inline the resolution?** → Yes, prioritize simplicity; document the rule in an XML comment if no helper is extracted.
