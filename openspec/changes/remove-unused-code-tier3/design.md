## Context

This change removes a third batch of dead code identified through whole-solution Roslyn reference analysis plus manual verification. Tiers 1 and 2 removed dead types, full interface–implementation–test chains, and dead interface-member-only items. This tier is smaller: two unused private fields, dead null-conditionals in a proven-non-null path, an unused method parameter propagated through one interface and its adapter, two dead test items, and six unused `using` directives.

## Goals / Non-Goals

**Goals:**
- Remove all verified dead code items listed in proposal.md with zero behavior change
- Remove `PluginFeedClient.versionProvider` field and clean up its constructor, tests, and the now-unused `using`
- Remove `BrowsePluginsDialog.viewModel` field and its assignment
- Replace dead null-conditionals with direct member access in `ApplicationHost.Shutdown()`
- Remove the `workingDirectory` parameter from `BuildInteractive` and propagate through interface, adapter, orchestrator, and test call sites
- Delete the dead `CreatePanel(params UIElement[])` test overload
- Delete the dead `AnotherEvent` private test record
- Remove six verified unused `using` directives

**Non-Goals:**
- Behavior changes, UI changes, or durable capability changes
- Removing `args` from `Program.Main` (explicit non-goal, see Decision 2)
- Cleaning `using` directives outside the six listed files
- Removing test-only seams (`CreateForTesting`, etc.)
- Refactoring or renaming live code

---

## Current State

### Item 1: `PluginFeedClient.versionProvider` (IDE0052)

**File:** `src/CLIHub.Core/Plugins/PluginFeedClient.cs`

**Field:** `private readonly IUpdateVersionProvider versionProvider;` at line 27.

**Assigned at:**
- Public constructor `PluginFeedClient(IPreferencesStore, IUpdateVersionProvider, ILogger<>)` at line 36: parameter `versionProvider` at line 37, assignment `this.versionProvider = versionProvider ?? throw ...` at line 57.
- Internal test constructor `PluginFeedClient(HttpMessageHandler, IPreferencesStore, IUpdateVersionProvider, ILogger<>)` at line 51: parameter `versionProvider` at line 53, same guard-assign at line 57.

**Never read:** No reference to `this.versionProvider` in any method body. The field is write-only.

**`using` directive:** `using CLIHub.Core.Updates;` at line 13. `IUpdateVersionProvider` lives in `CLIHub.Core.Updates`. After removing the field and constructor parameters, this namespace is no longer referenced in the file; the directive becomes unused.

**DI registration:** `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:111`:
```csharp
services.AddSingleton<IPluginFeedClient, PluginFeedClient>();
```
Type-based; the runtime DI container resolves parameters by type. Removing `IUpdateVersionProvider` from the constructor means DI stops requiring it, with no registration change needed.

**Test impact:** `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`
- Line 14: `using CLIHub.Core.Updates;` — becomes unused after removing the mock.
- Line 33: `var versionProvider = Mock.Of<IUpdateVersionProvider>(v => v.GetCurrentVersion() == "1.0.0");` — remove.
- Line 34: `return new PluginFeedClient(this.handler, preferences, versionProvider, NullLogger<PluginFeedClient>.Instance);` — drop `versionProvider` argument.

**Decision:** Remove the field, both constructor parameters, the `?? throw` guard, both `<param>` XML doc lines, the `using CLIHub.Core.Updates;` in `PluginFeedClient.cs`, the Mock setup in the test, the test `using`, and the test argument.

---

### Item 2: `BrowsePluginsDialog.viewModel` (IDE0052)

**File:** `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`

**Field:** `private readonly BrowsePluginsDialogViewModel viewModel;` at line 12.

**Assigned at:** `this.viewModel = viewModel;` at line 22.

**Never read:** The constructor sets `this.DataContext = viewModel;` at line 23 (using the parameter, not the field). The field is a write-only copy.

**No XAML binding:** The XAML-side DataContext binding is set programmatically via `this.DataContext`. No XAML attribute references `viewModel` by name.

**Decision:** Remove the field (line 12) and the assignment (line 22). Keep the constructor parameter and `this.DataContext = viewModel;` unchanged.

---

### Item 3: Dead null-conditionals in `ApplicationHost.Shutdown()` (CA1508)

**File:** `src/CLIHub/Startup/ApplicationHost.cs`

**Guard:** Lines 89-95 — if `this.services is null`, the method disposes the guard and returns immediately. After line 95, `this.services` is proven non-null for the rest of `Shutdown()`.

**Dead `?.` on `services`:**
- Line 101: `this.services?.GetService<IApplicationSession>()?.Dispose();` — the `?.` on `services` is unreachable; the `GetService<>()` call itself is always reached. The `?.Dispose()` on the returned session stays (session can be null if not registered or already disposed).
- Lines 110-111: `this.services?.GetService<IApplicationOperationLifetime>()?.StopAsync(...)` — same pattern; the `?.` on `services` is unreachable.
- Line 121: `this.services?.Dispose();` — `?.` on `services` is unreachable; `services` is non-null.

