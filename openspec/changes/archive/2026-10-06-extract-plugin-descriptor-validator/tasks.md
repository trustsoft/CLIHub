## 1. Add Descriptor Validator

- [x] 1.1 Add `PluginValidationResult`, `IPluginDescriptorValidator`, and the implementation with the existing ID/name/launch rules in their documented order.
- [x] 1.2 Add focused tests for each missing field, combined errors, and a valid descriptor.

## 2. Integrate Plugin Loading

- [x] 2.1 Inject the validator into `PluginManager`, remove inline validation, and preserve existing warning text and skip behavior.
- [x] 2.2 Register the validator in Core DI and verify PluginManager composition/direct construction.

## 3. Verify Preserved Behavior

- [x] 3.1 Run focused plugin/validator tests and the complete Core test project.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.3 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.4 Run `openspec validate extract-plugin-descriptor-validator` and verify the change remains explicitly spec-free.
