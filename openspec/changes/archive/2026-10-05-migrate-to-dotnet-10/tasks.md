## 1. Framework and SDK Baseline

- [x] 1.1 Add `global.json` with the stable .NET 10 SDK baseline, `latestFeature` roll-forward, and prerelease SDKs disabled; verify SDK resolution with `dotnet --version` and an SDK diagnostic/build evaluation.
- [x] 1.2 Retarget `CLIHub.Core`, `CLIHub`, and `CLIHub.Tests` to `net10.0`/`net10.0-windows`, add centralized `LangVersion` `14.0` in `Directory.Build.props`, and verify all solution project targets and language properties resolve as expected.
- [x] 1.3 Refresh direct production and test package references to current compatible releases, including the known System.Text.Json, Velopack, H.NotifyIcon.Wpf, Serilog, xUnit, test SDK, Moq, and coverlet candidates; verify restore completes without compatibility warnings that affect the solution.

## 2. Source and WPF Compatibility

- [x] 2.1 Build the retargeted solution and resolve source, analyzer, nullable, or API compatibility errors introduced by .NET 10 or refreshed dependencies; verify `dotnet build CLIHub.sln -c Release` succeeds.
- [x] 2.2 Review WPF XAML and integration points against .NET 10 breaking-change behavior, including grid definition collections and the dynamic scrollbar resource reference; correct only migration-blocking issues and verify the WPF project builds successfully.
- [x] 2.3 Run the complete test suite after the framework and dependency changes and verify `dotnet test CLIHub.sln -c Release --no-build` passes.

## 3. CI and Release Packaging

- [x] 3.1 Update CI workflow SDK setup and build/test steps to use the repository's stable .NET 10 policy; verify the workflow commands match the declared SDK policy and continue to gate pull requests and `master` pushes.
- [x] 3.2 Update release publish and Velopack commands from the .NET 8 Desktop Runtime metadata to `net10.0` and the .NET 10 Desktop Runtime while retaining framework-dependent `win-x64` packaging; verify a local or CI packaging run produces the expected framework metadata without bundling the runtime.
- [x] 3.3 Verify release notes extraction, tag-derived versioning, update metadata, and installer runtime bootstrap remain intact after the packaging change; verify the release workflow still blocks publishing when tests or release notes fail.

## 4. Documentation and Specification Synchronization

- [x] 4.1 Update `README.md`, `docs/architecture.md`, and `docs/releasing.md` with the .NET 10 Desktop Runtime requirement, .NET 10 SDK build requirement, C# 14.0 policy, and updated package/runtime wording; verify no obsolete .NET 8 baseline remains in those documents.
- [x] 4.2 Sync the durable `ci-build`, `release-pipeline`, and `code-style` specifications with the implemented .NET 10, C# 14.0, and SDK-policy behavior; verify each OpenSpec requirement has complete scenarios and validates successfully.

## 5. End-to-End Validation

- [x] 5.1 Run the Release build and full test suite under the selected .NET 10 SDK, then verify the application starts and the documented tray, launch window, settings, update check, and release-notes flows remain functional.
- [x] 5.2 Run OpenSpec validation and inspect the final change diff to verify only the approved migration, dependency, packaging, documentation, and planning artifacts are included.
