## 1. State And Mapping

- [x] 1.1 Make `ConfigurationSnapshot` the sole mutable repository state owner and preserve callback failure atomicity.
- [x] 1.2 Add explicit persistence DTOs and a complete bidirectional mapper without changing the flat JSON shape.

## 2. Persistence Semantics

- [x] 2.1 Preserve migrations, generation ordering, concurrent updates, debounce/Flush, and atomic replacement behavior.
- [x] 2.2 Add tests covering every mapped field and latest valid snapshots under concurrent updates.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`.
- [x] 3.2 Run `graphify update .`, validate the change, update `improvements.md`, and archive it.
