## MODIFIED Requirements

### Requirement: Fetch and parse feed index

The system SHALL fetch a JSON feed index from a configured HTTPS URL using HTTP conditional requests (if prior sync metadata exists), validate its schema (index `schemaVersion` 1 or 2), and return available plugins with metadata normalized across both schema versions. For the default official feed, the system SHALL fetch and verify an ECDSA P-256 cryptographic signature (`index.sig`) over the raw index body bytes before parsing; an invalid signature is a hard error, and a missing signature is accepted with a warning only during a time-bounded transition window. A 304 Not Modified response trusts the cached verified index without re-fetching the signature. Custom feed URLs skip signature verification.

#### Scenario: Valid index fetched successfully

- **WHEN** the feed URL is reachable and returns a valid `index.json` with `schemaVersion` 1 or 2 and a `plugins` array
- **AND** for the default feed, the associated `index.sig` contains a valid ECDSA P-256 signature over the index body
- **THEN** the feed result includes a parsed `PluginFeedIndex` with the list of plugins
- **AND** Success is true

#### Scenario: Valid signature verified before parse

- **WHEN** the feed URL is the default official URL
- **AND** the index body is fetched successfully
- **THEN** the `index.sig` file is fetched from the same URL directory over the same HTTPS/redirect/hop transport
- **AND** the ECDSA P-256 signature in the envelope is verified against the raw index body bytes using the embedded public key
- **AND** only after successful verification is the body parsed

#### Scenario: Invalid signature is a hard error

- **WHEN** the default feed's `index.sig` is fetched but the signature does not verify (tampered body, wrong key, algorithm mismatch, malformed envelope)
- **THEN** the feed result includes an error "Plugin feed signature is invalid. The feed may have been tampered with."
- **AND** Success is false
- **AND** the cached index is NOT used as a fallback

#### Scenario: Missing signature accepted during transition window

- **WHEN** the current UTC date is before the `SignatureRequiredAfter` cutoff
- **AND** the `RequireFeedSignature` flag is false
- **AND** the default feed's `index.sig` file is missing (HTTP 404)
- **THEN** a warning is logged
- **AND** the index is parsed normally with `SignatureWarning` set

#### Scenario: Missing signature rejected after transition window

- **WHEN** the current UTC date is on or after the `SignatureRequiredAfter` cutoff, or the `RequireFeedSignature` flag is true
- **AND** the default feed's `index.sig` file is missing
- **THEN** the feed result includes an error "Plugin feed signature is required but missing."
- **AND** Success is false

#### Scenario: Custom feed URL skips signature verification

- **WHEN** the resolved feed URL is a custom URL (not the default)
- **THEN** no `index.sig` is fetched
- **AND** the index is parsed without signature verification
- **AND** the feed result has `SignatureValid: true`

#### Scenario: Signature file exceeds 4 KB bound

- **WHEN** the `index.sig` response body exceeds 4 KB
- **THEN** the feed result includes an error "Plugin feed signature file too large"
- **AND** Success is false

#### Scenario: Signature fetch uses HTTPS and redirect rules

- **WHEN** the signature file URL has `http://` scheme or redirects to a non-HTTPS URL
- **THEN** the signature fetch is rejected
- **AND** the rejection is treated as a missing signature (transition-window logic applies)

#### Scenario: Index schema version unsupported

- **WHEN** the feed index has `schemaVersion` greater than 2
- **THEN** the feed result includes an error "Unsupported feed schema version X"
- **AND** Success is false

#### Scenario: Index missing required field

- **WHEN** the feed index is missing `schemaVersion` or `plugins` array
- **THEN** the feed result includes an error describing the missing field
- **AND** Success is false

#### Scenario: Index contains unknown fields

- **WHEN** the feed index or plugin entries contain fields not in the schema
- **THEN** those fields are ignored (forward compatibility)
- **AND** parsing succeeds if all required fields are present

#### Scenario: Index entry with invalid fields skipped

- **WHEN** a feed index entry is missing required fields for its schema version (v1: e.g. `id`, `name`, `minAppVersion`, `packageUrl`, `sha256`; v2: e.g. `id`, `name`, `compatibility`, `descriptor`)
- **THEN** that entry is skipped with a logged warning
- **AND** the rest of the index entries are loaded successfully
- **AND** the feed result Success is true if the index itself is valid

#### Scenario: Duplicate plugin IDs in index

- **WHEN** the feed index contains multiple entries with the same plugin `id`
- **THEN** the first entry is kept
- **AND** later entries with the same `id` are skipped with a logged warning

#### Scenario: Index exceeds size limit

