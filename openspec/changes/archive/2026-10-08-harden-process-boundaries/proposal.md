## Why

The process layer already exposes separate interactive and output runner ports, but cancellation previously collapsed timeout and caller cancellation into one result and the termination behavior was implicit. Command-line construction was also tested only through the launcher facade.

## What Changed

- Caller cancellation now terminates the entire process tree and reports `Process cancelled` distinctly from timeout.
- Interactive and output runner contracts remain independent.
- Command-line builder behavior has direct tests for quoting, escaping, and captured command construction.
- Existing launch, output capture, timeout, and error semantics remain unchanged outside cancellation reporting.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This was an internal process-boundary hardening change.
