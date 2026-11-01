## Why

`IStartupWindow` (`src/CLIHub/Startup/IStartupWindow.cs:6-8`) has been a member-less marker interface
since `remove-unused-code-tier2` deleted its last member, `ShowOnPointerMonitor`. It survives only as
the declared type of `IApplicationStartupUi.LaunchWindow` and the `ApplicationStartupContext.SetMainWindow`
delegate parameter, both of which always receive the one concrete implementer, `LaunchWindow`
(`src/CLIHub/Views/LaunchWindow.xaml.cs:21`). Typing those two members as `LaunchWindow` directly and
deleting the interface removes dead API surface with zero behavior change, per the backlog entry at
`docs/backlog.md:67-78`.

## What Changes

- Change `IApplicationStartupUi.LaunchWindow` (`src/CLIHub/Startup/IApplicationStartupUi.cs:16`) from
  `IStartupWindow LaunchWindow { get; }` to `LaunchWindow LaunchWindow { get; }`.
- Change `ApplicationStartupContext.SetMainWindow` (`src/CLIHub/Startup/ApplicationStartupContext.cs:9`)
  from `Action<IStartupWindow> SetMainWindow` to `Action<LaunchWindow> SetMainWindow`.
- Update `WpfApplicationStartupUi.LaunchWindow` (`src/CLIHub/Startup/WpfApplicationStartupUi.cs:31`) to
  return the already-held `LaunchWindow` field with the new property type (`IStartupWindow LaunchWindow =>`
  becomes `LaunchWindow LaunchWindow =>`).
- Remove `IStartupWindow` from `LaunchWindow`'s base list
  (`src/CLIHub/Views/LaunchWindow.xaml.cs:21`: `Window, IStartupWindow` becomes `Window`).
- Delete `src/CLIHub/Startup/IStartupWindow.cs`.
- Update `tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs` to use the concrete `LaunchWindow`
  type instead of `Mock<IStartupWindow>` (see Deviation below — this file is a required, compile-breaking
  consumer that the triggering brief did not list).

## Non-Goals

- No change to window show/activate/hide policy, startup orchestration, or single-instance handling.
- No introduction of a narrower window abstraction (e.g. a `IStartupWindow` with real members). The
  backlog remedy (`docs/backlog.md:75-76`) explicitly defers that until a second implementer of the
  marker appears; today there is exactly one, so the concrete type is used directly.
- No other dead-code sweep; no unrelated cleanup.
- No change to the null-argument guard behavior on `ApplicationBootstrapper.Start` — it keeps throwing
  `ArgumentNullException` with `ParamName == "setMainWindow"` when the context's delegate is null; only
  the delegate's parameter type changes, not the guard logic or its exercised path.

## Impact

- **Affected capability:** none (pure internal refactor, no behavior contract changes). `skip_specs: true`
  applies — see Specs below.
- **Affected code:** 5 production files, 1 test file (6 files total — see Deviation).
  - `src/CLIHub/Startup/IApplicationStartupUi.cs`
  - `src/CLIHub/Startup/ApplicationStartupContext.cs`
  - `src/CLIHub/Startup/WpfApplicationStartupUi.cs`
  - `src/CLIHub/Views/LaunchWindow.xaml.cs`
  - `src/CLIHub/Startup/IStartupWindow.cs` (deleted)
  - `tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs`
- **Affected tests:** `ApplicationBootstrapperTests` (must change to compile); `OptionalStartupCoordinatorTests`
  and `ApplicationHostTests` are unaffected (verified below, no edit needed).

## Specs

`skip_specs: true`. This is an internal type removal with no durable behavior change: a search of
`openspec/specs/` for `IStartupWindow` returns zero matches (only `openspec/changes/archive/**` mentions
it, as historical record of prior changes, not the current spec tree). No spec sentence names the
interface or any of its former members, so no spec delta applies.

## Deviation from the triggering brief

The brief's evidence list named five files and asserted "no test references `IStartupWindow`" beyond
`OptionalStartupCoordinatorTests.cs` (cited there at a path that does not exist — the real path is
`tests/CLIHub.Tests/OptionalStartupCoordinatorTests.cs`, with no `Startup/` subdirectory under
`tests/CLIHub.Tests/`). Re-verification found a sixth file with real, compile-breaking references:

`tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs`:
- line 94: `CreateContext(IStartupWindow window)` — parameter typed as the interface
- line 106: `Mock<IStartupWindow> Window { get; } = new(MockBehavior.Strict);`
- lines 30, 65: `fixture.Ui...SetupGet(x => x.LaunchWindow).Returns(fixture.Window.Object);` — requires
  `fixture.Window.Object` to be assignable to the new property type
- lines 35, 47, 69: `CreateContext(fixture.Window.Object)`

`LaunchWindow` is a sealed `partial class : Window` with no virtual members Moq can intercept and no
public parameterless constructor usable by a proxy, so `Mock<LaunchWindow>` is not viable; `IStartupWindow`
was deliberately mockable where the concrete type is not. Once the interface is deleted, this file will
not compile. Fixing it requires either a tiny trivial subclass test double or passing a real `LaunchWindow`
instance — both are test-file edits, which this proposal is not permitted to make. `tasks.md` stages this
file's update as an explicit, separate task for the implementing session; it is not optional and not
part of "no other dead-code sweep" — it is required for this change to build at all.

## Dependencies

None — standalone refactor on top of commit `6610e88`.
