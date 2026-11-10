# preferences-ui (partial): Pattern chip hover-reveal remove button

## MODIFIED Requirements

### Requirement: Projects and Paths page options

The Project Scanning page SHALL offer a watched-directories list with add, per-directory scan, and remove actions, a "Scan depth" field, an "Ignored patterns" list, a "Discovery" selector with the options "Git only", "Agents only", and "Git and agents", and a read-only background watcher status. The per-directory scan action SHALL rescan only that directory's watched root, unlike the page-header action that rescans every saved root. Each watched-directory row SHALL show, beneath its path, a depth chip and a discovery chip that open a menu of allowed values, plus the number of discovered repositories beneath that directory. The "Scan depth" and "Discovery" fields SHALL act as defaults for a directory added from now on and SHALL NOT change the settings of existing rows. The page SHALL show the stored values when opened.

Changes to the scanning settings SHALL apply immediately, without requiring the footer Save: adding or removing a watched directory, changing a row's depth or discovery chip, changing the default depth or discovery mode, and adding or removing an ignored pattern SHALL each persist atomically, reconfigure the background scanner, and trigger a scan. A newly added watched directory SHALL therefore be scanned while the page is open, the page-header scan action and the summary cards SHALL always describe the list shown on the page, and the page SHALL state that scanning changes apply immediately. The footer Save and Cancel SHALL continue to govern the remaining settings, and Cancel SHALL NOT revert scanning changes that were already applied.

**Each ignored-pattern chip SHALL show the pattern text at rest and a hover-reveal or focus-reveal remove button.** The remove button SHALL be hidden by default (not visible when the chip is neither hovered nor keyboard-focused). When the mouse cursor is over a specific ignored-pattern chip OR when that chip receives keyboard focus (Tab navigation), that chip's remove button SHALL appear centered over the chip (both horizontally and vertically), the pattern text SHALL be hidden, and the chip's outer dimensions SHALL remain unchanged (the WrapPanel layout SHALL NOT reflow). The revealed "×" button serves as the focus indicator when the chip is keyboard-focused (no additional dotted focus rectangle is shown). Tabbing again moves focus to the remove button itself, which remains visible while focused; pressing Enter or Space on the focused button removes the pattern. Removing a pattern via the revealed button (mouse click or keyboard activation) SHALL immediately persist the change and trigger a scan, consistent with the existing immediate-apply behavior for scanning settings. Other chips and watched-folder row action buttons (Rescan, Remove with distinct glyphs) SHALL remain always-visible and unaffected by the hover-reveal or focus-reveal behavior.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Project Scanning"
- **THEN** it shows the watched-directories list with add, scan, and remove actions, the depth and discovery defaults, the "Ignored patterns" list, and the watcher status
- **AND** it states that scanning changes apply immediately

#### Scenario: Stored values loaded

- **WHEN** the page opens
- **THEN** the watched directories show their stored depth, discovery mode, and repository count
- **AND** the default depth and discovery show the stored global values
- **AND** opening the page does not rescan

#### Scenario: Adding a watched directory applies immediately

- **WHEN** the user adds a watched directory
- **THEN** the list is persisted, the scanner is reconfigured, and that directory is scanned in the background
- **AND** the repository count for the new row and the summary card coverage include it without choosing Save
- **AND** the page-header scan action now rescans it too

#### Scenario: Adding a watched directory with apply failure reports error

- **WHEN** the user adds a watched directory and the immediate apply fails (persistence or scanner reconfiguration error)
- **THEN** the directory is not added to the list
- **AND** the validation error describes the failure
- **AND** the input text is not cleared so the user can retry or correct

#### Scenario: Adding a duplicate watched directory reports a message

- **WHEN** the user adds a folder that is already in the watched-directories list (by normalized path), whether by typing the path or by browsing to it
- **THEN** no new row is added and the watched-root count is unchanged
- **AND** an explicit message states that the folder is already watched
- **AND** the input the user entered or selected is not cleared

#### Scenario: Scanner reload failure after a successful scanning apply

- **WHEN** the user changes a scanning setting and the project scanner fails to reload after the write
- **THEN** the change remains stored
- **AND** the page does not report an apply failure
- **AND** the scanner retries the reload on the next scan or application restart

#### Scenario: Removing a watched directory applies immediately

- **WHEN** the user removes a watched directory
- **THEN** the list is persisted and the scanner is reconfigured
- **AND** discovered projects that are no longer under any watched root and were not registered manually are removed by the resulting scan

#### Scenario: Ignored pattern changes apply immediately

- **WHEN** the user adds or removes an ignored directory pattern
- **THEN** the pattern list is persisted and the next scan uses it without choosing Save

#### Scenario: Row chips change only that row

- **WHEN** the user picks a depth or discovery value from a row's chip menu
- **THEN** only that row's setting changes
- **AND** the global defaults and the other rows keep their values
- **AND** the new value is persisted and used by the next scan

#### Scenario: Defaults

- **WHEN** no scanning preferences have been stored
- **THEN** the watched-directories list is empty, the default depth shows the built-in default, the ignored patterns show the built-in defaults, and the default discovery mode is "Git and agents"

#### Scenario: New directory starts from the defaults

- **WHEN** the user adds a watched directory
- **THEN** its depth and discovery chips start from the current default values

#### Scenario: Per-directory scan

- **WHEN** the user chooses the scan action on one watched directory
- **THEN** only that directory is scanned
- **AND** the other watched directories are not rescanned
- **AND** the summary cards reflect any newly discovered projects in that directory

