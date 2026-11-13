# chrome-window-base Specification

## Purpose
Shared construction contract for the application's chromeless secondary windows: a C#
base class (`ChromeWindow`) that owns the borderless window profile — no OS chrome, the shared
`WindowChrome` metrics, themed background and text font, DWM rounded corners, minimum-size and
maximized work-area enforcement, Escape-to-close, and header drag — and a reusable drawn
header control (`WindowHeaderBar`) that renders the title, optional window icon, action slot,
optional maximize/restore toggle, close button, and optional separator. Both live in the
application assembly. The Settings, What's New,
and Browse Plugins windows are built on it; the launch window is deliberately not.

## Requirements

### Requirement: Chromeless window base class

The system SHALL provide a `ChromeWindow` WPF base class whose constructor sets the shared chromeless profile in code: `WindowStyle` None, `WindowChrome` with `CaptionHeight` 0, `CornerRadius` 0, `GlassFrameThickness` 0, `ResizeBorderThickness` 6, `UseAeroCaptionButtons` false, a background bound to the `ChromeBgBrush` palette brush, and the shared text font. Derived windows SHALL NOT redeclare these in XAML and keep their own title, icon, sizes, and show policy.

#### Scenario: Derived window renders without OS chrome

- **WHEN** a window rooted in `ChromeWindow` is shown
- **THEN** it shows no OS title bar or system frame, paints its background from the active palette's `ChromeBgBrush`, and uses the shared text font family

#### Scenario: Derived window keeps its own sizing and behavior

- **WHEN** a derived window declares its own size, minimum size, `ResizeMode`, `ShowInTaskbar`, and startup location in XAML
- **THEN** those values remain effective and are not overridden by the base

### Requirement: Rounded corners and window-message hook on source initialization

When the window handle is created, `ChromeWindow` SHALL apply DWM rounded corners through the shared chrome helper and register an `HwndSource` hook that handles the `WM_GETMINMAXINFO` window message for the window's lifetime.

#### Scenario: Rounded corners are applied once

- **WHEN** a derived window's handle is created
- **THEN** DWM rounded corners are applied to it and the window-message hook is registered exactly once

### Requirement: Minimum tracking size and maximized work-area clamp

The `WM_GETMINMAXINFO` handler SHALL enforce the window's declared `MinWidth`/`MinHeight` as the minimum tracking size using the window's DPI, and SHALL constrain a maximized window to its monitor's work area so the taskbar is never covered. Windows without a maximize affordance SHALL be unaffected in normal use; their declared minimum sizes are enforced through the same path.

#### Scenario: Window cannot be resized below its minimum

- **WHEN** the user drags a resize border to shrink a derived window below its declared `MinWidth`/`MinHeight`
- **THEN** the window stops at those minimum sizes

#### Scenario: Maximized window fills the work area only

- **WHEN** a derived window is maximized
- **THEN** it fills the monitor's work area and not the taskbar area

#### Scenario: Non-maximizable window is unchanged

- **WHEN** a derived window has no maximize affordance and is used normally
- **THEN** its interactive behavior is unchanged; only its declared minimum sizes are enforced through the shared handler

### Requirement: Escape closes the window

`ChromeWindow` SHALL close the window when the Escape key is pressed and not already handled by an inner element.

#### Scenario: Escape closes

- **WHEN** the user presses Escape while a derived window has focus and no inner handler has handled the key
- **THEN** the window closes

#### Scenario: Handled Escape is ignored

- **WHEN** an inner element has already handled the Escape key
- **THEN** the base does not close the window

### Requirement: Header drag helper

`ChromeWindow` SHALL provide a header-drag helper that moves the window with `DragMove` and tolerates a release before the drag starts. A press on a header button SHALL NOT start a drag, because buttons handle the press themselves.

#### Scenario: Dragging the header moves the window

- **WHEN** the user drags the drawn header with the left button
- **THEN** the window moves with the drag

#### Scenario: Press on a header button does not drag

- **WHEN** the user presses a button in the drawn header
- **THEN** no window drag is started

### Requirement: WindowHeaderBar shared drawn header

The system SHALL provide a `WindowHeaderBar` control that renders the drawn header strip on the shared `WindowHeader` style, with bindable properties `Title`, `IconSource`, `Actions`, `ShowMaximize` (default off), and `ShowSeparator` (default on).

#### Scenario: Title renders in the matching variant

- **WHEN** `IconSource` is set
- **THEN** the header shows the icon followed by the compact title; **WHEN** it is null, the header shows the `WindowTitle`-styled title with no icon

#### Scenario: Actions slot renders before the window buttons

- **WHEN** a window places buttons in the `Actions` slot
- **THEN** they render in the header's right side ahead of the maximize and close buttons

#### Scenario: Maximize toggle appears only when requested

- **WHEN** `ShowMaximize` is true
- **THEN** the maximize/restore button is shown and flips glyph and tooltip with the window state; **WHEN** false, it is absent

#### Scenario: Close and maximize raise events

- **WHEN** the user activates the close button or the maximize toggle
- **THEN** the control raises `CloseRequested` or `MaximizeRequested` respectively and takes no window action itself; the hosting window decides the action

#### Scenario: Separator is optional

- **WHEN** `ShowSeparator` is true
- **THEN** the hairline renders under the header; **WHEN** false, no separator is rendered

#### Scenario: Control resolves only application-level resources

- **WHEN** the control's XAML is loaded, before it is attached to a hosting window's tree
- **THEN** every keyed style and palette brush it references resolves from application-level resources, never from window-scoped dictionaries

### Requirement: Secondary windows are built on the base

The Settings, What's New, and Browse Plugins windows SHALL be rooted in `ChromeWindow` and render `WindowHeaderBar` as their drawn header. The What's New window SHALL gain minimum-size and work-area enforcement through the base without a visible change. The launch window SHALL NOT use `ChromeWindow`; it keeps its own topmost content-sized structure.

#### Scenario: Settings window keeps its chrome behavior

- **WHEN** the Settings window is opened
- **THEN** its header shows the icon and title with a maximize/restore toggle and close, drag moves it, resize stops at its minimums, maximize fits the work area, and Escape closes it — the behavior pinned by the `settings-theme` capability

#### Scenario: What's New window keeps its look

- **WHEN** the What's New window is opened
- **THEN** its header shows the text-only title with its action buttons and close button, no separator under the header, its window-bottom hairline, and Escape closes it

#### Scenario: Browse Plugins dialog keeps its pinned chrome

- **WHEN** the Browse Plugins dialog is opened
- **THEN** it stays modal with no taskbar entry, drag moves it, resize stops at its 520x360 minimum, rounded corners apply, and Escape closes it — the behavior pinned by the `plugin-feed` capability

#### Scenario: Launch window is unaffected

- **WHEN** the launch window is shown
- **THEN** it keeps its own structure: topmost, content-sized, non-resizable, and Escape hides it rather than closing
