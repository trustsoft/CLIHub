## 1. Add Descriptor Reader

- [x] 1.1 Add `PluginDescriptorReadResult` and `IPluginDescriptorReader.Read`, then implement descriptor file checks and JSON reading with the existing serializer behavior.
- [x] 1.2 Add focused reader tests for absent descriptor files, valid JSON, JSON null, and malformed/unreadable descriptor files.

## 2. Integrate Plugin Loading

- [x] 2.1 Inject the reader into `PluginManager`, remove direct descriptor file/JSON reads, and preserve directory enumeration, validation, duplicate, logo, and logging behavior.
- [x] 2.2 Register the reader in Core DI and verify direct PluginManager construction and composition resolution.

## 3. Verify Preserved Behavior

- [x] 3.1 Run focused plugin tests and the complete Core test project.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.3 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.4 Run `openspec validate extract-plugin-descriptor-reader` and verify the change remains explicitly spec-free.
