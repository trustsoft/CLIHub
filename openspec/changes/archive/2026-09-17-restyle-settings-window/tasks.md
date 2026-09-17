# Tasks — Restyle Settings Window

## 1. Shared visual resources

- [x] 1.1 Add the dark palette brushes and keyed styles (section label, dark field, ghost button, accent button, footer strip, version chip) to `App.xaml` resources; verify `dotnet build CLIHub.sln` succeeds
- [x] 1.2 Add `DwmSetWindowAttribute` P/Invoke (corner preference) to `Interop/NativeMethods.cs`; verify build succeeds

## 2. Window chrome

- [x] 2.1 Convert `SettingsWindow` to `WindowStyle="None"` with `WindowChrome` (CaptionHeight 36, NoResize), dark 1 px border, top title bar (title «Настройки» + close button with `IsHitTestVisibleInChrome`), bottom footer strip; verify the window has no system caption, drags by the title bar, and the close control closes it discarding edits (manual run)
- [x] 2.2 Apply DWM round-corner preference on `SourceInitialized`; verify rounded corners on Windows 11 and no exception on startup (manual run)

## 3. Content restyle

- [x] 3.1 Restyle the four sections with uppercase micro-labels: runtime as a horizontal `ListBox` segmented control bound to `Runtime`, probe TTL/timeout as labeled dark fields, updates section with styled checkbox + ghost «Проверить обновления» button + `StatusText` line; verify bindings still show and edit stored values (manual run)
- [x] 3.2 Restyle `HotkeyCaptureBox` in place with the dark field style, keeping capture semantics (focus to capture, modifier-only ignored, Esc cancels, canonical display); verify all capture scenarios from `settings-window` spec manually
- [x] 3.3 Move «Сохранить»/«Отмена» into the footer right side (accent/ghost), bind to existing command/handler; footer left shows brand + version chip; verify save still validates and persists, cancel still closes without saving (manual run)

## 4. Verification and docs

- [x] 4.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; both must pass with no Core changes
- [x] 4.2 Compare the running window against `docs\ui\settings.png` and fix visual deltas (spacing, colors, label case)
- [x] 4.3 Update the implementation-snapshot section of `docs/ui.md` (settings window now matches the mockup; footer settings entry still pending as before)