- **WHEN** the feed index response is larger than 2 MB
- **THEN** the feed result includes an error "Feed index too large"
- **AND** Success is false

#### Scenario: HTTP URL rejected

- **WHEN** the configured feed URL has `http://` scheme (not HTTPS)
- **THEN** the request is rejected before sending
- **AND** the feed result includes an error "Feed URL must use HTTPS"
- **AND** Success is false

#### Scenario: Redirect to HTTP rejected

- **WHEN** an HTTPS feed URL redirects to an `http://` URL
- **THEN** the request is rejected after detecting the redirect
- **AND** the feed result includes an error "Redirect to non-HTTPS URL rejected"
- **AND** Success is false

#### Scenario: Exactly five redirects are followed

- **WHEN** a feed fetch encounters a chain of exactly 5 HTTPS redirects
- **THEN** all 5 redirects are followed and the terminal response determines success or failure
- **AND** no redirect-limit error is produced

#### Scenario: Redirect beyond the hop limit is rejected

- **WHEN** a feed fetch encounters a chain of 6 or more HTTPS redirects
- **THEN** the fetch is rejected after the sixth redirect
- **AND** the feed result includes an error "Redirect limit exceeded (maximum 5 hops)"
- **AND** Success is false

#### Scenario: Network timeout

- **WHEN** the feed fetch exceeds the configured agent probe timeout
- **THEN** the feed result includes an error "Feed request timed out"
- **AND** Success is false

#### Scenario: Network unavailable

- **WHEN** the network is unavailable or the feed URL is unreachable
- **THEN** if a cached index exists, the feed result reports success with `FromCache: true`
- **AND** if no cache exists, the feed result includes an error "Could not load plugin feed"
- **AND** Success is false when no cache is available
- **AND** no exception is thrown

#### Scenario: Schema version 2 index parsed

- **WHEN** the feed index has `schemaVersion: 2` and entries with `id`, `name`, `description`, `version`, `publisher`, `homepage`, `compatibility`, `channel`, an inline `descriptor` object, and a `package` object with `url` and `sha256`
- **THEN** each entry exposes the package URL and checksum from `package`, the inline descriptor, the compatibility range, the channel, and `logoUrl`
- **AND** the entry's descriptor schema version comes from `descriptor.schemaVersion` (1 when absent)
- **AND** Success is true

#### Scenario: Descriptor-only v2 entry parsed

- **WHEN** a schema version 2 entry omits `package` entirely but carries a valid inline `descriptor`
- **THEN** the entry is parsed with null package URL and null checksum and the inline descriptor available for installation
- **AND** the entry is listed like any other feed entry

#### Scenario: Non-stable channel entry skipped

- **WHEN** a schema version 2 entry carries a `channel` value other than "stable" (absent, null, or "stable" are accepted)
- **THEN** that entry is skipped with a logged warning
- **AND** the rest of the index entries are loaded successfully

#### Scenario: Malformed compatibility range entry skipped

- **WHEN** a schema version 2 entry carries a `compatibility` value that does not parse as a list of `>=|<=|>|<|= major.minor.patch` constraints
- **THEN** that entry is skipped with a logged warning
- **AND** the rest of the index entries are loaded successfully

#### Scenario: Invalid inline descriptor entry skipped

- **WHEN** a schema version 2 entry's inline `descriptor` fails descriptor validation (e.g. no launch command, unsupported descriptor schema version) or its `id` differs from the entry's `id`
- **THEN** that entry is skipped with a logged warning
- **AND** the rest of the index entries are loaded successfully

#### Scenario: Conditional request with ETag returns 304

- **WHEN** a prior fetch stored an ETag header
- **AND** the next fetch sends `If-None-Match: <etag>` and receives HTTP 304 Not Modified
- **THEN** the feed result reports success
- **AND** the cached index is used without re-parsing
- **AND** the signature is NOT re-fetched (the cached index was verified before it was cached)
- **AND** no new data is written to cache

#### Scenario: Conditional request with Last-Modified returns 304

- **WHEN** a prior fetch stored a Last-Modified header (no ETag)
- **AND** the next fetch sends `If-Modified-Since: <date>` and receives HTTP 304 Not Modified
- **THEN** the feed result reports success
- **AND** the cached index is used without re-parsing

#### Scenario: Fetch fails, cached index used

- **WHEN** a feed fetch fails (network error, timeout, server error, parse error)
- **AND** a valid cached index exists
- **THEN** the feed result reports success
- **AND** the cached index is returned with a `FromCache: true` flag
- **AND** a timestamp indicating when the cache was created is included
- **AND** no signature re-fetch is performed (the cached index was verified before caching)