# Preferences UI (Delta)

## ADDED Requirements

### Requirement: Decorative interface glyphs are hidden from assistive technology

Decorative icon glyphs that duplicate adjacent text labels SHALL be excluded from the UIA tree and SHALL not be announced by screen readers.

#### Scenario: Decorative glyphs in Settings are not announced

**WHEN** the user opens Settings and navigates pages with Windows Narrator or another screen reader active  
**THEN** page-header icons, sidebar page glyphs, SettingCard icons, SummaryCard icons, status chip glyphs, input-field decorations (Add buttons), checkmarks in toggles, ComboBox chevrons, and menu chevrons are not announced  
**AND** the adjacent text labels (page title, setting name, badge text, input labels) are announced correctly  
**AND** icon-only buttons (Refresh, Rescan, FolderAdd, icon-only chip remove buttons) with ToolTips and no adjacent labels remain announced with their accessible names

#### Scenario: Decorative glyph conversion does not change visual rendering

**WHEN** the user opens Settings and Launch windows after the DecorativeGlyph element is applied  
**THEN** all glyphs render identically to the pre-change appearance (same font, size, color, position)  
**AND** no layout shifts, missing glyphs, or style regressions are visible

### Requirement: Actions menu entries expose an accessible name and tooltip

Actions menu entries in the Settings plugin catalog page SHALL bind optional tooltip and accessible-name properties from MenuAction, with a fallback to the label.

#### Scenario: ACTIONS toggle has an accessible name

**WHEN** the user navigates the CLI Agents Settings page with a screen reader  
**AND** the user Tabs to the ACTIONS toggle button  
**THEN** the screen reader announces "Plugin catalog actions" (or the set accessible name)

#### Scenario: Actions menu entries announce meaningful names

**WHEN** the user opens the ACTIONS menu on the CLI Agents page  
**AND** navigates the menu with a screen reader  
**THEN** each menu entry is announced with its effective accessible name (either the explicit AccessibleName or the Label fallback)

#### Scenario: MenuAction.EffectiveAccessibleName falls back to Label

**WHEN** a MenuAction is constructed with Label="Test Action" and AccessibleName is null or empty  
**THEN** EffectiveAccessibleName returns "Test Action"

**WHEN** a MenuAction is constructed with Label="Test Action" and AccessibleName="Custom Name"  
**THEN** EffectiveAccessibleName returns "Custom Name"

### Requirement: Ignored-pattern chip announces the remove affordance

The focusable ignored-pattern chip SHALL have a persistent AutomationProperties.Name that includes the pattern text and a hint that Tab reveals the remove button.

#### Scenario: Pattern chip announces the remove hint before reveal

**WHEN** the user navigates the Projects and Paths page with a screen reader  
**AND** the user Tabs to an ignored-pattern chip  
**THEN** the screen reader announces "Ignored pattern <pattern text>, Tab reveals the remove button" (or equivalent)  
**AND** the user understands the remove affordance before pressing Tab

#### Scenario: Pattern chip remove button is announced after Tab

**WHEN** the user presses Tab from the focused ignored-pattern chip  
**THEN** focus moves to the remove button  
**AND** the screen reader announces "Remove pattern" (the button's ToolTip and accessible name)
