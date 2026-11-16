## Context

See `proposal.md` — Why and What Changes.

### Verified current state

**Build warning baseline** (measured 2026-11-16 at commit `6d4f6ba`, clean tree, full rebuild):

```
dotnet build CLIHub.sln -c Release -warnaserror --no-incremental
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

The repository already maintains a zero-warning Release build under a genuine `--no-incremental`
rebuild, not just an incremental one. This is a social discipline, not a
tool-enforced one — `TreatWarningsAsErrors` is absent from `Directory.Build.props` (line 19: only
`EnforceCodeStyleInBuild` is set).

**Style-rule severities** (`.editorconfig`):
- IDE0009 (`dotnet_style_require_accessibility_modifiers` omission, effectively
  `csharp_style_prefer_this_qualifier`): `warning`
- IDE0011 (add braces): `warning`
- IDE0065 (`using` directive placement): `warning`
- IDE0161 (convert to file-scoped namespace): `warning`
- IDE1006 (naming rule violation): `warning`

None of these are `error`. `testing.md:317-323` explicitly states: "`TreatWarningsAsErrors` is not
set, so a style or naming violation is reported as a build warning and does not fail `dotnet build`
or `dotnet test`."

**code-style spec** (`openspec/specs/code-style/spec.md:24-29`): "The style configuration SHALL flag
violations of the project naming conventions." The spec requires flagging, not build-breaking. Local
builds keep warnings as warnings; the spec does not forbid CI from being stricter.

**CI workflow** (`.github/workflows/ci.yml`):
- `build-and-test` job, `windows-latest`, single job
- Step order: Checkout → Check-DevVersion → Setup .NET 10.0.x → Build → Test
- Build: `dotnet build CLIHub.sln -c Release` (no `-warnaserror`) — line 27
- Test: three separate `dotnet test` invocations with `--no-build` — lines 32-34
- No `-warnaserror`, no `--collect`, no `--results-directory`, no `upload-artifact`
- No `continue-on-error` or conditional gates on the test step

**Coverage infrastructure:**
- `coverlet.collector` 10.1.0 is referenced by all three test projects
  (`tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj:14`,
  `tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj:14`,
  `tests/CLIHub.Tests/CLIHub.Tests.csproj:15`)
- Version is centrally managed: `tests/Directory.Packages.props:15`
- No `.runsettings` or `.coverlet` configuration file exists
- `artifacts/` is in `.gitignore:2` — coverage results under `artifacts/coverage/` will be ignored

**Architecture-boundary test** (`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:147-154`):
`CiWorkflow_RunsBothTestProjects` reads `.github/workflows/ci.yml` as text and asserts all three
`.csproj` file names appear. The changed workflow must keep those three names.

**Backlog entry** (`docs/backlog.md:273-285`): two-part proposed remedy:
- (a) `-warnaserror` or narrow `WarningsAsErrors` to categories at zero
- (b) collect and publish coverage as artifact/trend, no hard threshold; 80% gate rejected for a WPF
  codebase

**Release pipeline** (`.github/workflows/release.yml` — not the target of this change) runs its own
build + test independently. This change does not modify the release pipeline, but coverage and
warning enforcement there are an open question for a follow-up.

## Decisions

### Decision 1: `-warnaserror` on the CI build command (not a narrow `WarningsAsErrors` list)

**Chosen:** add `-warnaserror` to the CI `dotnet build` step: `dotnet build CLIHub.sln -c Release -warnaserror`.

**Rationale:**

1. **Matches the existing discipline.** The repository already ships 0 warnings (measured). Making CI
   enforce it turns the social contract into a tool-enforced gate without changing local developer
   behavior.

2. **CI-only scope.** `-warnaserror` is a CLI flag on one `dotnet build` invocation; it does not
   change `Directory.Build.props`, `TreatWarningsAsErrors`, or `.editorconfig` severities. Local
   `dotnet build CLIHub.sln` and `dotnet test` continue to report warnings without failing. CI is the
   gate.

3. **Future SDK analyzer surprises are manageable, not prevented.** A new SDK that introduces an
   analyzer warning the team hasn't triaged would make CI red. The remedy is
   `-warnaserror-:CSXXXX` or `-warnaserror-:IDEXXXX` on the build step to exclude specific codes,
   not pre-emptive narrowing that misses real regressions. Pre-emptively narrowing to "categories the
   repository already keeps at zero" is circular when all categories are at zero — there is no subset
   to enumerate. CI going red is the signal that a new warning category needs a decision; the
   `-warnaserror-:` escape hatch keeps it from being a blocker.

4. **Precedent.** The `-warnaserror` convention is standard across .NET CI pipelines; it is what the
   .NET SDK team itself recommends for CI gating style and quality.

5. **Does not conflict with the code-style spec.** The spec requires violations to be *flagged*, not
   to be build-breaking. CI failure is flagging (the reviewer sees it), and the spec's language was
   written for local builds (`testing.md:317-323` is explicit about this: "`TreatWarningsAsErrors` is
   not set, so a style or naming violation … does not fail `dotnet build` or `dotnet test`"). The
   change does not touch `TreatWarningsAsErrors`.

**Alternatives considered:**

- **A — Narrow `WarningsAsErrors` in `Directory.Build.props` (rejected).** Set
  `<WarningsAsErrors>CS;IDE0009;IDE0011;IDE0065;IDE0161;IDE1006</WarningsAsErrors>` (or equivalent)
  in the root props file. Pros: applies to every project automatically; no CLI flag to forget.
  Cons: (1) affects local builds — crosses the boundary the team deliberately set between local
  flagging and CI enforcement; (2) requires enumerating every IDE code the repo uses, which drifts
  with each rule addition; (3) does not cover future CS warnings from SDK updates, which is exactly
  the gap the backlog entry wants to protect against. Rejected because local-build impact is not
  desired, and the narrow list is maintenance-heavy for the same coverage.

- **B — `.editorconfig` severity changes (rejected).** Change IDE0009, IDE0011, IDE0065, IDE0161,
  IDE1006 from `warning` to `error` in `.editorconfig`. Pros: targets only the style rules the team
  already flags. Cons: affects every build (local and CI), changes the role of `.editorconfig` from
  "flag" to "fail", and the change is harder to scope-exclude for specific future analyzer codes.
  Rejected for the same local-build boundary concern.

- **C — Both `-warnaserror` and `TreatWarningsAsErrors` (rejected).** Over-engineered; combining
  them creates ambiguity about which mechanism controls what, and the local-build impact of
  `TreatWarningsAsErrors` is explicitly unwanted.

### Decision 2: `--collect:"XPlat Code Coverage"` with per-project `--results-directory`, single `upload-artifact`

**Chosen:** each `dotnet test` invocation adds `--collect:"XPlat Code Coverage" --results-directory
artifacts/coverage/<ProjectName>`; a single `actions/upload-artifact@v7` step after tests, with
`if: always()` and `if-no-files-found: ignore`, collects `artifacts/coverage/**/*.cobertura.xml`
into one artifact named `coverage-results`, default 90-day retention (GitHub Actions default for
public repos). No threshold gate, no badge, no merging step.

The CLI shape per test project:

```
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj -c Release --no-build --collect:"XPlat Code Coverage" --results-directory artifacts/coverage/Core.Tests
dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj -c Release --no-build --collect:"XPlat Code Coverage" --results-directory artifacts/coverage/Core.Windows.Tests
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj -c Release --no-build --collect:"XPlat Code Coverage" --results-directory artifacts/coverage/CLIHub.Tests
```

The upload-artifact step:

```yaml
- name: Upload coverage
  if: always()
  uses: actions/upload-artifact@v7
  with:
    name: coverage-results
    path: artifacts/coverage/**/*.cobertura.xml
    if-no-files-found: ignore
