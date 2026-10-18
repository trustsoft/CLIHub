## 1. Scanning-only apply path

- [x] 1.1 Add a scanning-only input record and apply method to `SettingsApplicationService` that validates the depth values, normalizes the root/pattern lists and per-root settings, persists them atomically, reconfigures the scanner, and returns the existing application result; verify with `SettingsApplicationServiceTests` that it writes only scanning preferences, calls the scanner reload, and rejects an out-of-range depth
- [x] 1.2 Keep the full Save path writing the same values so both entry points stay consistent; verify the existing save tests still pass

## 2. Live page behavior

- [x] 2.1 Apply immediately after adding or removing a watched directory, changing a row's depth or discovery chip, changing a default depth/discovery selector, and adding or removing an ignored pattern; verify with `SettingsViewModelApplicationTests` that each action persists through the service and reloads the scanner
- [x] 2.2 Suppress applies while the page loads stored values, so opening the window never writes or rescans; verify with a test that asserting no apply during `Load`
- [x] 2.3 Report an apply failure through the existing validation surface instead of silently keeping the editor ahead of the configuration; verify the add flow keeps the error visible

## 3. Communication

- [x] 3.1 State on the Projects & Paths page that scanning changes apply immediately, and align the "Scan Now" tooltip with the live list; verify by running the application and adding a folder
- [x] 3.2 Update `docs/architecture/scanning.md` for the immediate-apply flow and the remaining Save behavior

## 4. Validation

- [x] 4.1 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`
