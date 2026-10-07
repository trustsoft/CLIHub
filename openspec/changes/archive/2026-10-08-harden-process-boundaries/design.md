## Context

`IInteractiveProcessRunner` exposes interactive launch plus runtime selection, while `IProcessOutputRunner` captures output. `IProcessLauncher` composes both for the concrete Windows implementation.

## Decision

When output capture is cancelled, `ProcessLauncher` kills the entire process tree, waits for exit with a bounded cleanup wait, and returns `Process cancelled`. When only the internal timeout fires, it returns `Timed out waiting for the command`. Runtime selection remains on the interactive contract, and command-line construction is tested directly through `WindowsCommandLineBuilder`.
