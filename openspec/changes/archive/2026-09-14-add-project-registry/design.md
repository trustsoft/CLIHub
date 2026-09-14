## Context

See `proposal.md` — Why. The skeleton left `ConfigStore` read-only, and the popup
receives a snapshot of `Config` at startup (`PopupViewModel`), adding a synthetic
current-directory project when `config.json` has none. `Config` currently models
only `schemaVersion`/`runtime`/`hotkey`/`projects`; unknown JSON fields are dropped
on load and would be lost on the first write. `IFileSystem` exposes only
read/exists/enumerate, so there is no way to replace a file atomically yet.

## Goals / Non-Goals

**Goals:**

- Persist user configuration safely and make the project registry usable end to end.
- Keep project domain rules separate from file I/O so they are testable without a
  real filesystem.
- Avoid silently destroying a hand-edited or corrupt `config.json`.

**Non-Goals:**

- Editing a project's name or logo after it is added (deferred to the settings window).
- Logo auto-resolution.
- The full future `config.json` schema (`probe`, `update`, `agents`).

## Decisions

### D1. Thin `ConfigStore` + `ProjectRegistry`

`ConfigStore` keeps only I/O (`Load`, `Save`, corrupt detection/backup). A new
`ProjectRegistry` holds the in-memory `Config` and implements add/remove/validation,
calling `ConfigStore.Save`.

- **Why:** domain rules stay testable in isolation; `ConfigStore` remains the single
  writer that the future probe round can also route through.
- **Alternative rejected:** a fat `ConfigStore` owning project rules (mixes domain
  with I/O).

### D2. Safe persistence

- Unknown fields are preserved with `[JsonExtensionData]` on `Config` rather than by
  pre-modeling `probe`/`update`/`agents`.
- Before overwriting a `config.json` that fails to parse, the current bytes are
  copied to `config.json.bak`.
- `Save` writes to a temporary file in the same directory and replaces the target
  via `IFileSystem.Move(source, destination, overwrite: true)`.

- **Why:** no data loss for unknown or manually edited keys, no clobbering of a
  broken file, and no observable partial write.
- **Alternatives rejected:** model the full schema now (speculative); accept field
  loss; plain `WriteAllText` (not atomic).

### D3. `IFileSystem` addition

Add `void Move(string source, string destination, bool overwrite)`. Backup needs no
new primitive (read + `WriteAllText`).

### D4. `ProjectRegistry` surface

```
Load once -> hold Config
Projects : IReadOnlyList<ProjectConfig>
Add(path)    -> ProjectAddResult (Success, Project, Error)
Remove(id)   -> bool
```

- On add: validate, assign a new `Guid` id, set name from the folder name, append,
  then `Save`. A rejected add returns an error string and does not persist.
- Validation: path exists and is a directory; path is not already registered
  (case-insensitive); resulting name is non-empty.

### D5. UI wiring

- The **Projects** pane header gets an `Actions` menu: **Add** and **Remove**.
- **Add** uses `Microsoft.Win32.OpenFolderDialog` (WPF, .NET 8+, no WinForms
  dependency); on success the new project is added to the pane's
  `ObservableCollection` and selected.
- **Remove** asks for confirmation, then removes the selected project.
- The synthetic current-directory fallback is deleted; an empty registry renders an
  empty state that prompts to add a project.
- Validation/launch errors keep surfacing through the footer status text.

### D6. Single writer

All mutations go through `ProjectRegistry` -> `ConfigStore.Save` on the UI thread.
The popup reads from the same registry instance, so there is one in-memory `Config`
and one writer.

## Risks / Trade-offs

- [`[JsonExtensionData]` round-trip with `PropertyNamingPolicy = CamelCase`] -> verify
  in a unit test that unknown keys survive a load/save cycle.
- [Backup overwrites a previous `.bak`] -> acceptable for v1; a single most-recent
  backup is enough to recover.
- [Folder picker on the UI thread while the popup is transient] -> the popup hides on
  focus loss; opening the dialog is modal and owned by the popup, so it must not hide
  the popup while the dialog is open (verify manually).
- [Concurrent writers later (probe)] -> the single-writer rule must be kept; the probe
  round will call `ConfigStore.Save` through the same path.

## Migration Plan

Not applicable: `config.json` may not exist yet; a missing file yields defaults and is
created on the first successful save.

## Open Questions

- Renaming a project and logo resolution — a later change.
- When `probe`/`update`/`agents` sections arrive, they will be modeled explicitly and
  written through `ConfigStore`.
