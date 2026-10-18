## 1. Discovery coverage

- [x] 1.1 Count every directory the walk examines and return per-root counts from `RepositoryDiscovery.Enumerate` through `DiscoveryResult`; verify with `RepositoryDiscoveryTests` covering a root that is itself a repository, a depth-limited walk, and a missing root that reports no count
- [x] 1.2 Extend `ProjectScanStatus` with the aggregated examined-folder count and keep `NotConfigured` at zero; verify the record and every `with` update compile and the existing status scenarios still pass

## 2. Service aggregation

- [x] 2.1 Replace per-root coverage on a full scan, update only the rescanned root on `Rescan`, and clear coverage on `Stop` and on reload with no watched roots; verify with `ProjectScanningServiceTests` that a full pass reports the sum across roots and a single-root rescan keeps the other roots' coverage
- [x] 2.2 Prune coverage for watched roots removed by a reload; verify the aggregated count never includes a root that is no longer configured
- [x] 2.3 Count registered discovered projects in every pass (not the pass's found paths, which include manual repositories) and expose the manual-project count; verify with `ProjectScanningServiceTests` that a full pass and a single-root pass report the same discovered count and that manual projects are counted separately

## 3. Summary cards

- [x] 3.1 Expose `TotalProjectCount` and the "N discovered · M manual" split on `SettingsViewModel`, refreshed from `ProjectScanStatus` and the manual-project count; verify with `SettingsViewModelApplicationTests`
- [x] 3.2 Bind the PROJECTS card to the total and the split, and move the examined-folder count into the watcher card's last-scan detail with singular/plural wording; verify with `SettingsViewModelApplicationTests` and by running the application

## 4. Documentation and validation

- [x] 4.1 Update `docs/architecture/scanning.md` for the status metric, the per-root aggregation rules, the discovered-count definition, and the summary card content
- [x] 4.2 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`
