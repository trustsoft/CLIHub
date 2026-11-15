# Architecture Principles

This is the decision checklist for new work: [`docs/architecture.md`](architecture.md) describes the current
structure, [`docs/adr/`](adr/README.md) records why past decisions were made, and this file says which rules apply
when adding something. Each principle gives the rule, why it exists, how it looks in practice, and the smell that
means it is broken; claims are checkable against the linked ADR, doc, or test.

## Boundaries and dependencies

### 1. Core stays platform-neutral; Windows lives behind ports
**Rule:** `CLIHub.Core` never references WPF or a Windows-only API; platform work sits in
`src/CLIHub.Core.Windows` behind a port declared in Core, while API-free shell conventions stay in Core. The
direction is compiler-enforced and keeps Core testable on any .NET 10 SDK
([ADR-0001](adr/0001-core-ui-boundaries.md), [ADR-0006](adr/0006-windows-platform-library.md)).
**Apply:** declare the port in Core, implement it in `CLIHub.Core.Windows`, register the adapter there.
**Red flag:** `System.Windows` or `Microsoft.Win32` used under `src/CLIHub.Core`.

### 2. Depend on the capability you use, not the implementation
**Rule:** consumers take the narrowest interface carrying what they call — `IPreferencesStore` instead of the
configuration document, `IAgentProbeService` instead of `AgentManagementService`. A wide dependency forces every
test to stub unused surface ([ADR-0018](adr/0018-capability-based-service-boundaries.md)).
**Apply:** inject the slice; use the composite `IUpdateService` only when the whole surface is used.
**Red flag:** a constructor typed as a concrete service class, or a test stubbing unasserted members.

### 3. One composition root per layer; one public constructor per service
**Rule:** `AddSingleton`/`AddTransient`/`AddScoped` appear only in the app's `Composition/ServiceRegistration.cs`,
Core's `Composition/ServiceCollectionExtensions.cs`, and `WindowsServiceCollectionExtensions.cs`, and a production
Core service exposes one public constructor (`DependencyRegistration_IsRestrictedToCompositionRoots`;
`openspec/specs/app-lifecycle/spec.md`).
**Apply:** a Core service gets a private `Add…Services` method; optionality uses a defaulted parameter.
**Red flag:** an `AddSingleton` in a feature folder, or a second public "for tests" constructor.

## Data and persistence

