# Design: ci-cd-release

## Context

The solution is a .NET 8 WPF app (`src/CLIHub`) plus a Core library and an xUnit test project
(`tests/CLIHub.Tests`, Core only). Builds must run on Windows runners — WPF cannot build on Linux.
Version lives in `Directory.Build.props` (`0.6.0`, `IncludeSourceRevisionInInformationalVersion=false`,
so informational versions are clean). `CLIHub.Core` references Velopack **1.2.158**; `UpdateService`
resolves its update feed from the `RepositoryUrl` constant (`src/CLIHub.Core/Services/UpdateService.cs:18`,
currently the `your-org/clihub` placeholder) through `GithubSource` with prerelease disabled. The
repository is `trustsoft/CLIHub` with the default branch `master`; there are no tags yet. GitHub
Actions workflows do not exist.

See proposal.md for motivation and the user's four decisions.

## Goals / Non-Goals

**Goals**

- Two workflows: `ci.yml` (build + test) and `release.yml` (tag → test → package → publish).
- Reproducible release packaging driven by a small number of explicit steps.
- The in-app updater and the published packages share the same repository and notes.

**Non-Goals**

- Code signing (no certificate available yet; the installer stays unsigned).
- Beta/prerelease channels (stable `win` channel only).
- Changelog generation from commits — notes stay hand-written (decision 3 in
  `docs/changelog-and-release-notes.md`).
- Version bump automation for `Directory.Build.props` — the tag wins at release time; the file is
  only the development default and moves forward as part of normal work.

## Decisions

### D1 — Two separate workflows

`ci.yml` (triggers: `pull_request`, `push` to `master`) and `release.yml` (trigger: push of tags
`v*`). Separate files keep the cheap always-on path independent from the publish-capable path, so
the release workflow's `contents: write` permission is scoped to tag pushes only. Alternative — one
workflow with conditionals — was rejected: permission scoping and readability get worse.

### D2 — Release pipeline as two sequential jobs in one workflow

`test` (build + `dotnet test` on the solution) then `release` (`needs: test`). A failing gate stops
before any packaging. Single job was rejected: the gate result stays visible as its own check.

### D3 — Version flow: tag overrides at publish time

The version is `GITHUB_REF_NAME` minus the leading `v`, passed as `-p:Version=<version>` to
`dotnet publish` and as `-v <version>` to `vpk pack`. `Directory.Build.props` keeps `0.6.0` as the
development default and is not validated against the tag (user decision). No workflow code needs to
touch the props file.

### D4 — Framework-dependent publish + Velopack runtime bootstrap

`dotnet publish src/CLIHub/CLIHub.csproj -c Release -r win-x64 --self-contained false
-p:Version=<version> -o publish` followed by `vpk pack` with `--runtime win-x64 --framework
net8.0-x64-desktop`. The explicit runtime keeps the setup bundle x64 (vpk's default is x86, verified
locally) while the asset channel stays `win`, which is the channel the app's `GithubSource` feed
expects; the `--framework` flag makes Velopack's installer offer the .NET 8 Desktop Runtime when it
is missing (documented bootstrapping pattern). Self-contained was rejected per user decision
(package size).

### D5 — vpk as a global tool, pinned to the library's exact version

`dotnet tool install -g vpk --version 1.2.158` in the release job, exactly matching the `Velopack`
**1.2.158** package the app links. Delta packages and the updater must speak the same format; an
exact pin avoids the "library lower than vpk" compatibility warning a `1.2.*` pin produced in local
verification. Bump the NuGet package and this pin together. Alternative — latest `vpk` — rejected:
a format change in a newer major would silently break updates for installed clients.

### D6 — Release notes extraction step

A small PowerShell step scans `RELEASE-NOTES.md` for the `## <version>` heading and copies that
section into a temporary markdown file. The file is fed to `vpk pack --releaseNotes` and used as the
GitHub release body. A missing section fails the job before packaging (spec requirement). The
documented heading format (`## <version> — <date>`) is guaranteed by the `release-notes` capability.

### D7 — Publishing with `vpk upload github`

`vpk upload github --repoUrl https://github.com/trustsoft/clihub --token $GITHUB_TOKEN
--publish true --tag <tag> --releaseName "CLIHub <version>" --merge true --outputDir Releases`,
with `permissions: contents: write` on the release job and `secrets.GITHUB_TOKEN` as the token.
Velopack's own uploader creates/updates the release and attaches installer, portable, and delta
assets — exactly what `GithubSource` consumes. `--merge true` makes re-runs for the same tag update
the existing release. Alternatives (`softprops/action-gh-release` over raw assets, or a manual
release step) were rejected: they bypass Velopack's feed handling.

### D8 — `RepositoryUrl` points at the real repository

`UpdateService.RepositoryUrl` becomes `https://github.com/trustsoft/clihub` (case-insensitive
`github.com` check already routes it to `GithubSource`; prerelease flag already `false` = stable
channel, matching the default `win` channel the packer emits). No other code changes.

## Risks / Trade-offs

- [vpk/velopack format drift] → vpk pinned to exactly 1.2.158 (D5); bump the NuGet package and the
  tool together.
- [Unsigned installer triggers SmartScreen warnings] → accepted for now; signing is a future change
  (needs a certificate).
- [Framework-dependent package fails on runtime-less machines] → `--framework net8.0-x64-desktop`
  bootstrap; README documents the runtime prerequisite.
- [Release notes drift from the tag version] → the extraction step hard-fails the release (D6), so a
  missing section can never publish silently.
- [`GITHUB_TOKEN` mis-scoped] → workflow declares `contents: write` only on `release.yml`.

## Migration Plan

1. Merge the change; CI runs on the next PR/push automatically.
2. Cut the first release by preparing `RELEASE-NOTES.md`/`CHANGELOG.md` sections and pushing
   `v0.6.0`.
3. Rollback: delete the GitHub release and the tag; installed clients simply never see the update.

## Open Questions

None.
