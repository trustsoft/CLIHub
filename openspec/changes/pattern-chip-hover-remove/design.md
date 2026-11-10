# Design: Pattern Chip Hover-Reveal Remove Button

## Verified Current State (HEAD `37ef2dc`)

### Ignored-Pattern Chip Structure

**File:** `src/CLIHub/Views/SettingsProjectsPage.xaml`  
**Lines:** 215-248

The ignored-pattern chips are rendered inside an `ItemsControl` bound to `Scanning.IgnoredPatterns`, using a `WrapPanel` as the ItemsPanel (`:218`). The DataTemplate (`:222-246`) for each chip:

```xaml
<Border Margin="0,0,8,8"
        Padding="8,4,4,4"
        Background="{DynamicResource ChipBgBrush}"
        BorderBrush="{DynamicResource FieldStrokeBrush}"
        BorderThickness="1"
        CornerRadius="{StaticResource Sizing.CornerRadius.Chip}">
    <StackPanel Orientation="Horizontal">
        <TextBlock VerticalAlignment="Center"
                   FontSize="{StaticResource Sizing.FontSize.Chip}"
                   Foreground="{DynamicResource TextPrimaryBrush}"
                   Text="{Binding}"/>
        <Button Margin="4,0,0,0"
                AutomationProperties.Name="Remove pattern"
                Command="{Binding DataContext.Scanning.RemoveIgnoredPatternCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                CommandParameter="{Binding}"
                Style="{StaticResource ChipRemoveButton}"
                ToolTip="Remove pattern">
            <TextBlock FontFamily="{StaticResource Sizing.FontFamily.Icon}"
                       FontSize="8"
                       Text="{x:Static themes:IconGlyphs.Close}"/>
        </Button>
    </StackPanel>
</Border>
```

- **Border:** Outer chip container with 8px right/bottom margin (for WrapPanel spacing), 8/4/4/4 padding (left, top, right, bottom), ChipBgBrush background, FieldStrokeBrush border, 1px border thickness, and Chip corner radius.
- **StackPanel:** Horizontal layout with TextBlock (pattern text, Chip font size, TextPrimaryBrush) and Button (ChipRemoveButton style, 4px left margin, Close glyph at font size 8).
- **Button dimensions:** 20×20 pixels (from ChipRemoveButton style, see below) plus 4px left margin = 24px total horizontal space when visible.

### ChipRemoveButton Style

**File:** `src/CLIHub/Themes/Common/SettingsControls.xaml`  
**Lines:** 500-530

