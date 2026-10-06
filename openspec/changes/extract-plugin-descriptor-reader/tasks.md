## 1. Add Descriptor Reader

- [ ] 1.1 Add `PluginDescriptorReadResult` and `IPluginDescriptorReader.ReadAll`, then implement directory discovery and JSON reading with the existing file and serializer behavior.
- [ ] 1.2 Add focused reader tests for missing root, absent descriptor files, valid JSON, JSON null, malformed JSON, and enumeration order.

## 2. Integrate Plugin Loading

- [ ] 2.1 Inject the reader into `PluginManager`, remove direct descriptor file/JSON reads, and preserve validation, duplicate, logo, and logging behavior.
- [ ] 2.2 Register the reader in Core DI and verify direct PluginManager construction and composition resolution.

## 3. Verify Preserved Behavior

- [ ] 3.1 Run focused plugin tests and the complete Core test project.
- [ ] 3.2 Run `dotnet build CLIHub.sln -c Release`.
- [ ] 3.3 Run `dotnet test CLIHub.sln -c Release`.
- [ ] 3.4 Run `openspec validate extract-plugin-descriptor-reader` and verify the change remains explicitly spec-free.
