# Design: simplify-mechanical-sweep

## Context (verified current state)

Every `file:line` below was verified by direct reads and solution-wide greps against the working
tree at HEAD `8ca4c3f` (`openspec/changes/` additionally holds two sibling changes prepared
concurrently, see Q2). The porting source is branch `backup-before-reset` commit `02aaf6a`, read via
`git show`, and its archived proposal/design/tasks under
`openspec/changes/archive/2026-10-28-simplify-mechanical-sweep/` on that branch.

### 1 — `RelayCommand` / `RelayCommand<T>` duplication

- `src/CLIHub/ViewModels/Mvvm/RelayCommand.cs`: `public sealed class RelayCommand : ICommand` (`:8`),
  fields `execute`/`canExecute` (`:10-11`), `private EventHandler? explicitCanExecuteChanged;`
  (`:12`), constructor (`:19-23`), the `CanExecuteChanged` event with add/remove wiring
  `CommandManager.RequerySuggested` (`:31-44`, doc `:25-30`),
  `public void RaiseCanExecuteChanged() => this.explicitCanExecuteChanged?.Invoke(this, EventArgs.Empty);`
  (`:50`), `CanExecute` (`:53`), `Execute` (`:56`).
- `src/CLIHub/ViewModels/Mvvm/RelayCommandOfT.cs`: `public sealed class RelayCommand<T> : ICommand`
  (`:9`), the same field (`:13`), the same event block (`:32-45`, doc `:26-31`), the same
  `RaiseCanExecuteChanged` (`:51`), `CanExecute` (`:54-55`), `Execute` (`:58-64`).
- The field, event accessors, `RaiseCanExecuteChanged`, and their doc text are byte-identical between
  the two files; only offsets differ. Master's two files equal the backup's pre-change state exactly.
- Precedent for a public abstract base in the same folder: `public abstract class ObservableObject :
  INotifyPropertyChanged` at `src/CLIHub/ViewModels/Mvvm/ObservableObject.cs:9`.
- Behavioral contract to preserve: `Execute` does **not** consult `CanExecute` — recorded in
  `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.UpdateAvailability.cs:738`
  ("RelayCommand<T>.Execute does not consult CanExecute — the handler re-applies the gate").
- No test constructs `RelayCommand`/`RelayCommand<T>` directly (solution-wide grep over `tests/`
  finds only that comment), so nothing pins the event wiring directly.
- Build constraints that decide the base's shape: `src/CLIHub/CLIHub.csproj:10` and
  `src/CLIHub.Core/CLIHub.Core.csproj:7` set `GenerateDocumentationFile=true` (CS1591 on undocumented
  public members), and `Directory.Build.props:18` sets `EnforceCodeStyleInBuild=true` with the
  `.editorconfig` analyzer severities (IDE0009 `this.` qualification, IDE0065 using placement,
  IDE0161 file-scoped namespace at warning). The repository keeps a zero-warning build gate
  (`docs/backlog.md:132`).

### 2 — the eight one-way converters

All eight live under `src/CLIHub/Converters/`, all eight throw `NotSupportedException` from
`ConvertBack`, and all eight match the backup's pre-change state byte-for-byte inside their members:

| Converter | Declaration | `Convert` signature (master) | `ConvertBack` |
|---|---|---|---|
| `EmptyToCollapsedVisibilityConverter` | `:11` `sealed` : `IValueConverter` | `:22` `object Convert(object? value, Type targetType, object? parameter, CultureInfo)` | `:34-43` |
| `IndexToDividerVisibilityConverter` | `:11` `sealed` : `IValueConverter` | `:21` `object Convert(object? value, Type targetType, object parameter, CultureInfo)` | `:24-28` |
| `PageKeyVisibilityConverter` | `:11` `sealed` : `IValueConverter` | `:24` `object Convert(object? value, Type targetType, object? parameter, CultureInfo)` | `:34-43` |
| `PathToImageConverter` | `:13` `public class` (non-sealed) : `IValueConverter` | `:39` `object? Convert(object? value, Type targetType, object? parameter, CultureInfo)` | `:42-46` |
| `ThemeBrushKeyConverter` | `:12` `sealed` : `IValueConverter` | `:23` `object Convert(object? value, Type targetType, object parameter, CultureInfo)` | `:35-44` |
| `UpperCaseTextConverter` | `:9` `sealed` : `IValueConverter` | `:19` `object Convert(object? value, Type targetType, object parameter, CultureInfo)` | `:22-26` |
| `PathDisplayConverter` | `:17` `sealed` : `IMultiValueConverter` | `:34` `object Convert(object[] values, Type targetType, object parameter, CultureInfo)` | `:68-72` |
| `ResourceKeyToTemplateConverter` | `:17` `sealed` : `IMultiValueConverter` | `:33` `object Convert(object[] values, Type targetType, object? parameter, CultureInfo)` | `:48-57` |

- The `object` vs `object?` parameter annotations are inconsistent across the eight converters; four
  single-value converters (`IndexToDividerVisibilityConverter`, `ThemeBrushKeyConverter`,
  `UpperCaseTextConverter`) and `PathDisplayConverter` declare non-nullable `object parameter`.
- `PathToImageConverter.Convert` (`:39-40`) delegates to `this.imageCache.GetOrLoad(value as
  string)`; `LogoImageSourceCache.GetOrLoad(string? path)` returns a **non-nullable**
  `ImageSource` (`src/CLIHub/Imaging/LogoImageSourceCache.cs:39` — null/missing/undecodable paths
  yield the default logo image), so the `object?` return annotation on master is over-permissive and
  can narrow to `object` without any runtime change.
- Registrations — every one addresses the converter by its concrete type name, so a base-class change
  is drop-in:
  - `src/CLIHub/App.xaml:36-46`: `PathDisplayConverter`, `ThemeBrushKeyConverter`,
    `ResourceKeyToTemplateConverter`, `EmptyToCollapsedVisibilityConverter`,
    `PageKeyVisibilityConverter` (XAML element declarations → parameterless constructors must stay).
  - `src/CLIHub/Startup/ApplicationHost.cs:138`: `PathToImageConverter` assigned from DI under its
    `ResourceKey` const (`PathToImageConverter.cs:18`).
  - `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml:18`: `IndexToDividerVisibilityConverter`;
    `src/CLIHub/Views/SettingsWindow.xaml:33`: `UpperCaseTextConverter`.
  - `App.xaml:47`'s `BooleanToVisibilityConverter` carries no `conv:` prefix — it is WPF's built-in,
    not a project converter, and is out of scope.
- Tests: four `ConvertBack_AnyValue_Throws` facts call the member that becomes inherited —
  `EmptyToCollapsedVisibilityConverterTests.cs:43-47`,
  `PageKeyVisibilityConverterTests.cs:49-53`,
  `PathToImageConverterTests.cs:64-70`,
  `ResourceKeyToTemplateConverterTests.cs:92-97`. They keep passing unchanged because the base's
  `ConvertBack` throws the same exception.