```xaml
<Style x:Key="ChipRemoveButton" TargetType="Button">
    <Setter Property="Width" Value="20"/>
    <Setter Property="Height" Value="20"/>
    <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border x:Name="Bd"
                        Background="Transparent"
                        CornerRadius="{StaticResource Sizing.CornerRadius.Control}">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource HoverOverlayBrush}"/>
                        <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}"/>
                    </Trigger>
                    <Trigger Property="IsPressed" Value="True">
                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource PressedOverlayBrush}"/>
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter Property="Foreground" Value="{DynamicResource DisabledTextBrush}"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

- Fixed 20×20 size, Hand cursor, Transparent background with hover/pressed overlays.
- Shared by watched-folder row buttons (SettingsProjectsPage.xaml:94, 106), SettingsProjectRegistryPage, and SettingsAgentsPage.
- **Decision:** Do NOT modify this style; it is shared and must remain always-visible for other usages.

### Other ChipRemoveButton Usages (Verified)

**Watched-folder rows (SettingsProjectsPage.xaml:89-111):**  
Two buttons (Rescan, Remove) in a SpacedPanel, each using ChipRemoveButton style with distinct glyphs (Refresh, Delete). Always-visible by design.

**SettingsProjectRegistryPage.xaml:**  
Verified no ignored-pattern chip usage; the page lists projects with action buttons, not chips.

**SettingsAgentsPage.xaml:**  
Verified chips are used for agent state/version, but no ChipRemoveButton on those chips (they are read-only status indicators).

### ViewModel (No Changes Expected)

**File:** `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs`  
**Line:** 620

`RemoveIgnoredPattern(string pattern)` method removes the pattern from the `IgnoredPatterns` collection and applies scanning settings. The Command binding in the Button already uses `RelativeSource AncestorType=UserControl` to reach the ViewModel's `Scanning.RemoveIgnoredPatternCommand`. No ViewModel changes needed.

### WPF Trigger Mechanism (Verified)

**Context7 WPF docs + codebase precedent:**

- `DataTemplate.Triggers` only supports `DataTrigger`, `MultiDataTrigger`, and `EventTrigger` — **NOT** property `Trigger` with `SourceName`.
- `ControlTemplate.Triggers` supports property `Trigger` with `SourceName` (verified in LaunchWindowStyles.xaml:239).
- `Style.Triggers` on an element (e.g., `<Border.Style>` with `<Style.Triggers>`) supports property `Trigger` on the styled element itself (e.g., `<Trigger Property="IsMouseOver" Value="True">`), and can use `Setter TargetName` to affect **named children within a ControlTemplate** — but NOT children in a DataTemplate (no ControlTemplate boundary here).

**Correction:** The inline Style approach for a Border in a DataTemplate can use `<Trigger Property="IsMouseOver">` to set properties on the Border itself, but **cannot use `Setter TargetName`** to affect sibling elements (TextBlock, Button) because those are not inside a ControlTemplate boundary.

**Alternative approaches:**

1. **Grid with layering + Border Style trigger setting Grid child Visibility:** Not possible; Style triggers on the Border cannot target siblings in the same DataTemplate.
2. **ControlTemplate wrapper:** Introduce a custom control (or ContentControl with a ControlTemplate) to wrap the chip's visual tree, so ControlTemplate.Triggers can use `SourceName` and `TargetName`. This adds complexity and a new control definition.
3. **DataTrigger on a wrapper ViewModel property:** Add an `IsHovered` property to a chip ViewModel and bind Visibility to it. Requires ViewModel changes (violates the XAML-only constraint) and wrapper models for simple strings.
4. **EventTrigger + Storyboard:** Use `<EventTrigger RoutedEvent="MouseEnter">` in `DataTemplate.Triggers` with a Storyboard to animate Visibility. However, Visibility is not animatable (it's an enum, not a double), so this would require Opacity animation instead, which doesn't remove the button from layout/tab order.
5. **Attached behavior or code-behind:** Handle MouseEnter/MouseLeave events on the Border in code-behind to toggle Visibility of named children. This violates the XAML-only constraint.

**Decision (revised after WPF constraint verification):**

Use a **ControlTemplate-based approach** to enable `Trigger` with `SourceName` and `TargetName`. Wrap the chip's visual structure inside a `ContentControl` with a custom `ControlTemplate`, so the template's triggers can reference named elements:

- The ContentControl's Content is bound to the pattern string (the DataTemplate's data context).
- The ControlTemplate contains the Border (chip outer), a Grid with the TextBlock (pattern text), a placeholder Border (sizing reserve), and the Button (remove, centered, collapsed by default).
- ControlTemplate.Triggers on the **chip Border's** `IsMouseOver` property use `Setter TargetName` to toggle the TextBlock, placeholder, and Button visibility.

This is the only **pure XAML approach** that works within WPF's trigger system constraints while keeping the chip dimensions stable.

**Alternative (simpler, accepted tradeoff):** Use **MouseEnter/MouseLeave events** in code-behind (2-3 lines per event) to toggle Visibility on named elements. This is the most pragmatic path and is common in WPF applications. The "XAML-only" preference is negotiable if the pure-XAML path is disproportionately complex.

**Open Question for User:** Should we accept the ControlTemplate approach (pure XAML, more verbose) or use code-behind event handlers (simpler, 6 lines of C#)? The latter is standard WPF practice for hover interactions that don't fit Style.Triggers constraints.

## Design Decisions

### Chosen Approach

**Use a ContentControl with a custom ControlTemplate** to wrap each ignored-pattern chip, enabling ControlTemplate.Triggers to reference named elements via `SourceName` and `TargetName`.

**Rationale:**
- Pure XAML solution, no C# code changes.
- ControlTemplate.Triggers support property triggers with SourceName/TargetName, which DataTemplate.Triggers do not.
- The ContentControl is lightweight and does not introduce observable behavior changes beyond the hover effect.
- The pattern string remains the data context, so no ViewModel wrapper is needed.

**Structure:**

```xaml
<ItemsControl.ItemTemplate>
    <DataTemplate>
        <ContentControl Content="{Binding}">
            <ContentControl.Template>
                <ControlTemplate TargetType="ContentControl">
                    <Border x:Name="ChipBorder"
                            Margin="0,0,8,8"
                            Padding="8,4"
                            Background="{DynamicResource ChipBgBrush}"
                            BorderBrush="{DynamicResource FieldStrokeBrush}"
                            BorderThickness="1"
                            CornerRadius="{StaticResource Sizing.CornerRadius.Chip}"
                            Focusable="True"
                            FocusVisualStyle="{x:Null}">
                        <Grid>
                            <!-- Sizing layer: TextBlock only (chip hugs text) -->
                            <TextBlock x:Name="PatternText"
                                       VerticalAlignment="Center"
                                       FontSize="{StaticResource Sizing.FontSize.Chip}"
                                       Foreground="{DynamicResource TextPrimaryBrush}"
                                       Text="{TemplateBinding Content}"/>
                            
                            <!-- Overlay layer: centered remove button -->
                            <Button x:Name="RemoveButton"
                                    HorizontalAlignment="Center"
                                    VerticalAlignment="Center"
                                    Visibility="Hidden"
                                    AutomationProperties.Name="Remove pattern"
                                    Command="{Binding DataContext.Scanning.RemoveIgnoredPatternCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=Content}"
                                    Style="{StaticResource ChipRemoveButton}"
                                    ToolTip="Remove pattern">
                                <TextBlock FontFamily="{StaticResource Sizing.FontFamily.Icon}"
                                           FontSize="8"
                                           Text="{x:Static themes:IconGlyphs.Close}"/>
                            </Button>
                        </Grid>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger SourceName="ChipBorder" Property="IsMouseOver" Value="True">
                            <Setter TargetName="PatternText" Property="Visibility" Value="Hidden"/>
                            <Setter TargetName="RemoveButton" Property="Visibility" Value="Visible"/>
                        </Trigger>
                        <Trigger SourceName="ChipBorder" Property="IsKeyboardFocusWithin" Value="True">
                            <Setter TargetName="PatternText" Property="Visibility" Value="Hidden"/>
                            <Setter TargetName="RemoveButton" Property="Visibility" Value="Visible"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </ContentControl.Template>
        </ContentControl>
    </DataTemplate>
