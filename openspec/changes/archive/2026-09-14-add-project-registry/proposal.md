## Why

The skeleton can launch an agent, but there is no way to register a project:
`config.json` is read-only and the popup falls back to the current directory.
Without a persisted project registry the core loop — pick a project, launch an
agent in it — is not usable.

## What Changes

- `ConfigStore` becomes read+write and the single writer of `config.json`,
  preserving unknown fields, backing up a corrupt file before overwriting, and
  writing atomically.
- New `ProjectRegistry` (domain logic): add a project from a folder (name defaults
  to the folder name), remove a project, and validate entries (path exists and is
  a directory, no duplicate path, non-empty name).
- Popup **Projects** pane gains an `Actions` menu: **Add** (folder picker) and
  **Remove** (with confirmation); a newly added project becomes selected.
- The synthetic current-directory fallback is removed; an empty registry shows an
  explicit empty state prompting to add a project.
- Logo resolution on add is **not** included (deferred to a later change).

## Capabilities

### New Capabilities
- `app-config`: reading and writing `config.json`, including unknown-field
  preservation, corrupt-file backup, and atomic replacement.
- `project-registry`: adding and removing projects and validating project entries.

### Modified Capabilities
- `popup`: the **Projects** pane gains an `Actions` menu (Add/Remove) and an
  explicit empty state instead of the synthetic fallback.

## Impact

- Core: `ConfigStore` gains write support; new `ProjectRegistry`; `IFileSystem`
  gains `Move`/`Replace` for atomic writes.
- App: popup XAML/ViewModel for the `Actions` menu, `Microsoft.Win32.OpenFolderDialog`
  folder picker, and a confirmation dialog for remove.
- `%AppData%\CLIHub\config.json` starts being written by the application.
- `Config` model gains unknown-field preservation; full future schema
  (`probe`/`update`/`agents`) is still out of scope.
