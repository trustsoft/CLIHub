# Design: Launch Window Settings Page Copy Revision

## Verified Current State

All strings exist at the stated locations with exact wording:

1. **Page description** (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:52`):
   ```
   "The global hotkey, where the launch window appears, and how project paths are shortened in it.",
   ```

2. **Global hotkey hint** (`src/CLIHub/Views/SettingsLaunchWindowPage.xaml:13`):
   ```xml
   Hint="Press a combination in the field. Esc cancels capture."
   ```

3. **Launcher window position hint** (`src/CLIHub/Views/SettingsLaunchWindowPage.xaml:39`):
   ```xml
   Hint="Where the launch window appears when it is shown."
   ```

4. **Path display hint** (`src/CLIHub/Views/SettingsLaunchWindowPage.xaml:56`):
   ```xml
   Hint="How long project paths are shortened in the launch window."
   ```

5. **Hotkey error — no modifier** (`src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs:54`):
   ```csharp
   viewModel.SetHotkeyError("Hotkey must include at least one modifier (Ctrl, Shift, Alt, Win).");
   ```

6. **Hotkey error — unsupported key** (`src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs:61`):
   ```csharp
   viewModel.SetHotkeyError("Unsupported key. Use a letter, digit, F1-F24, or a named key (Space, Enter, Tab, Escape, arrows, and so on) with a modifier.");
   ```

### Unchanged Similar Wording

Three locations contain similar wording but are not changed because they serve different purposes:

1. **`ui/mockups/README.md:38-39`** - quotes "How long project paths are shortened in the launch window." and "Press a combination in the field. Esc cancels capture." as a snapshot description of the historical `settings.png` render (v0.9.0). This is documentation of what the image shows, not documentation of the current UI. Updating the description would make it disagree with the rendered image it describes.

2. **`src/CLIHub.Core/Models/Configuration/AppConfig.cs:139`** - XML doc comment for the `PathDisplayStyle` property: "How long project paths are shortened in the launch window: 'leftTrim' (default) keeps the end of the path, 'middleEllipsis' keeps both ends." This documents the configuration property's purpose and is not UI copy.

3. **`src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs:4`** - XML doc comment for the `PathDisplayStyle` enum: "How long project paths are shortened for display in the launch window." This documents the type's purpose and is not UI copy.

These three locations document the *preference* or *image content*, not the UI strings shown to users in the Settings window. They remain unchanged.

## Decisions

### 1. Replace page description with shorter enumeration

**Chosen:** Change the page description from "The global hotkey, where the launch window appears, and how project paths are shortened in it." to "Global hotkey, launcher position, and path display."

**Rationale:** The old description recapitulated the three section headers (`ACTIVATION`, `POSITION`, `PROJECT PATHS`) using nearly identical wording but longer phrasing. The new description names the same three areas more concisely, keeping "global" and "launcher" because they add meaning, and dropping "in it" and "how project paths are shortened" which duplicated the path display card hint.

**Alternative rejected:** Variant B (empty description) — removes the description entirely, relying on the three section headers to convey page content. Rejected because the Settings window design pattern includes a page description for every page, and removing it would make the page header visually sparse. Variant C (remove all hints too) was rejected for the same reason and because it loses the Esc cancel instruction and the "Active monitor" clarification.

### 2. Shorten hotkey hint by removing redundant location

**Chosen:** Change "Press a combination in the field. Esc cancels capture." to "Press a combination. Esc cancels capture."

**Rationale:** "in the field" points to the only control in the card row and adds no information. "Capture" is retained because without it, "Esc cancels" reads as "Esc closes the window" rather than "Esc cancels capture and reverts to the previous value."

**Alternative rejected:** "Include Ctrl, Shift, Alt, or Win. Esc cancels capture." (9 words) — states the modifier rule before the error, but the error message already explains the rule, and the hint should not duplicate error-state documentation.

### 3. Replace position hint with "Active monitor" definition

**Chosen:** Change "Where the launch window appears when it is shown." to "Active monitor is the one under the pointer."

**Rationale:** The old hint restated the card title ("Launcher window position") without adding context. Two of the four ComboBox options begin with "Active monitor", and that term is not explained anywhere else. The new hint defines "Active monitor" at nearly the same length but now informs the user.

**Alternative rejected:** "Uses the monitor under the pointer." (6 words) — shorter, but the subject is implied. '"Active" is the monitor under the pointer.' (7 words) — explicitly references the option label with quotes, but reads more technically.

### 4. Replace path hint with scope and condition

**Chosen:** Change "How long project paths are shortened in the launch window." to "Long paths are shortened in project rows."

**Rationale:** "How long" is imprecise — the option selects the shortening method, not a length. "In the launch window" names the page itself. The new hint states the scope (project rows) and the condition (only long paths are shortened), which were not previously communicated.

**Alternative rejected:** "Applies to paths too long for a row." (8 words) — emphasizes the condition ("too long") over the action ("shortened"). The chosen variant keeps the action as the subject because that is what the ComboBox options describe (Left trim / Middle ellipsis).

### 5. Shorten no-modifier error to imperative form

**Chosen:** Change "Hotkey must include at least one modifier (Ctrl, Shift, Alt, Win)." to "Add a modifier: Ctrl, Shift, Alt, or Win."

**Rationale:** The imperative form ("Add a modifier") is shorter and more direct than "Hotkey must include at least one modifier." The list of modifiers is preserved because users need to know which keys qualify.

**Alternative rejected:** Keep the old wording — it is correct but longer and more formal. The imperative tone matches the error context (the user just pressed an invalid combination and needs to know what to do).

### 6. Condense unsupported-key error and remove "Escape" example

**Chosen:** Change "Unsupported key. Use a letter, digit, F1-F24, or a named key (Space, Enter, Tab, Escape, arrows, and so on) with a modifier." to "Unsupported key. Use a letter, digit, F1–F24, or a named key (Space, Enter, Tab, arrows), plus a modifier."

**Rationale:**
- Remove "Escape" from the examples — Esc is the cancel key per the card hint, so suggesting it as a hotkey example contradicts the hint.
- Remove "and so on" — the listed examples (Space, Enter, Tab, arrows) sufficiently illustrate "named key."
- Change "with a modifier" to "plus a modifier" — makes the modifier read as a separate condition rather than part of the "named key" list.
- Use en dash (U+2013) in "F1–F24" — matches the project's dash convention per the brief.

**Alternative rejected:** "Use a letter, digit, F-key, or named key with a modifier." (12 words, 64 characters) — shorter, but loses the precision of "F1–F24" and the examples of named keys, which help users understand what qualifies.

### 7. No spec delta for path display requirement wording

**Chosen:** Do not change the `preferences-ui` spec requirement wording "how long project paths are shortened" in this change. Mark `.openspec.yaml` with `skip_specs: true` and an `x-skip_specs_rationale` stating that these are UI copy changes only, not quoted spec requirements.

**Rationale:** The path display requirement (line 131 of `openspec/specs/preferences-ui/spec.md`) states "The Settings window SHALL offer a choice of how long project paths are shortened in the launch window" as a capability statement, not a quoted UI string. The approved variant changes the UI hint but does not alter the contract. Aligning the requirement wording ("how long" → "how") would be a separate editorial change to the spec, not an implementation task.

**Alternative rejected:** Align the requirement wording in this change — the requirement is a capability description, and changing it to match the new UI copy would be out of scope for a copy-only change. Recorded as an open question for future work.

## Open Questions

1. Should the `preferences-ui` spec requirement wording "how long project paths are shortened" also be updated to "how project paths are shortened" to align with the new hint, as a separate editorial change to the spec? (The requirement is a capability statement, not a quoted UI string, so the current wording remains technically correct.)

## Deviations

None. All changed strings are UI copy, and no specification quotes them.
