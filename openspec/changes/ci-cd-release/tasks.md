# Tasks: ci-cd-release

## 1. CI workflow

- [x] 1.1 Create `.github/workflows/ci.yml`: windows-latest, triggers `pull_request` + `push` to `master`, checkout, `dotnet build CLIHub.sln -c Release`, `dotnet test CLIHub.sln -c Release --no-build`. Verify: `dotnet build` and `dotnet test` commands run locally with exit code 0; YAML parses (`openspec validate` not applicable — check with a YAML parser or `gh workflow list` after push).

- [x] 1.2 Verify the workflow file against GitHub's schema and the repository's default branch name (`master`). Verify: push a branch, open a draft PR, and confirm the CI check runs and is green.

## 2. Release workflow

- [x] 2.1 Create `.github/workflows/release.yml` with trigger `push: tags: ['v*']` and two jobs: `test` (build + `dotnet test` on the solution) and `release` (`needs: test`, `permissions: contents: write`). Verify: YAML parses; triggers and permissions visible in the file.

- [x] 2.2 In the `release` job, derive the version: strip the leading `v` from `GITHUB_REF_NAME`, fail when the remainder is not a valid `major.minor[.patch]` version. Verify: step logic exercised with a local PowerShell snippet for `v1.2.3`, `1.2.3`, `nightly`.

- [x] 2.3 Add the publish step: `dotnet publish src/CLIHub/CLIHub.csproj -c Release -r win-x64 --self-contained false -p:Version=<version> -o publish`. Verify: same command runs locally and `publish/CLIHub.exe` exists.

- [x] 2.4 Add the notes-extraction PowerShell step: find the `## <version>` section in `RELEASE-NOTES.md`, write it to a temp file, fail the job when the section is missing. Verify: local run finds the `0.6.0` section and fails for a bogus version.

- [x] 2.5 Add packaging: `dotnet tool install -g vpk --version 1.2.158` (exact pin, see design D5), then `vpk pack -u CLIHub -v <version> -p publish -e CLIHub.exe -i src/CLIHub/app.ico --packTitle CLIHub --runtime win-x64 --framework net8.0-x64-desktop --releaseNotes <notes-file> -o Releases`. Verify: run the two commands locally against the publish output from 2.3 and confirm `Releases/` contains the setup, portable, and `.nupkg` assets.

- [x] 2.6 Add the upload step: `vpk upload github --repoUrl https://github.com/trustsoft/clihub --token <GITHUB_TOKEN env> --publish true --tag <tag> --releaseName "CLIHub <version>" --merge true -o Releases`. Verify: command present with `secrets.GITHUB_TOKEN`; live verification happens in group 4.

## 3. Real update feed

- [x] 3.1 Change `UpdateService.RepositoryUrl` to `https://github.com/trustsoft/clihub` in `src/CLIHub.Core/Services/UpdateService.cs` and adjust the nearby comment if needed. Verify: `dotnet build CLIHub.sln -c Release` succeeds and the full test suite passes (`dotnet test`).

## 4. End-to-end release verification

- [ ] 4.1 Push the change to `master` (CI green), prepare the `0.6.0` sections in `RELEASE-NOTES.md`/`CHANGELOG.md` if missing, then push tag `v0.6.0`. Verify: the release workflow completes, the GitHub release `v0.6.0` is published with installer + portable + update metadata, and the release body matches the notes section.

- [ ] 4.2 Install the produced `CLIHub-win-Setup.exe` on a machine and confirm the app runs, reports version 0.6.0, and the in-app "Check for updates" reports no update (it is the latest). Verify: observed on a real machine.

## 5. Documentation

- [x] 5.1 `docs/architecture.md`: add a "Packaging & CI/CD" section (workflows, vpk, channels, runtime prerequisite) and reference it from the tech stack. Verify: section present, links resolve.

- [x] 5.2 `docs/repo-structure.md`: add `.github/workflows/` to the layout. Verify: layout matches the tree.

- [x] 5.3 `docs/vision.md`: remove the "Packaging and release setup" item from the not-yet-delivered list. Verify: the gap is no longer listed.

- [x] 5.4 `README.md`: document the release process for maintainers (tag push) and the .NET 8 Desktop Runtime prerequisite for users. Verify: instructions match the workflow files.