- Spec check: the only spec text mentioning a converter is
  `openspec/specs/project-management/spec.md:318` ("the view renders it through the shared logo image
  converter") — a rendering-behavior statement unaffected by a base-class move. No spec constrains
  command plumbing, `CanExecuteChanged` wiring, or `ConvertBack` behavior.

### 3 — `PluginUpdateCheck`, `UpdateCheckSource`, `AgentReleaseClient` (the nullable fix area)

- `src/CLIHub.Core/Models/Plugins/PluginUpdateCheck.cs:14`:
  `public sealed record PluginUpdateCheck(string Source, string? Package = null, string? Repo = null);`
  — a positional record with **no** body. `HasPackage`/`HasRepo` do not exist on master
  (solution-wide grep for `HasPackage|HasRepo`: zero matches). On the backup branch the same record
  carried `NpmSource`/`GitHubReleasesSource`/`IsNpm`/`IsGitHubReleases`/`HasPackage`/`HasRepo`; master
  instead centralized the source-kind rule in `UpdateCheckSource`.
- `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs`: `internal static class` (`:8`);
  `internal static bool TryResolve(PluginUpdateCheck? updateCheck, out SourceKind kind, out Problem
  problem)` (`:63`); npm branch checks `string.IsNullOrWhiteSpace(updateCheck.Package)` (`:74`),
  GitHub branch checks `string.IsNullOrWhiteSpace(updateCheck.Repo)` (`:88`).
- `src/CLIHub.Core/Agents/AgentReleaseClient.cs` `TryBuildRequest` (`:93-131`): delegates validation
  to `UpdateCheckSource.TryResolve` (`:95`), then builds the URL with null-forgiving suppressions —
  `Uri.EscapeDataString(updateCheck.Package!)` (`:119`) and `updateCheck.Repo!` (`:120`) — under the
  comment "The resolver only succeeds for a kind whose address field is present." (`:117`).
  Because `!` suppresses it, master has **no** live CS8604 here today (the warning the backup commit
  fixed existed only in the backup's inline-guard shape).
- `TryResolve` callers on master: `AgentReleaseClient.cs:95`,
  `AgentUpdateAvailabilityService.cs:163`, `AgentProcessMatcher.cs:145`, and
  `tests/CLIHub.Core.Tests/Models/Plugins/UpdateCheckSourceTests.cs` with **14 call sites** of the
  3-out-argument shape (`:11,21,35,…`).
- On the backup branch, `TryBuildRequest` validated inline (`if (!updateCheck.HasPackage) … return
  false;` before `updateCheck.Package`), so `[MemberNotNullWhen]` let the compiler prove non-null and
  the `!` disappeared. Master's indirection breaks that flow: the compiler cannot know that
  `TryResolve == true` implies `HasPackage`/`HasRepo`.

### 4 — `NumericField` parse-or-placeholder duplication

`src/CLIHub/Controls/NumericField.cs` (`public sealed class NumericField : Control`):

- `ResolveNextValue(int delta)` (`:259-273`): `TryParseNumber(this.Text, …)` →
  `(int)Math.Clamp((long)value + delta, …)` (`:263`); else `TryParseNumber(this.Placeholder, …)` with
  the comment "An empty field whose placeholder is a number shows the effective default; step from
  it." (`:266-267`) → `(int)Math.Clamp((long)placeholder + delta, …)` (`:269`); else
  `Math.Clamp(this.Minimum, this.Minimum, this.Maximum)` (`:272`).
- `ResolveCurrentValue()` (`:275-288`): the same two parse attempts (`:277`, `:282`), clamps without
  the `(long)` widening (`:279`, `:284`); else `Math.Clamp(0, this.Minimum, this.Maximum)` (`:287`).
- `private static bool TryParseNumber(string? text, out int value)` (`:310-311`).
- This region is byte-identical to the backup's pre-change state — the `02aaf6a` diff
  (extract `TryResolveBaseValue(out long value)`; both callers keep their own clamp and distinct
  fallback) applies verbatim.
- No test references `NumericField` (grep over `tests/`).

### 5 — `SettingsViewModel` probe resolvers

`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`:

- `ResolveProbeTtlMinutes()` (`:309-312`) and `ResolveProbeTimeoutSeconds()` (`:314-317`) are the
  duplicated `int.TryParse(this.probeXText.Trim(), out var v) && v > 0 ? v : (int)AgentVersionService.Default…`
  shape — byte-identical to the backup's pre-change state; the `02aaf6a` diff (extract
  `private static int ResolvePositiveIntOr(string? text, int fallback)`) applies verbatim.
- Consumers unchanged by the port: summaries at `:342` (`ResolveProbeTtlMinutes()… min`) and `:354`,
  and the setters `:293-307` that call `RaiseProbePolicyChanges()` (`:319-327`).
- Existing pins: `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:867`
  (`"15 min"`) and `:894` (`"15 min · 10 sec"`) — the resolver outputs must not change.

### 6 — what the backup did that master no longer has

- `src/CLIHub/ViewModels/Menu/MenuActionCoordinator.cs` (backup item 6: delete eight duplicated
  `ThrowIfNull` guards) — the class **does not exist** on master (path check fails). The eight guards
  live exactly once, in `src/CLIHub/ViewModels/Launch/LaunchWindowActionBuilder.cs:35-42`; there is
  no duplication left to remove.
- `docs/backlog.md` bookkeeping (backup item 7) — master's backlog has no entries for the
  `AgentReleaseClient` nullable warning, the command/converter boilerplate, or the parse helpers
  (grep for `AgentReleaseClient|MemberNotNull|RelayCommand|converter`: zero relevant matches).
- File-header comment blocks (`// <file name="…" … />`) — the backup's three new files carried them;
  no file under master's `src/` does (grep for `<file name=`: zero matches; master ran
  `dotnet format`, commit `63e3ed6`). The new files must follow master style without headers.

