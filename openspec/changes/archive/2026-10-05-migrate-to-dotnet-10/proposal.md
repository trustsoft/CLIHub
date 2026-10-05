## Why

CLIHub currently targets .NET 8 while the repository and CI environment are already prepared for .NET 10. Moving the application and test suite to .NET 10 keeps the Windows desktop runtime supported, enables the C# 14 compiler baseline, and establishes a reproducible SDK policy before the next release.

## What Changes

- Retarget the Core, WPF application, and test projects from .NET 8 to the corresponding .NET 10 target frameworks.
- Set an explicit repository-wide C# 14.0 language version.
- Add a `global.json` policy for the stable .NET 10 SDK with controlled feature-band roll-forward and prerelease SDKs disabled.
- Refresh direct NuGet dependencies to current compatible releases, including runtime, UI, update, logging, and test tooling packages.
- Update CI to build and test with the .NET 10 SDK.
- **BREAKING** Update framework-dependent Velopack packaging and runtime bootstrap metadata from the .NET 8 Desktop Runtime to the .NET 10 Desktop Runtime.
- Update architecture, build, release, and user-facing documentation plus durable capability specs to describe the new framework, language, SDK, and runtime requirements.
- Review .NET 10 WPF compatibility changes and preserve existing application behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ci-build`: Require the .NET 10 SDK/toolchain for the Windows build and test workflow.
- `release-pipeline`: Publish framework-dependent Windows packages against the .NET 10 Desktop Runtime and target framework.
- `code-style`: Make the repository-wide C# 14.0 language policy and SDK baseline explicit.

## Impact

- Project files and shared MSBuild configuration under `src/`, `tests/`, and the repository root.
- New `global.json` SDK selection policy.
- GitHub Actions workflows under `.github/workflows/`.
- Velopack publish and installer runtime metadata.
- Direct NuGet dependency versions and the generated restore/build graph.
- WPF XAML compatibility review, especially resource references and grid definitions.
- `README.md`, `docs/architecture.md`, `docs/releasing.md`, and related OpenSpec capability contracts.
- No application feature or persisted configuration format is intended to change.
