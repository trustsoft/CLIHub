# release-notes Specification

## Purpose
Keeps a technical changelog for developers and short user-facing release notes for end users, and makes the user-facing notes available to the application offline through a parser that turns them into ordered, per-version records.

## Requirements

### Requirement: Release notes documents

The repository SHALL maintain two release-note documents at its root: `CHANGELOG.md` for a technical audience and `RELEASE-NOTES.md` for end users. Both SHALL be hand-written and SHALL use the same version headings so a release lines up across the two files.

#### Scenario: Both documents exist
- **WHEN** a release is prepared
- **THEN** `CHANGELOG.md` and `RELEASE-NOTES.md` both exist at the repository root

#### Scenario: Shared version headings
- **WHEN** a version section is added to either document
- **THEN** it uses the heading form `## <version> — <date>` and the same version and date appear in the other document

#### Scenario: Technical grouping
- **WHEN** `CHANGELOG.md` records a change
- **THEN** the change is listed under `Added`, `Changed`, `Fixed`, or `Removed`, and a change that breaks existing behavior or configuration is marked `**BREAKING**`

#### Scenario: User-facing grouping
- **WHEN** `RELEASE-NOTES.md` records a change
- **THEN** the change is listed under `New`, `Improved`, or `Fixed`, described in short user-facing wording without internal identifiers

#### Scenario: Seeded history
- **WHEN** the documents are first added
- **THEN** they contain a section for the product version currently declared in `Directory.Build.props`, summarizing the capabilities delivered so far

### Requirement: User-facing notes format

`RELEASE-NOTES.md` SHALL follow a parseable shape: a version section heading `## <version> — <date>`, optionally followed by `### New`, `### Improved`, and `### Fixed` subsections whose entries are list items. Date separators MAY be an em dash or a hyphen.

#### Scenario: Well-formed section
- **WHEN** a version section contains a heading and one or more of the `New`, `Improved`, and `Fixed` subsections with list items
- **THEN** the section is readable by the application as one release note with those groups

#### Scenario: Subsection omitted
- **WHEN** a version section has no `Fixed` subsection
- **THEN** the note is still read, with an empty `Fixed` group

#### Scenario: Text before the first version heading
- **WHEN** the document starts with a title or introductory paragraph
- **THEN** that text is ignored and does not become a release note

#### Scenario: Content outside the known subsections
- **WHEN** a version section contains prose or a subsection other than `New`, `Improved`, and `Fixed`
- **THEN** that content is not shown as a note entry

### Requirement: Offline availability of notes

The user-facing notes SHALL be embedded in the application so they are readable without network access and independently of the update mechanism.

#### Scenario: Notes available without a network
- **WHEN** the application reads the release notes while offline
- **THEN** the notes for the shipped version are available

#### Scenario: Notes ship with the binary
- **WHEN** a build is produced
- **THEN** the user-facing notes are part of the application's own files rather than data fetched at runtime

### Requirement: Release notes parsing

The application SHALL expose the parsed user-facing notes as release-note records, each carrying a version, a date, and the `New`, `Improved`, and `Fixed` entry lists, ordered from the newest version to the oldest.

#### Scenario: Notes are ordered newest first
- **WHEN** the document contains more than one version section
- **THEN** the notes are returned with the newest version first and the oldest last

#### Scenario: Version ordering is by version, not text
- **WHEN** versions such as `0.9.0` and `0.10.0` are both present
- **THEN** `0.10.0` is ordered before `0.9.0`

#### Scenario: Latest note is available
- **WHEN** the application needs the newest release note
- **THEN** the newest parsed note is returned without the caller having to order the collection

#### Scenario: Entry content is preserved
- **WHEN** a note entry contains punctuation, marks such as `**text**`, or inline `code`
- **THEN** the entry text is returned as written in the document

#### Scenario: Notes for a specific version
- **WHEN** a caller asks for the note of a given version
- **THEN** the note whose version matches is returned, or no result when the version is absent

### Requirement: Parsing resilience

A malformed, empty, or missing notes document SHALL NOT prevent the application from running.

#### Scenario: Malformed version heading
- **WHEN** a version section heading cannot be interpreted
- **THEN** that section is skipped, the remaining sections are still parsed, and the problem is logged

#### Scenario: Empty or missing document
- **WHEN** the notes document is empty or cannot be found
- **THEN** no release notes are returned, the situation is logged, and the application continues normally
