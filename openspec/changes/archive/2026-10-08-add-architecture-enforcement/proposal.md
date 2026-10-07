## Why

The repository has established Core/application boundaries and canonical contracts, but CI currently verifies only compilation and test execution. A future change could reintroduce WPF references into Core, bypass application ports with concrete UI dependencies, or restore retired compatibility APIs without a failing check.

## What Changes

- Add architecture tests that inspect project/source boundaries and fail on forbidden dependencies or retired symbols.
- Keep WPF references constrained to the WPF application and explicit UI/infrastructure adapter files.
- Assert the Core project has no WPF dependency and the application project references Core in the supported direction.
- Assert both Core and application test projects remain represented in CI.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This adds verification only; `skip_specs: true` is set because runtime behavior is unchanged.

## Impact

- Adds architecture-focused tests and a small documented allowlist for intentional WPF adapters.
- CI fails when forbidden references or retired compatibility names are introduced.
