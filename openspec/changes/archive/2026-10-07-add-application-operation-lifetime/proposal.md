## Why

CLIHub still starts startup checks, update downloads, agent commands, and some ViewModel refresh work as fire-and-forget tasks without a shared owner or application cancellation token. During shutdown these operations can outlive the UI, continue using disposed services, or lose cancellation and completion errors.

## What Changes

- Add one application operation-lifetime boundary that owns the application cancellation token and tracked asynchronous operations.
- Pass the application token to startup update checks, update downloads, agent command workflows, settings checks, and agent version population.
- Register fire-and-forget UI/event operations through the lifetime boundary so unexpected exceptions remain observable and tasks are awaited during shutdown.
- Cancel tracked operations when application shutdown begins and await them with a bounded shutdown policy before disposing the service provider.
- Preserve existing service result handling, update behavior, and user-visible status messages.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `app-lifecycle`: Define ownership, cancellation, exception handling, and bounded shutdown behavior for application background operations.

## Impact

- Affected application lifetime, bootstrapper, update coordinators, and ViewModels under `src/CLIHub`.
- Existing async contracts gain optional cancellation-token parameters where they represent application operations.
- New application lifetime tests and cancellation-path tests under `tests/CLIHub.Tests`.
- No change to `config.json`, plugin descriptors, process termination semantics, or update result meanings.