## Decisions

### D1 — Port source and adaptation rule

Port `02aaf6a`'s diffs item by item, verbatim except where master's code has since moved: (a) no
`// <file …>` header comments on the three new files, (b) the nullable fix is reduced to the model
side (D6), (c) the `MenuActionCoordinator` and backlog items are dropped (D8/D9). Rationale: the
sweep's value is mechanical dedup; wherever master's pre-change state is byte-identical to the
backup's (both command classes, all eight converters, `NumericField`, the two probe resolvers), the
backup diff is already the reviewed answer. Verified equivalents are listed in Context §1, §2, §4,
§5.

### D2 — `RelayCommandBase` shape

`public abstract class RelayCommandBase : ICommand` owning `private EventHandler?
explicitCanExecuteChanged`, the `public event EventHandler? CanExecuteChanged` add/remove copied
verbatim (both accessors wire `CommandManager.RequerySuggested`), `public void
RaiseCanExecuteChanged()`, and `public abstract` `CanExecute(object? parameter)` /
`Execute(object? parameter)` with `<inheritdoc />`. The derived classes stay `public sealed`, keep
constructors/fields/bodies, and mark their `CanExecute`/`Execute` `override`. `Execute` continues not
to consult `CanExecute` (test-comment contract, Context §1). Alternatives rejected: a
`CommunityToolkit.Mvvm`-style command (new dependency, changes semantics); making `RaiseCanExecuteChanged`
virtual (no caller needs to override it; YAGNI); keeping the duplication (the status quo this change
removes).

### D3 — Converter base shapes and nullability normalization

- `OneWayValueConverter : IValueConverter`: `public abstract object Convert(object? value, Type
  targetType, object? parameter, CultureInfo culture);` and non-virtual `public object
  ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new
  NotSupportedException();`.
- `OneWayMultiValueConverter : IMultiValueConverter`: `public abstract object Convert(object[]
  values, Type targetType, object? parameter, CultureInfo culture);` and non-virtual `public
  object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
  throw new NotSupportedException();`.
- Every derived `Convert` override is normalized to exactly these annotations: the four
  `object parameter` declarations become `object? parameter` (a mismatched non-nullable override
  parameter is CS8765, which the zero-warning gate forbids), and `PathToImageConverter`'s return
  narrows `object?` → `object` (legal without a warning; truthful because `GetOrLoad` returns
  non-nullable `ImageSource`, Context §2).
- `ConvertBack` stays non-virtual: no converter may silently start supporting back-conversion; the
  four `ConvertBack_AnyValue_Throws` facts pin the throw through inheritance.
- `ConvertBack`'s own annotations are the most permissive already in the codebase
  (`EmptyToCollapsedVisibilityConverter.cs:42`, `ResourceKeyToTemplateConverter.cs:56`), matching
  WPF's reference annotations for the interfaces; `ConvertBack` is unreachable in one-way bindings,
  so its parameter nullability has no runtime meaning.
