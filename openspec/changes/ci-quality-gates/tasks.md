## Stage 1: Warning enforcement in CI

- [ ] 1.1 Edit `.github/workflows/ci.yml:27` — change the `Build` step to
      `dotnet build CLIHub.sln -c Release -warnaserror`.
- [ ] 1.2 Run `dotnet build CLIHub.sln -c Release -warnaserror --no-incremental` locally — confirm
      it matches the measured baseline (0 warnings, 0 errors under a full rebuild), so the CI flag
      cannot fail on the current tree.
- [ ] 1.3 Re-read `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:147-154`
      (`CiWorkflow_RunsBothTestProjects`) — confirm no test project file name was removed by the edit.

## Stage 2: Coverage collection in CI

- [ ] 2.1 Edit `.github/workflows/ci.yml:32-34` — add `--collect:"XPlat Code Coverage"
      --results-directory artifacts/coverage/<ProjectName>` to each of the three `dotnet test`
      invocations (`Core.Tests`, `Core.Windows.Tests`, `CLIHub.Tests`), keeping `-c Release --no-build`.
- [ ] 2.2 Edit `.github/workflows/ci.yml` — add an `Upload coverage` step after the `Test` step that
      uses `actions/upload-artifact@v7` with `name: coverage-results`,
      `path: artifacts/coverage/**/*.cobertura.xml`, `if: always()`, and
      `if-no-files-found: ignore`.
- [ ] 2.3 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj -c Release --no-build
      --collect:"XPlat Code Coverage" --results-directory artifacts/coverage/Core.Tests` locally —
      confirm `cobertura.xml` files are produced under `artifacts/coverage/Core.Tests/` and the tests
      pass. Delete the local `artifacts/coverage/` directory afterward (it is gitignored, but it is a
      build artifact).

## Stage 3: Spec delta review

- [ ] 3.1 Review `openspec/changes/ci-quality-gates/specs/ci-build/spec.md` (the plan-stage delta)
      against the implemented workflow in `.github/workflows/ci.yml` — amend the delta if the
      implementation diverged from the plan (for example if `-warnaserror` or coverage arguments
      differ from what was planned).
- [ ] 3.2 Run `openspec validate --all` — confirm 0 failed (37 items: 36 specs + this change), with
      only the pre-existing `>500 character` and placeholder-Purpose warnings unrelated to this change.

## Stage 4: Documentation and backlog

- [ ] 4.1 Update `docs/testing.md` — the section "What a green build does and does not mean"
      (lines 317-329) says a green build with new warnings still passes CI and the suite result is
      independent of coverage. Amend it: local builds still flag warnings without failing, CI now
      enforces `-warnaserror`, and CI publishes a coverage artifact (still no threshold gate).
- [ ] 4.2 Check `docs/releasing.md` for stale claims about CI gates. Line 10 says "All work is
      merged to `master` with green CI" and line 79 says "wait for CI to go green" — both remain
      accurate under the new stricter CI. No change needed. Record this check result in the task
      output.
- [ ] 4.3 Edit `docs/backlog.md` — mark the entry "CI enforces neither the warning baseline nor
      coverage" (lines 273-285) as resolved by `ci-quality-gates`. Follow the file's existing
      convention: add `**Status: Resolved by ci-quality-gates.**` at the top of the entry. Do not
      delete the evidence text.
- [ ] 4.4 Edit `docs/backlog.md` — add a new entry "Release pipeline does not enforce warnings or
      coverage" with evidence (`.github/workflows/release.yml:29` build without `-warnaserror`,
      `.github/workflows/release.yml:32` test without `--collect`), severity Low, proposed remedy
      (mirror the CI gates — `-warnaserror` on build, coverage collection on test — in the release
      pipeline's `test` job), and trigger (the next release-pipeline or release-preparation change).

## Stage 5: Verification and graph refresh

- [ ] 5.1 Run `dotnet build CLIHub.sln -c Release` — confirm 0 warnings, 0 errors (source untouched;
      this gate verifies the planning-stage measurement still holds).
- [ ] 5.2 Run `dotnet test CLIHub.sln` — confirm all tests pass (the workflow edits do not affect
      local behavior; this confirms the tree is still green before the change is reviewed).
- [ ] 5.3 Run `openspec validate --all` — confirm 0 failed (37 items).
- [ ] 5.4 Run `graphify update .` — refresh the knowledge graph so the workflow edit is reflected.

## Build / Test Gates

Each stage ends with a gate; a failure stops the stage:

1. After 1.1: `dotnet build CLIHub.sln -c Release -warnaserror --no-incremental` (0 warnings, 0 errors).
2. After 2.2: YAML sanity — `ci.yml` names all three test `.csproj` files, the new
   `upload-artifact` step uses `actions/upload-artifact@v7` with `if: always()` and
   `if-no-files-found: ignore`.
3. After 3.1: `openspec validate --all` (0 failed, 37 items).
4. After 4.4: re-read `docs/backlog.md` and `docs/testing.md` — confirm the resolution status line
   and the amended section read correctly.
5. Final: `dotnet build CLIHub.sln -c Release`, `dotnet test CLIHub.sln`, `openspec validate --all`,
   then `graphify update .`.

Note: CI can only be fully exercised by the actual GitHub Actions run, which happens after push/PR.
Tasks 1.2 and 2.3 reproduce the CI commands locally as the best available pre-merge gate.