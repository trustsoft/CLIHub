# Process Subsystem Refinement

## Problem

`ProcessLauncher` currently mixes multiple responsibilities (~200+ lines):
- Runtime detection (Windows Terminal/CMD/PowerShell)
- Process spawning (interactive vs captured)
- Output capture and streaming
- Command line building logic

This makes it hard to:
- Test process scenarios in isolation
- Add new runtimes or process modes
- Understand and modify behavior for specific use cases

## Goal

Separate runtime selection, interactive process launching, and output capture into focused components while maintaining backward compatibility.

## Success Criteria

1. ProcessLauncher becomes a thin composition layer
2. Runtime selection logic extracted to dedicated component
3. Interactive and output capture concerns separated
4. All existing tests continue to pass
5. New tests added for each isolated component
6. No breaking changes to public APIs

## Scope

**In Scope:**
- Extract RuntimeSelector for runtime detection and selection
- Create InteractiveProcessRunner for interactive launches
- Create OutputCaptureRunner for output capture
- Update ProcessLauncher to coordinate the new components
- Add comprehensive tests for new components

**Out of Scope:**
- Adding new runtime types
- Changing output capture timeout defaults
- Modifying command line building logic
- UI changes