- Alternatives rejected: making `ConvertBack` virtual (reopens the boilerplate this change removes);
  an `abstract` base per converter family with generic helpers (over-engineering for eight
  one-liners); leaving annotations inconsistent per derived class (CS8765 warnings fail the gate).

### D4 — XAML/DI drop-in compatibility

No XAML file, no `ResourceKey`, and no DI registration changes. Every registration names the concrete
converter type (Context §2), the constructors are untouched, and each `Convert` body stays
byte-identical, so return values for null/empty/`UnsetValue` inputs and target types are unchanged.
The base only replaces *where* `ConvertBack` and the interface plumbing live.

### D5 — `HasPackage`/`HasRepo` as the single presence rule

Add to `PluginUpdateCheck` (a `public sealed record`, so the properties are new public surface with
XML docs):

```csharp
[MemberNotNullWhen(true, nameof(Package))]
public bool HasPackage => !string.IsNullOrWhiteSpace(this.Package);

[MemberNotNullWhen(true, nameof(Repo))]
public bool HasRepo => !string.IsNullOrWhiteSpace(this.Repo);
```

and route `UpdateCheckSource.TryResolve`'s two blank checks (`:74`, `:88`) through them. The
predicate is identical (`HasPackage` ≡ `!string.IsNullOrWhiteSpace(Package)`), so `TryResolve`
behavior — and its 14 test call sites — are unchanged. `using System.Diagnostics.CodeAnalysis;` goes
after the file-scoped namespace per repo style. The attributes cost nothing at runtime (annotation
metadata only) and teach the compiler the presence⇒non-null consequence for any future caller that
guards with the properties. Alternative rejected: adding the properties but keeping the raw
`IsNullOrWhiteSpace` checks in `TryResolve` (two spellings of one rule invites drift).

### D6 — The two null-forgiving operators in `AgentReleaseClient` stay (excluded from the port)

The brief asked to "remove the now-unneeded null-forgiving suppressions"; verification shows they
are **not** now-unneeded on master. The backup removed them because `TryBuildRequest` guarded inline
with `!updateCheck.HasPackage` before reading `updateCheck.Package`; master's `TryBuildRequest`
delegates that validation to `UpdateCheckSource.TryResolve` (`AgentReleaseClient.cs:95`), and
nothing tells the compiler that `TryResolve == true` implies presence. Every no-suppression route
exceeds a mechanical sweep:

- **Re-asserting presence per kind after `TryResolve`** — dead-by-construction guards after the
  resolver already validated, and Roslyn's nullable flow analysis cannot narrow `Package` through the
  resulting compound condition anyway (the disjunction `npm && !HasPackage || gh && !HasRepo`
  negated does not yield a member-not-null state), so it would likely still warn.
- **`updateCheck.HasPackage ? updateCheck.Package : string.Empty` inside the ternary** — replaces a
  suppression with a silent wrong-value fallback in an impossible branch; strictly worse than `!`.
- **Extending `TryResolve` with `out string? address`** — honest and behavior-identical, but touches
  all 17 call sites: 3 production (`AgentReleaseClient.cs:95`, `AgentUpdateAvailabilityService.cs:163`,
  `AgentProcessMatcher.cs:145`) and **14 test sites** in `UpdateCheckSourceTests.cs`, breaking the
  sweep's zero-test-edit property, and forces a nullability decision for the failure value.