### 4. One document per concern, one atomic write path
**Rule:** `config.json` is the only persisted configuration and `registry.json` the only plugin-lifecycle record;
both use a temp-file-then-rename path and neither carries a schema version. Read is version-tolerant: unknown
fields are ignored, missing fields default ([ADR-0002](adr/0002-single-configuration-document.md),
[ADR-0014](adr/0014-remove-configuration-migrations.md), [ADR-0015](adr/0015-plugin-registry-v2.md)).
**Apply:** add the field with its default and a narrow store accessor; no schema bump or migration.
**Red flag:** a second `*.json` holding preferences or plugin state, or a direct write under `%APPDATA%\CLIHub\`.

### 5. Persistence and cache stay concrete until three consumers share a policy
**Rule:** no `IPersistenceService<T>` or `ICacheService`; keep each policy inline. The consumers share two
statements but mutually exclusive policies (debounce, dirty flag, TTL, prefix invalidation), and `AtomicFileWriter`
came at the third consumer ([ADR-0013](adr/0013-no-generic-persistence-cache.md)).
**Apply:** write the concrete policy next to its consumer; extract only the shared fragment at the third case.
**Red flag:** an interface with optional TTL/persistence/dedup parameters, or a generic with one instantiation.

## Services and composition

### 6. Split interfaces by capability, only on demonstrated partial use
**Rule:** one interface per capability plus an optional composite forwarding to the same instance; a further split
needs a real consumer or test subset, and folding slices back requires a new ADR
([ADR-0018](adr/0018-capability-based-service-boundaries.md), `docs/adr/README.md`).
**Apply:** add the capability, register both to the same instance, and name the consumer that benefits.
**Red flag:** an interface extracted "for flexibility", or a consumer injecting both a capability and its class.

### 7. Orchestration lives where the workflow lives
**Rule:** domain workflows are orchestrated in Core (`ProcessOrchestrator` composes runtime resolution, command
building, launch, capture, process finding); UI coordination stays thin in the app layer
(`WindowActionCoordinator`) ([ADR-0012](adr/ADR-0012-process-domain-unification.md),
`docs/architecture/processes.md`).
**Apply:** a multi-service workflow becomes a Core orchestrator; a UI coordinator calls one Core method per step.
**Red flag:** a ViewModel resolving a runtime, building a command line, or deciding if an update is allowed.

### 8. Inject dependencies; never read ambient state
**Rule:** collaborators arrive through constructors; no static current-context, service locator, or ambient logger
— operation identity is a disposable scope object ([ADR-0019](adr/0019-logging-operation-scopes.md)), because
ambient state hides dependencies and breaks across thread hops.
**Apply:** add the dependency to the constructor and the composition file; pass context as a parameter.
**Red flag:** a static field holding a service, a service locator, or `AsyncLocal` application state.

## Domain events

### 9. Events are audit and diagnostic; an event must earn its existence
**Rule:** publish after the durable fact, one publisher per event type, synchronous in-process; subscribers are
idempotent-safe, never throw, never change behavior; add an event only for a non-UI consumer or durable diagnostic
value ([ADR-0017](adr/0017-synchronous-domain-event-bus.md), `docs/architecture/domain-events.md`).
**Apply:** publish once, after success, at the single site; a subscriber opens a logging scope.
**Red flag:** a state-mutating subscriber, two publish sites for one event, or an event replacing a direct call.

## Process and agent safety

### 10. Interactive launch and captured output are separate contracts
**Rule:** `IInteractiveProcessRunner` and `IProcessOutputRunner` stay separate; no compatibility aggregate is
registered. One attaches to the user's console and returns, the other captures bounded output with a timeout, so
an aggregate invites the wrong choice (`docs/architecture.md` invariants).
**Apply:** choose the port by what the caller does with the result; `ProcessOrchestrator` composes both.
**Red flag:** a new interface exposing both launch and capture "for convenience".

### 11. A running matching agent blocks its own update — twice
**Rule:** the Update action is disabled while a matching agent runs, and the check is repeated at execution time
before anything starts; the name-only tier is advisory, the execution gate uses command-line detail, because state
changes between render and click (`docs/architecture/processes.md`).
**Apply:** keep both checks; a new update entry point calls the full-tier check itself.
**Red flag:** an update that starts the process before validating, or a UI-only gate.

### 12. Project folders are launch context, never managed content
**Rule:** registration, path change, and removal touch the project state document and logo cache only; nothing
under the folder is created, moved, or deleted (`docs/architecture.md` invariants) — the folder is launch context
the user owns.
**Apply:** copy the path, validate existence, persist.
**Red flag:** `Directory.CreateDirectory`, a file write, or a git call against a registered project path.

## Plugins are data

### 13. Plugin code never executes in-process
**Rule:** a plugin is a JSON descriptor plus an optional logo; no DLL loading, no reflection over plugin code,
nothing from the descriptor runs in-process ([ADR-0004](adr/0004-plugin-catalog-and-precedence.md),
[ADR-0016](adr/0016-plugin-lifecycle-manager-rejected.md)); its commands launch in an external terminal.
**Apply:** extend the descriptor schema and validator; a feature that "needs plugin code" needs a Core capability.
**Red flag:** `Assembly.Load`, `Activator.CreateInstance` over a plugin path, or a plugin-provided callback.

### 14. Plugin files are user data
**Rule:** a built-in refresh overwrites a descriptor only when the on-disk hash still matches what CLIHub last
wrote; user-modified files are preserved and reported. The three-hash comparison (embedded, stored, disk) is the
only way to distinguish an app update from a user edit (`docs/architecture/plugins.md`).
**Apply:** compare before writing; update the stored hash only for bytes CLIHub wrote.
**Red flag:** an unconditional copy of an embedded descriptor, or a hash rewritten after detecting a mismatch.

## UI and theming

### 15. ViewModels own state and commands; code-behind stays thin
**Rule:** `INotifyPropertyChanged` state, commands, and workflows live in ViewModels and their existing
collaborators; code-behind handles view-only mechanics; UI text is English (`docs/architecture.md`, `AGENTS.md`).
ViewModel state is testable without a window.
**Apply:** add the property and command to the ViewModel, bind it, reuse `tests/CLIHub.Tests/Builders/`.
**Red flag:** a click handler calling a domain service, or logic behind a `Visibility` converter.

### 16. Use the design tokens, not literals
**Rule:** colours from the palettes, metrics from `Sizing.*` in `Themes/Common/Sizing.xaml`, glyphs from
`IconGlyphs`; no new literal colour, size, radius, or glyph where a token exists (`docs/architecture/ui.md`).
Palettes must stay structurally identical and swappable.
**Apply:** reference `{StaticResource Sizing.*}` and `{x:Static themes:IconGlyphs.Name}`; add a token on the third repeat.
**Red flag:** `Foreground="#FF..."`, a `FontSize` for a role that has a token, or a raw `&#xE7..;` in XAML.

