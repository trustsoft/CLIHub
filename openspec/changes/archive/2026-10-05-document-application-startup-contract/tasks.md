## 1. Document Current Lifecycle

- [x] 1.1 Add the observed process bootstrap, startup, and shutdown sequence to the architecture documentation and verify each step against `Program.cs`, `App.xaml.cs`, and service registration.
- [x] 1.2 Document startup failure handling and optional startup work, verifying each statement against current exception handling and preference checks.
- [x] 1.3 Link the lifecycle behavior to the existing `app-lifecycle` specification without duplicating its normative requirements.

## 2. Validate the Documentation Change

- [x] 2.1 Review the rendered Markdown structure and verify that the documented order matches the current implementation.
- [x] 2.2 Run `openspec validate document-application-startup-contract` and resolve any validation errors.