- **Inlining validation back into `AgentReleaseClient`** (the backup branch's shape) — reverts
  master's newer single-rule resolver design; the brief's own rule says master's newer code wins.

Therefore: keep `updateCheck.Package!`/`updateCheck.Repo!` (`:119-120`) with the existing invariant
comment (`:117`), and record the `TryResolve` address variant as open question Q1 for a possible
follow-up change.

### D7 — `NumericField` and `SettingsViewModel` helpers port verbatim

Both duplications are byte-identical to the backup's pre-change state (Context §4, §5), so the
`02aaf6a` diffs apply as-is: `TryResolveBaseValue(out long value)` performs the same two parse
attempts in the same order and carries the placeholder comment; `ResolveNextValue` keeps
`Clamp(Minimum)` and `ResolveCurrentValue` keeps `Clamp(0)` as their distinct final fallbacks;
`ResolvePositiveIntOr(string? text, int fallback)` keeps both `(int)AgentVersionService.Default*`
expressions exactly at their call sites. No other member of either file changes; the probe summaries
(`:342`, `:354`) and their pinned outputs (`SettingsViewModelApplicationTests.cs:867,894`) are
untouched. Applicability verdict for the brief's item 4: **both touch-ups apply, verbatim** —
neither conflicts with master's update-chip or other newer work (the resolvers' consumers and the
watermark strings are unchanged).

### D8 — Backup item 6 (`MenuActionCoordinator`) dropped as moot

`MenuActionCoordinator` does not exist on master; the eight `ThrowIfNull` guards live once in
`LaunchWindowActionBuilder.cs:35-42`. There is no double validation to collapse, and nothing in this
change touches that file.

### D9 — Backup item 7 (backlog bookkeeping) dropped

Master's `docs/backlog.md` contains no entry this change consumes (Context §6); no bookkeeping task
is created and the file is not modified.

### D10 — Zero test edits; gates instead of new pins

No test file is added, edited, or skipped. Coverage of the moved members already exists where it
matters: the four `ConvertBack` facts exercise the inherited throw, view-model tests exercise
`RaiseCanExecuteChanged`/`CanExecuteChanged` semantics indirectly, and the probe pins
(`:867`, `:894`) plus `UpdateCheckSourceTests`' 14 facts exercise the routed predicates and the
unchanged resolver outputs. The gates require a full build (0 warnings/0 errors) and the full test
run at the count captured at the stage-1 baseline — an unchanged count, not a hardcoded one
(master's suite has grown since the backup's 1215).

## Deviations from the porting source (02aaf6a)

1. **No `// <file …>` header comments** on `RelayCommandBase.cs`, `OneWayValueConverter.cs`,
   `OneWayMultiValueConverter.cs` — master's `src/` has none (Context §6).
2. **Nullable fix reduced in scope**: model-side properties + predicate routing only;
   `AgentReleaseClient`'s two `!` suppressions remain (D6). On the backup branch the same item also
   removed a live CS8604; no such warning exists on master.
3. **`MenuActionCoordinator` guard removal not ported** — target class deleted on master (D8).
4. **`docs/backlog.md` bookkeeping not ported** — no corresponding entries on master (D9).
5. **Test-count gates are captured at baseline**, not pinned to the backup's 1215 figure (D10).

## Open questions

1. **Follow-up for the two null-forgiving operators** (`AgentReleaseClient.cs:119-120`): should a
   later change extend `UpdateCheckSource.TryResolve` with `out string? address` (production call
   sites `AgentUpdateAvailabilityService.cs:163` and `AgentProcessMatcher.cs:145` add `out _`;
   `UpdateCheckSourceTests.cs`'s 14 call sites each add `out _`) so both suppressions can be removed
   honestly? This change deliberately does not decide it (D6).
2. **Sibling changes**: `probe-cache-extraction` and `dialog-and-chrome-helpers` are being prepared
   concurrently and neither shares a file with this change, but both are ports from the same backup
   lineage — if either is implemented first, re-run this change's stage gates and re-check that
   `SettingsViewModel.cs` / `AgentUpdateAvailabilityService.cs` still match the state described here
   (probe-cache touches `AgentVersionService`, whose `DefaultTtl`/`DefaultProbeTimeout` constants
   this change's item 3 references but does not move).
3. **Zero-warning baseline**: this preparation did not build the solution (no builds for a
   preparation-only session). The stage-1 gate captures the actual baseline; if the baseline already
   has unrelated warnings, they are out of scope — the gate is "no new warnings from this change",
   and any CS8765/CS8766/CS1591 in the touched files is a defect of this change, not a pre-existing
   condition.
