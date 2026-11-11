# Tasks: simplify-mechanical-sweep

Master line numbers below were verified at HEAD `8ca4c3f`; if a sibling change
(`probe-cache-extraction`, `dialog-and-chrome-helpers`) lands first, re-read the touched files and
re-verify the cited lines before editing (design.md Q2).

## 1. Model annotations and parse-fallback helpers (proposal items 1-3)

- [ ] 1.1 Baseline capture: run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; record the
      warning count (expected 0; if non-zero for unrelated reasons, record them — they are out of
      scope, design.md Q3) and the exact passed/failed test totals as the gate baseline for every
      later stage
- [ ] 1.2 In `src/CLIHub.Core/Models/Plugins/PluginUpdateCheck.cs` add
      `using System.Diagnostics.CodeAnalysis;` after the file-scoped namespace (`:1`), then add two
      documented get-only properties to the record (`:14`):
      `[MemberNotNullWhen(true, nameof(Package))] public bool HasPackage =>
      !string.IsNullOrWhiteSpace(this.Package);` and the symmetric
      `[MemberNotNullWhen(true, nameof(Repo))] public bool HasRepo` — full XML docs on both, bodies
      exactly as written (design.md D5)
- [ ] 1.3 In `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs` replace
      `string.IsNullOrWhiteSpace(updateCheck.Package)` (`:74`) with `!updateCheck.HasPackage` and
      `string.IsNullOrWhiteSpace(updateCheck.Repo)` (`:88`) with `!updateCheck.HasRepo` — nothing
      else in the method changes; do NOT edit `AgentReleaseClient.cs` in this change (design.md D5/D6)
- [ ] 1.4 In `src/CLIHub/Controls/NumericField.cs` add `private bool TryResolveBaseValue(out long
      value)` next to `TryParseNumber` (`:310-311`): attempt `TryParseNumber(this.Text, out var
      text)` first, then `TryParseNumber(this.Placeholder, out var placeholder)`; on either success
      assign the parsed value to `value` and return `true`; otherwise assign `0` and return `false`.
      Keep the comment "An empty field whose placeholder is a number shows the effective default;
      step from it." with the placeholder attempt (design.md D7)
- [ ] 1.5 In the same file rewrite `ResolveNextValue(int delta)` (`:259-273`) to
      `this.TryResolveBaseValue(out var value)` → `(int)Math.Clamp(value + delta, this.Minimum,
      this.Maximum)` on success, `Math.Clamp(this.Minimum, this.Minimum, this.Maximum)` on failure;
      and `ResolveCurrentValue()` (`:275-288`) to `(int)Math.Clamp(value, this.Minimum,
      this.Maximum)` on success, `Math.Clamp(0, this.Minimum, this.Maximum)` on failure — keep the
      two final fallbacks distinct (`Minimum` for stepping, `0` for reporting) and the `(int)`
      narrowings at both call sites (design.md D7)
- [ ] 1.6 In `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` add
      `private static int ResolvePositiveIntOr(string? text, int fallback) =>
      int.TryParse(text?.Trim(), out var value) && value > 0 ? value : fallback;` and reduce
      `ResolveProbeTtlMinutes()` (`:309-312`) to `ResolvePositiveIntOr(this.probeTtlText,
      (int)AgentVersionService.DefaultTtl.TotalMinutes)` and `ResolveProbeTimeoutSeconds()`
      (`:314-317`) to `ResolvePositiveIntOr(this.probeTimeoutText,
      (int)AgentVersionService.DefaultProbeTimeout.TotalSeconds)`; keep both fallback expressions
      exactly as written and leave the summaries (`:342`, `:354`), setters (`:293-307`), and
      `RaiseProbePolicyChanges` (`:319-327`) untouched (design.md D7)
- [ ] 1.7 Gate: run `dotnet build CLIHub.sln` (0 warnings / 0 errors) and `dotnet test CLIHub.sln`
      (0 failed; passed/failed totals exactly equal to the 1.1 baseline — an unchanged count, not a
      hardcoded number) — both must pass before stage 2