**Decision:** Replace each `services?.` with `services.` (direct member access). Keep the `?.` on the results of `GetService<IApplicationSession>()` and `GetService<IApplicationOperationLifetime>()` — those can return null.

**Boundaries:** The `HasBuiltServiceProvider` internal property at line 44 (`this.services is not null`) is a test-only seam — it is explicitly kept by the tier-2 design. Nothing in this change touches it.

---

### Item 4: `workingDirectory` parameter of `BuildInteractive` (IDE0060/RCS1163)

**Root:** `src/CLIHub.Core/Infrastructure/Processes/WindowsCommandLineBuilder.cs`

Method signature at line 27-31:
```csharp
internal static (string FileName, string Arguments) BuildInteractive(RuntimeKind runtime,
                                                                     PluginCommand command,
                                                                     string workingDirectory,
                                                                     string? gitBashExecutable,
                                                                     string? powerShell7Executable)
```
`workingDirectory` (parameter 3) is never used in the method body (lines 33-44). The method builds a command line from `runtime` and `command` only; the working directory is applied externally by the process runner.

**Propagation chain:**

1. **Interface:** `src/CLIHub.Core/Processes/ICommandLineBuilder.cs:17-20` —
   ```csharp
   (string FileName, string Arguments) BuildInteractive(
       RuntimeExecutable runtime,
       PluginCommand command,
       string workingDirectory);
   ```
   Remove `workingDirectory` from the interface member and its XML doc (`<param name="workingDirectory">` at line 15).

2. **Adapter:** `src/CLIHub.Core/Processes/CommandLineBuilderAdapter.cs:12-26` —
   ```csharp
   public (string FileName, string Arguments) BuildInteractive(
       RuntimeExecutable runtime,
       PluginCommand command,
       string workingDirectory)
   ```
   The `workingDirectory` parameter is passed through to `WindowsCommandLineBuilder.BuildInteractive` at line 24. Remove from the method signature; remove from the inner call.

3. **Orchestrator call site:** `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:69-72` —
   ```csharp
   var (fileName, arguments) = this.commandLineBuilder.BuildInteractive(
       runtime,
       request.Command,
       request.WorkingDirectory);
   ```
   Remove `request.WorkingDirectory` from this call. `request.WorkingDirectory` is still used at lines 94-95 for the runner (`LaunchAsync(arguments, runtimeInfo, request.WorkingDirectory, ...)`); that usage is not touched.

4. **Test call sites** — `CommandLineBuilderAdapterTests.cs`:
   - Line 21: `this.adapter.BuildInteractive(runtime, command, @"C:\work")` → remove `@"C:\work"`.
   - Line 36: same pattern.
   - Line 51: same pattern.
   - Line 66: same pattern.

5. **Test call sites** — `ProcessOrchestratorTests.cs`:
   - Line 52: `this.mockBuilder.Setup(x => x.BuildInteractive(runtime, command, @"C:\work"))` → remove `@"C:\work"`.
   - Line 95: same pattern.
   - Line 120: same pattern.

6. **Test call sites** — `WindowsInteractiveProcessRunnerTests.cs`:
   - Lines 137-138: `WindowsCommandLineBuilder.BuildInteractive(RuntimeKind.PowerShell, command, shimDir, gitBashExecutable: null, powerShell7Executable: null)` → remove `shimDir`.
   - Lines 177-178: `WindowsCommandLineBuilder.BuildInteractive(runtimeKind, command, workingDirectory, gitBashExecutable: null, powerShell7Executable)` → remove `workingDirectory`.

**Live usage confirmed:** `request.WorkingDirectory` at `ProcessOrchestrator.cs:94` (passed to `interactiveRunner.LaunchAsync`) is untouched — that is where the working directory is actually applied.

**Decision:** Remove the parameter from all six locations listed above. No behavior change; the working directory was never read inside `BuildInteractive`.

---

### Item 5: `SpacedPanelTests.CreatePanel(params UIElement[])` (IDE0051/RCS1213)

**File:** `tests/CLIHub.Tests/Controls/SpacedPanelTests.cs:182-186`

```csharp
private static SpacedPanel CreatePanel(
    params UIElement[] children)
{
    return CreatePanel(children, Orientation.Vertical, 0);
}
```

**Zero callers:** No test method calls this overload. The other three overloads at lines 188-195, 197-204, and 206-223 are all live (called by multiple test methods).

**Decision:** Delete only the `params UIElement[]` overload. The three other `CreatePanel` overloads stay.

---

### Item 6: `DomainEventBusTests.AnotherEvent` (CA1812)

**File:** `tests/CLIHub.Core.Tests/Events/DomainEventBusTests.cs:10`

```csharp
private record AnotherEvent(int Number) : IDomainEvent;
```

**Never referenced:** `AnotherEvent` is not instantiated, not used in any assertion, not used in any `Subscribe<>()` or `Publish()` call anywhere in the file (confirmed by grep: single match at line 10 only).

**Decision:** Delete the record declaration.

---

### Item 7: Six unused `using` directives (IDE0005)

Each directive is unused at HEAD as confirmed by the absence of any symbol from the imported namespace in the file body.

