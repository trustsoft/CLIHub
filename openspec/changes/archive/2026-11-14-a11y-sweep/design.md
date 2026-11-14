# Accessibility Sweep Design

## Verified Current State

### Problem 1: Decorative glyphs exposed to assistive technology

**Evidence: 20 decorative-glyph TextBlock sites across templates and pages**

- `src/CLIHub/Themes/Common/SettingsStyles.xaml:228-233` — SettingCard template IconBox TextBlock with `Text="{TemplateBinding Icon}"` adjacent to Title
- `src/CLIHub/Themes/Common/SettingsStyles.xaml:418-423` — SummaryCard template IconBox TextBlock with `Text="{TemplateBinding Icon}"` adjacent to Label
- `src/CLIHub/Views/SettingsWindow.xaml:104` — Status chip glyph TextBlock with `Style="{StaticResource StatusChipGlyph}" Text="{Binding UpdateCard.Glyph}"` adjacent to Badge text
- `src/CLIHub/Views/SettingsWindow.xaml:297-302` — Sidebar page-item TextBlock with `Text="{Binding Icon}"` FontSize 13, adjacent to Title TextBlock
- `src/CLIHub/Views/SettingsWindow.xaml:359-365` — Page-header emblem TextBlock with `Text="{Binding SelectedPage.Icon}"`, adjacent to page title. Existing comment (352-358) already records WPF's TextBlockAutomationPeer behavior.
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:23-26` — Reset button TextBlock with `Text="{x:Static themes:IconGlyphs.Refresh}"` adjacent to label "Reset" (lines 27-30)
- `src/CLIHub/Views/SettingsAppearancePage.xaml:103-109` — Custom-theme swatch TextBlock with `x:Name="CustomGlyph" Text="{x:Static themes:IconGlyphs.Palette}"` adjacent to theme name
- `src/CLIHub/Views/SettingsProjectsPage.xaml:49-54` — Watched-folder row emblem TextBlock with `Text="{x:Static themes:IconGlyphs.FolderOpen}"` adjacent to path text (Grid.Column="0" paired with path in Column="1")
- `src/CLIHub/Views/SettingsProjectsPage.xaml:133-138` — Add-watched-folder input TextBlock glyph with `Text="{x:Static themes:IconGlyphs.Add}"` adjacent to TextBox (lines 140+)
- `src/CLIHub/Views/SettingsProjectsPage.xaml:283-287` — Add-ignored-pattern input TextBlock glyph with `Text="{x:Static themes:IconGlyphs.Add}"` adjacent to TextBox (line 288+)
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:55-60` — Add-project input TextBlock glyph adjacent to TextBox
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:114-119` — Project emblem TextBlock adjacent to project name
- `src/CLIHub/Themes/Common/SettingsControls.xaml:277-282` — Checkmark TextBlock in toggle template with `Text="{x:Static themes:IconGlyphs.Checkmark}"` paired with content Text in the template
- `src/CLIHub/Themes/Common/SettingsControls.xaml:415-420` — ComboBox chevron TextBlock with `Text="{x:Static themes:IconGlyphs.ChevronDown}"` adjacent to selected content
- `src/CLIHub/Themes/Common/SettingsControls.xaml:552-559` — Menu chevron TextBlock with `x:Name="Chevron" Text="{x:Static themes:IconGlyphs.ChevronDown}"` adjacent to Content text (line 551)
- `src/CLIHub/Views/SettingsAgentsPage.xaml:66-72` — Actions menu button glyph TextBlock in ActionsMenuActionTemplate with `Text="{Binding Glyph}"` adjacent to label
- `src/CLIHub/Views/SettingsAgentsPage.xaml:144-147` — Detect button glyph TextBlock with `Text="{x:Static themes:IconGlyphs.Refresh}"` adjacent to label "Detect"
- `src/CLIHub/Views/SettingsAgentsPage.xaml:160-163` — Check updates button glyph TextBlock with `Text="{x:Static themes:IconGlyphs.Sync}"` adjacent to label "Check updates"
- `src/CLIHub/Views/LaunchWindow.xaml:54-60` — Actions menu button glyph TextBlock in ActionsMenuActionTemplate with `Text="{Binding Glyph}"` adjacent to label (line 61)
- `src/CLIHub/Views/LaunchWindow.xaml:71-77` — Actions menu toggle glyph TextBlock in ActionsMenuToggleTemplate with `Text="{Binding Glyph}"` adjacent to label (line 78)

**Non-decorative glyphs (not converted):**
- `src/CLIHub/Views/WindowHeaderBar.xaml` maximize/restore/close buttons: icon-only with `ToolTip` and no adjacent text. Element carries the label itself.
- `src/CLIHub/Views/SettingsWindow.xaml` action buttons (116-118, 135-137, 146-148, etc.): icon-only buttons with `ToolTip` and `AutomationProperties.Name` already set.
- `src/CLIHub/Views/SettingsProjectsPage.xaml:244-256` ignored-pattern chip remove button: icon-only button with `ToolTip="Remove pattern"` and `AutomationProperties.Name="Remove pattern"` (line 248).
- Launch window footer buttons (437-472): `FolderOpen`, `Settings`, `Pin`/`Pinned`, `Exit` — icon-only buttons with `ToolTip`s and no adjacent labels. **Note:** These footer buttons fall outside scope of this change per the brief.

### Problem 2: Actions-menu entries lack accessible names and tooltips

**Evidence:**

- `src/CLIHub/ViewModels/Shared/MenuAction.cs` (lines 1-50): Public properties are `Label` (init, line 14), `Glyph` (init, 19), `Command` (init, 24), `IsCheckable` (init, 29), `IsSeparator` (init, 34), `IsChecked` (line 39-43). No `Tooltip` or `AccessibleName` properties.

- `src/CLIHub/Views/SettingsAgentsPage.xaml:171-173` — ACTIONS toggle: `ToggleButton x:Name="PluginActionsButton" Content="Actions"` with no `AutomationProperties.Name`. Screen readers announce only the glyph or "Toggle button".

- `src/CLIHub/Views/SettingsAgentsPage.xaml:59-74` — ActionsMenuActionTemplate Button: `Click="OnMenuActionInvoked" Command="{Binding Command}"`, content is a StackPanel with glyph TextBlock (64-70, `Text="{Binding Glyph}"`) and label TextBlock (71, `Text="{Binding Label}"`). No `ToolTip` binding, no `AutomationProperties.Name` binding.

- `src/CLIHub/Views/LaunchWindow.xaml:49-64` — ActionsMenuActionTemplate Button: glyph TextBlock (54-60) and label TextBlock (61), no bindings for ToolTip or AutomationProperties.Name.

- `src/CLIHub/Views/LaunchWindow.xaml:66-81` — ActionsMenuToggleTemplate ToggleButton: same structure, no ToolTip or accessible-name bindings.

**Current binding:** Both templates bind only `Content` (or nested TextBlock `Text`) to `Label`; tooltips and accessible names are not bound from MenuAction.

### Problem 3: Ignored-pattern chip gives no hint before reveal

**Evidence:**

- `src/CLIHub/Views/SettingsProjectsPage.xaml:226-268` — Ignored-pattern chip ContentControl ControlTemplate with focusable ChipBorder (line 226, `x:Name="ChipBorder" Focusable="True" FocusVisualStyle="{x:Null}"`), PatternText TextBlock (237-242, `Text="{TemplateBinding Content}"`), RemoveButton (244-256, `Visibility="Hidden"` by default, revealed by IsMouseOver/IsKeyboardFocusWithin triggers at 260-267). The DataContext for the chip is the pattern string (a plain string from IgnoredPatterns ObservableCollection). ChipBorder has no `AutomationProperties.Name`.

**Current state:** Screen reader announces only the pattern text; no hint about the remove affordance or Tab interaction.

## Design Decisions

### Decision 1: DecorativeGlyph as a sealed TextBlock subclass

**Rationale:** Deriving from TextBlock preserves identical visual rendering, font metrics, and all TextBlock-targeted styles (e.g., `StatusChipGlyph`, `FooterIcon`, TemplateBindings). A custom `FrameworkElement` with `FormattedText` risks pixel-level rendering differences. Overriding `OnCreateAutomationPeer()` to return `null` is a sealed method behavior available in TextBlock subclasses; it is cleaner than any attached-behavior or indirect suppression mechanism. The element has no new public properties; it is a sealed override.

**Implementation:**

```csharp
namespace CLIHub.Controls;