## 2. Command base and converter bases (proposal items 4-5)

- [ ] 2.1 Create `src/CLIHub/ViewModels/Mvvm/RelayCommandBase.cs` (NO `// <file …>` header comment —
      master style, design.md D1): `namespace CLIHub.ViewModels;`, then
      `using System.Windows.Input;`, then `public abstract class RelayCommandBase : ICommand` with
      `private EventHandler? explicitCanExecuteChanged;`, the `public event EventHandler?
      CanExecuteChanged` add/remove copied verbatim from `RelayCommand.cs:31-44` (both accessors wire
      `CommandManager.RequerySuggested`), `public void RaiseCanExecuteChanged() =>
      this.explicitCanExecuteChanged?.Invoke(this, EventArgs.Empty);`, and `public abstract bool
      CanExecute(object? parameter);` / `public abstract void Execute(object? parameter);` — full XML
      docs on the type, event, and raise method (moved text), `<inheritdoc />` on the two abstract
      members (design.md D2)
- [ ] 2.2 In `src/CLIHub/ViewModels/Mvvm/RelayCommand.cs` change the declaration (`:8`) to
      `public sealed class RelayCommand : RelayCommandBase` and delete the field (`:12`), the event
      with its doc block (`:25-44`), and `RaiseCanExecuteChanged` with its doc block (`:46-50`);
      keep the class doc, fields (`:10-11`), and constructor (`:19-23`), and turn `CanExecute`
      (`:53`) / `Execute` (`:56`) into `public override` with unchanged bodies and `<inheritdoc />`
      docs
- [ ] 2.3 Do the same in `src/CLIHub/ViewModels/Mvvm/RelayCommandOfT.cs`: declaration (`:9`) to
      `public sealed class RelayCommand<T> : RelayCommandBase`, delete the field (`:13`), event block
      (`:26-45`), and `RaiseCanExecuteChanged` (`:47-51`), keep the `<typeparam>` doc and constructor
      (`:20-24`), and make `CanExecute` (`:54-55`) / `Execute` (`:58-64`) overrides with their exact
      bodies — `parameter is T typed` behavior and the do-not-consult-CanExecute `Execute` unchanged
      (design.md D2)
- [ ] 2.4 Create `src/CLIHub/Converters/OneWayValueConverter.cs` (no header comment):
      `namespace CLIHub.Converters;`, usings `System.Globalization` + `System.Windows.Data`,
      `public abstract class OneWayValueConverter : IValueConverter` with `public abstract object
      Convert(object? value, Type targetType, object? parameter, CultureInfo culture);` and a
      non-virtual `public object ConvertBack(object? value, Type targetType, object? parameter,
      CultureInfo culture) => throw new NotSupportedException();` — the type and both members fully
      documented, with the "one-way bindings only" wording owned once here (design.md D3)
- [ ] 2.5 Create `src/CLIHub/Converters/OneWayMultiValueConverter.cs` symmetrically (no header
      comment): `public abstract class OneWayMultiValueConverter : IMultiValueConverter` with
      `public abstract object Convert(object[] values, Type targetType, object? parameter,
      CultureInfo culture);` and `public object[] ConvertBack(object value, Type[] targetTypes,
      object? parameter, CultureInfo culture) => throw new NotSupportedException();` — full docs
      (design.md D3)
- [ ] 2.6 In `EmptyToCollapsedVisibilityConverter.cs` (`:11`) and `PageKeyVisibilityConverter.cs`
      (`:11`) derive from `OneWayValueConverter` and mark `Convert` as `public override object
      Convert(object? value, Type targetType, object? parameter, CultureInfo culture)` — these two
      already match the base annotations; verify character by character — then delete each
      `ConvertBack` with its doc block (`:34-43` in both files), keep each converter's purpose doc on
      `Convert`, and remove the now-unused `using System.Windows.Data;` only if nothing else in the
      file references that namespace
