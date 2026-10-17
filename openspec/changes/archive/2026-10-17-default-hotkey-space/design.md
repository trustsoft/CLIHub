# Design: Default hotkey becomes Ctrl+Shift+Space

## Context

The hotkey is stored as a human-readable string in `preferences.hotkey`. Three layers currently duplicate the
default: `HotkeyParser.Default` (parsed fallback), `AppPreferences.Hotkey` (new configuration objects), and
`AppConfigDocument.PreferencesDocument.Hotkey` (a stored document that omits the property). The parser already
recognizes `Space` as a canonical named key mapped to `0x20`, so no parsing, capture, or registration logic needs
to change.

## Decisions

- **Change the three defaults together.** A partial change would make new installs, stored documents, and invalid
  values disagree. The startup warning should also derive its text from `HotkeyParser.Default` so it cannot drift.
- **Do not migrate existing values.** `preferences.hotkey` is an explicit user choice once written. Rewriting it
  would silently override a configured shortcut. Existing installs keep `Ctrl+Shift+A` until the user changes it.
- **Keep `VK_SPACE` with two modifiers.** `Ctrl+Shift+Space` satisfies the existing modifier requirement and is
  accepted by `RegisterHotKey`. If another application owns the combination, the existing conflict path logs a
  warning and the tray icon remains available.
- **Update the Settings hint only.** The capture field, parser, formatter, and named-key aliases already support
  `Space`; the message now lists it to make that support discoverable.

## Risks

- `Ctrl+Shift+Space` may be claimed by another application. The existing graceful degradation and the Settings
  hotkey editor provide a recovery path.
- The Settings window and README show the combination as illustrative; both are updated with the default.