</ItemsControl.ItemTemplate>
```

**Key points:**
- `ContentControl.Content` is bound to the pattern string (the data context).
- `Text="{TemplateBinding Content}"` in the TextBlock reads the pattern string from the ContentControl's Content property.
- `CommandParameter="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=Content}"` passes the pattern string to the RemoveIgnoredPatternCommand (TemplatedParent is the ContentControl, whose Content is the pattern).
- **ChipBorder is keyboard-focusable:** `Focusable="True"` makes the chip reachable via Tab navigation. `FocusVisualStyle="{x:Null}"` suppresses the default dotted focus rectangle — the revealed "×" button serves as the focus indicator, providing a clear visual cue without additional chrome.
- **Constant chip dimensions via Hidden visibility on both children:** Both PatternText (on hover/focus) and RemoveButton (at rest) use `Visibility="Hidden"` to preserve their layout slots in the Grid's measure calculation. `Hidden` elements are invisible but remain in layout measurement, so the Grid measures the max of both children in all states → constant chip size.
  - **Height stability:** RemoveButton defaults to `Visibility="Hidden"` (not `Collapsed`), so the 20×20px button is always measured by the Grid even when invisible at rest. The chip content height is constant at 20px in both at-rest and hovered/focused states. Chip outer height = 20px content + 8px vertical padding + 2px border = 30px (equals the original pre-change chip height).
  - **Width stability:** PatternText uses `Visibility="Hidden"` (not `Collapsed`) on hover/focus, preserving its layout slot. The chip width is determined by max(text width, 20px button width) + 16px horizontal padding + border. For typical pattern text the text is wider, so chip width ≈ text width + 16px padding + border; for very short patterns the chip may be marginally wider than text-only sizing (max-of-both-states, consistent with constant-height goal) — accepted.
- **Hidden element behavior notes:**
  - (a) **Keyboard navigation:** `Visibility="Hidden"` elements are skipped by tab traversal and cannot receive keyboard focus, identical to `Collapsed` behavior. The RemoveButton at rest is not tabbable; the focus/hover triggers set it to `Visible`, making it tabbable. Keyboard flow is unchanged from the Collapsed design.
  - (b) **Hit testing:** Hidden elements are not hit-testable — the invisible at-rest button cannot steal mouse clicks from the chip or underlying elements.
  - (c) **UIA tree presence:** Unlike `Collapsed`, a `Hidden` element remains in the UIA tree as offscreen, providing a mild automation discoverability improvement (screen readers can enumerate the button even when invisible). No regression; acceptable side effect.
  - (d) **Width side effect for short text:** For very short pattern text, the reserved 20px button slot may make the chip slightly wider than text+padding alone. This is max-of-both-states sizing, consistent with the user's accepted constant-height approach.
- **Two independent triggers (OR semantics):** The "×" button reveals on hover OR keyboard focus:
  - `Trigger SourceName="ChipBorder" Property="IsMouseOver"` — reveals button when the mouse is over the chip
  - `Trigger SourceName="ChipBorder" Property="IsKeyboardFocusWithin"` — reveals button when the chip or any descendant (including the RemoveButton itself) has keyboard focus
- **Why `IsKeyboardFocusWithin` on ChipBorder works:** ChipBorder is the focusable element (`Focusable="True"`), so keyboard focus lands on it first. When the user tabs again, focus moves to the RemoveButton (which is now `Visible` and therefore tabbable). `IsKeyboardFocusWithin` remains `True` while focus is on the button (because the button is a descendant of ChipBorder), keeping the button visible. **Pitfall avoided:** Setting `Focusable="True"` on the outer ContentControl would NOT work — focus on a parent is not "within" its child, so ChipBorder.IsKeyboardFocusWithin would not fire.
- Both children are vertically centered: TextBlock has `VerticalAlignment="Center"`, RemoveButton has `VerticalAlignment="Center"` (and `HorizontalAlignment="Center"`), so both center within the Grid's content area.

**Keyboard flow:**
1. User presses Tab → focus lands on ChipBorder → `IsKeyboardFocusWithin` becomes `True` → "×" button reveals (centered, Visibility changes from Hidden to Visible), pattern text hides (Visibility changes to Hidden).
2. User presses Tab again → focus moves from ChipBorder to the RemoveButton itself → `IsKeyboardFocusWithin` remains `True` (button is a descendant of ChipBorder) → "×" stays visible.
3. User presses Enter or Space → RemoveIgnoredPatternCommand fires → pattern is removed from the collection → chip disappears from the WrapPanel → focus moves to the next focusable element in the tab order.
4. User presses Shift+Tab (or tabs away without removing) → focus leaves ChipBorder and its descendants → `IsKeyboardFocusWithin` becomes `False` → "×" becomes Hidden again, pattern text reappears (Visible).

**Edge behaviors (acceptable):**
- Clicking the chip body (not the button) with the mouse focuses ChipBorder → "×" reveals even without hover. This is consistent focus-indicator behavior: the chip is now the focused element, and the "×" is the visual focus indicator.
- After removing a pattern via keyboard, the chip is gone, so focus advances to the next tab stop (the next chip, or the next focusable element after the ignored-patterns list if no chips remain). This is standard WPF focus behavior when the focused element is removed from the visual tree.

### Keyboard Accessibility

**Decision:** The remove button is **keyboard-accessible** — it reveals when the chip receives keyboard focus, using the same visual treatment as hover.

**Implementation:**
- ChipBorder (the Border inside the ControlTemplate) is marked `Focusable="True"`, making it reachable via Tab navigation.
- `FocusVisualStyle="{x:Null}"` suppresses the default dotted focus rectangle — the revealed "×" button itself serves as the focus indicator.
- A second trigger (`<Trigger SourceName="ChipBorder" Property="IsKeyboardFocusWithin" Value="True">`) applies the same setters as the hover trigger: PatternText → `Hidden`, RemoveButton → `Visible`.
- **Why `IsKeyboardFocusWithin`:** When the user tabs to the chip, focus lands on ChipBorder (`IsKeyboardFocusWithin` becomes `True` → "×" reveals). Tabbing again moves focus to the RemoveButton itself; `IsKeyboardFocusWithin` remains `True` (the button is a descendant of ChipBorder), keeping the "×" visible. Tabbing away (or removing the pattern) collapses the button.

**Keyboard flow:**
1. Tab → focus lands on ChipBorder → "×" reveals, text hides.
2. Tab → focus moves to RemoveButton (now tabbable because it's `Visible`).
3. Enter/Space → RemoveIgnoredPatternCommand fires → pattern removed → chip disappears → focus moves to next tab stop.
4. Shift+Tab (or tabbing away without removing) → focus leaves → "×" collapses, text reappears.

**Edge behaviors (acceptable):**
- Clicking the chip body focuses it → "×" reveals. This is consistent: the chip is focused, and the "×" is its focus indicator.
- After keyboard removal, the chip is gone; focus advances to the next tab stop (standard WPF behavior when the focused element is removed).

**Rationale:**
- The two triggers (hover and keyboard focus) are independent and provide symmetric behavior: mouse users hover to reveal, keyboard users tab to reveal.
- No ViewModel changes or code-behind required — pure XAML solution.
- Screen readers announce the chip as focusable and the button when it's revealed and focused.

### Chip Sizing Stability

**Decision:** Use `Visibility="Hidden"` (not `Collapsed`) on both the RemoveButton (at rest) and the PatternText (on hover/focus) to preserve their layout slots in the Grid's measure calculation. `Hidden` elements are invisible but remain in layout measurement, so the Grid always measures the max of both children → constant chip dimensions in all states, preventing WrapPanel reflow.

**Rationale:**

**Height stability (Hidden button at rest):**
- A WPF Grid measures the max of its visible children's desired sizes. With `Visibility="Collapsed"` (excluded from measure), the at-rest Grid would measure only PatternText (~16px text height). On hover/focus when RemoveButton becomes `Visible`, the Grid would measure both, and the 20×20px button would determine the height, growing the Grid to 20px — a 4px chip outer height change causing WrapPanel reflow.
- RemoveButton defaults to `Visibility="Hidden"` (not `Collapsed`), so the 20×20px button is always included in the Grid's measure calculation even when invisible. The Grid measures max(text height ~16px, button height 20px) = 20px content in both states. Chip outer height = 20px content + 8px vertical padding + 2px border = 30px constantly (equals the original pre-change chip height, so the chip returns to its familiar height while staying text-hugging in width).
- `Hidden` elements are skipped by tab navigation (same as `Collapsed`), so keyboard accessibility is unchanged: the button is not tabbable at rest, becomes tabbable when the triggers set it to `Visible`.
- `Hidden` elements are not hit-testable, so the invisible button cannot steal clicks from the chip or underlying elements.
- `Hidden` elements remain in the UIA tree as offscreen (unlike `Collapsed`, which removes them), providing a mild automation discoverability improvement — screen readers can enumerate the button even when invisible. No regression; acceptable.

**Width stability (Hidden text on hover/focus):**
- PatternText uses `Visibility="Hidden"` (not `Collapsed`) on hover/focus to make it invisible while preserving its layout slot. Using `Collapsed` would remove the text from layout, shrinking the Grid (and therefore the chip) by the text width, violating the width stability requirement.
- At rest: PatternText is `Visible`, RemoveButton is `Hidden` (both measured). On hover/focus: PatternText is `Hidden`, RemoveButton is `Visible` (both still measured). The Grid measures max(text width, 20px button width) + padding in both states.
- For typical pattern text the text is wider than the 20px button, so chip width ≈ text width + 16px horizontal padding + border. For very short pattern text, the reserved 20px button slot may make the chip marginally wider than text-only sizing — this is max-of-both-states sizing, consistent with the constant-height approach, and accepted.

**Revision history:**
- Original design: StackPanel with TextBlock + placeholder Border (Width="20", Height="20", Margin="4,0,0,0"), Padding="8,4,4,4" (asymmetric), both width and height stable via the always-in-layout placeholder. Chip width = text + 24px placeholder + 12px padding.
- Revision 1: Removed placeholder Border, changed to Grid with TextBlock only, RemoveButton `Collapsed` at rest, Padding="8,4" (symmetric). Chip width compacted (text + 16px padding), but chip height became unstable (shorter at rest, taller on hover).
- Revision 2 (documented, never implemented): Proposed `MinHeight="20"` on Grid to floor the content height. User decision: superseded by revision 3 before implementation.
- Revision 3 (current): Changed RemoveButton default Visibility from `Collapsed` to `Hidden` (layout slot preserved). No Grid MinHeight needed. Chip width still compact (max of text, button, both + 16px padding), chip height now stable at 30px (the original pre-change height) in both states. Both children use `Hidden` for size stability: button at rest (height), text on hover/focus (width).

### No Animation

**Decision:** The button appears/disappears instantly on hover enter/leave, with no fade or slide animation.

**Rationale:**
- Animations add complexity (Storyboards, DoubleAnimation on Opacity, managing animation clocks).
- The hover-reveal is a utility affordance, not a hero interaction; instant visibility toggling is appropriate and avoids animation-timing edge cases (rapid hover on/off, multiple chips hovered in sequence).
- If animation is desired later, it can be added as a follow-up change using `<Trigger.EnterActions>` and `<Trigger.ExitActions>` with DoubleAnimation on Opacity (requires changing the approach to use Opacity instead of Visibility for the button).

## Alternatives Considered

### 1. Inline Style on Border with Property Trigger (Original Plan)

**Rejected:** WPF's `Style.Triggers` on a Border inside a DataTemplate cannot use `Setter TargetName` to affect sibling elements (TextBlock, Button) because there is no ControlTemplate boundary. The triggers can only set properties on the Border itself.

### 2. DataTrigger on ViewModel Wrapper Property

**Rejected:** Requires adding an `IsHovered` property to a wrapper ViewModel (e.g., `PatternChipViewModel`) and binding it from MouseEnter/MouseLeave events or an attached behavior. This violates the XAML-only constraint and adds unnecessary ViewModel complexity for a purely presentational concern.

### 3. Code-Behind Event Handlers

**Alternative (simpler):** Handle `MouseEnter` and `MouseLeave` events on the chip Border in SettingsProjectsPage.xaml.cs to toggle the Visibility of named children (TextBlock, Button).

**Pros:**
- Simple, direct, standard WPF practice for hover interactions.
- 6 lines of C# (3 per event: find named elements, set Visibility).
- No ControlTemplate verbosity, no ContentControl wrapper.

**Cons:**
- Violates the stated "XAML-only" preference.
- Slightly less declarative than a pure XAML solution.

**Verdict:** If the ControlTemplate approach is too verbose, the code-behind path is a pragmatic alternative. The choice depends on project conventions and the user's preference. (Open question: which is preferred?)

### 4. EventTrigger + Storyboard with Opacity Animation

**Rejected:** `DataTemplate.Triggers` can use `<EventTrigger RoutedEvent="MouseEnter">` with a Storyboard to animate the button's Opacity from 0 to 1. However:
- Opacity 0 does not remove the button from the tab order or UIA tree (keyboard users would still tab to an invisible button).
- Requires managing animation clocks and timing for MouseLeave (reverse animation).
- Does not address the chip sizing stability issue (Opacity 0 button still occupies layout space unless positioned absolutely, which complicates the Grid structure).

### 5. Separate Hover State ViewModel Property with MultiBinding

**Rejected:** Bind a ViewModel's `IsPatternChipHovered` property via an attached behavior or event, then use `MultiDataTrigger` on the button's Visibility to show it when the pattern matches the hovered pattern. This adds ViewModel state for presentation logic and requires tracking which chip is hovered globally, which is error-prone and over-engineered.

## Open Questions

1. **ControlTemplate vs Code-Behind:** The pure XAML ControlTemplate approach is verbose (~50 lines including both triggers) compared to code-behind event handlers (~10 lines for MouseEnter/MouseLeave + GotKeyboardFocus/LostKeyboardFocus). Which is preferred given project conventions? (The brief stated "XAML-only" but did not forbid minimal code-behind for interaction logic.)

2. **Screen reader announcement:** When the button's `Visibility` is `Collapsed`, screen readers will not announce it. When revealed (hover or focus), they announce "Remove pattern, Button" (from AutomationProperties.Name). Should the chip itself have a custom AutomationProperties.Name like "Pattern X, Tab to reveal remove button" that is always announced, providing a discoverable hint for screen reader users before they tab to the chip?

## Files to Modify

- `src/CLIHub/Views/SettingsProjectsPage.xaml` (lines ~221-246): Replace the current DataTemplate with a ContentControl-based template including ControlTemplate.Triggers for hover-reveal.

## Files NOT Modified

- `src/CLIHub/Themes/Common/SettingsControls.xaml` (ChipRemoveButton style remains shared and unchanged)
- `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs` (RemoveIgnoredPatternCommand unchanged)
- All other chip usages (watched-folder rows, other pages) remain always-visible
