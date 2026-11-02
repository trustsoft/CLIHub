# Tasks: Shorter section labels on the Launch Window page

## Stage 1: Labels

- [x] 1.1 In `src/CLIHub/Views/SettingsLaunchWindowPage.xaml`, rename the `LAUNCHER POSITIONING` section header to `POSITION` and the `PATHS` section header to `PROJECT PATHS`
- [x] 1.2 In `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`, replace the em dash in the popup position and path display option labels with an en dash (`Active monitor – centered`, `Left trim – keep the end of the path`, and so on)

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 2: Spec delta

- [x] 2.1 Write `openspec/changes/launch-window-section-labels/specs/preferences-ui/spec.md` with MODIFIED "Popup screen position" (header `POSITION`) and "Path display preference" (header `PROJECT PATHS`), keeping every original scenario name

Gates:

```powershell
openspec validate --all
```

## Verification

- [x] Build passes: `dotnet build CLIHub.sln`
- [x] Tests pass: `dotnet test CLIHub.sln`
- [x] OpenSpec validation passes: `openspec validate --all`
- [x] Smoke: the Launch Window page shows the sections `POSITION` and `PROJECT PATHS`, with the same cards and stored values, and the selector options use the shorter dash
