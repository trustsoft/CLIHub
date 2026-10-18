## 1. Row summary data

- [x] 1.1 Add a discovered-project count query beneath a root to `IProjectScanner` and implement it through the project service
- [x] 1.2 Expose per-root counts from the settings view model, refreshed when the watched list changes and whenever the scanner status changes
- [x] 1.3 Shorten the scan summary label from "recursive depth" to "depth"
- [x] 1.4 Rename the discovery options to "Git only", "Agents only", and "Git and agents", and rewrite the Repository discovery hint with concrete agent-marker examples

## 2. Row presentation

- [x] 2.1 Add a multi-value converter that joins the scan summary with the per-row repository count, using singular/plural wording
- [x] 2.2 Bind the row secondary line to the summary and the per-root count; keep the path, actions, and layout unchanged

## 3. Verification

- [x] 3.1 Update the scan-summary tests for the "depth" label and add view-model tests for per-root counts
- [x] 3.2 Add a scanning-service test for the count query
- [x] 3.3 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; run `openspec validate --all`
- [x] 3.4 Confirm each row shows its own repository count after a scan
