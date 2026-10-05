## MODIFIED Requirements

### Requirement: Explicit C# and SDK policy

The repository SHALL declare C# 14.0 as its explicit language version and SHALL declare a stable .NET 10 SDK baseline with controlled feature-band roll-forward and prerelease SDKs disabled. All production and test projects SHALL use this shared policy.

#### Scenario: Project compilation uses the declared language version
- **WHEN** a production or test project is compiled through the repository solution
- **THEN** the compiler uses C# 14.0 rather than an implicit or future SDK-selected language version

#### Scenario: SDK baseline is reproducible
- **WHEN** a developer or CI runner builds the solution with a compatible stable .NET SDK installed
- **THEN** SDK selection starts from the declared .NET 10 baseline and may roll forward only according to the repository's configured feature-band policy

#### Scenario: Prerelease SDK is unavailable by policy
- **WHEN** a prerelease .NET SDK is installed alongside stable SDKs
- **THEN** repository SDK resolution does not select the prerelease SDK for a normal build
