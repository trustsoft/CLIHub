## Context

CLIHub.Core targets `net10.0` and is intentionally UI-independent. The WPF application contains explicit UI adapters and views that may use `System.Windows`; application-facing services, contracts, and view models should not gain direct WPF dependencies. The retired `PluginManager`, `IPluginManager`, and `IProcessLauncher` names must not return to production source.

## Decisions

Add `ArchitectureBoundaryTests` to `CLIHub.Tests` using repository-root discovery from the test output path. The tests inspect source and project files, not generated `obj`/`artifacts` files.

Rules:

- Core source and project files contain no `System.Windows`, `UseWPF`, or WPF project reference.
- Application source files outside an explicit WPF adapter allowlist contain no `System.Windows` using/reference.
- Production source contains none of the retired compatibility symbols.
- The application project references Core and Core does not reference the application.
- CI YAML invokes both test projects.

The allowlist is limited to `Views`, `Interop`, and named WPF adapter files that own OS/UI integration. It is stored in the test so a new WPF dependency requires an intentional review and explicit addition.

## Risks / Trade-offs

- [Risk] Text inspection can miss semantic dependencies. -> Pair checks with project references and keep the rules narrow; compile/test remains the primary gate.
- [Risk] Generated files create false positives. -> Scan only tracked source/project/workflow paths and exclude `obj`, `artifacts`, and graph output.
- [Risk] Legitimate adapters become noisy. -> Use a short explicit allowlist with a test failure that names the unexpected file.
