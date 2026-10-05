## Context

The solution contains three SDK-style projects: `CLIHub.Core` targets `net8.0`, the WPF application targets `net8.0-windows`, and the tests target `net8.0-windows` because they reference the application project. `Directory.Build.props` centralizes output paths, versioning, and build style enforcement, but currently does not declare a language version or SDK selection policy. CI and release workflows install .NET 8 and .NET 10 side by side, while the projects and Velopack metadata still target .NET 8.

The repository uses WPF, Velopack, H.NotifyIcon.Wpf, Microsoft.Extensions, Serilog, System.Text.Json, xUnit, Moq, and coverlet. A local restore under SDK `10.0.401` succeeds. The migration must preserve the existing Windows-only application behavior, framework-dependent packaging model, configuration format, and update flow.

## Goals / Non-Goals

**Goals:**

- Establish `net10.0` and `net10.0-windows` as the solution target frameworks.
- Make C# 14.0 and the stable .NET 10 SDK selection policy explicit and shared.
- Refresh direct dependencies to current compatible versions, keeping package updates within the migration's compatibility boundary.
- Keep CI, Velopack packaging, installer bootstrap behavior, and documentation aligned with .NET 10.
- Check and correct any .NET 10 WPF source or XAML compatibility issues while preserving observable behavior.

**Non-Goals:**

- Adding application features or changing the tray, launch-window, settings, plugin, or update workflows.
- Changing the persisted `%APPDATA%\CLIHub\config.json` shape or migration behavior.
- Converting the framework-dependent release to self-contained or changing the Windows `win-x64` distribution model.
- Adopting C# features merely because they are available; the migration establishes the compiler baseline but does not require source modernization.
- Introducing multi-targeting or cross-platform support.

## Decisions

### Target every project consistently

Change the Core and test projects to `net10.0`/`net10.0-windows` and the WPF application to `net10.0-windows`. Keeping one target per project avoids compatibility ambiguity between project references and ensures the tests exercise the same framework family as the application.

An alternative was to leave Core on `net8.0` for a smaller diff. That would retain a split runtime baseline and weaken the stated migration goal, so it is not selected.

### Centralize C# and SDK policy

Add `LangVersion` `14.0` to `Directory.Build.props` so all projects inherit the same language contract. Add `global.json` at the repository root with SDK version `10.0.401`, `rollForward` `latestFeature`, and `allowPrerelease` `false`. CI continues to install stable `10.0.x`; the repository policy controls which installed SDK is selected.

An alternative was to use `LangVersion` `latest` and omit `global.json`. That would allow future compiler behavior to change without a deliberate repository decision, which conflicts with the requested policy visibility and reproducibility.

### Refresh dependencies as part of the compatibility pass

Review every direct package reference after retargeting and update it to the current compatible release. The known candidates from the baseline audit include `System.Text.Json` `10.0.12`, `Velopack` `1.2.161`, `H.NotifyIcon.Wpf` `2.4.1`, newer Serilog packages, and newer test tooling. Microsoft.Extensions packages are already on `10.0.12` in the baseline.

Package updates must be validated through restore, build, tests, and targeted review of public release notes where behavior affects WPF, update packaging, logging, or test execution. If a package's latest release is incompatible, retain the newest compatible version and record the reason in the implementation summary.

### Keep release packaging framework-dependent

Update the publish and Velopack framework metadata from `net8.0`/the .NET 8 Desktop Runtime to `net10.0`/the .NET 10 Desktop Runtime while retaining `win-x64` and `--self-contained false`. This preserves package size and the existing installer runtime bootstrap contract while changing only the required runtime generation.

### Treat WPF compatibility as a verification gate

Review the existing XAML and WPF integration against .NET 10 compatibility notes, especially grid definition collections and dynamic resource references. Fix only issues required to compile or preserve runtime behavior; do not redesign the visual layer during the framework migration.

## Risks / Trade-offs

- [Dependency regressions] A package refresh can introduce behavioral or API changes beyond the TFM update. -> Update in a controlled pass, run the complete test suite, and manually verify startup, tray, launch, settings, update, and release-note flows.
- [Runtime availability] Existing installations require the .NET 10 Desktop Runtime after the package transition. -> Keep the Velopack runtime bootstrap configured and update all user-facing prerequisites and release metadata.
- [SDK drift] `latestFeature` can select a newer .NET 10 feature band when installed. -> Keep the minimum baseline explicit in `global.json`, disable prereleases, and use stable .NET 10 setup in CI.
- [WPF compatibility] .NET 10 may reject or alter invalid XAML patterns that previously loaded. -> Validate the full WPF build and inspect the known .NET 10 WPF breaking-change areas before implementation is considered complete.
- [Rollback compatibility] A .NET 10 package is not a runtime-compatible downgrade from a .NET 8 package. -> Retain the existing tag-based release rollback procedure and verify that any rollback release is built from a commit with matching runtime metadata.

## Migration Plan

1. Update project targets, shared C# policy, SDK policy, and direct package references.
2. Restore and build the solution with the selected .NET 10 SDK; resolve source, WPF, analyzer, and package compatibility issues.
3. Run the full test suite and perform the documented manual WPF/tray verification.
4. Update CI setup and release publish/Velopack commands, then validate the generated framework-dependent package metadata.
5. Update architecture, README, release runbook, and OpenSpec main specifications to match the completed baseline.
6. Merge only after CI is green; publish the first .NET 10 release through the existing version-tag workflow.

Rollback before release is a normal source revert. After publishing, rollback requires deleting or replacing the affected release/tag according to the existing runbook; installed clients should not be expected to downgrade automatically.
