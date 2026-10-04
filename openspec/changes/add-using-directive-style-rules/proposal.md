# Proposal: Using-directive organization rules in the C# style contract

## Why

The root `.editorconfig` already enforces where using directives go (inside the namespace) but not how they are organized. Today every C# file sorts its usings into one flat alphabetical block, so BCL namespaces (`System.*`) are mixed with project, third-party, and framework namespaces, and nothing visually separates the groups. The result is noisier import blocks, slower scanning during review, and organization that depends on each developer's local IDE settings instead of the repository contract.

## What Changes

- Extend the root `.editorconfig` with using-directive organization rules for C#:
  - BCL-related namespaces (`System.*`, plus `Windows.*` which the standard rule covers) are placed first when sorting.
  - Using directives form ordered groups — BCL, then `Microsoft.*`, then the project's own `CLIHub.*`, then remaining third-party namespaces — each group sorted alphabetically, with a blank line between consecutive groups.
- Normalize existing C# sources under `src/` and `tests/` to the new organization.
- Document the new rules in the `AGENTS.md` Code Style section.

Assumption (recorded): "BCL-related namespaces" is interpreted as the `System.*` namespaces, which is what the standard `dotnet_sort_system_directives_first` rule hoists first (it also hoists `Windows.*`). Groups follow fixed buckets (BCL / `Microsoft.*` / `CLIHub.*` / remaining third-party) per user feedback; alphabetical order applies within each group.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `code-style`: new requirements that the style configuration organizes using directives into ordered, sorted groups: BCL-related namespaces first, alphabetical order within each group, and a blank line between different groups.

## Impact

- `.editorconfig` — new formatter entries for using-directive organization.
- All C# files under `src/` and `tests/` — mechanical normalization of using blocks (ordering and blank-line separation).
- `AGENTS.md` — Code Style section gains the three new rules.
- No runtime behavior change and no dependency change; the only build-visible effect is IDE-level style feedback if sources are left unnormalized.