```

**Rationale:**

1. **No new packages needed.** `coverlet.collector` 10.1.0 is already referenced. `--collect:"XPlat
   Code Coverage"` is a `dotnet test` built-in that uses the installed collector; no additional
   MSBuild or NuGet dependency.

2. **Per-project directories prevent filename collisions.** Each test project generates a
   `cobertura.xml` inside a GUID-named subfolder under the results directory. Separate top-level
   directories keep the files distinguishable without a post-processing merge step.

3. **`if: always()` ensures coverage is uploaded even when tests fail.** A failing test run still
   produces partial coverage data — valuable for diagnosing what was exercised before the failure.

4. **Single artifact, flat glob.** The glob `artifacts/coverage/**/*.cobertura.xml` collects all
   three projects' XML files into one artifact. No third-party merge action, no `reportgenerator`, no
   `dotnet-coverage merge`. The implementer can add merging as a follow-up if needed.

5. **No threshold.** The backlog explicitly rejects an 80% gate for a WPF codebase. Coverage is an
   observable signal the reviewer can consult; the CI run is green or red based on build and tests
   alone.

6. **`actions/upload-artifact@v7`** — the current latest major. The GitHub API `tags` for
   `actions/upload-artifact` lists v7.0.2 (with v7.0.1, v7.0.0, v6.0.0, v5.0.0 below it), verified
   2026-11-16. This matches the currency of the repo's other workflows (`actions/checkout@v7`,
   `actions/setup-dotnet@v6`). No third-party coverage action needed.

