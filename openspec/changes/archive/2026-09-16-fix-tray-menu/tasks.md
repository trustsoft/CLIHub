# fix-tray-menu — Tasks

## 1. Architecture gate

- [x] 1.1 Run the Scryer `orient` for the tray/App files (`src/CLIHub.App/App.xaml.cs`, `src/CLIHub.App/Interop/NativeMethods.cs`) and record the planned menu change in the model (`open_change` + plan) before editing code; verify the orient output names the owning node for the tray wiring

## 2. Replace the tray menu implementation

- [x] 2.1 Add a `SetForegroundWindow` P/Invoke to `src/CLIHub.App/Interop/NativeMethods.cs` (fully qualified types per the csproj implicit-using constraints); verify `dotnet build CLIHub.sln` succeeds
- [x] 2.2 In `App.CreateTrayIcon`, build a `System.Windows.Forms.ContextMenuStrip` with the exit item always present and the update item hidden until an update is ready, and stop assigning `TaskbarIcon.ContextMenu`; verify by review that no WPF `ContextMenu` remains and the build passes
- [x] 2.3 Show the strip from the tray right-click handler at the cursor, with foreground activation for outside-click dismissal; verify in a manual run that right-clicking the tray icon shows the menu at the cursor

## 3. Manual verification (both target PCs)

- [x] 3.1 First open after startup with the cursor near the taskbar: the menu opens adjacent to the cursor and does not overlap the taskbar (spec: Menu placement — first open)
- [x] 3.2 Open, close, reopen without moving the cursor: the menu opens at the same position as before (spec: Menu placement — repeated open)
- [x] 3.3 Cursor on the 150% display and on the mixed-DPI machine: the menu opens at the cursor on that display (spec: Menu placement — display scale)
- [x] 3.4 With no update ready the menu contains only "Выход"; after reaching an update-ready state and reopening, the update item appears and activating it starts the update (spec: Menu items)
- [x] 3.5 Activating "Выход" terminates the app; clicking outside the open menu closes it and the app keeps running (spec: Menu dismissal)
- [x] 3.6 `dotnet test CLIHub.sln` stays green, and `openspec validate --specs` passes
