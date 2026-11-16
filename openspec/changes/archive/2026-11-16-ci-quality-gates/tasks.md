## Stage 1: Warning enforcement in CI

- [x] 1.1 Edit `.github/workflows/ci.yml:27` — change the `Build` step to
      `dotnet build CLIHub.sln -c Release -warnaserror`.
- [x] 1.2 Run `dotnet build CLIHub.sln -c Release -warnaserror --no-incremental` locally — confirm
      it matches the measured baseline (0 warnings, 0 errors under a full rebuild), so the CI flag
      cannot fail on the current tree.
- [x] 1.3 Re-read `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:147-154`
      (`CiWorkflow_RunsBothTestProjects`) — confirm no test project file name was removed by the edit.

## Stage 2: Coverage collection in CI

- [x] 2.1 Edit `.github/workflows/ci.yml:32-34` — add `--collect:"XPlat Code Coverage"
      --results-directory artifacts/coverage/<ProjectName>` to each of the three `dotnet test`
      invocations (`Core.Tests`, `Core.Windows.Tests`, `CLIHub.Tests`), keeping `-c Release --no-build`.
- [x] 2.2 Edit `.github/workflows/ci.yml` — add an `Upload coverage` step after the test steps that
      uses `actions/upload-artifact@v7` with `name: coverage-results`,
      `path: artifacts/coverage/**/*.cobertura.xml`, `if: always()`, and
      `if-no-files-found: ignore`.
- [x] 2.3 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj -c Release --no-build
      --collect:"XPlat Code Coverage" --results-directory artifacts/coverage/Core.Tests` locally —
      confirm `cobertura.xml` files are produced under `artifacts/coverage/Core.Tests/` and the tests
      pass. Delete the local `artifacts/coverage/` directory afterward (it is gitignored, but it is a
      build artifact).
- [x] 2.4 Split the single `Test` step into three steps — `Test CLIHub.Core.Tests`,
      `Test CLIHub.Core.Windows.Tests`, `Test CLIHub.Tests` — one per test project, each `shell: pwsh`
      and each running its own `dotnet test ... -c Release --no-build --collect:"XPlat Code Coverage"
      --results-directory artifacts/coverage/<Project>`. Reason: a `pwsh` block only fails the step on
      its last command's exit code (`$PSNativeCommandUseErrorActionPreference` is not set), so a shared
      block reported green when an earlier project failed. Keep `Upload coverage` on `if: always()` and
      add no `continue-on-error`.

## Stage 3: Spec delta review

- [x] 3.1 Review `openspec/changes/ci-quality-gates/specs/ci-build/spec.md` (the plan-stage delta)
      against the implemented workflow in `.github/workflows/ci.yml` — amend the delta if the
      implementation diverged from the plan (for example if `-warnaserror` or coverage arguments
      differ from what was planned).
- [x] 3.2 Run `openspec validate --all` — confirm 0 failed (37 items: 36 specs + this change), with
      only the pre-existing `>500 character` and placeholder-Purpose warnings unrelated to this change.

## Stage 4: Documentation and backlog

- [x] 4.1 Update `docs/testing.md` — the section "What a green build does and does not mean"
      (lines 317-329) says a green build with new warnings still passes CI and the suite result is
      independent of coverage. Amend it: local builds still flag warnings without failing, CI now
      enforces `-warnaserror`, and CI publishes a coverage artifact (still no threshold gate).
- [x] 4.2 Check `docs/releasing.md` for stale claims about CI gates. Line 10 says "All work is
      merged to `master` with green CI" and line 79 says "wait for CI to go green" — both remain
      accurate under the new stricter CI. No change needed. Record this check result in the task
      output.
- [x] 4.3 Edit `docs/backlog.md` — mark the entry "CI enforces neither the warning baseline nor
      coverage" (lines 273-285) as resolved by `ci-quality-gates`. Follow the file's existing
      convention: add `**Status: Resolved by ci-quality-gates.**` at the top of the entry. Do not
      delete the evidence text.
- [x] 4.4 Edit `docs/backlog.md` — add a new entry "Release pipeline does not enforce warnings or
      coverage" with evidence (`.github/workflows/release.yml:29` build without `-warnaserror`,
      `.github/workflows/release.yml:32` test without `--collect`), severity Low, proposed remedy
      (mirror the CI gates — `-warnaserror` on build, coverage collection on test — in the release
      pipeline's `test` job), and trigger (the next release-pipeline or release-preparation change).

## Stage 5: Verification and graph refresh

- [x] 5.1 Run `dotnet build CLIHub.sln -c Release` — confirm 0 warnings, 0 errors (source untouched;
      this gate verifies the planning-stage measurement still holds).
- [x] 5.2 Run `dotnet test CLIHub.sln` — confirm all tests pass (the workflow edits do not affect
      local behavior; this confirms the tree is still green before the change is reviewed).
- [x] 5.3 Run `openspec validate --all` — confirm 0 failed (37 items).
- [x] 5.4 Run `graphify update .` — refresh the knowledge graph so the workflow edit is reflected.

## Build / Test Gates

Each stage ends with a gate; a failure stops the stage:

1. After 1.1: `dotnet build CLIHub.sln -c Release -warnaserror --no-incremental` (0 warnings, 0 errors).
2. After 2.4: YAML sanity — `ci.yml` names all three test `.csproj` files, each in its own `shell: pwsh`
   test step with its own `dotnet test` command (three separate steps, no `continue-on-error`), and the
   `upload-artifact` step uses `actions/upload-artifact@v7` with `if: always()` and
   `if-no-files-found: ignore`. Parse the file (`python -c "import yaml; yaml.safe_load(...)"`) and
   confirm the step sequence.
3. After 3.1: `openspec validate --all` (0 failed, 37 items).
4. After 4.4: re-read `docs/backlog.md` and `docs/testing.md` — confirm the resolution status line
   and the amended section read correctly.
5. Final: `dotnet build CLIHub.sln -c Release`, `dotnet test CLIHub.sln`, `openspec validate --all`,
   then `graphify update .`.

Note: CI can only be fully exercised by the actual GitHub Actions run, which happens after push/PR.
Tasks 1.2 and 2.3 reproduce the CI commands locally as the best available pre-merge gate.