## 1. Specification and composition

- [x] 1.1 Record the ownership boundary, shutdown behavior, and non-goals in proposal and design artifacts.
- [x] 1.2 Add the app-lifecycle delta covering singleton resolution and provider-owned disposal.
- [x] 1.3 Register `SingleInstanceGuard` as the explicit application singleton and resolve it from `App.OnStartup`.

## 2. Regression coverage

- [x] 2.1 Add a composition test proving the production guard registration is a single singleton descriptor.
- [x] 2.2 Add a disposal test proving provider disposal releases the mutex for a subsequent guard.

## 3. Verification and completion

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release` with zero warnings and errors.
- [x] 3.2 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.3 Run `openspec validate "unify-single-instance-ownership"`.
- [x] 3.4 Archive the completed change and update `improvements.md` progress.
