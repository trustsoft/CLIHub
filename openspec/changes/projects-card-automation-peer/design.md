# Design

## Context

See `proposal.md — Why` for motivation.

`SummaryCard` (`src/CLIHub/Controls/SummaryCard.cs:42`) is a `Control` subclass with
`Focusable=False` (template: `src/CLIHub/Themes/Common/SettingsStyles.xaml:354`) and no
`OnCreateAutomationPeer` override (grep of `src/CLIHub` confirmed: zero matches). A bare `Control`
does not create an automation peer by default: `UIElement.OnCreateAutomationPeer` returns null and
`Control` does not override it. With no peer the card has no UIA element at all — its template
`TextBlock`s can appear in the raw tree, but they report `IsControlElement=False`/
`IsContentElement=False` because the card boundary is absent from the sensible tree. UIA inspection
on 2026-11-08 confirmed zero `SummaryCard` elements in the raw tree and all card text reporting
`IsControlElement=False`/`IsContentElement=False`; the control and content views — the views Narrator
uses — therefore exclude the whole card.

Dependency properties on `SummaryCard` used by the peer:
- `Label` (`string`, line 53) — card caption, e.g. `"PROJECTS"`.
- `Value` (`string`, line 59) — primary value, e.g. `"14"`.
- `Detail` (`string?`, line 77) — optional detail, e.g. `"In the launch window"`.
- `IsBadgeVisible` (`bool`, line 101) — master badge gate.
- `BadgeTooltip` (`string?`, line 95) — full status description, e.g.
  `"1 folder missing · folders that no longer exist"`.
- `BadgeText` (`string?`, line 89) — compact chip label, e.g. `"1 missing"`; fallback when tooltip
  is empty.

Test harness: `RunOnSta` helper in `tests/CLIHub.Tests/Support/TestHelpers.cs:26` runs code on an
STA thread; existing `SummaryCardTests` at line 8 already uses it. Test project has `UseWPF=true`
(`CLIHub.Tests.csproj:8`), so `UIElementAutomationPeer` and `FrameworkElementAutomationPeer` are
available without extra references.

## Goals / Non-Goals

Goals:
- Expose `SummaryCard` in the UIA control view and content view so Narrator announces it in
  browse/scan mode.
- Compose a useful accessible name from the card's existing properties without a new ViewModel
  property or XAML attribute at call sites.
- Keep the card out of the tab order (`Focusable=False` unchanged).

Non-goals: See `proposal.md — Non-goals`.

## Decisions

### D1 — `FrameworkElementAutomationPeer` as the base class

`FrameworkElementAutomationPeer` (not `UIElementAutomationPeer`) is the correct base for a
`FrameworkElement` subclass. It has a constructor that accepts the owner element and forwards to
`UIElementAutomationPeer`. `ControlAutomationPeer` was considered but requires a `Control` parameter
and carries default overrides for `IsControlElement`/`IsContentElement` that are not needed here.
`FrameworkElementAutomationPeer` gives a clean slate with no unwanted default behavior.

### D2 — `AutomationControlType.Group` vs. `Text`

`Group` is chosen. A summary card is a read-only grouping of a label, a value, a detail, and an
optional status — semantically a labelled container of related data, not a plain string. `Text`
would expose the composed name as a text run with no indication that it is a card boundary. Narrator
announces `Group` type as "group" after the name (e.g. "PROJECTS 14, In the launch window, 1 folder
missing · folders that no longer exist group"), which signals a semantic boundary to the user.
`Text` was considered and rejected: it gives no grouping cue and is semantically incorrect for a
composite element.

### D3 — `GetNameCore()` composition

The name is composed at peer-query time from the owner's dependency properties:
1. If `AutomationProperties.Name` is explicitly set on the owner (non-null, non-empty), return it
   unchanged — this is the WPF convention for explicit overrides and is tested by a future caller
   who sets the property directly.
2. Otherwise compose the name:
   - join `Label` and `Value` with a single space into one leading part (skip empty members);
   - append `Detail` and, when `IsBadgeVisible` is true, the first non-empty of `BadgeTooltip` /
     `BadgeText`, as separate parts;
   - join the parts with `", "`.
3. Return the joined string, or `string.Empty` if all parts are empty.

This avoids adding a new ViewModel property (no `AutomationLabel`), no XAML change at call sites,
and produces the composed name exactly once when Narrator queries it. The separator choice `", "`
inserts a natural pause in TTS (comma followed by space), which Narrator respects.

Example with badge: `"PROJECTS 14, In the launch window, 1 folder missing · folders that no longer
exist"`. Without badge: `"PROJECTS 14, In the launch window"`.

### D4 — `GetHelpTextCore()` left as default (empty)

`GetHelpTextCore` is not overridden. The badge tooltip content already appears in `GetNameCore()`
when the badge is visible. Duplicating it in `HelpText` would cause Narrator to read the status
twice. The `AutomationProperties.HelpText` set on the chip's inner `TextBlock` in the template
(archive task 4.1) still fires in the raw view for direct element inspection tools like Inspect.exe.

### D5 — Peer defined in the same file as the control

`SummaryCardAutomationPeer` is a `private sealed` nested class inside `SummaryCard.cs`. This keeps
the peer internal to the control — no other file needs access to it, and a separate file would add a
new file for a ~30-line class with no independent consumers.

### D6 — `IsControlElementCore` and `IsContentElementCore` both return `true`

Both return `true` to place the card in both the UIA control view (structural navigation) and the
content view (content-only navigation). Narrator uses both views. Making `IsContentElementCore`
return `false` (content view only) or `IsControlElementCore` return `false` (not navigable) would
leave gaps in one of the two navigation modes.

### D7 — Test strategy: `UIElementAutomationPeer.CreatePeerForElement`

Tests call the static factory `UIElementAutomationPeer.CreatePeerForElement(card)` on an STA thread
without loading a template. This creates the peer via the same code path WPF uses at runtime (calls
`OnCreateAutomationPeer`). No template load is needed because `GetNameCore`, `GetClassNameCore`, and
`GetAutomationControlTypeCore` read dependency properties only, not template-generated named parts.
`IsControlElementCore` and `IsContentElementCore` do not touch the visual tree either. If template
loading were required for any reason, `card.ApplyTemplate()` could be called first — but it is not
needed here and would add complexity without benefit.

## Risks / Trade-offs

- **Narrator ordering** — the composed name reads the badge tooltip after the detail. If a future
  card has a very long detail and a long tooltip, the combined name may be verbose. Acceptable for
  now; a future caller can set `AutomationProperties.Name` explicitly to override (D3 step 1).
- **`string.Empty` return from `GetNameCore`** — if all properties are empty (a card with no label,
  value, detail, or badge), the peer returns `string.Empty`. WPF UIA infrastructure silently accepts
  an empty name; Narrator may announce only the control type. No card in the current UI hits this
  case, but it is a defined edge case.

## Open Questions

None. The approach, API surface, and test strategy are fully resolved by the verified evidence in the
brief and the source inspection above.