| File | Line | Directive | Why unused |
|---|---|---|---|
| `src/CLIHub.Core/Agents/AgentCommandService.cs` | 7 | `using CLIHub.Core.Infrastructure.Processes;` | No symbol from `CLIHub.Core.Infrastructure.Processes` is referenced; the file uses `IProcessOrchestrator` from `CLIHub.Core.Processes` (line 9). |
| `src/CLIHub.Core/Agents/AgentVersionService.cs` | 9 | `using CLIHub.Core.Infrastructure.Processes;` | Same — no symbol from the infrastructure namespace is used; orchestrator and request types come from `CLIHub.Core.Processes`. |
| `src/CLIHub.Core/Plugins/PluginFeedClient.cs` | 3 | `using System.Net;` | IDE0005 at HEAD; no `System.Net`-only symbol (`Uri` is `System`, HTTP types are `System.Net.Http`, implicit usings cover the rest) is referenced. |
| `src/CLIHub.Core/Plugins/PluginFeedClient.cs` | 13 | `using CLIHub.Core.Updates;` | Becomes unused after removing `versionProvider` (item 1). Removed once, as part of item 1. |
| `src/CLIHub/Composition/ServiceRegistration.cs` | 10 | `using CLIHub.Core.Infrastructure.Processes;` | No type from this namespace is referenced in `ServiceRegistration.cs`; infrastructure types are registered via `AddWindowsInfrastructure` in `CLIHub.Core.Windows`. |
| `src/CLIHub/ViewModels/Settings/WatchedDirectoryRow.cs` | 3 | `using CLIHub.Core.Models;` | `CLIHub.Core.Models` is the file's own enclosing namespace via `namespace CLIHub.ViewModels;` — the file does not reference any type name from `CLIHub.Core.Models` directly. |
| `src/CLIHub/ViewModels/Settings/AccentSelection.cs` | 4 | `using CLIHub.Theming;` | No symbol from `CLIHub.Theming` is referenced in `AccentSelection.cs`. |

Note: `PluginFeedClient.cs:13` is the same directive removed in item 1; it is listed both places to account for the cross-item dependency. The implementation removes it once, as part of item 1.

---

### Item 8 (judgment): `Program.Main(string[] args)` — Keep

**File:** `src/CLIHub/Program.cs:15`

```csharp
public static void Main(string[] args)
```

`args` is not passed to any other call in the method body. Roslyn reports IDE0060.

**Decision: KEEP the parameter.** Reasons:
1. `Main(string[] args)` is the canonical C# and .NET 10 application entry-point signature. WPF's `App.Run()` and the Velopack `VelopackApp.Build().Run()` call immediately before it do not need the args, but the entry point form is idiomatic.
2. The `args` parameter is part of the standard `[STAThread] static void Main(string[] args)` convention and any future use of `VelopackApp.Build().Run(args)` or environment introspection would require it back.
3. Removing it gains nothing visible and deviates from the conventional WPF entry-point signature without a concrete reason.

This is explicitly recorded as a non-goal and will not be revisited by this change.

---

## Spec-Required Members (Not Touched)

None of the items in this change are spec-required. No spec delta is needed.

---

## Decisions

### Decision 1: Remove `workingDirectory` parameter from `BuildInteractive`

The parameter is present in `WindowsCommandLineBuilder.BuildInteractive` (the internal static builder), `ICommandLineBuilder.BuildInteractive` (the interface), `CommandLineBuilderAdapter.BuildInteractive` (the adapter), and is threaded through from `ProcessOrchestrator`. It is also used directly in `WindowsInteractiveProcessRunnerTests.cs` (lines 137-138, 177-178), which calls the static builder directly to produce command lines for integration testing.

The working directory has always been applied by the runner (`interactiveRunner.LaunchAsync`), not by the command-line builder. The parameter was present but unused across its entire propagation chain.

Removing it from all call sites in the test file is safe: the tests pass the working directory to the runner (`runner.LaunchAsync(arguments, runtime, workingDirectory, ...)`) which is the correct place.

**Alternatives considered:**
- Keep the parameter with a `#pragma warning disable` or `[SuppressMessage]`: rejected — suppressing a real unused-parameter finding is worse than fixing it.
- Remove only from `WindowsCommandLineBuilder` and add a discard in the adapter: rejected — leaves a dead parameter in the interface, which is the primary dead surface.

### Decision 2: Keep `Program.Main(string[] args)`

See Item 8 above. Explicitly recorded as a non-goal.

### Decision 3: Separate stage for `using` directives

Six unused `using` directives are removed as a dedicated final stage rather than inline with each field or parameter removal. This isolates the pure-whitespace changes and makes `dotnet build` the verification gate for each removal unit.

---

## Risks

**Low-risk change:**
- All removals are verified through whole-solution Roslyn reference analysis plus manual XAML/spec checks.
- The `workingDirectory` parameter removal is the highest-touch item (six files), but each change is a mechanical argument removal — no logic changes.
- Build and test gates after each stage catch any missed reference.

---

## Open Questions

None — all items are evidence-verified and the `Program.Main` judgment is documented.
