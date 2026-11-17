## Context

See `proposal.md` — Why and What Changes. This is a single-method behavior fix in the tray menu
builder plus test coverage and a one-requirement spec delta.

### Verified current state (2026-11-17, clean working tree)

**`TrayMenuBuilder.PopulateProjectSubmenu` (`src/CLIHub/Tray/TrayMenuBuilder.cs:143-177`).**

- Default branch (`:146-157`): when `entry.DefaultAgent is { } defaultAgent`, builds the default
  `MenuItem` — `Header = defaultAgent.Name`, `IsEnabled = defaultAgent.Commands?.Launch is not null`
  (`:151`), `Icon = CreateGlyph(IconGlyphs.Play)` (`:152`), click wired to
  `commands.SelectProjectAndLaunchAgentAsync(project.Id, defaultAgent)` (`:154`) — adds it
  (`:155`), then **unconditionally** adds `items.Add(new Separator())` (`:156`).
- Initialized loop (`:159-171`): iterates `entry.InitializedAgents`, skipping the default id
  (`if (entry.DefaultAgent?.Id == plugin.Id) continue;` at `:162-165`), adds each remaining agent as
  a `Play`-iconed item with the same launch click (`:167-169`), and counts them in `initializedCount`
  (`:170`) — so the counter already measures *non-default* agents only.
- Empty rule (`:173-176`): the disabled `(no agents)` item is added only when
  `entry.DefaultAgent is null && initializedCount == 0`.

**State shape (`src/CLIHub/Tray/TrayMenuState.cs:11-13`).** `TrayProjectAgents(Project Project,
Plugin? DefaultAgent, IReadOnlyList<Plugin> InitializedAgents)` — `InitializedAgents` is not
pre-filtered against `DefaultAgent`; the de-duplication lives in the builder (the skip at
`TrayMenuBuilder.cs:162-165` is what makes an initialized default appear once). No change to this
shape is needed.

**Tests (`tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs`).**

- `Build_ConfiguredDefaultAgent_IsFirstFollowedBySeparatorEvenWhenNotInitialized` (`:80-102`):
  plugins `default` (not installed, via `installed: plugin => plugin.Id != "default"`) + `other`;
  asserts 3 items — default, `Separator` at index 1, `other`. The separator must remain.
- `Build_DefaultWithoutLaunchCommand_IsFirstAndDisabledWithSeparator` (`:105-130`): default without
  a launch command + `other`; asserts 3 items — disabled default, `Separator`, `other`, and that
  `default` appears exactly once. The separator must remain.
- `Build_InitializedDefaultAgent_AppearsOnceAsFirstItemWithSeparator` (`:133-155`): default + `other`
  both initialized; asserts 3 items — default once, `Separator`, and the de-duplication. The
  separator must remain.
- `Build_NoDefaultConfigured_HasNoDefaultItemOrSeparator` (`:158-172`) and
  `Build_DefaultIdNotInCatalog_HasNoDefaultItemOrSeparator` (`:174-189`): assert an empty separator
  set when no default is rendered — unchanged by this change.
- `Build_ProjectWithNoDefaultAndNoInitializedAgents_ShowsDisabledNoAgentsItem` (`:192-206`): the
  `(no agents)` rule — unchanged.
- The `CreateState` helper (`:440-471`) already supports `defaultAgentId`, `installed`, and
  `available` predicate overrides, so the new default-only cases need no fixture changes.

**Coverage gap.** Every existing test that renders a default item also includes a second agent
(`other`) in the submenu; no test exercises a default-only submenu, which is exactly the case whose
separator output changes.

**Spec.** `openspec/specs/tray-menu/spec.md:103-141` holds the durable requirement "Default agent is
first in every project submenu" with six scenarios. The requirement preamble promises "followed by a
separator" unconditionally (`:105-108`); "Default agent is first" (`:110-114`) and "An initialized
default agent appears exactly once" (`:127-131`) repeat the promise in their THEN clauses. The other
scenarios and the neighboring requirements ("Initialized agents per project submenu" `:63-101`,
"Actionable menu items carry MDL2 icons" `:143-149` — separators carry no icon) are unaffected: the
placeholder rule and the "exactly once" de-duplication stay as they are.

