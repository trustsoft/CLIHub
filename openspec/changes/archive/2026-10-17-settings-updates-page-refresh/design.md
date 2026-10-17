## Context

The Settings Updates page is a single scrollable page under the shared fixed page header. It currently consists of a version line, an "AUTOMATIC" group of four update toggles, and a "MANUAL" status card with the check button. `SettingsViewModel` already receives `IUpdateWorkflow`, which exposes `LastKnownAvailableVersion` and check results; release notes are owned by `IReleaseNotesLauncher` and presented in `WhatsNewWindow`.

## Goals / Non-Goals

**Goals:**

- Make the update status and the check action the first thing on the page.
- Show the current version and check outcome without internal build metadata.
- Reuse the existing release-notes launcher from Settings.
- Keep all existing update preferences and save/cancel behavior unchanged.

**Non-Goals:**

- Adding an update channel selector or beta channel support.
- Rendering a full changelog inside Settings.
- Changing the shared page header or adding a page-specific header action slot.
- Changing the update check, download, or notification workflows.

## Decisions

### D1: The page header owns the update status and actions

The shared page header gains page-specific slots selected by the active page key. For Updates, the title row shows the version and channel chips, the trailing cluster shows the update-status chip and the accent "Check for updates" icon button, and the bottom-right of the header shows "View release notes".

Rationale: the update identity and result belong with the page title and description, matching the Stitch hierarchy while leaving the body for preferences. The slots are intentionally page-key driven, so other pages keep the current header without empty action areas.

Alternative considered: keep a status card below the header. Rejected because the user wants the status, version, channel, and release-notes action in the header.

### D2: No status card in the page body

The body contains only the "UPDATE PREFERENCES" section and its grouped toggles. The previous status card is removed because all of its information now lives in the header; its detail text is retained as the status chip tooltip.

Rationale: avoids duplicating version and status information on one page and reduces the page height.

### D3: Status is derived in the view model, not in XAML

`SettingsViewModel` exposes `UpdateStatusTitle`, `UpdateStatusDetail`, `UpdateStatusBadge`, and `UpdateStatusBadgeTone`, and updates them from a check result and from `LastKnownAvailableVersion` when the window loads. `UpdateMessage` remains as the concise status line used by existing tests and presentation code.

Rationale: the mapping from `UpdateStatus` to user-facing copy is behavior, and keeping it in the view model keeps XAML declarative and testable.

### D4: Release notes use the existing launcher

`ShowReleaseNotesCommand` calls `IReleaseNotesLauncher.ShowReleaseNotes()`. The launcher already owns the single What's New window and records the running version as seen.

Rationale: reuses the existing single-window behavior and does not duplicate release-note rendering in Settings.

### D5: No channel selector or changelog copy

The status card shows only current version, availability, and check timing. The page omits the Stitch "Update Channel" dropdown because only stable builds are published, and it omits the changelog block because What's New already presents the notes.

## Risks / Trade-offs

- The status card is only refreshed by a check initiated on this page or by the availability state known when the window loads. A check started elsewhere while Settings is open does not push a new status into the card. This keeps the change small; a later change can subscribe to `UpdateStateChanged` if live synchronization is needed.
- "Last checked" is only known after a check in the current session. The initial detail therefore shows the current version rather than a fabricated timestamp.
- The header action slot is selected by page key in XAML. Adding actions to more pages means adding a template and a trigger there; no page metadata or view model type carries UI content.