### 17. Accessibility is part of done
**Rule:** decorative glyphs use `controls:DecorativeGlyph` and carry no automation name; interactive controls,
especially icon-only buttons, declare an explicit `AutomationProperties.Name` (pinned by `DecorativeGlyphTests`).
`DecorativeGlyph` returns no automation peer, so a glyph beside a label is not announced twice.
**Apply:** a glyph beside a label is a `DecorativeGlyph`; a glyph that is the only label goes on the control.
**Red flag:** an icon-only button with no `AutomationProperties.Name`, or a glyph as an interactive label.

## Errors, cancellation, logging

### 18. Structured results across boundaries; cancellation is not a failure
**Rule:** expected failures return a result record — `PluginInstallResult`, `PluginFeedResult`,
`AgentCommandResult`, `ProcessResult`, `PluginValidationResult` — instead of throwing. Cancellation is caught only
when the token is actually cancelled, and `ProcessOrchestrator` maps it to `ProcessResultKind.Cancelled`, never
`Failure`.
**Apply:** return the result with an error list; filter the catch on the token, and let bugs propagate.
**Red flag:** `catch (Exception)` turning cancellation into a failure, or a throw for an invalid package.

### 19. Fail-soft versus hard-fail is per subsystem, and it is written down
**Rule:** decide per operation whether a failure aborts or degrades, and record the policy where the operation
lives (`docs/architecture/startup.md` failure table, `docs/architecture/plugins.md`). Seeding warns and startup
continues; an invalid feed signature is a hard error with no cache fallback.
**Apply:** name the policy in the topic doc; degraded reads log, integrity failures withhold data.
**Red flag:** a new failure path with no documented policy, or a security check that degrades to cached data.

### 20. Log through operation scopes opened at entry points
**Rule:** `ILoggingContextProvider.BeginOperation(name)` is called once at the boundary of the triggered operation,
never in a helper and never around a long-lived loop; the scope carries `OperationName` and `CorrelationId` as
structured properties ([ADR-0019](adr/0019-logging-operation-scopes.md)).
**Apply:** `using var scope = this.loggingContext.BeginOperation("VerbNoun");` first in the operation.
**Red flag:** a long-running path with no scope, or `BeginOperation` inside a private helper.

## Testing and verification

### 21. Run the three gates, and never weaken a test to pass them
**Rule:** `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`, and read the warnings as
part of the gate; no assertion is deleted, skipped, or loosened to reach green (`docs/testing.md`).
`TreatWarningsAsErrors` is not set, so deleting a test produces the same green run.
**Apply:** fix new warnings in the change that introduced them; cite the change when updating a test.
**Red flag:** "it built" as evidence, a warning tolerated because CI is green, or `Assert.True(true)`.

### 22. Follow the naming and isolation patterns
**Rule:** tests are named `MethodOrScenario_Condition_ExpectedResult`, assert one behavior with one act, use
`FakeTimeProvider` instead of sleeping, and create temp directories with a `Guid` suffix deleted in `finally`
(`docs/testing.md`). Fake time keeps the suite deterministic under default parallelism.
**Apply:** a Core behavior goes in `tests/CLIHub.Core.Tests` even when WPF consumes it; strict for interaction.
**Red flag:** a sleep, a temp path derived from the test name alone, or a second `Act` in one test.

## Change discipline

### 23. OpenSpec for behavior, an ADR for decisions, a trigger for reopening
**Rule:** a durable capability change gets an OpenSpec change before implementation; a decision that constrains
future work gets an ADR; a finding not being fixed now goes to `docs/backlog.md` with a revisit trigger; an
accepted ADR is reopened only via that trigger or a new ADR that supersedes it (`AGENTS.md`,
`docs/adr/README.md`).
**Apply:** propose, implement with its test, update the ADR index and backlog, mark the old ADR `Superseded`.
**Red flag:** a user-visible change with no spec delta, or a decision recorded only in a commit message.

## When in doubt

Prefer the smallest seam that satisfies these principles: one interface, one registration, one document, one
orchestrator. Add the abstraction when the second real consumer arrives, not before. If two principles conflict,
the binding constraint is the one in the invariants of [`docs/architecture.md`](architecture.md) or in an accepted
ADR. Record the conflict: a rule that needed a judgment call needs an ADR.