using System.Windows.Automation.Peers;
using System.Windows.Controls;

/// <summary>
///   A TextBlock that is hidden from assistive technology.
///   Use this for decorative glyphs that duplicate adjacent text labels.
/// </summary>
public sealed class DecorativeGlyph : TextBlock
{
    /// <summary>
    ///   Creates an automation peer that returns null, excluding this element from the UIA tree.
    /// </summary>
    /// <returns> null. </returns>
    protected override AutomationPeer? OnCreateAutomationPeer() => null;
}
```

**Location:** `src/CLIHub/Controls/DecorativeGlyph.cs` (no theme style needed; inherits all TextBlock properties and styles).

### Decision 2: MenuAction extension with computed fallback property

**Properties to add:**
- `public string? Tooltip { get; init; }` — Optional tooltip for the menu item.
- `public string? AccessibleName { get; init; }` — Optional accessible name for the menu item.
- `public string EffectiveAccessibleName => string.IsNullOrEmpty(this.AccessibleName) ? this.Label : this.AccessibleName;` — Computed property that falls back to Label when AccessibleName is null or empty.

**Bindings in templates:**
- `ToolTip="{Binding Tooltip}"` — WPF handles null gracefully.
- `AutomationProperties.Name="{Binding EffectiveAccessibleName}"` — Always returns a non-null string.

**ACTIONS toggle accessible name:** Add directly to the ToggleButton in SettingsAgentsPage.xaml (line 171): `AutomationProperties.Name="Plugin catalog actions"`.

**Rationale:** A computed property keeps XAML bindings simple (one binding per attribute) and testable logic in the ViewModel. Avoids a MultiBinding converter for a simple fallback operation.

### Decision 3: Pattern-chip accessible name via StringFormat

**Attribute on ChipBorder (line 226):**
```xaml
AutomationProperties.Name="{Binding StringFormat='Ignored pattern {0}, Tab reveals the remove button'}"
```

**Rationale:** The pattern chip's DataContext is a plain string (the pattern itself, bound via `Text="{TemplateBinding Content}"`). WPF's `StringFormat` on a string-valued property works during binding evaluation. This avoids a dedicated converter for a simple string concatenation. The pattern string is substituted into the template, producing the hint that is announced by screen readers.

### Decision 4: Decorative-glyph conversion scope: 20 sites across 8 XAML view files and 2 template styles

**Decorative glyphs converted (20 total):**

| File | Line(s) | Context | Note |
|------|---------|---------|------|
| SettingsStyles.xaml | 228-233 | SettingCard icon template | Replaces `TextBlock` in IconBox |
| SettingsStyles.xaml | 418-423 | SummaryCard icon template | Replaces `TextBlock` in IconBox |
| SettingsWindow.xaml | 104 | Status chip glyph | Replaces `TextBlock` |
| SettingsWindow.xaml | 297-302 | Sidebar page icon | Replaces `TextBlock` |
| SettingsWindow.xaml | 359-365 | Page-header emblem | Replaces `TextBlock` |
| SettingsAgentDetectionPage.xaml | 23-26 | Reset button glyph | Replaces `TextBlock` |
| SettingsAppearancePage.xaml | 103-109 | Custom theme swatch | Replaces `TextBlock` |
| SettingsProjectsPage.xaml | 49-54 | Watched-folder emblem | Replaces `TextBlock` |
| SettingsProjectsPage.xaml | 133-138 | Add-folder input glyph | Replaces `TextBlock` |
| SettingsProjectsPage.xaml | 283-287 | Add-pattern input glyph | Replaces `TextBlock` |
| SettingsProjectRegistryPage.xaml | 55-60 | Add-project input glyph | Replaces `TextBlock` |
| SettingsProjectRegistryPage.xaml | 114-119 | Project emblem | Replaces `TextBlock` |
| SettingsControls.xaml | 277-282 | Checkmark in toggle | Replaces `TextBlock` |
| SettingsControls.xaml | 415-420 | ComboBox chevron | Replaces `TextBlock` |
| SettingsControls.xaml | 552-559 | Menu chevron | Replaces `TextBlock` |
| SettingsAgentsPage.xaml | 66-72 | Actions button glyph | Replaces `TextBlock` |
| SettingsAgentsPage.xaml | 144-147 | Detect button glyph | Replaces `TextBlock` |
| SettingsAgentsPage.xaml | 160-163 | Check updates button glyph | Replaces `TextBlock` |
| LaunchWindow.xaml | 54-60 | Actions button glyph | Replaces `TextBlock` |
| LaunchWindow.xaml | 71-77 | Actions toggle glyph | Replaces `TextBlock` |

**Not converted (out of scope):**
- Icon-only buttons (Windows chrome, action buttons, chip remove button) — keep TextBlocks with proper `AutomationProperties.Name`.
- Launch window footer buttons — outside the stated scope.

## Open Questions

1. **Should `DecorativeGlyph` be public or internal?** — **Recommendation:** Public (no overhead; allows future use in extensibility scenarios).

2. **Pattern-chip wording clarity:** Is "Tab reveals the remove button" clear, or should it be "Press Tab to remove"? — **Recommendation:** "Tab reveals the remove button" is more descriptive of the interaction model; implementation can iterate based on smoke testing feedback.

3. **SettingsAgentsPage ACTIONS toggle:** Should its accessible name be exactly "Plugin catalog actions" or something else? — **Recommendation:** "Plugin catalog actions" clearly describes the menu's function; finalize during implementation.

4. **MenuAction constructor backward compatibility:** Adding init properties does not break existing object-initializer call sites (existing code that constructs `MenuAction { Label = "...", Command = ... }` remains valid). — **Confirmed:** Safe.

5. **Do SettingsControls.xaml chevron glyphs apply to multiple instances (via shared template)?** — Yes (ComboBox template, menu template). Both are converted in one pass.

6. **Are there glyph TextBlocks in WindowHeaderBar.xaml that should be converted?** — No; those are icon-only buttons (maximize, restore, close) with ToolTips and no adjacent labels. The glyph IS the button's label; not decorative.
