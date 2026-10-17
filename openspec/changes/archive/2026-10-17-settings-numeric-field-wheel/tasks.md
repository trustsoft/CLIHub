## 1. Wheel input

- [x] 1.1 Handle `OnPreviewMouseWheel` in `NumericField`, step by the wheel direction and notch count, and consume the event; verify the WPF project compiles.
- [x] 1.2 Reuse the existing clamping so wheel steps cannot leave the field's minimum or maximum and the chip states refresh through `Text` changes.

## 2. Documentation and verification

- [x] 2.1 Document wheel stepping in `docs/architecture/ui.md`.
- [x] 2.2 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; all tests pass.
- [x] 2.3 Run `openspec validate --all`; every spec and change validates.
- [x] 2.4 Archive this change to `openspec/changes/archive/2026-10-17-settings-numeric-field-wheel/` and merge the delta into `openspec/specs/settings-theme/spec.md`.
