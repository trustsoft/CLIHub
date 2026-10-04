## Context

See proposal.md — Why. The style contract lives in the single root `.editorconfig`, which already pins `csharp_using_directive_placement = inside_namespace:warning` (IDE0065) but has no rules for ordering or grouping of using directives. All three projects use `ImplicitUsings` (global usings), so file-level using blocks are relatively short, yet they still mix `System.*` with `Microsoft.*`, project, and third-party namespaces in one flat alphabetical block (verified in current sources, e.g. `App.xaml.cs`). The `.editorconfig` already mixes standard `dotnet_*`/`csharp_*` keys with JetBrains `resharper_*` keys, and pins build-enforced warnings in a dedicated "Build-time severity pins" section.

## Goals / Non-Goals

**Goals:**

- Encode the three rules (BCL-first ordering, alphabetical sorting within groups, blank line between groups) in the root `.editorconfig` so all conforming tools (Visual Studio, Rider/ReSharper) agree on one configuration.
- Bring every existing C# file under `src/` and `tests/` into the new organization in one mechanical pass.
- Keep `AGENTS.md` (Code Style section) in sync as the developer/agent-facing summary of the contract.

**Non-Goals:**

- Build-time enforcement of these rules via analyzer severity pins (see Decisions).
- CI-level format verification (`dotnet format --verify-no-changes`) — a separate workflow change if ever wanted.
- Changing global usings, `ImplicitUsings` settings, or any using-related placement rules.

## Decisions

**D1 — Use the standard `dotnet_*` keys plus explicit JetBrains formatter keys.**
Add to the `.editorconfig` "Microsoft .NET properties" section:

- `dotnet_sort_system_directives_first = true` — the standard key; JetBrains honors it ("Place `System.*` and `Windows.*` namespaces first when sorting 'using' directives").
- `dotnet_separate_import_directive_groups = true` — the standard key for the blank line between groups.
- JetBrains equivalents so the formatter behavior in Rider/ReSharper is explicit and not dependent on how their tooling maps the `dotnet_*` keys (confirmed against the JetBrains EditorConfig index): `resharper_blank_lines_between_using_groups = 1` and `resharper_sort_usings_with_system_first = true`.
- The full bucket layout (BCL → `Microsoft.*` → `CLIHub.*` → remaining third-party, per user feedback) has no standard tool switch: IDE organize-usings only guarantees the BCL group first plus blank-line separation, and an alphabetical tail sort would place `CLIHub.*` before `Microsoft.*`. The bucket order is therefore a maintained convention enforced by the normalization pass (D3), with the `.editorconfig` keys anchoring the BCL-first part.

Alternative considered: only the `dotnet_*` keys — rejected because the repo's formatting behavior is largely driven by JetBrains keys already; duplicating the intent in both key families removes any ambiguity about which settings the IDE applies.

**D2 — No build-time severity pins for these rules.**
Sorting and grouping of usings have no dedicated analyzer rule ID; the closest is IDE0055 ("Fix formatting"), which covers *all* formatting options (indentation, spacing, line breaks). Pinning `dotnet_diagnostic.IDE0055.severity = warning` like the existing pins would flood builds with unrelated formatting diagnostics across the whole solution. The rules therefore stay formatter/IDE-enforced (organize-usings, Rider Cleanup, `dotnet format`), matching how the file already treats non-pinned formatting options (`:suggestion` entries). Alternative rejected: pinning IDE0055 — too broad for this change and would require a codebase-wide formatting normalization first.

**D3 — Normalize existing sources via a deterministic pass, verified by build.**
Rewrite each file-level using block into the fixed buckets — `System.*` (and `Windows.*`) alphabetically, blank line, `Microsoft.*` alphabetically, blank line, `CLIHub.*` alphabetically, blank line, remaining third-party namespaces alphabetically — always comparing namespace names (without the `using ` prefix and `;`), not whole lines. Sorting is applied by the normalization script and can be repeated (idempotent); IDE cleanup alone cannot reproduce the full bucket order (see D1). No logic edits, no commit mixing with feature work.

**D4 — Document the rules in `AGENTS.md` only.**
The Code Style section in `AGENTS.md` is the canonical summary for humans and agents; the `.editorconfig` remains the single enforceable source. No new docs page.

## Risks / Trade-offs

- [Enforcement is IDE/formatter-level only; a violating file compiles cleanly] → Accepted trade-off per D2, consistent with existing `:suggestion` options; the normalization pass leaves the tree compliant, and reviewers see diffs at group boundaries.
- [IDE organize-usings/cleanup reorders the tail alphabetically (`CLIHub.*` before `Microsoft.*`) and may collapse the buckets] → The normalization pass is the source of truth for the bucket layout; re-run it after any bulk IDE cleanup, and keep manual edits in the bucket order.
- [`dotnet_separate_import_directive_groups` is not listed in JetBrains' EditorConfig index] → JetBrains-equivalent keys are set explicitly (D1), so Rider/ReSharper behavior does not depend on that mapping.
- [Normalization touches nearly every C# file, producing a large mechanical diff that can obscure review] → Keep it in one dedicated commit with a conventional message; verify `dotnet build CLIHub.sln` stays green; forbid mixing logic changes into it.
- [Global usings mean some `System.*` namespaces never appear in file-level blocks, so the BCL group may be empty in many files] → Harmless; the rules simply degenerate to one alphabetically sorted group with no separator.

## Migration Plan

1. Add the `.editorconfig` keys (D1) in one commit.
2. Run the cleanup/normalization pass over `src/` and `tests/` (D3) in a second commit; build to verify.
3. Update `AGENTS.md` Code Style section (D4), optionally in the second commit.
4. Rollback: revert the commits; config-only plus ordering edits, no data or runtime impact.

## Open Questions

None.
