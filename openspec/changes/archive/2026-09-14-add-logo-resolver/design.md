# Design: add-logo-resolver

## Context

See proposal.md — Why/What. Today `PluginLoader` enumerates plugin
directories but returns bare manifests, discarding the folder; popup item
templates are text-only; `ProjectConfig.Logo` round-trips through config
without a reader. The filesystem abstraction (`IFileSystem`) is enough for
all resolution checks — no process or clock needed this time.

## Goals / Non-Goals

**Goals:**

- Path-level resolution fully in Core, testable with the real filesystem
  against temp directories (project test convention).
- Popup items in both panes show logo + caption; default logo when nothing
  resolves.
- Plugin loading keeps the folder alongside the manifest.

**Non-Goals:**

- Logo-picker UI for projects (future "Edit" flow).
- Tray-menu logos (blocked on the open tray-menu composition question).
- SVG or arbitrary-format support (chain is png/ico/favicon per vision).
- Writing the `logo` field from anywhere (this change only reads it).

## Decisions

### D1: Core resolves paths, App renders pixels

`LogoResolver` returns a file path or `null`; it never touches WPF. The App
side maps `null` (and decode failures) to the bundled default asset. Same
boundary pattern as detection: logic testable in Core, pixels isolated in
App.

### D2: Plugin loader returns a folder-aware wrapper

`PluginLoader.Load()` returns `IReadOnlyList<AgentPlugin>` where
`AgentPlugin(AgentManifest Manifest, string Folder)` replaces the bare
manifest list. Alternative considered — a `[JsonIgnore]` folder property on
`AgentManifest` — rejected: it pollutes the serialized manifest contract
with loader metadata. `PopupViewModel` and `AgentDetector` call sites take
`plugin.Manifest` where they used the manifest directly.

### D3: Override semantics (spec'd): override wins only when the file exists

Relative override paths resolve against the project folder; absolute are
used as-is. A missing override silently falls through to the auto chain —
no user-facing error: a hand-edited config must never break the popup.

### D4: Chain order — png before ico within a name pair

PNG decodes crisply at arbitrary small sizes in WPF; ICO stays in the chain
for compatibility with assets that only ship an icon. Case differences are
covered by the case-insensitive Windows filesystem.

### D5: App-side image cache with bounded decode

One `BitmapImage` per resolved path in a small dictionary, created with
`DecodePixelWidth` set to the list-icon size and
`BitmapCacheOption.OnLoad` (no file locks on project folders). The default
logo is a bundled PNG loaded once via pack URI. A decode failure for any
resolved file falls back to the default — corrupt or exotic images never
blank the list.

## Risks / Trade-offs

- [Corrupt or huge images in project roots] → decode is bounded and
  failures fall back to the default visual.
- [Frequent popup opens re-decode] → per-path cache; invalidation is not
  needed for v1 (restarting the app refreshes; the popup is short-lived).
- [Wrapper changes the loader's public shape] → mechanical update of two
  call sites; loader tests adjusted in the same task.

## Migration Plan

None: no config or manifest format changes.

## Open Questions

- The default logo's actual artwork — any neutral placeholder works; can
  be swapped later without touching specs or code shape.
