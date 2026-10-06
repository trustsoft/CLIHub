## Context

`PluginManager.LoadPlugins` currently consumes `Directory.GetDirectories` directly and checks duplicates against the already loaded list. The existing first-loaded policy is therefore dependent on file-system enumeration order, while the current warning names only the skipped directory.

## Goals / Non-Goals

**Goals:**

- Make plugin loading and duplicate selection reproducible across runs.
- Preserve the existing first-valid-plugin behavior after introducing stable ordering.
- Make duplicate warnings identify both the winner and the skipped directory.
- Keep the change local to plugin discovery and its tests.

**Non-Goals:**

- Changing plugin descriptor validation or JSON format.
- Changing plugin seeding precedence or user-visible plugin ordering beyond the defined directory order.
- Adding file watching, reload behavior, or a new catalog abstraction.

## Decisions

- Sort the directory paths before iteration with `StringComparer.OrdinalIgnoreCase`, followed by `StringComparer.Ordinal` for deterministic tie-breaking. This avoids relying on Windows file-system enumeration and does not require a new abstraction for a local operation.
- Preserve the first-loaded duplicate policy. Because iteration is now stable, the winner is stable without changing the plugin model or introducing priority metadata.
- Track the loaded directory when checking duplicates and include both paths in the warning. This provides enough diagnostic context without changing the existing warning level or skip behavior.
- Assert loaded order and duplicate winner in `PluginManagerTests`; capture logs with an existing test logger pattern only if needed to verify the expanded warning message.

## Risks / Trade-offs

- [Risk] Directory path casing or equivalent paths can make ordering surprising on Windows. -> Mitigation: use case-insensitive comparison with ordinal tie-breaking and test distinct directory names.
- [Risk] Existing consumers may have implicitly relied on file-system enumeration order. -> Mitigation: the behavior is currently unspecified; the change documents and tests the new deterministic order.
