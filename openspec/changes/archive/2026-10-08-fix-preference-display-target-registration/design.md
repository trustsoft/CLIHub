## Context

`PreferenceApplier` depends on `IPathDisplayStyleTarget`. `LaunchWindowViewModel` implements that interface and is already registered as a singleton, but the interface mapping was omitted from `ServiceRegistration`.

## Decision

Add a factory registration that resolves the existing `LaunchWindowViewModel` singleton:

```csharp
services.AddSingleton<IPathDisplayStyleTarget>(
    sp => sp.GetRequiredService<LaunchWindowViewModel>());
```

This preserves the ViewModel instance used by the launch window and avoids constructing a second target. A registration test verifies the mapping exists.

## Verification

- Release build succeeds without warnings or errors.
- All Core and application tests pass.
