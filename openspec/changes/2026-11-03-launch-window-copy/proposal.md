# Proposal: Launch Window Settings Page Copy Revision

## Why

The Settings → Launch Window page currently contains duplicative and over-long copy. The page description recapitulates the three section headers using nearly identical wording, and two of the three card hints merely restate their card titles without adding new information:

- **Page description:** "The global hotkey, where the launch window appears, and how project paths are shortened in it." (16 words, 94 characters) — enumerates the three sections below with the same terms, just longer.
- **Launcher window position hint:** "Where the launch window appears when it is shown." (9 words, 49 characters) — restates the card title "Launcher window position" without adding context.
- **Path display hint:** "How long project paths are shortened in the launch window." (10 words, 58 characters) — names the context twice ("project paths", "in the launch window") and says "how long" when the choice actually selects the shortening method, not a length.
- **Global hotkey hint:** "Press a combination in the field. Esc cancels capture." (9 words, 54 characters) — points to "the field" when there is only one control in the row.

The two hotkey validation error messages are the longest text strings on the page (66 and 124 characters):

- **No modifier:** "Hotkey must include at least one modifier (Ctrl, Shift, Alt, Win)." (11 words, 66 characters)
- **Unsupported key:** "Unsupported key. Use a letter, digit, F1-F24, or a named key (Space, Enter, Tab, Escape, arrows, and so on) with a modifier." (22 words, 124 characters) — includes "Escape" as an example even though Esc is the cancel key per the card hint.

**Current total:** 44 words and 258 characters for description+hints; 33 words and 191 characters for the two errors.

## What

Replace six UI strings with shorter, non-duplicative variants (variant A from the approved HTML mockup):

1. **Page description** (line 52 in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`):
   - From: `The global hotkey, where the launch window appears, and how project paths are shortened in it.`
   - To: `Global hotkey, launcher position, and path display.`

2. **Global hotkey card hint** (line 13 in `src/CLIHub/Views/SettingsLaunchWindowPage.xaml`):
   - From: `Press a combination in the field. Esc cancels capture.`
   - To: `Press a combination. Esc cancels capture.`

3. **Launcher window position card hint** (line 39 in the same file):
   - From: `Where the launch window appears when it is shown.`
   - To: `Active monitor is the one under the pointer.`

4. **Path display card hint** (line 56 in the same file):
   - From: `How long project paths are shortened in the launch window.`
   - To: `Long paths are shortened in project rows.`

5. **Hotkey error — no modifier** (line 54 in `src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs`):
   - From: `Hotkey must include at least one modifier (Ctrl, Shift, Alt, Win).`
   - To: `Add a modifier: Ctrl, Shift, Alt, or Win.`

6. **Hotkey error — unsupported key** (line 61 in the same file):
   - From: `Unsupported key. Use a letter, digit, F1-F24, or a named key (Space, Enter, Tab, Escape, arrows, and so on) with a modifier.`
   - To: `Unsupported key. Use a letter, digit, F1–F24, or a named key (Space, Enter, Tab, arrows), plus a modifier.`
   
   Note: F1–F24 uses en dash (U+2013), matching the project's dash convention.

**New total:** 28 words and 180 characters for description+hints (−36% words, −30% characters); 26 words and 148 characters for the two errors (−21% words, −23% characters).

## Impact

Three source files change:

- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` (page description)
- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` (three card hints)
- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs` (two error strings)

Plus the new change directory:

- `openspec/changes/2026-11-03-launch-window-copy/` (this proposal, design, tasks, and `.openspec.yaml`)

No other files change: section headers (`ACTIVATION`, `POSITION`, `PROJECT PATHS`), card titles, ComboBox option labels, layout, bindings, icons, and behavior remain unchanged.

## Non-goals

- Changing section headers or card titles — `openspec/specs/preferences-ui` pins them (three-section order scenario, "Launcher window position" selector named literally).
- Changing ComboBox option labels — the spec pins the "\<monitor\> – \<placement\>" pattern and the "Left trim"/"Middle ellipsis" names.
- Spec delta for the path display requirement wording ("how long" → "how") — left as an open question; the changed hint is UI copy, not a quoted spec requirement.
- README, documentation, or CHANGELOG updates — behavior is unchanged. The mockup README (`ui/mockups/README.md`) quotes similar wording, but it describes the historical `settings.png` image (v0.9.0), not the current UI, and updating it would make the description disagree with the image. XML doc comments in `AppConfig.cs` and `PathDisplayStyle.cs` document the preference, not UI copy, and remain unchanged.
- Test changes — no test asserts these strings (`SettingsPageMetadataTests` only checks page keys and header template slots; confirmed by searching test files).

## Dependencies

None. The concurrent `general-startup-copy` change edits a different line of `SettingsNavigation.cs` (the "General & Startup" page description), so there is no merge-order dependency.