7. **`if-no-files-found: ignore`** — when the `Build` step fails, the `Test` step is skipped (GitHub
   Actions skips downstream steps on a failed step by default), so no `*.cobertura.xml` files exist.
   Without `if-no-files-found: ignore`, the always-run upload would fail a second time (or warn) on
   an already-red run. With it, the upload degrades to a clean no-op when there is nothing to upload.

**Alternatives considered:**

- **A — `--collect:"Code Coverage"` (Windows-only, `.coverage` binary format) (rejected).** Produces
  `.coverage` files that need Visual Studio or `dotnet-coverage` to read. `XPlat Code Coverage`
  produces `cobertura.xml`, which is human-readable and tool-agnostic. Rejected because the
  cross-platform format is more useful to a team that may review coverage outside Visual Studio.

- **B — `reportgenerator` or `dorny/test-reporter` action (rejected).** Adds a third-party
  dependency for PR comment decoration. The brief says "avoid third-party coverage actions." The
  implementer can add a trend report as a follow-up if the team wants it.

- **C — Single `dotnet test CLIHub.sln` with `--collect` (rejected).** Consolidates the three
  invocations into one. Pros: less YAML. Cons: the three-project split is pinned by
  `CiWorkflow_RunsBothTestProjects`; a solution-level `dotnet test` would still name the test
  projects somewhere, and the per-project `--results-directory` pattern is clearer about which
  coverage file belongs to which project. Rejected as an unnecessary restructuring outside the
  scope.

- **D — `--results-directory` omitted, coverage files under `TestResults/` (rejected).** Without
  `--results-directory`, coverage files land in each test project's output directory, which is
  `artifacts/<project>/Release/<tfm>/TestResults/<guid>/`. The glob would be harder to write and
  the path is coupled to the Build.props `BaseOutputPath`. An explicit `--results-directory` under
  `artifacts/coverage/` is independent and simpler to reason about.

### Decision 3: Keep the three separate test invocations, one workflow step each

**Chosen:** the three `dotnet test` invocations stay separate, one per test project, and each runs as its own
workflow step (`Test CLIHub.Core.Tests`, `Test CLIHub.Core.Windows.Tests`, `Test CLIHub.Tests`) with
`--no-build` preserved. No `continue-on-error`, no consolidation.

