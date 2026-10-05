## Why

`App.OnStartup` currently owns hotkey preference parsing, fallback selection, and Win32 hotkey registration inline. Extracting this sequence gives startup orchestration a focused boundary and makes invalid-configuration behavior testable without constructing the WPF application.

## What Changes

- Add a startup hotkey registrar that consumes the loaded application preferences.
- Preserve parsing of valid configured combinations and fallback to `Ctrl+Shift+A` for invalid values.
- Preserve registration through the existing global hotkey service.
- Add a narrow registration seam so the startup workflow can be tested independently of WPF window handles.
- Replace the inline hotkey parsing and registration code in `App.OnStartup`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal refactor; existing hotkey support and application lifecycle requirements remain unchanged.

## Impact

- Affected runtime code: `App.xaml.cs`, WPF hotkey composition, and the new startup registrar.
- Affected tests: focused valid/invalid preference and composition tests.
- No change to hotkey syntax, default combination, Win32 registration behavior, settings UI, or Core APIs.
- OpenSpec specs are intentionally skipped because no observable requirement changes.