#### Scenario: Changing the default depth or discovery does not affect existing rows

- **WHEN** the user changes the default depth or discovery mode
- **THEN** existing watched-directory rows keep their current values
- **AND** the new default applies to directories added after the change

#### Scenario: Page-header scan rescans all watched directories

- **WHEN** the user chooses the page-header scan action
- **THEN** every watched directory is rescanned
- **AND** the summary cards reflect the results

#### Scenario: Row summary shows the per-directory count

- **WHEN** a watched directory is listed
- **THEN** it shows the number of discovered repositories beneath that directory
- **AND** the count uses the singular "repository" for one and the plural "repositories" otherwise
- **AND** the count refreshes after a scan completes

#### Scenario: Watcher status is read-only

- **WHEN** the watcher status is shown
- **THEN** it reports the watcher state, the last scan time, and the discovered-project count, and it is not user-editable

#### Scenario: Cancel keeps applied scanning changes

- **WHEN** the user changes a scanning setting and then closes the window with Cancel
- **THEN** the scanning change remains stored and scanned
- **AND** the remaining unsaved settings are discarded

#### Scenario: Ignored-pattern chip at-rest state

- **WHEN** the Settings → Project Scanning page is displayed with one or more ignored patterns
- **THEN** each ignored-pattern chip shows the pattern text
- **AND** no remove button ("×") is visible on any chip

#### Scenario: Hover reveals remove button on one chip

- **WHEN** the user moves the mouse cursor over a specific ignored-pattern chip
- **THEN** that chip's pattern text is hidden
- **AND** a centered remove button ("×") appears over that chip
- **AND** the chip's outer dimensions remain unchanged (no WrapPanel reflow)

#### Scenario: Hover off hides the button

- **WHEN** the user moves the mouse cursor off an ignored-pattern chip that was showing its remove button
- **THEN** the remove button disappears
- **AND** the pattern text reappears

#### Scenario: Hover one chip does not affect others

- **WHEN** the user hovers one ignored-pattern chip
- **THEN** only that chip's remove button appears
- **AND** other ignored-pattern chips remain in their at-rest state (pattern text visible, no button)

#### Scenario: Hover-revealed button removes the pattern

- **WHEN** the user hovers an ignored-pattern chip, revealing its remove button, and clicks the button
- **THEN** the pattern is removed from the list immediately
- **AND** the change is persisted and the next scan uses the updated pattern list
- **AND** the remaining ignored-pattern chips re-layout in the WrapPanel if needed

#### Scenario: Watched-folder row buttons remain always-visible

- **WHEN** the user views the watched-directories list on the Project Scanning page
- **THEN** each watched-directory row's action buttons (Rescan, Remove) are always visible
- **AND** hovering a watched-directory row does not hide or reveal any buttons (no hover-reveal behavior on these rows)

#### Scenario: Chip dimensions stable during hover

- **WHEN** the user hovers and un-hovers multiple ignored-pattern chips in sequence
- **THEN** no chip changes its width or height between the at-rest and hovered states
- **AND** the WrapPanel layout does not reflow due to hovering

#### Scenario: Remove button not keyboard-accessible when hidden

- **WHEN** the user navigates the Settings → Project Scanning page using only the keyboard (Tab, Shift+Tab) and no ignored-pattern chip is focused
- **THEN** the ignored-pattern chips' remove buttons are not in the tab order while the buttons are hidden (at-rest state)
- **AND** tabbing through the page skips over the hidden remove buttons

#### Scenario: Keyboard focus reveals remove button on chip

- **WHEN** the user presses Tab and focus lands on an ignored-pattern chip
- **THEN** that chip's pattern text is hidden
- **AND** a centered remove button ("×") appears over that chip
- **AND** the chip's outer dimensions remain unchanged (no WrapPanel reflow)
- **AND** no dotted focus rectangle is shown (the "×" button itself serves as the focus indicator)

#### Scenario: Tabbing to the revealed button

- **WHEN** the user presses Tab while an ignored-pattern chip is focused (remove button revealed)
- **THEN** focus moves to the remove button itself
- **AND** the button remains visible (because keyboard focus is within the chip's focus tree)
- **AND** the pattern text remains hidden

#### Scenario: Keyboard removal via focused button

- **WHEN** the user presses Enter or Space while the remove button is focused
- **THEN** the pattern is removed from the list immediately
- **AND** the change is persisted and the next scan uses the updated pattern list
- **AND** focus moves to the next focusable element (the next chip, or the next element after the list if no chips remain)

#### Scenario: Keyboard focus leaves chip collapses button

- **WHEN** the user presses Shift+Tab or Tab (moving focus away from the chip and its button) after the remove button was revealed via keyboard focus
- **THEN** the remove button disappears
- **AND** the pattern text reappears
- **AND** the chip returns to its at-rest state

#### Scenario: Mouse click on chip body focuses it

- **WHEN** the user clicks the chip body (not the remove button) with the mouse
- **THEN** the chip receives keyboard focus
- **AND** the remove button reveals (same behavior as tabbing to the chip)
- **AND** the pattern text is hidden
- **AND** the chip's outer dimensions remain unchanged

#### Scenario: Focus one chip does not affect others

- **WHEN** the user tabs to or clicks one ignored-pattern chip (button reveals)
- **THEN** only that chip's remove button appears
- **AND** other ignored-pattern chips remain in their at-rest state (pattern text visible, no button)