## Decisions

### Decision 1: Compute the non-default agents first; add the separator conditionally (DECIDED)

Inside `PopulateProjectSubmenu`, materialize the other agents before touching `items`:

```csharp
var otherAgents = entry.InitializedAgents
    .Where(plugin => entry.DefaultAgent?.Id != plugin.Id)
    .ToList();
```

Then: keep the default branch exactly as today **except** that `items.Add(new Separator())`
(`TrayMenuBuilder.cs:156`) becomes conditional on `otherAgents.Count > 0`; iterate `otherAgents` for
the remaining items; and replace the `initializedCount` counter with `otherAgents.Count` in the
`(no agents)` gate, keeping its `entry.DefaultAgent is null` conjunct unchanged (`:173`). The empty
rule's semantics are identical: `initializedCount` already excluded the default (the skip precedes
the increment, `:162-170`), and the `DefaultAgent is null` conjunct makes that exclusion moot for the
placeholder anyway.

Rationale: the separator decision is made once, at the single point where the separator is created;
item order in the separator-present case is byte-identical to today; no post-hoc collection surgery;
no new types or parameters. `System.Linq` is available via implicit usings, and the file already uses
collection iteration idioms of the same shape.

### Alternatives rejected

- **Post-filter / trailing-separator removal.** Build the submenu as today, then remove a separator
  that ended up as the last item (or sweep `items` after population). Rejected: it mutates a
  populated `ItemCollection` after the fact, hides the invariant behind a cleanup pass, and invites
  confusion with the legitimate top-level trailing separator before `Exit`
  (`TrayMenuBuilder.cs:76`). The conditional add states the intent at the source.
- **Builder flag or tail-append helper.** Extract an `AppendTail(ItemCollection, IReadOnlyList<Plugin>,
  bool hasDefault)`-style helper or thread a `showSeparator` boolean through the population. Rejected:
  new surface for a one-call-site rule; `PopulateProjectSubmenu` stays readable with the local list
  and one conditional.

## Test Plan

Keep (must stay green, separator still present because a second agent is in the submenu):

- `Build_ConfiguredDefaultAgent_IsFirstFollowedBySeparatorEvenWhenNotInitialized`
- `Build_DefaultWithoutLaunchCommand_IsFirstAndDisabledWithSeparator`
- `Build_InitializedDefaultAgent_AppearsOnceAsFirstItemWithSeparator`
- `Build_NoDefaultConfigured_HasNoDefaultItemOrSeparator`,
  `Build_DefaultIdNotInCatalog_HasNoDefaultItemOrSeparator`,
  `Build_ProjectWithNoDefaultAndNoInitializedAgents_ShowsDisabledNoAgentsItem` (unchanged behavior)

Add (`tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs`, `RunOnSta`, naming pattern
`MethodOrScenario_Condition_ExpectedResult`):

1. `Build_DefaultAgentOnly_ShowsSingleItemWithoutSeparator` — default configured and resolvable, no
   other plugin in the catalog; assert `submenu.Items.Count == 1`, the default header, and
   `Assert.Empty(submenu.Items.OfType<Separator>())`.
2. `Build_InitializedDefaultAgentOnly_ShowsSingleItemWithoutSeparator` — default configured and also
   initialized (the de-duplication path with an empty remainder); assert exactly one item — the
   default — and no separator.
3. `Build_DefaultAgentWithAllOthersExcluded_ShowsSingleItemWithoutSeparator` — `default` + `other` in
   the catalog, `other` excluded from the project (e.g. `available: plugin => plugin.Id == "default"`);
   assert exactly one item and no separator, proving the rule keys on *rendered* others, not catalog
   contents.

Gates: `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`,
`graphify update .` (see `tasks.md`).

## Deviations

None. The fix lands exactly where the brief points (`PopulateProjectSubmenu`) and no other behavior
moves.

## Open Questions

- None blocking. One naming nit for the implementation session: the two existing tests whose names
  embed `WithSeparator` / `FollowedBySeparator` keep their names — they genuinely render the
  separator — so no renames are expected.
