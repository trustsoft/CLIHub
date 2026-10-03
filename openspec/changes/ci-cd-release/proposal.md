# Proposal: ci-cd-release

## Why

The repository has no CI at all: nothing builds or tests pull requests automatically, and releases
are produced entirely by hand. The application already checks Velopack's GitHub Releases feed, but
the feed points at the placeholder `https://github.com/your-org/clihub` and no one publishes real
packages there, so self-update can never work. `docs/vision.md` lists CI packaging with `vpk` and the
real release repository as an open gap, and the `release-notes` change explicitly deferred the
`vpk --releaseNotes` wiring to this packaging change.

## What Changes

- Add a CI workflow (`.github/workflows/ci.yml`) that builds the solution in Release and runs the
  test suite on Windows for every pull request and every push to `master`.
- Add a release workflow (`.github/workflows/release.yml`) triggered by pushing a `v*` tag:
  - runs the tests as a gate;
  - publishes the WPF app framework-dependent for `win-x64` with the version taken from the tag
    (the tag is the release source of truth; `Directory.Build.props` stays the development default);
  - extracts the released version's section from `RELEASE-NOTES.md` and fails the release when the
    section is missing;
  - packages the app with `vpk pack` (portable + installer, `--framework net8.0-x64-desktop` so the
    installer offers to install the .NET 8 Desktop Runtime when it is absent);
  - publishes the packages to GitHub Releases with `vpk upload github`, with the extracted notes as
    the release body.
- Point `UpdateService.RepositoryUrl` at the real repository `https://github.com/trustsoft/clihub`
  so the shipped app resolves its update feed.
- Update the documentation (architecture, repo structure, vision, README) to describe the pipeline
  and the real release repository.

Decisions already taken with the user (2026-10-03):

| # | Question | Decision |
|---|----------|----------|
| 1 | Release trigger | Tag push `v*` only (no manual dispatch for now) |
| 2 | Version source | The tag; CI overrides `-p:Version=`, no strict check against `Directory.Build.props` |
| 3 | Distribution | Framework-dependent `win-x64` (requires .NET 8 Desktop Runtime, bootstrapped by the installer) |
| 4 | Update channels | Stable only (default `win` channel); beta deferred |

## Capabilities

### New Capabilities

- `ci-build`: every pull request and push to `master` is built in Release and tested on Windows; a
  red build or failing test fails the check.
- `release-pipeline`: pushing a `v*` tag produces and publishes a Velopack release to GitHub
  Releases — version from the tag, framework-dependent win-x64 package, embedded runtime bootstrap,
  release notes extracted from `RELEASE-NOTES.md`, notes as the GitHub release body.

### Modified Capabilities

None — the in-app update behavior (`update-checking`) and the notes documents (`release-notes`)
keep their current requirements; this change feeds the pipeline those capabilities already assume.

## Impact

- New files: `.github/workflows/ci.yml`, `.github/workflows/release.yml`.
- `src/CLIHub.Core/Services/UpdateService.cs`: the `RepositoryUrl` constant becomes the real
  repository URL (update checks then resolve against the actual GitHub Releases feed).
- `docs/architecture.md`, `docs/repo-structure.md`, `docs/vision.md`, `README.md`: describe the
  pipeline; the vision's open gap is closed.
- End users need the .NET 8 Desktop Runtime (or accept the installer's runtime bootstrap); the
  update feed becomes live once the first `v*` tag is pushed.
- No change to plugin format, config schema, or agent behavior.
