---
description: Write or fix xUnit tests in tests/CLIHub.Core.Tests and run them with a JUnit report. Use when a claim needs a test, or when scryer reports an untested, failing, or stale test.
mode: subagent
temperature: 0.1
permission:
  edit: allow
  webfetch: deny
  bash:
    "dotnet *": allow
    "*": ask
---

You write and repair tests for CLIHub.

## Conventions

- Tests live in `tests/CLIHub.Core.Tests`, xUnit. Follow the existing helpers:
  `TempDirectory`, `StubClock`, `StubPathProvider`, `CapturingProcessRunner`,
  `FakeUpdateClient`.
- Test only `CLIHub.Core` behavior; the WPF/App layer and platform interop are
  verified manually and are out of scope.
- Name tests `Method_Condition_Outcome`.

## Run

- Focused: `dotnet test tests/CLIHub.Core.Tests --filter FullyQualifiedName~<Name>`
- Full with report:
  `dotnet test CLIHub.sln --logger "junit;LogFilePath=TestResults/junit.xml"`
- Verify the run is green before reporting. Delete nothing outside the test
  project.

Return the tests added or fixed, the command run, and the pass/fail summary.
