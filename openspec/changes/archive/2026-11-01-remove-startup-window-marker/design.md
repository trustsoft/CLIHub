## Context

`IStartupWindow` lost its only member, `ShowOnPointerMonitor`, in `remove-unused-code-tier2`
(`openspec/changes/archive/2026-10-30-remove-unused-code-tier2/tasks.md:62`). Since then it has been a
member-less marker interface with exactly one implementer. `docs/backlog.md:67-78` records the removal
as deferred cleanup, to be picked up "the next change that touches startup composition or app
bootstrapping, or a second implementer of `IStartupWindow`" — no second implementer has appeared.

## Goals / Non-Goals

**Goals:**
- Delete `IStartupWindow` entirely.
- Type its two consumers (`IApplicationStartupUi.LaunchWindow`, `ApplicationStartupContext.SetMainWindow`)
  as the concrete `CLIHub.Views.LaunchWindow`.
- Zero behavior change: no change to window show/activate/hide policy, startup orchestration order, or
  the null-argument guard's observable behavior.

**Non-Goals:**
- No narrower window abstraction. Only one implementer exists; introducing an interface with real
  members ahead of a second implementer is the exact premature abstraction the backlog entry asks to
  avoid (`docs/backlog.md:75-76`: "or introduce a narrower window abstraction if a second implementation
  appears" — conditional, not triggered here).
- No other dead-code sweep.
- No edits to `OptionalStartupCoordinatorTests.cs` or `ApplicationHostTests.cs` — verified below that
  neither needs to change.

## Current State (verified against the working tree at commit `6610e88`)

### `IStartupWindow.cs` — the type being deleted

`src/CLIHub/Startup/IStartupWindow.cs:1-8`:
```csharp
namespace CLIHub.Startup;

/// <summary>
///   Application-facing operations needed from the launch window during startup.
/// </summary>
public interface IStartupWindow
{
}
```
No members. Confirmed by direct read.

### Consumer 1 — `IApplicationStartupUi.LaunchWindow`

`src/CLIHub/Startup/IApplicationStartupUi.cs:13-16`:
```csharp
/// <summary>
///   Gets the launch window exposed to the WPF lifecycle adapter.
/// </summary>
IStartupWindow LaunchWindow { get; }
```
Sole implementation, `WpfApplicationStartupUi.LaunchWindow` at `src/CLIHub/Startup/WpfApplicationStartupUi.cs:31`:
```csharp
public IStartupWindow LaunchWindow => this.launchWindow;
```
where `this.launchWindow` is declared as `private readonly LaunchWindow launchWindow;`
(`WpfApplicationStartupUi.cs:12`) and constructor-injected as `LaunchWindow launchWindow`
(`WpfApplicationStartupUi.cs:20`). The field is already concretely typed; only the property's declared
return type is `IStartupWindow`. `WpfApplicationStartupUi.cs:3` already has `using CLIHub.Views;`, so
`LaunchWindow` (the view, `CLIHub.Views.LaunchWindow`) is already in scope in this file — confirmed by
direct read, no new `using` needed here.

### Consumer 2 — `ApplicationStartupContext.SetMainWindow`

`src/CLIHub/Startup/ApplicationStartupContext.cs:6-9`:
```csharp
/// <param name="Dispatch"> Invokes an action on the UI dispatcher. </param>
/// <param name="SetMainWindow"> Assigns the application's main window. </param>
public sealed record ApplicationStartupContext(Action<Action> Dispatch,
                                               Action<IStartupWindow> SetMainWindow);
```
This file is in namespace `CLIHub.Startup` with no `using CLIHub.Views;` today (confirmed: the file has
no `using` directives at all, lines 1-9). Changing the parameter type to `Action<LaunchWindow>` requires
adding `using CLIHub.Views;` to this file. This is a new `using` in a file that didn't need one before —
not a new assembly reference (`CLIHub` already references `CLIHub.Views.LaunchWindow` throughout the
`Startup` namespace) and not a new layering direction: `WpfApplicationStartupUi.cs` (also in
`CLIHub.Startup`) already depends on `CLIHub.Views.LaunchWindow` concretely. So the `Startup -> Views`
namespace dependency already exists at the namespace level; this just extends it to one more file within
the same namespace and assembly. No new project reference, no new circular risk.

### The only caller of `SetMainWindow`

`src/CLIHub/Startup/ApplicationBootstrapper.cs:49,58`:
```csharp
ArgumentNullException.ThrowIfNull(context.SetMainWindow);
...
context.SetMainWindow(startupUi.LaunchWindow);
```
`startupUi.LaunchWindow` is `IApplicationStartupUi.LaunchWindow`, which after this change returns
`CLIHub.Views.LaunchWindow` — exactly the type `SetMainWindow` will accept. No change needed to this
call site's source text; the call keeps compiling because both sides move to the same concrete type
together.

### The implementer losing its interface

`src/CLIHub/Views/LaunchWindow.xaml.cs:21`:
```csharp
public partial class LaunchWindow : Window, IStartupWindow
```
Becomes:
```csharp
public partial class LaunchWindow : Window
```
`LaunchWindow.xaml.cs` is in `using CLIHub.Startup;` (line 15) only to reach `IStartupWindow`. After the
interface is deleted, grep confirms no other `CLIHub.Startup` symbol is referenced in this file (the
file's only other dependencies are `CLIHub.Core.Configuration`, `CLIHub.Core.Models`, `CLIHub.Interop`,
`CLIHub.ViewModels`). The `using CLIHub.Startup;` directive (line 15) becomes unused and should be
removed as part of this change, since it exists solely for the type being deleted.

## Consumer Inventory — all `IStartupWindow` references in the repository

Full-repository search (not scoped to the brief's five files) found:

| File | Line(s) | Reference | Disposition |
|---|---|---|---|
| `src/CLIHub/Startup/IStartupWindow.cs` | 1-8 | the interface itself | delete file |
| `src/CLIHub/Startup/IApplicationStartupUi.cs` | 16 | property type | retype to `LaunchWindow` |
| `src/CLIHub/Startup/ApplicationStartupContext.cs` | 9 | delegate parameter type | retype to `LaunchWindow`, add `using CLIHub.Views;` |
| `src/CLIHub/Startup/WpfApplicationStartupUi.cs` | 31 | property return type | retype to `LaunchWindow` |
| `src/CLIHub/Views/LaunchWindow.xaml.cs` | 21 | base list | drop `, IStartupWindow`; drop now-unused `using CLIHub.Startup;` (line 15) |
| `tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs` | 94, 106, 30, 65, 35, 47, 69 | test fixture type, mock declaration, mock setups, call sites | see "Test file disposition" below — **not in the brief's file list** |
| `openspec/changes/archive/**` (6 files) | various | historical record in already-archived changes | leave untouched — archived changes are historical record, not live spec text |
| `docs/backlog.md` | 67-78 | the backlog entry this change resolves | mark resolved / remove the entry in `tasks.md`'s final stage |

No other source, test, or XAML file references `IStartupWindow`. `openspec/specs/` has zero matches
for `IStartupWindow` (confirmed by direct grep), so no spec delta is needed — see Decision 3.

## Test file disposition (verified per-file)

### `tests/CLIHub.Tests/OptionalStartupCoordinatorTests.cs` — no change needed

Full file read. Three `Mock<IApplicationStartupUi>` instances exist, at lines 24, 61, 99 — exactly as the
brief states, three mocks. None of the three ever calls `.Setup(x => x.LaunchWindow)` or
`.SetupGet(x => x.LaunchWindow)` anywhere in the 194-line file (confirmed: no occurrence of `LaunchWindow`
in this file at all). `ApplicationStartupContext` is constructed directly three times (lines 25, 62, 100)
as `new ApplicationStartupContext(_ => { }, _ => { })` — a lambda literal for `SetMainWindow`, not a typed
reference. A lambda `_ => { }` is valid for `Action<IStartupWindow>` today and remains valid for
`Action<LaunchWindow>` after the change, because C# infers the lambda parameter's type from the target
delegate type at each call site — the literal text does not change, no cast, no explicit type annotation
to update. **Verified: this file compiles unchanged and behaves identically after the type change.**

### `tests/CLIHub.Tests/ApplicationHostTests.cs` — no change needed

Full file read. The null-argument guard is tested at:
- `Start_WithNullSetMainWindow_ThrowsArgumentNullException` (lines 33-44): `host.Start(action => action(), null!)`, asserts `ex.ParamName == "setMainWindow"`.
- `Start_WithNullSetMainWindow_DisposesGuardAcquiredBeforeTheFailure` (lines 78-92): same pattern, plus guard disposal.
- `Start_SecondInstance_SignalsActivationAndSkipsEnvironmentSetup` (line 68): `host.Start(action => action(), window => { })` — a lambda, same inference argument as above.

`null!` is the null-forgiving literal — it has no declared type, so retyping the delegate parameter from
`Action<IStartupWindow>` to `Action<LaunchWindow>` does not require any change here either. `ApplicationHost.Start`
is not shown in the five cited files but is the method under test here; it is not among the files this
change edits (`ApplicationStartupContext`, `IApplicationStartupUi`, `WpfApplicationStartupUi`, `LaunchWindow.xaml.cs`,
`IStartupWindow.cs`), and this inventory found no `IStartupWindow` reference inside it. **Verified: this
file compiles unchanged and the null-argument guard's `ParamName == "setMainWindow"` behavior is
unaffected — the guard checks the delegate reference for null, which has nothing to do with the
delegate's parameter type.**

### `tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs` — change required (see proposal.md Deviation)

Full file read. This file is the real compile-time blocker:
```csharp
private static ApplicationStartupContext CreateContext(IStartupWindow window) =>
    new(_ => { }, _ => { });                                    // line 94-95

public Mock<IStartupWindow> Window { get; } = new(MockBehavior.Strict);  // line 106

fixture.Ui.InSequence(sequence).SetupGet(x => x.LaunchWindow)
    .Returns(fixture.Window.Object);                            // line 30
fixture.Ui.SetupGet(x => x.LaunchWindow).Returns(fixture.Window.Object); // line 65

fixture.Bootstrapper.Start(CreateContext(fixture.Window.Object)); // lines 35, 47, 69
```
Note `CreateContext`'s `window` parameter (line 94) is declared but never used in its body (`new(_ => { }, _ => { })`
ignores it) — it exists purely as a type-check convenience at call sites, which pass `fixture.Window.Object`
without it being threaded anywhere. After the interface is deleted:
- `Mock<IStartupWindow>` has no backing type. `LaunchWindow` is a `partial class : Window` with no
  `protected`/`public virtual` members over which Moq could generate a proxy, and WPF `Window` requires a
  live `Application`/dispatcher context that a bare `Mock<LaunchWindow>` cannot satisfy — a mocked
  concrete `LaunchWindow` is not a drop-in replacement.
- `fixture.Ui.SetupGet(x => x.LaunchWindow).Returns(...)` needs a value assignable to the new
  `LaunchWindow` property type.

This requires a genuine test-file edit (not a source file), which is outside this preparation agent's
authority ("No source edits... no test edits"). `tasks.md` Stage 2 stages it explicitly as the first
thing the implementing session must resolve once the production retyping compiles, with two documented
options for the implementer to choose from — it is not this proposal's place to pick one, since that
is itself a test-design decision requiring judgment calls this document should not make unilaterally:
  - **Option A:** Replace `Mock<IStartupWindow> Window` with a real, minimal `LaunchWindow` test instance
    (existing precedent: `tests/CLIHub.Tests/Views/LaunchWindowClosedGuardTests.cs` already constructs a
    real `LaunchWindow` for assertions outside an `Application` context — the implementer should check
    whether that pattern transfers here).
  - **Option B:** Introduce a small internal test-only subclass or fake that satisfies the `LaunchWindow`
    type requirement for the mock substitution (only viable if `LaunchWindow`'s constructor and members
    allow a lightweight subclass; `LaunchWindow` is `public partial class` with a public constructor
    taking `LaunchWindowViewModel`, `PromptState`, `IPreferencesStore` — not `sealed`, so subclassing is
    possible, but the real constructor still runs `InitializeComponent()`, which has the same WPF context
    requirement as Option A's direct construction).

Either option is a test-file change the implementing session must make and verify against
`ApplicationBootstrapperTests`'s three existing test methods (`Start_FirstInstance_ExecutesStartupInOrder`,
`Start_RequiredStartupFailure_LogsAndThrows`, `Start_OptionalReleaseNotesFailure_ContinuesStartup`), all
of which currently pass `fixture.Window.Object` through `CreateContext` and `SetupGet(x => x.LaunchWindow)`.

## Decisions

### Decision 1: Retype in place rather than introduce a replacement abstraction

**Chosen:** `IApplicationStartupUi.LaunchWindow` and `ApplicationStartupContext.SetMainWindow` both move
directly to `CLIHub.Views.LaunchWindow`.

**Rationale:** One implementer exists today (`WpfApplicationStartupUi.LaunchWindow` always returns the
injected concrete `LaunchWindow`); the member-less interface added a layer of indirection with no present
benefit. The backlog's own proposed remedy (`docs/backlog.md:75-76`) names this as the default path,
reserving a new abstraction only for when "a second implementation appears."

**Alternatives rejected:**
- Keep `IStartupWindow` as a documentation-only marker: rejected — it has already lost every member and
  adds a lookup indirection for zero typing benefit; the backlog flags it as removable now.
- Introduce a narrower interface with real members anticipating future implementers: rejected by the
  brief's explicit instruction and by YAGNI — no second implementer exists, and speculative interface
  design is harder to get right without a second real use case to generalize from.

### Decision 2: Add `using CLIHub.Views;` to `ApplicationStartupContext.cs` rather than fully-qualify

**Chosen:** Add the `using` directive to keep the file's existing unqualified-name style consistent with
sibling files in `CLIHub.Startup` (e.g. `WpfApplicationStartupUi.cs:3`).

**Rationale:** `ApplicationStartupContext.cs` currently has zero `using` directives because its only
referenced types (`Action<Action>`, record syntax) need none. Adding one `using` for one new dependency
is the smallest change that matches the codebase's existing convention of unqualified type names within
a file, rather than writing `CLIHub.Views.LaunchWindow` inline.

**Alternatives rejected:**
- Fully-qualify `CLIHub.Views.LaunchWindow` inline without a `using`: rejected — inconsistent with the
  rest of the `Startup` namespace's style and adds visual noise for a single-use type that recurs across
  the file's one property.

### Decision 3: `skip_specs: true`, no spec delta

**Chosen:** No `specs/<capability>/spec.md` delta; `.openspec.yaml` carries `skip_specs: true`.

**Rationale:** `openspec/specs/` has zero matches for `IStartupWindow` (confirmed by direct grep across
the live spec tree, excluding `openspec/changes/archive/**`, which is historical record of past changes,
not current spec text). No requirement or scenario names the interface, its (already-removed) member, or
the `LaunchWindow` property/delegate by the interface type. This is a pure internal type substitution
with no observable behavior change — the same object (`CLIHub.Views.LaunchWindow`) flows through the same
call sites in the same order; only its declared/static type at two call sites changes from an empty
interface to the concrete class.

**Alternatives rejected:**
- Writing a spec delta anyway for traceability: rejected — there is no durable behavior requirement this
  change affects; inventing one would misrepresent the change as behavior-affecting when it is not.

## Risks

**Low risk, with one real compile hazard:**
- The production retyping (4 source-file edits + 1 deletion) is mechanical and type-checked by the
  compiler at every call site; a build failure surfaces immediately if anything is missed.
- The one genuine risk is `ApplicationBootstrapperTests.cs`'s `Mock<IStartupWindow>` — if the implementing
  session deletes `IStartupWindow.cs` before updating this test file, the solution will not build. `tasks.md`
  orders the test-file fix immediately after the production retyping and before the final build gate to
  make this ordering explicit, and calls out that skipping it is not an option (unlike a normal "no other
  cleanup" boundary, this file's edit is required for the change to compile at all).
- `OptionalStartupCoordinatorTests.cs` and `ApplicationHostTestExs.cs`[sic — see below] were verified by
  full-file read to need no changes; still, the final test gate (`dotnet test CLIHub.sln`) will catch any
  overlooked reference immediately, since every reference compiles to a symbol the compiler checks.

(Typo note: the risk above is corrected in `tasks.md` — the file is `ApplicationHostTests.cs`.)

## Open Questions

1. **Which test-double option (A or B) should `ApplicationBootstrapperTests.cs` use for the retyped
   `LaunchWindow` property/parameter?** This design intentionally leaves the choice to the implementing
   session, which can run a quick spike against both and see which one actually constructs successfully
   outside an `Application`/dispatcher context (the existing `LaunchWindowClosedGuardTests.cs` precedent
   is the strongest lead but was not re-verified against `ApplicationBootstrapperTests`'s specific usage
   pattern — `Window` is set up via `SetupGet` on a mock and returned polymorphically, not directly
   asserted against, which is a different usage shape than `LaunchWindowClosedGuardTests.cs`).
2. **Should the now-resolved `docs/backlog.md:67-78` entry be deleted or marked resolved as part of this
   change, or left for a separate docs pass?** `tasks.md` stages its removal as the final task, consistent
   with this change being the entry's own stated trigger, but this is a judgment call the brief did not
   address and the implementing session should confirm before deleting backlog prose.

## Orchestrator resolution of the open questions (2026-11-01)

Both questions were resolved before implementation; `tasks.md` carries the outcomes.

1. **Neither option A nor B is possible.** Constructing any `LaunchWindow` in this test host throws:
   `LaunchWindow.xaml.cs` calls `InitializeComponent()`, and the XAML root resolves
   `StaticResource Sizing.FontFamily.Text` from application-level merged dictionaries, so it raises
   `XamlParseException` without an `Application`. `LaunchWindowClosedGuardTests.cs:12-20` documents that
   exact limitation, and it is **not** a precedent for constructing a `LaunchWindow` — that test uses a
   plain `Window`. A subclass fails for the same reason (its base constructor runs `InitializeComponent`).
   The chosen approach is a null window: the two strict UI setups return `(LaunchWindow)null!` and the
   unused `CreateContext(IStartupWindow window)` parameter is dropped. This is sound because the tests
   never read the window — `ApplicationStartupContext` receives it and hands it to a no-op
   `SetMainWindow` delegate, and no assertion inspects it. `tasks.md` 2.1-2.2 now prescribe this
   explicitly instead of leaving a spike to the implementation session.
2. **Mark resolved, do not delete.** `docs/backlog.md` has an established convention: resolved entries keep
   their heading and evidence text with a ` - **[done <date>: <change-id>]**` suffix (for example
   `provenance-mark-contrast`, `agent-row-inset-alignment`). Deleting the prose would contradict the
   file's own header, which says closed items keep their evidence trail. `tasks.md` 4.1 now annotates
   rather than deletes.
