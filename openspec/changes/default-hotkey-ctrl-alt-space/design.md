# Design: Default hotkey becomes Ctrl+Alt+Space

## Context

The hotkey is stored as a human-readable string in `preferences.hotkey`. Three layers currently duplicate the default: `HotkeyParser.Default` (parsed fallback), `AppPreferences.Hotkey` (new configuration objects), and `AppConfigDocument.PreferencesDocument.Hotkey` (a stored document that omits the property). The parser already recognizes `Space` as a canonical named key mapped to `0x20`, and `Alt` is a recognized modifier, so no parsing, capture, or registration logic needs to change.

## Current State

**HotkeyParser.Default** (src/CLIHub.Core/Hotkeys/HotkeyParser.cs:12-13):
```csharp
public static HotkeyDefinition Default { get; } =
    new(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x20);
```
The XML comment on line 10-11 says "The default hotkey: Ctrl+Shift+Space (virtual key 0x20)."

**AppPreferences.Hotkey** (src/CLIHub.Core/Models/Configuration/AppConfig.cs:38):
```csharp
public string Hotkey { get; set; } = "Ctrl+Shift+Space";
```
The XML comment on line 36 says "Global hotkey combination (e.g., "Ctrl+Shift+Space")."

**PreferencesDocument.Hotkey** (src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:104):
```csharp
public string Hotkey { get; set; } = "Ctrl+Shift+Space";
```

**HotkeyStartupRegistrar** (src/CLIHub/Startup/HotkeyStartupRegistrar.cs:41-44):
The invalid-configuration warning hard-codes a message but formats `HotkeyParser.Default` dynamically:
```csharp
this.logger.LogWarning(
    "Invalid hotkey '{Hotkey}' in config; using default {DefaultHotkey}",
    preferences.Hotkey,
    HotkeyParser.Format(HotkeyParser.Default));
```

**Test: Format_Default_IsCanonical** (tests/CLIHub.Core.Tests/Hotkeys/HotkeyParserTests.cs:218):
```csharp
Assert.Equal("Ctrl+Shift+Space", HotkeyParser.Format(HotkeyParser.Default));
```

**Test: AppConfigSerializationTests** (tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs:69, 81):
```csharp
Assert.Equal("Ctrl+Shift+Space", new AppConfig().Preferences.Hotkey);
// ...
Assert.Equal("Ctrl+Shift+Space", config.Preferences.Hotkey);
```

**Settings UI validation** (src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs:61):
The error message lists `Space` among the accepted keys but does not mention the default combination:
```csharp
viewModel.SetHotkeyError("Unsupported key. Use a letter, digit, F1–F24, or a named key (Space, Enter, Tab, arrows), plus a modifier.");
```
No change is required here; the message already accepts `Space`.

**Documentation**:
- `README.md:59` — "Global hotkey (`Ctrl+Shift+Space` by default)"
- `README.md:87` — "Open CLIHub from the tray icon or press `Ctrl+Shift+Space`."
- `docs/glossary.md:124` — "Global hotkey — a configurable key combination (default `Ctrl+Shift+Space`)"
- `docs/architecture/configuration.md:6` — "`hotkey` (`Ctrl+Shift+Space` by default)"
- `docs/images/tools/Capture-ReadmeImages.ps1:417` — `hotkey = 'Ctrl+Shift+Space'` in the mock config

**OpenSpec**: `openspec/specs/hotkey-support/spec.md` references the default in three requirement scenarios (lines 11, 21, 41).

## Decisions

- **Change the three defaults together.** A partial change would make new installs, stored documents, and invalid values disagree. The startup warning already derives its text from `HotkeyParser.Default` so it cannot drift.
- **Do not migrate existing values.** `preferences.hotkey` is an explicit user choice once written. Rewriting it would silently override a configured shortcut. Existing installs keep `Ctrl+Shift+Space` until the user changes it. This follows the migration precedent in `openspec/changes/archive/2026-10-17-default-hotkey-space/`.
- **Keep `VK_SPACE` with two modifiers.** `Ctrl+Alt+Space` satisfies the existing modifier requirement and is accepted by `RegisterHotKey`. If another application owns the combination, the existing conflict path logs a warning and the tray icon remains available.
- **Update documentation, tests, and the mock config.** The README, glossary, architecture doc, screenshot harness mock config, and OpenSpec spec all reference the default and must stay aligned with the code.
- **No Settings UI change required.** The capture field and error messages do not hard-code the old default; they already accept `Space` and list it in the validation hint.

## Alternatives Rejected

- **Ctrl+Alt+A** — using `A` loses the mnemonic for "launcher" / "quick access" that `Space` provides.
- **Ctrl+Win+Space** — the `Win` modifier is less commonly used in user-defined shortcuts and may conflict with OS-level shortcuts.
- **Keeping Ctrl+Shift+Space** — does not address the conflict potential with existing application shortcuts.

## Risks

- `Ctrl+Alt+Space` may be claimed by another application. The existing graceful degradation (conflict logging, tray icon availability) and the Settings hotkey editor provide a recovery path.
- The README and documentation show the combination as illustrative; all references are updated with the default.
- No migration means existing users keep their configured value until they change it. This is consistent with how preferences work elsewhere.

## Open Questions

None. The parser, registration, and Settings capture logic already support the new combination.
