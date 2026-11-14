# plugin-feed-sync Specification

## Purpose
Allows the application to automatically refresh the plugin feed in the background using HTTP conditional requests to reduce bandwidth, maintains a last-good index cache for resilience when the network is unavailable, and publishes domain events when the feed changes so that UI components can update their state without requiring the user to navigate to the Browse Plugins dialog.

## Requirements

### Requirement: Background feed refresh

The system SHALL run a background service that refreshes the plugin feed on application startup and then at regular intervals without blocking the application or the UI.

#### Scenario: Service starts on application startup

- **WHEN** the application starts and plugin initialization completes
- **THEN** the feed sync service begins a background refresh operation
- **AND** the application is fully interactive before the refresh completes
- **AND** the service is ready to refresh again after 1 hour

#### Scenario: Service refreshes on interval

- **WHEN** the feed sync service has completed a refresh
- **THEN** the service schedules the next refresh after 1 hour
- **AND** the next refresh begins automatically without user action

#### Scenario: Service stops on application shutdown

- **WHEN** the application begins shutdown
- **THEN** any in-flight refresh is cancelled
- **AND** the service waits up to the configured shutdown timeout for the refresh to complete

#### Scenario: Service is cancellation-aware

- **WHEN** the application requests shutdown while a refresh is in progress
- **THEN** the cancellation token supplied to the refresh is signalled
- **AND** the refresh operation exits promptly on cancellation

---

### Requirement: Conditional fetch with ETag or Last-Modified

The system SHALL store the `ETag` response header (or `Last-Modified` if ETag is absent) from feed index responses and send `If-None-Match` (or `If-Modified-Since`) headers on subsequent requests to reduce unnecessary data transfer.

#### Scenario: Initial fetch stores ETag

- **WHEN** the feed index is fetched and the response includes an `ETag` header
- **THEN** the ETag is stored in the sync metadata file
- **AND** the raw index bytes are cached

#### Scenario: Initial fetch stores Last-Modified if no ETag

- **WHEN** the feed index is fetched without an `ETag` header but with `Last-Modified`
- **THEN** the Last-Modified date is stored in the sync metadata file
- **AND** the raw index bytes are cached

#### Scenario: Subsequent fetch sends If-None-Match

- **WHEN** a stored ETag exists and the next refresh occurs
- **THEN** the request includes the `If-None-Match: <etag>` header

#### Scenario: Subsequent fetch sends If-Modified-Since

- **WHEN** no ETag is stored but a Last-Modified date exists
- **THEN** the request includes the `If-Modified-Since: <date>` header

#### Scenario: Server provides no conditional headers

- **WHEN** the feed server does not include `ETag` or `Last-Modified` headers
- **THEN** the full index is downloaded on the next refresh

---

### Requirement: Last-good index cache

The system SHALL store the raw index bytes in a cache file and fall back to the cached index if the network is unavailable or the server returns an error.

#### Scenario: Index bytes stored in cache

- **WHEN** the feed index is successfully fetched
- **THEN** the raw response bytes are written to `%APPDATA%\CLIHub\cache\feed-index.json`
- **AND** the sync metadata is written to `%APPDATA%\CLIHub\cache\feed-sync.json`

#### Scenario: Corrupt cache file deleted

- **WHEN** a cache file is corrupt
- **THEN** the corrupt file is deleted
- **AND** a warning is logged
- **AND** the application continues normally

#### Scenario: Cached index exceeds 2 MB, not used

- **WHEN** a cached index file exceeds 2 MB
- **THEN** the cache file is considered invalid
- **AND** the fetch falls back to the live feed

#### Scenario: Cache file missing on first startup

- **WHEN** the application starts and no cache files exist
- **THEN** the feed is fetched unconditionally
- **AND** the index bytes and metadata are cached for future refreshes

---

### Requirement: Change detection and domain event

The system SHALL compare the current feed index with the last known one and publish a domain event when the feed changes, enabling subscribers to react.

#### Scenario: Index change detected

- **WHEN** a successful refresh obtains a new index (HTTP 200, not 304)
- **AND** the set of `{ id, version, compatibility }` tuples differs from the previous index
- **THEN** a `PluginFeedUpdatedEvent` is published with the hash of the new index
- **AND** the new hash is stored in the sync metadata

#### Scenario: No change, event not published

- **WHEN** a successful refresh obtains a new index
- **AND** the set of `{ id, version, compatibility }` tuples is identical
- **THEN** no `PluginFeedUpdatedEvent` is published

#### Scenario: 304 Not Modified, event not published

- **WHEN** a conditional request returns 304 Not Modified
- **THEN** no `PluginFeedUpdatedEvent` is published

#### Scenario: Fetch error, event not published

- **WHEN** a refresh fails
- **THEN** no `PluginFeedUpdatedEvent` is published

#### Scenario: Change detection uses correct comparison key

- **WHEN** a refresh compares indices
- **THEN** only `{ id, version, compatibility }` tuples are compared
- **AND** logo URLs and descriptions do NOT trigger an event

---

### Requirement: HTTPS and transport security

The system SHALL enforce HTTPS on all feed requests and maintain the same redirect, hop-count, and size-bound guarantees as the standard feed fetch.

#### Scenario: HTTP feed URL rejected

- **WHEN** the feed URL has `http://` scheme
- **THEN** the request is rejected
- **AND** the feed result includes an error "Feed URL must use HTTPS"

#### Scenario: Redirect to HTTP rejected

- **WHEN** an HTTPS feed URL redirects to `http://`
- **THEN** the request is rejected
- **AND** the feed result includes an error "Redirect to non-HTTPS URL rejected"

#### Scenario: Redirect hop limit enforced

- **WHEN** a feed fetch involves redirects
- **THEN** no more than 5 hops are followed

#### Scenario: Index size limit enforced

- **WHEN** a feed index response is larger than 2 MB
- **THEN** the response is rejected
- **AND** the feed result includes an error "Feed index too large"
