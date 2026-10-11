## Context

See `proposal.md` — Why. Current pages are `StackPanel`s of muted `FieldLabel` + control + `HintText` (for example `SettingsGeneralPage.xaml`, `SettingsTerminalPage.xaml`). `SettingsStyles.xaml` (app-level merged) holds the keyed styles (`PageHeader`, `FieldLabel`, `HintText`, `SegmentGroup`, `SettingsInput`, `SettingsCheck`, `NeutralButton`, `StatusLine`, `SettingsList`) and already merges `Controls.xaml` for the palette/sizing tokens. The architecture boundary test restricts `System.Windows` usage to `Views/`, `Converters/`, `Interop/`, `Hotkeys/`, `ViewModels/` and a few named files, so a new control must live under `Views/`.

## Goals / Non-Goals

**Goals:**

- One reusable card primitive shared by all converted pages, so the card look cannot drift.
- Card layout applied to the six simpler pages with page descriptions and section headers.
- No change to controls, bindings, or Save/Cancel.

**Non-Goals:**

- Projects & Paths and CLI Agents (a follow-up change), Apply/status footer, search, badges.

## Decisions

### D1 — A `SettingCard` `ContentControl` with the full parameter set

Add `src/CLIHub/Views/SettingCard.cs` as `sealed class SettingCard : ContentControl` with DPs: `Title` (string), `Hint` (string), `Icon` (string glyph, optional leading icon), `Badge` (string, optional), `BadgeTone` (`SettingBadgeTone`: `Neutral`/`Accent`/`Success`), `Placement` (`SettingCardPlacement`: `Right` default, `Stacked`), and `Grouped` (bool: no own surface; a row inside a `SettingsGroup`), plus the inherited `Content`. An implicit style in `SettingsStyles.xaml` targets the type. **Why:** a ContentControl with a template keeps code-behind empty and one place holds the card visual; these parameters cover every composition in the target mockups. **Alternative rejected:** `HeaderedContentControl` with `Tag` for the hint — abuses `Tag`; per-page `<Border>` markup — no slots and lots of duplication.

### D2 — One leading header + one content host, repositioned by `Placement`

The template is a `Border` containing a two-column/two-row `Grid`: the header (`Icon` block + `Title` + optional `Badge` + `Hint`) in row 0 / column 0, and a single `ContentPresenter` host. A trigger on `Placement=Stacked` moves the host to row 1 spanning both columns with a top margin and `Stretch`. When `Grouped` is true, the `Border` drops its own chrome so the row sits on the group surface. **Why:** one template serves both layouts with one content presenter. **Alternative rejected:** separate templates — duplication.

### D3 — `SettingsGroup` renders a shared surface with hairline separators

Add `src/CLIHub/Views/SettingsGroup.cs` as an `ItemsControl` whose template is a rounded bordered surface containing an `ItemsPresenter`; its item container carries a top hairline that is removed for the first item (the container style keys off `ItemsControl.AlternationIndex` with a large `AlternationCount`). Child `SettingCard`s use `Grouped=true`. **Why:** reproduces the target's grouped surface (test3) while keeping standalone cards (test2) available. **Alternative rejected:** a manual `StackPanel` with hand-edited separators — repetitive and fragile.

### D4 — Section headers and page descriptions

Add a **`SectionHeader`** control (`Text` + optional `ShowMarker`) rendering an uppercase muted label with an optional accent marker; it has no trailing caption. Page descriptions use the existing `HintText` under the existing `PageHeader`. **Why:** reuses the established type rhythm and matches the target. **Alternative rejected:** a casing converter — labels are written uppercased.

### D5 — Each setting row gets a leading icon

Populate `Icon` for every setting on the converted pages using `IconGlyphs`, adding glyph constants where needed. **Why:** the target rows are icon-led; it improves scannability. **Alternative rejected:** icon-less rows — less faithful to the target.

### D6 — Convert the six simpler pages now

Apply the card layout to General & Startup, Hotkeys & Quick Launcher, Terminal Profiles, Appearance, Telemetry & Logs, and Updates. Leave Projects & Paths and CLI Agents for a follow-up change (they carry list editors, watcher status, metrics, and nested agent cards that need their own treatment). **Why:** keeps each slice reviewable and de-risks the data-heavy pages.

### D7 — Re-layout only; keep every control and binding

Selectors keep `SegmentGroup`/`SegmentItem`, inputs keep `SettingsInput`, checkboxes keep `SettingsCheck`, lists keep `SettingsList`; only their container changes to the card (right or stacked). **Why:** behavior and tests stay intact; the change is visual.

## Risks / Trade-offs

- **Wide controls overflow on a narrow window** → default wide cards to `Stacked` so controls take the full card width; segmented groups keep their existing wrapping/scroll behavior.
- **Vertical density** → cards add padding; acceptable for a settings surface and consistent with the target.
- **Custom control styling** → an implicit style for `SettingCard` in the app-level dictionary is required for the template to apply; verified in smoke.

## Migration Plan

- None; presentational only. No stored preference or API changes.

## Open Questions

- Exact section grouping and description copy per page are fixed in the tasks; adjust during implementation if a grouping reads awkwardly.
