## 1. ConfigService background writer

- [ ] 1.1 Add `Flush()` to `IConfigService` with XML docs; add a no-op `Flush()` to `FakeConfigService`; verify the solution compiles
- [ ] 1.2 Add an optional `configFilePath` constructor parameter to `ConfigService` (default unchanged: `%APPDATA%\CLIHub\config.json`) so tests can isolate the state file; verify existing tests still pass
- [ ] 1.3 Rework `Save` to serialize synchronously and hand the JSON payload to a lazily started single background worker with a 250 ms debounce (atomic write via temp + `File.Move`); verify the project builds
- [ ] 1.4 Implement `Flush()` (wait for the worker, then synchronously drain any remaining payload) and `Dispose()` → `Flush()` with a warning log on failure; verify with unit tests: Save→Flush writes the file, two rapid saves coalesce to the latest content, dispose flushes pending writes
- [ ] 1.5 Update `AppConfigSerializationTests` to use an isolated temp config path and `Flush()`; add a test that a failed disk write is logged and does not throw

## 2. Observable Project model

- [ ] 2.1 Make `Project` implement `INotifyPropertyChanged`; raise notifications from `IsFavorite`, `LastUsed`, and `LogoPath` setters; verify with a unit test that each setter raises `PropertyChanged`

## 3. Launch window diff-sync

- [ ] 3.1 Replace the clear-and-rebuild in `LaunchWindowViewModel.RefreshProjects` with a diff sync (remove missing IDs, append new IDs, keep existing instances and selection); verify the solution builds and existing tests pass

## 4. Verification

- [ ] 4.1 Run the full test suite (`dotnet test CLIHub.sln`) and confirm all tests pass
- [ ] 4.2 Run `dotnet build CLIHub.sln` with no warnings; manually verify: selecting a project and toggling a favorite updates the row in place, `config.json` is written shortly after changes, and exiting right after a change flushes it to disk
