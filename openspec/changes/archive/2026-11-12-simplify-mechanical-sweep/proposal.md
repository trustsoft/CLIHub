# Port the mechanical MVM/boilerplate simplification sweep from the backup branch

## Why

Four duplicated or suppressed blocks are a re-reading tax on every future edit of the MVM layer, and
they invite the next copy of the same boilerplate:

- `RelayCommand` and `RelayCommand<T>` carry a byte-identical ~25-line availability-plumbing block
  (`explicitCanExecuteChanged` field, the `CanExecuteChanged` add/remove that also wires
  `CommandManager.RequerySuggested`, and `RaiseCanExecuteChanged`) — `RelayCommand.cs:12,25-50`
  versus `RelayCommandOfT.cs:13,26-51`.
- All eight `src/CLIHub/Converters/*.cs` converters redeclare the same `ConvertBack` that throws
  `NotSupportedException`, plus the `IValueConverter`/`IMultiValueConverter` plumbing; a future
  one-way converter copies that boilerplate again.
- `PluginUpdateCheck` answers presence of `Package`/`Repo` only through ad-hoc
  `string.IsNullOrWhiteSpace` checks scattered over `UpdateCheckSource.TryResolve`
  (`UpdateCheckSource.cs:74,88`); there is no compiler-teaching `HasPackage`/`HasRepo` on the model
  (solution-wide grep: zero occurrences on master).
- `NumericField.ResolveNextValue`/`ResolveCurrentValue` duplicate the text-then-placeholder parse
  (`NumericField.cs:259-288`), and `SettingsViewModel.ResolveProbeTtlMinutes`/
  `ResolveProbeTimeoutSeconds` duplicate the positive-int-or-default resolve
  (`SettingsViewModel.cs:309-317`).

The branch `backup-before-reset` already solved exactly this in commit `02aaf6a` ("remove the last
compiler warning and collapse command/converter boilerplate", archive
`2026-10-28-simplify-mechanical-sweep`), and the porting analysis
(`backup-port-05-applicable-mechanical-sweep.md`, verified against master @ `8ca4c3f`) classifies the
sweep as "absent and applicable — ports almost verbatim". This change re-lands that sweep, adapted to
master's current code. Each item is mechanical, isolated, and behavior-preserving; existing tests pin
the behavior and are not edited.

## What Changes

All master facts below were re-verified by this preparation session at HEAD `8ca4c3f` against the
working tree (evidence with `file:line` in `design.md`).

**Stage 1 — model annotations and the two parse-fallback helpers:**

1. **`PluginUpdateCheck` presence properties.** In
   `src/CLIHub.Core/Models/Plugins/PluginUpdateCheck.cs` (positional record at `:14`) add
   `using System.Diagnostics.CodeAnalysis;` after the namespace and two documented get-only
   properties: `[MemberNotNullWhen(true, nameof(Package))] public bool HasPackage` and
   `[MemberNotNullWhen(true, nameof(Repo))] public bool HasRepo`, bodies
   `!string.IsNullOrWhiteSpace(...)`. Route `UpdateCheckSource.TryResolve`'s blank checks
   (`UpdateCheckSource.cs:74,88`) through them — the predicate is identical, so the single-rule
   design of the resolver is kept. **No edit to `AgentReleaseClient`**: master's
   `updateCheck.Package!`/`updateCheck.Repo!` suppressions (`AgentReleaseClient.cs:119-120`) are NOT
   removable by this port (see decision D6 and Non-goals).
2. **`NumericField` parse-or-placeholder helper.** In `src/CLIHub/Controls/NumericField.cs` extract
   `private bool TryResolveBaseValue(out long value)` for the text-then-placeholder sequence that
   `ResolveNextValue` (`:259-273`) and `ResolveCurrentValue` (`:275-288`) duplicate; the two callers
   keep their own clamp expressions and distinct final fallbacks (`Minimum` for stepping, `0` for
   reporting). Verbatim port of `02aaf6a` — master's code is byte-identical to the backup's
   pre-change state.
3. **`SettingsViewModel` positive-int helper.** In
   `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` add
   `private static int ResolvePositiveIntOr(string? text, int fallback)` and reduce
   `ResolveProbeTtlMinutes()` (`:309-312`) and `ResolveProbeTimeoutSeconds()` (`:314-317`) to calls;
   both `(int)AgentVersionService.Default*` fallback expressions stay exactly as written. Verbatim
   port.

**Stage 2 — command base and the one-way converter bases:**