- [ ] 2.7 In `IndexToDividerVisibilityConverter.cs` (`:11`), `ThemeBrushKeyConverter.cs` (`:12`),
      and `UpperCaseTextConverter.cs` (`:9`) derive from `OneWayValueConverter` and change `Convert`
      to `public override object Convert(object? value, Type targetType, object? parameter,
      CultureInfo culture)` — this normalizes each `object parameter` to `object? parameter`; a
      remaining `object parameter` override is CS8765 and fails the gate — then delete each
      `ConvertBack` (`:24-28`, `:35-44`, `:22-26`), keep the per-converter purpose docs, and drop a
      now-unused `using System.Windows.Data;` (design.md D3)
- [ ] 2.8 In `PathToImageConverter.cs` (the only non-sealed converter — keep it `public class`)
      derive from `OneWayValueConverter`, change `Convert` (`:39`) to `public override object
      Convert(object? value, Type targetType, object? parameter, CultureInfo culture)` so the return
      narrows from `object?` to `object` (truthful: `GetOrLoad` returns non-nullable `ImageSource`,
      `LogoImageSourceCache.cs:39`), delete `ConvertBack` (`:42-46`), and keep `ResourceKey` (
      `:18`), the constructor (`:26-29`), and the class doc unchanged (design.md D3)
- [ ] 2.9 In `PathDisplayConverter.cs` (`:17`) and `ResourceKeyToTemplateConverter.cs` (`:17`)
      derive from `OneWayMultiValueConverter` and change `Convert` to `public override object
      Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)`
      (`PathDisplayConverter`'s `object parameter` at `:34` becomes `object? parameter`;
      `ResourceKeyToTemplateConverter` at `:33` already matches), delete the `ConvertBack` members
      (`:68-72`, `:48-57`), and keep every body, constant, private helper, and doc otherwise intact;
      `using System.Windows.Data;` becomes unused once the interface name disappears, so drop it
      where nothing else in the file references that namespace
- [ ] 2.10 Gate: run `dotnet build CLIHub.sln` (0 warnings / 0 errors — any CS8765, CS8766, CS1591,
      or CS0108 means a signature or doc was not normalized) and `dotnet test CLIHub.sln` (0 failed;
      totals exactly equal to the 1.1 baseline; the four `ConvertBack_AnyValue_Throws` facts at
      `EmptyToCollapsedVisibilityConverterTests.cs:43`,
      `PageKeyVisibilityConverterTests.cs:49`, `PathToImageConverterTests.cs:64`,
      `ResourceKeyToTemplateConverterTests.cs:92` must still pass through the inherited member) —
      both must pass before stage 3

## 3. Final gates and scope check

- [ ] 3.1 Run `dotnet build CLIHub.sln` and confirm 0 warnings / 0 errors; any warning in the touched
      files is a defect of this change (design.md Q3)
- [ ] 3.2 Run `dotnet test CLIHub.sln` and confirm 0 failed with totals exactly equal to the 1.1
      baseline
- [ ] 3.3 Run `openspec validate --all` and confirm 0 failed (expect the pre-change items plus this
      change; the pre-existing WARNING items on unrelated specs are not this change's to fix)
- [ ] 3.4 Run `graphify update .` so the knowledge graph reflects the edits and the three new source
      files
- [ ] 3.5 Scope check: `git status --short` lists only this change's files — `openspec/changes/
      simplify-mechanical-sweep/`, the new `src/CLIHub/ViewModels/Mvvm/RelayCommandBase.cs`,
      `src/CLIHub/Converters/OneWayValueConverter.cs`, `OneWayMultiValueConverter.cs`, the modified
      `RelayCommand.cs`, `RelayCommandOfT.cs`, the eight `src/CLIHub/Converters/*.cs` converters,
      `src/CLIHub.Core/Models/Plugins/PluginUpdateCheck.cs`,
      `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs`,
      `src/CLIHub/Controls/NumericField.cs`, and
      `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — no test file, no `.csproj`, no XAML,
      no `AgentReleaseClient.cs`, and none of the untracked `backup-port-*.md` analysis files may
      appear as modified or staged; then report the result and ask the user for `git commit` and
      `openspec archive` approval (approval never carries over, and nothing is staged before it)
