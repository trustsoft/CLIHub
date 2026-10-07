## Context

See `proposal.md` for the motivation. The current ViewModel performs project collection synchronization, project mutations, agent composition, selection identity preservation, cache invalidation, and asynchronous version population directly. It already has application-operation lifetime support, which the extracted agent workflow must retain.

The extracted controllers must not depend on WPF windows or `Application.Current`. The ViewModel remains responsible for presentation-only status text and command construction.

## Goals / Non-Goals

**Goals:**

- Make project and agent workflows independently constructible and testable.
- Keep collection identity and selected-item identity behavior unchanged.
- Keep asynchronous version population cancellable through the application operation lifetime.
- Reduce direct infrastructure dependencies in `LaunchWindowViewModel`.
- Preserve the existing binding surface used by `LaunchWindow.xaml`.

**Non-Goals:**

- Redesigning the XAML layout or changing the visual behavior of either pane.
- Splitting agent command execution into a new workflow; it remains behind `IAgentCommandWorkflow`.
- Changing preferences, update control, dialogs, or notification contracts.
- Introducing new projects or moving code out of the WPF application project.

## Decisions

### Use pane controllers with observable collections

`ProjectPaneController` owns project collection synchronization, current-project selection, and project mutations. `AgentPaneController` owns agent composition, filtering inputs, selected-agent identity, cache invalidation, and version population. Both expose only application/UI-neutral state and methods; the ViewModel forwards its existing binding properties to them.

An alternative was to create stateless workflow services returning collection snapshots. That would make the ViewModel responsible for diff synchronization and selection identity again, leaving the hardest behavior in the facade.

### Keep presentation messages in the ViewModel

Controllers return operation outcomes or expose state changes without setting WPF-bound status text. The ViewModel translates outcomes into the existing status messages and owns notifications where the current behavior requires them.

### Pass refresh inputs explicitly

The agent controller receives current project path and availability-filter state when refreshing rather than depending on a ViewModel callback or service locator. This avoids construction cycles and keeps the controller deterministic in tests.

### Register controllers as ViewModel-owned dependencies

The composition root registers the controllers with the same lifetime as `LaunchWindowViewModel`. The controller constructors receive their existing service dependencies, while the ViewModel receives the two workflow boundaries instead of each underlying project/agent service.

## Risks / Trade-offs

- [Risk] Selection can reset while a collection is synchronized. -> Keep diff synchronization and selected-id restoration in the controllers and add identity-focused tests.
- [Risk] Version tasks can update stale agent rows. -> Retain generation checks and linked refresh/application cancellation tokens in `AgentPaneController`.
- [Risk] Controller outcomes can drift from existing status text. -> Keep message mapping in the ViewModel and preserve existing strings in focused tests.

## Migration Plan

1. Extract project and agent controllers with focused tests.
2. Replace direct ViewModel workflow logic with controller delegation while preserving the binding surface.
3. Register controllers and update direct construction tests.
4. Run build, tests, graphify update, and archive the pure refactor change.
