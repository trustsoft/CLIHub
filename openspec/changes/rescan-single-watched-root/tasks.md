## 1. Scoped scanner pass

- [x] 1.1 Add `Rescan(string root)` to `IProjectScanner` and implement it in `ProjectScanningService` with the same start/dispose guard and scan-gate serialization as the full pass
- [x] 1.2 Enumerate only the requested root with the stored scan settings and reconcile only projects beneath it; keep discovered projects under other watched roots and manual projects
- [x] 1.3 Skip removal entirely when the requested root is missing or unreadable; report the scoped pass through the existing status with the total discovered-project count

## 2. Settings page action

- [x] 2.1 Add a `ScanWatchedDirectoryCommand` to the settings view model that forwards the row's directory to `IProjectScanner.Rescan`
- [x] 2.2 Add the icon-only Scan action before the remove action in each watched-directory row, with tooltip and accessible name, and leave the header "Scan Now" as the full rescan
- [x] 2.3 Update `docs/architecture/scanning.md` with the scoped pass and its reconciliation rules

## 3. Verification

- [x] 3.1 Add scanning-service tests for scoped discovery, scoped removal, other-root isolation, manual-project retention, and a missing root
- [x] 3.2 Add a view-model test that the row command rescans exactly the row's directory
- [x] 3.3 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; run `openspec validate --all`
- [x] 3.4 Launch the app, open Settings → Projects & Paths, and confirm the row action scans only that folder
