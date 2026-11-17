# Tray Default Separator: Implementation Tasks

Hide the separator after the tray submenu's default agent when the default is the submenu's only
item; keep it when other agents follow. One MODIFIED requirement in `specs/tray-menu/spec.md`
("Default agent is first in every project submenu"); all original scenario names preserved, one new
scenario added.

Reference design: `design.md` (Decision 1). Line numbers below are from the clean working tree at
plan time; re-read `src/CLIHub/Tray/TrayMenuBuilder.cs` before editing if anything shifted.

## Stage 1: Conditional separator in the builder

- [ ] 1.1 In `src/CLIHub/Tray/TrayMenuBuilder.cs`, at the top of `PopulateProjectSubmenu`
      (`:143-145`), materialize the non-default initialized agents before any `items.Add`:
      `var otherAgents = entry.InitializedAgents.Where(plugin => entry.DefaultAgent?.Id != plugin.Id).ToList();`
      (Decision 1). This replaces the inline skip at `:162-165`.
- [ ] 1.2 In the default branch (`:146-157`), keep the default `MenuItem` construction and
      `items.Add(defaultItem)` (`:155`) unchanged, but make `items.Add(new Separator())` (`:156`)
      conditional: add the separator only when `otherAgents.Count > 0`.
- [ ] 1.3 Replace the `initializedCount` counter loop (`:159-171`) with a `foreach` over
      `otherAgents` adding each agent item exactly as today (`:167-169`); delete the counter and the
      skip.
- [ ] 1.4 Change the `(no agents)` gate (`:173`) to
      `if (entry.DefaultAgent is null && otherAgents.Count == 0)` — semantics identical to the old
      `initializedCount` (it already excluded the default, `:162-170`).
- [ ] 1.5 Confirm nothing else in the builder changed: default-first order, labels, `Play` icons,
      `IsEnabled` gating, click wiring, and the top-level separator before `Exit` (`:76`).

### Gate: build check
```powershell
dotnet build CLIHub.sln
```

## Stage 2: Default-only test coverage

- [ ] 2.1 In `tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs`, add
      `Build_DefaultAgentOnly_ShowsSingleItemWithoutSeparator` — one favorite project, catalog with
      only `default` (launch command present), `defaultAgentId: "default"`; assert the submenu has
      exactly one item with the `default` header and `Assert.Empty(submenu.Items.OfType<Separator>())`.
- [ ] 2.2 Add `Build_InitializedDefaultAgentOnly_ShowsSingleItemWithoutSeparator` — same shape with
      the default fully initialized (no `installed`/`available` overrides); assert exactly one
      `default` item (de-duplication holds with an empty remainder) and no separator.
- [ ] 2.3 Add `Build_DefaultAgentWithAllOthersExcluded_ShowsSingleItemWithoutSeparator` — catalog
      with `default` + `other`, `other` excluded from the project via the `available` predicate
      (helper at `:440-471`); assert exactly one item (`default`) and no separator, proving the rule
      keys on rendered others, not catalog contents.
- [ ] 2.4 Confirm the separator-present tests keep passing unchanged:
      `Build_ConfiguredDefaultAgent_IsFirstFollowedBySeparatorEvenWhenNotInitialized` (`:80-102`),
      `Build_DefaultWithoutLaunchCommand_IsFirstAndDisabledWithSeparator` (`:105-130`), and
      `Build_InitializedDefaultAgent_AppearsOnceAsFirstItemWithSeparator` (`:133-155`), plus the
      no-default and `(no agents)` tests (`:158-206`).

### Gate: build and tray menu tests
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~TrayMenuBuilderTests"
```

## Stage 3: Full verification

- [ ] 3.1 `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings.
- [ ] 3.2 `dotnet test CLIHub.sln` — expect all green.
- [ ] 3.3 `openspec validate --all` — expect 0 errors.
- [ ] 3.4 `graphify update .`

### Gate: all gates passed
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
graphify update .
```

## Manual smoke (user-confirmed, before commit/archive)

- [ ] 4.1 Launch the app with a default agent configured; open a favorite project's submenu where the
      default is the only agent (or where detection excludes the others) and confirm it shows exactly
      the default item with no trailing separator and no `(no agents)` item.
- [ ] 4.2 Open a favorite project's submenu that lists other agents besides the default and confirm
      the layout is unchanged: default first, separator, then the other agents; invoking any agent
      still makes the project current and launches it.
- [ ] 4.3 Confirm a project with no default and no initialized agents still shows the disabled
      `(no agents)` item, and that the top-level separator before `Exit` is still present.