**Rationale:**
- `CiWorkflow_RunsBothTestProjects` (ArchitectureBoundaryTests.cs:147-154) asserts all three project
  file names appear in `ci.yml`. Consolidating changes the assertion.
- Per-project invocations make it immediately clear which test project failed from the job log.
- No restructuring is needed beyond adding the coverage arguments and the step boundaries.

**Why separate steps — a spec-conformance gap, not style (measured 2026-11-16 on the implementation
machine):**

```
pwsh -NoProfile -Command "cmd /c exit 1; cmd /c exit 0"                  -> $LASTEXITCODE = 0
pwsh -NoProfile -Command "$ErrorActionPreference='stop'; $PSNativeCommandUseErrorActionPreference=$true; cmd /c exit 1; cmd /c exit 0"
                                                                        -> exit 1 (NativeCommandExitException)
```

GitHub's `pwsh` shell sets `$ErrorActionPreference = 'stop'` and appends an `exit $LASTEXITCODE` check, but it
does not set `$PSNativeCommandUseErrorActionPreference`, so a native command's non-zero exit is non-terminating
and only the last command in a `run:` block decides the step. With all three `dotnet test` invocations in one
`pwsh` block, a failure in `CLIHub.Core.Tests` or `CLIHub.Core.Windows.Tests` followed by a passing
`CLIHub.Tests` reported a green step — contradicting the `ci-build` delta ("SHALL fail when … any test fails")
and its "Failing test on a pull request" scenario. The three-in-one-block shape predates `ci-quality-gates`, but
this change is what promises the failure semantics, so the split ships with it: a failing step fails the job by
default, and `Upload coverage` keeps `if: always()` so a failed run still publishes what it collected.

**Alternatives considered:**

- **A — One `pwsh` block with `$PSNativeCommandUseErrorActionPreference = $true` as its first line
  (rejected).** It works locally, but it makes the gate depend on PowerShell 7.3+ native-command error
  semantics in the runner image, which GitHub does not pin through the shell name. Separate steps need no such
  dependency and keep the three invocations visible as three named steps.
- **B — One `dotnet test CLIHub.sln` invocation (rejected).** Still a non-goal: it collapses the per-project
  signal and the three project-name lines the architecture test reads.

## Consequences

- A PR that introduces a new warning (compiler, analyzer, or style rule at `warning` severity) now
  fails CI instead of passing silently.
- Coverage data is available as a downloadable artifact on every CI run for PR review; there is
  still no automated threshold comparison.
- The local developer workflow (`dotnet build`, `dotnet test`) is unchanged — warnings are still
  flagged, not fatal.
- Future SDK updates that introduce new analyzer warnings will turn CI red. The remedy
  (`-warnaserror-:CSXXXX`) is documented in `design.md` and the CI workflow comment.

## Resolved Open Questions

1. **Release pipeline warning/coverage parity — resolved: out of scope, follow-up as a backlog
   entry.** The release pipeline (`.github/workflows/release.yml`) runs its own `dotnet build` at
   line 29 without `-warnaserror` and `dotnet test` at line 32 without `--collect`. This change does
   not touch the release pipeline; instead, a new backlog entry (`docs/backlog.md`) records the gap
   with the proposed remedy of mirroring the CI gates there, triggered by the next release-pipeline or
   release-preparation change.

2. **Coverage merge step — resolved: three per-project cobertura files in one artifact.** The
   artifact `coverage-results` contains three separate `cobertura.xml` files under distinct subfolders
   (`Core.Tests/`, `Core.Windows.Tests/`, `CLIHub.Tests/`). No `dotnet-coverage merge` step is added.
   The per-project files are already an observable signal; merging can be added as a follow-up if
   needed.

3. **Coverage retention — resolved: keep the action default (90 days).** The default is reasonable
   for PR review and trend-watching. No custom `retention-days` pin is set.