4. **`RelayCommandBase`.** Add `src/CLIHub/ViewModels/Mvvm/RelayCommandBase.cs`:
   `public abstract class RelayCommandBase : ICommand` owning the `explicitCanExecuteChanged` field,
   the `CanExecuteChanged` add/remove (both accessors wire `CommandManager.RequerySuggested`), and
   `RaiseCanExecuteChanged()`, copied verbatim from `RelayCommand.cs:12,25-50` (identical at
   `RelayCommandOfT.cs:13,26-51`), plus `public abstract` `CanExecute`/`Execute`. `RelayCommand` and
   `RelayCommand<T>` stay `public sealed`, keep constructors, fields, and bodies, and become
   overrides onto the base. New file follows master style: **no** `// <file …>` header comment (none
   of master's `src/` files carry one).
5. **One-way converter bases.** Add `src/CLIHub/Converters/OneWayValueConverter.cs` and
   `OneWayMultiValueConverter.cs`: `public abstract object Convert(...)` plus a non-virtual
   `ConvertBack` that throws `NotSupportedException`, owning the "one-way bindings only"
   documentation once. All eight converters derive from them —
   `EmptyToCollapsedVisibilityConverter`, `IndexToDividerVisibilityConverter`,
   `PageKeyVisibilityConverter`, `PathToImageConverter` (the only non-sealed one),
   `ThemeBrushKeyConverter`, `UpperCaseTextConverter` from the value base; `PathDisplayConverter`
   and `ResourceKeyToTemplateConverter` from the multi-value base — every derived `Convert` is
   normalized to exactly the base's parameter and return annotations (`object parameter` →
   `object? parameter`; `PathToImageConverter`'s return narrows `object?` → `object`, justified by
   `LogoImageSourceCache.GetOrLoad` returning non-nullable `ImageSource`). Convert bodies, XAML
   resource declarations, and the DI registration (`ApplicationHost.cs:138`) stay unchanged — the
   bases are drop-in because every registration addresses the converter by its unchanged concrete
   type name (`App.xaml:36-46`, `LaunchWindowStyles.xaml:18`, `SettingsWindow.xaml:33`).

**Stage 3 — final gates and scope check** (build, test, validate, graphify; no coordinator or
backlog work — see Non-goals).

## Impact

- Source files changed (11 modified, 3 new):
  - modified `src/CLIHub.Core/Models/Plugins/PluginUpdateCheck.cs` (using + two documented
    properties),
    `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs` (two predicate call-sites),
    `src/CLIHub/Controls/NumericField.cs` (one helper, two call sites),
    `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (one helper, two call sites),
    `src/CLIHub/ViewModels/Mvvm/RelayCommand.cs`, `RelayCommandOfT.cs`,
    and all eight `src/CLIHub/Converters/*.cs`;
  - new `src/CLIHub/ViewModels/Mvvm/RelayCommandBase.cs`,
    `src/CLIHub/Converters/OneWayValueConverter.cs`, `OneWayMultiValueConverter.cs`.
- `AgentReleaseClient.cs` is deliberately untouched (D6).
- Test impact: **none** — no test file added, edited, or skipped. The four `ConvertBack_AnyValue_Throws`
  facts keep passing through the inherited member; probe-policy pins
  (`SettingsViewModelApplicationTests.cs:867,894`) keep passing because the resolver outputs are
  unchanged.
- User-visible impact: none.
- Spec impact: none — pure internal refactor plus compiler annotations, so `.openspec.yaml` declares
  `skip_specs: true` and no `specs/` deltas are created. The only spec that mentions a converter at
  all (`openspec/specs/project-management/spec.md:318`, "the shared logo image converter") constrains
  rendering behavior, which does not change.

## Non-goals

- No behavior change of any kind; `Convert`/`CanExecute`/`Execute` bodies stay byte-identical; XAML
  resource declarations and DI registrations unchanged.
- **Not** removing the two null-forgiving suppressions `updateCheck.Package!`/`updateCheck.Repo!`
  (`AgentReleaseClient.cs:119-120`). Master moved presence validation into
  `UpdateCheckSource.TryResolve`, which breaks the compiler-visible guard chain that made the
  suppressions removable on the backup branch; every no-suppression alternative exceeds a mechanical
  sweep (D6, open question Q1).
- **Not** porting the backup's `MenuActionCoordinator` guard removal — the class no longer exists on
  master; the eight `ThrowIfNull` guards live once in `LaunchWindowActionBuilder.cs:35-42` (D8).
- **Not** the backup's `docs/backlog.md` bookkeeping — master's backlog has no corresponding entries
  (D9).
- **Not** the `ApplicationSession` factory refactor, **not** `ProbeCache` extraction (concurrent
  change `probe-cache-extraction`), **not** the dialog/chrome helpers (concurrent change
  `dialog-and-chrome-helpers`).
- No new converters, no new interfaces, no helpers beyond the three base types and the two private
  parse helpers named above; no test edits; no build-configuration change.
- No commit, push, or archive in this change's tasks; those follow the repo approval policy.

## Dependencies

- None blocking. Two sibling changes are being prepared concurrently in `openspec/changes/` —
  `probe-cache-extraction` (new `src/CLIHub.Core/Agents/ProbeCache.cs`; touches
  `AgentVersionService`/`AgentUpdateAvailabilityService`) and `dialog-and-chrome-helpers`
  (`src/CLIHub/Launching/*`, `src/CLIHub/Views/*` code-behind). Neither shares a file with this
  change; if either lands first, re-run the stage gates before continuing (open question Q2).
- Porting source: branch `backup-before-reset` commit `02aaf6a` and its archived proposal/design
  (`git show backup-before-reset:openspec/changes/archive/2026-10-28-simplify-mechanical-sweep/`).
  Master adapted: no file-header comments, nullable-fix scope reduced to the model side.
- No spec dependencies: `skip_specs: true`, pure internal refactor.
