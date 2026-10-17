## Context

The Appearance page has roughly 590px of content width at the default settings size and about 370px
at the minimum width. The former segmented selector only showed labels, so the palette could not be
recognized without applying it. The Stitch Appearance target presents each accent as a card with a
color dot, name, and hex value.

## Goals / Non-Goals

**Goals**

- Make every accent recognizable before saving through a color dot and hex label.
- Keep the selected card obvious with the shared accent ring and a check inside the dot.
- Keep the custom value independent of named presets while the application runs.
- Add hues that stay legible on the dark chrome while remaining usable on the light palette.

**Non-Goals**

- No persistence of the last custom accent across application restarts; it is runtime UI memory.
- No change to how the accent is applied or stored as the active preference.
- No new accent-related configuration keys.

## Decisions

- **Preview cards in a three-column grid.** Three columns keep names and hex values readable at the
  minimum window width; the cards reuse the theme picker's chrome, hover, and selection ring.
- **The active color name is shown as a badge.** It mirrors the Stitch "Active: …" chip and reuses
  the existing `SettingCard` badge slot.
- **The custom field is conditional.** It is collapsed while a named preset is selected, and its
  row is bound to `SelectedAccent.IsCustom`.
- **The last custom accent lives in a singleton `AccentSelectionMemory`.** The Settings window and
  its view model are recreated when the window closes; a singleton keeps the custom value available
  on the next open without adding a configuration field.
- **Preset contrast drives the color choices.** Each added hue was checked against the dark card
  surface (`#26262C`, priority) and the light surface (`#F3F3F5`). Teal and Tangerine are balanced
  for both themes; Indigo is a bright periwinkle-leaning hue that glows on the dark chrome while
  staying distinguishable from Blue and Purple.

## Risks / Trade-offs

- Indigo is tuned for the dark palette and has lower contrast on light surfaces (about 2.7:1); as a
  filled accent it remains usable, and the dark theme keeps the priority.
- The runtime custom memory is not persisted across restarts; the saved accent is still restored.
