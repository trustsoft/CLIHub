# plugin-index-signing Specification

## Purpose
Provides cryptographic signature verification of the plugin feed index so the client can authenticate the index before parsing. The official feed index is signed using ECDSA with the NIST P-256 curve and SHA-256 hashing; the client verifies the signature against an embedded public key before any entry is parsed. A time-bounded transition window allows the feed to begin publishing signatures before the client enforces them. Verification applies only to the default official feed; custom feed URLs configured by the user are trusted as-is.

## Requirements

### Requirement: Signature format and verification primitive

The system SHALL verify an ECDSA P-256 signature over the raw index body bytes against an embedded public key. The signature is transported in a JSON envelope (`index.sig`) with an `algorithm` field declaring the signing algorithm, a `signature` field holding the base64-encoded DER signature, and an optional `signedAt` timestamp.

#### Scenario: Valid signature verified successfully

- **WHEN** the `index.sig` envelope contains `algorithm: "ecdsa-p256-sha256"` and a valid base64-encoded DER signature
- **AND** the signature verifies against the index body bytes using the embedded public key
- **THEN** verification returns true
- **AND** the index proceeds to parsing

#### Scenario: Tampered index body rejected

- **WHEN** the index body bytes differ from the bytes that were signed
- **AND** the signature is valid for the original bytes but not the modified bytes
- **THEN** verification returns false
- **AND** the feed result includes an error "Plugin feed signature is invalid. The feed may have been tampered with."

#### Scenario: Wrong public key rejected

- **WHEN** the signature was produced with a different key pair than the embedded public key
- **THEN** verification returns false

#### Scenario: Unsupported algorithm rejected

- **WHEN** the `index.sig` envelope has an `algorithm` value other than `"ecdsa-p256-sha256"`
- **THEN** the feed result includes an error "Unsupported signature algorithm: {algorithm}"
- **AND** Success is false

#### Scenario: Malformed signature envelope rejected

- **WHEN** the `index.sig` file is not valid JSON, is missing the `algorithm` field, or the `signature` field is not valid base64
- **THEN** the feed result includes an error describing the malformation
- **AND** Success is false

#### Scenario: Signature envelope with unknown fields accepted

- **WHEN** the `index.sig` envelope contains fields beyond `algorithm`, `signature`, and `signedAt`
- **THEN** those fields are ignored (forward compatibility)
- **AND** verification proceeds normally

---

### Requirement: Signature fetch and verify pipeline

The system SHALL fetch the `index.sig` file after fetching the index body and before parsing it, using the same HTTPS-only transport with manual redirect enforcement and a 5-hop cap. The signature file is bounded to 4 KB. Invalid signatures produce a hard error with no cache fallback; missing signatures are accepted with a warning during the transition window and treated as a hard error after.

#### Scenario: Signature fetched and verified before parse

- **WHEN** the index body is fetched successfully (HTTP 200) from the default official feed URL
- **THEN** the `index.sig` file is fetched from the same URL directory
- **AND** the signature is verified against the index body bytes
- **AND** only after successful verification is the body passed to `ParseFeedIndex`

#### Scenario: Invalid signature is a hard error with no cache fallback

- **WHEN** signature verification fails (tampered body, wrong key, algorithm mismatch, malformed envelope)
- **THEN** the feed result includes a user-facing error
- **AND** the cached index is NOT used as a fallback
- **AND** Success is false

#### Scenario: Signature fetch uses HTTPS and redirect enforcement

- **WHEN** the signature file URL has `http://` scheme or redirects to a non-HTTPS URL
- **THEN** the signature fetch is rejected before sending or after detecting the redirect
- **AND** the feed result includes an appropriate error
- **AND** the rejection is treated as a missing signature (transition-window logic applies)

#### Scenario: Signature file exceeds 4 KB bound

- **WHEN** the `index.sig` response body exceeds 4 KB
- **THEN** the signature file is rejected
- **AND** the feed result includes an error "Plugin feed signature file too large"
- **AND** Success is false

#### Scenario: Signature fetched after redirect follows hop limit

- **WHEN** the signature file URL involves redirects
- **THEN** no more than 5 hops are followed
- **AND** exceeding the hop limit rejects the fetch

---

### Requirement: Trust anchor for official feed only

The system SHALL embed the official feed's ECDSA P-256 public key as a compile-time constant and SHALL verify the index signature only when the resolved feed URL is the default official URL. Custom feed URLs configured by the user SHALL skip signature verification entirely.

#### Scenario: Default feed URL triggers verification

- **WHEN** the resolved feed URL equals `FeedUrlProvider.DefaultFeedUrl`
- **THEN** the signature is fetched and verified
- **AND** the embedded public key constant is used for verification

#### Scenario: Custom feed URL skips verification

- **WHEN** the `PluginFeedUrl` preference is a non-empty custom HTTPS URL
- **THEN** no `index.sig` is fetched
- **AND** the index is parsed without signature verification
- **AND** the feed result has `SignatureValid: true` (non-applicable, not a failure)

#### Scenario: Disabled feed skips verification

- **WHEN** the `PluginFeedUrl` preference is an empty string (feed disabled)
- **THEN** no feed fetch occurs (existing behavior)
- **AND** no signature fetch occurs

#### Scenario: Embedded public key cannot be changed at runtime

- **WHEN** the application is running
- **THEN** the public key used for verification is the compile-time constant
- **AND** no file I/O, registry read, or configuration field can override it

---

### Requirement: Transition window for missing signatures

The system SHALL accept a missing `index.sig` file with a logged warning before a hardcoded cutoff date, and SHALL treat a missing signature as a hard error after that cutoff. A boolean flag allows the requirement to be enforced regardless of the date.

#### Scenario: Missing signature accepted with warning before cutoff

- **WHEN** the current UTC date is before `SignatureRequiredAfter`
- **AND** the `RequireFeedSignature` flag is false
- **AND** the signature file is missing (HTTP 404) or the signature GET fails
- **THEN** a warning is logged: "Plugin feed signature is missing; accepted during transition window."
- **AND** the index is parsed normally
- **AND** the feed result has `SignatureWarning` set and `SignatureValid: true`

#### Scenario: Missing signature is a hard error after cutoff

- **WHEN** the current UTC date is on or after `SignatureRequiredAfter`
- **OR** the `RequireFeedSignature` flag is true
- **AND** the signature file is missing
- **THEN** the feed result includes an error "Plugin feed signature is required but missing."
- **AND** Success is false

#### Scenario: Flag overrides date — missing rejected before cutoff

- **WHEN** the `RequireFeedSignature` flag is true
- **AND** the current UTC date is before `SignatureRequiredAfter`
- **AND** the signature file is missing
- **THEN** the missing signature is treated as a hard error
- **AND** the flag takes precedence over the date

#### Scenario: After-cutoff enforcement cannot be silenced by configuration

- **WHEN** the current UTC date is on or after `SignatureRequiredAfter`
- **THEN** a missing signature is always a hard error
- **AND** no configuration or preference field can disable this enforcement
