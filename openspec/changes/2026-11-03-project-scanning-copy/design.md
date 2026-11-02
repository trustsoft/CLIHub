# Design: revise the Project Scanning page copy

## Copy decisions

| Area | Before | After | Rationale |
| --- | --- | --- | --- |
| Page description | `Watched directories, scanning rules, and the background watcher.` | `Watched folders, scan rules, and watcher status.` | Shorter; "watcher status" names the card the page actually shows. |
| Header actions | `Add Directory` / `Add a watched root`; `Scan Now` / `Rescan every watched root` | `Add watched folder` / `Add a watched folder`; `Rescan all watched folders` / `Rescan all watched folders` | One vocabulary; each accessible name matches its tooltip. |
| Watched section | `WATCHED REPOSITORY DIRECTORIES`; `N roots monitored` | `WATCHED FOLDERS`; `N folders watched` | Removes two redundant qualifiers and the root/directory split. |
| Immediate-apply note | `Scanning changes apply immediately. Save and Cancel only affect the other settings.` | `Changes apply immediately. Save and Cancel only affect the other pages.` | The note applies to the whole page, so it moves under the page header and stops repeating "scanning". |
| Row chips | `Scan depth for this folder`; `What counts as a repository in this folder` | `Folder scan depth`; `What counts as a repository` | The chip is visibly inside a folder row; the deictic "this/here" is noise and reads badly out of context. |
| Add row | `Enter absolute directory path (e.g., C:\repos)…`; `Register` | `Absolute folder path, e.g. C:\repos`; `Add` | The example already gives the format; "Add" matches the other list actions. |
| Duplicate error | `That folder is already monitored.` | `This folder is already watched.` | Uses the page's verb and points at the entered folder. |
| Rules section | `DISCOVERY & SCANNING RULES` | `SCANNING RULES` plus one line: `Depth and discovery are defaults for new folders; each row can override them.` | Discovery is part of scanning; the default/override fact is stated once instead of in two hints. Ignored patterns are global and are not covered by "each row". |
| Depth card | `Repository scan depth`; `Default depth for a directory you add from now on. Each row can override it.` | `Scan depth`; `Levels below a watched folder to search.` | The section line owns the default/override rule; the hint explains the value. |
| Discovery card | `Repository discovery`; long 154-character hint | `Discovery`; `Git repositories, AI agent folders (.claude, .opencode), or both.` | Keeps both agent-folder examples and all three options; 154 → 66 characters. |
| Ignored card | `Ignored directory patterns`; `Folders skipped while scanning. Globs are matched case-insensitively; '*' matches any characters.`; badge `Globs supported` | `Ignored patterns`; `Folder names skipped in every scan. Case-insensitive; * is a wildcard.` | "Glob" becomes "wildcard"; the badge repeated the hint and is removed. |
| Validation | `Repository scan depth must be …` | `Scan depth must be …` | Matches the shortened field label. |

## Rejected changes

- **`DAEMON` → `WATCHER`.** Rejected during review. `WATCHER / Watching` is tautological, and
  "watcher" describes only one of the card's states (`Scanning`, `Idle`, `Off`, `Error`). `DAEMON`
  names the whole background process and is familiar to the developer audience.
- **`Levels below this folder to search`.** Rejected because "this" is a deictic that only works
  while the row is visible and reads poorly to a screen reader. Replaced with `Folder scan depth`.
- **`What counts as a repository here`.** Same reason; replaced with `What counts as a repository`.
- **Renaming `Repository scan depth` / `Repository discovery` / `Ignored directory patterns` without
  a spec delta.** Rejected: those strings are quoted in `preferences-ui`, so the change carries a
  delta for the three labels.

## Specification impact

- `preferences-ui` — the "Projects and Paths page options" requirement quotes the three field
  labels; update the quotes and the `Options offered` scenario. Behavior is unchanged.
- `settings-theme` — the "Persistent page header" scenario quotes the section header and the first
  scrollable element; update both to `WATCHED FOLDERS` and the immediate-apply note.
- `docs/glossary.md` keeps "watched root" as the concept name; the UI now calls the same object a
  watched folder.

## Non-goals

- No change to configuration keys, `ScanningPreferences`, the scanner, defaults, option names, or
  persistence.
- No change to the Projects page (its `Register` button and placeholder are a separate surface).
- No new badge, tooltip, or control.
