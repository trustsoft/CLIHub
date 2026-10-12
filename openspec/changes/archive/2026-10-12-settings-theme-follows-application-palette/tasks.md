## 1. Spec delta

- [x] 1.1 Write the `settings-theme` delta correcting the input, footer, status, error, sidebar, and card requirements to the active palette.
- [x] 1.2 Merge the delta into `openspec/specs/settings-theme/spec.md`.
- [x] 1.3 Confirm no dark-only wording remains except the palette definition in the purpose and chrome requirement.

## 2. Repository documents

- [x] 2.1 `docs/glossary.md`: remove the `init` command, correct the runtime list, the favorite/recent wording, and the preferences note.
- [x] 2.2 `README.md`: correct the runtime, theme, and Requirements statements.
- [x] 2.3 `docs/architecture/startup.md`: use `startMinimized` instead of `ShowWindowOnStartup`.
- [x] 2.4 `docs/releasing.md`: use the unbracketed `## <version> — <date>` changelog heading.
- [x] 2.5 `docs/adr/0001-core-ui-boundaries.md`: replace "Windows Terminal spawning" with "external terminal spawning".

## 3. Verification

- [x] 3.1 Scan `openspec/specs/`, `docs/`, and `README.md` for `init`, `Windows Terminal`, `wt.exe`, `ShowWindowOnStartup`, and bracketed changelog headings.
- [x] 3.2 Confirm no `src/` or `tests/` file changed.
- [x] 3.3 Archive this change to `openspec/changes/archive/2026-10-12-settings-theme-follows-application-palette/`.
