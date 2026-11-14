# 0018. Capability-based service boundaries

## Status

Accepted — 2026-11-01 (recorded 2026-11-14). Implements Proposal 3 of the deleted 2026-10-29
architecture analysis. Implementing commit: `774d468` "split IAgentManagementService into probe and
update capabilities".

## Context

Proposal 3 observed that `IAgentManagementService` combined three unrelated capabilities — on-demand
probing, update availability, and update execution — behind one interface. A consumer that needed
only one of them had to depend on the whole surface, and a test that exercised one capability had to
stub all of them. The audit trail for the slice is recorded in `docs/backlog.md:44-46` and
`:49-53`, including the explicit decision that further decomposition waits for evidence.

The forces at play:

- The ViewModel is the only production consumer, and it needs both capabilities
  (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:41-42` holds one field per capability).
- The test builder wants to configure each capability independently
  (`tests/CLIHub.Tests/Builders/AgentManagementViewModelBuilder.cs:33-34`, materialized as separate
  mocks at `:265-266`), so a single combined interface would force every test to stub two methods to
  reach one.
- The update stack already had the counter-example: five small interfaces with one composite
  registration, which consumers depend on selectively.

## Decision

**Split interfaces by capability, not by role.**
`IAgentProbeService` owns probing (`src/CLIHub.Core/Agents/IAgentProbeService.cs:8-16`) and
`IAgentUpdateService` owns update execution (`src/CLIHub.Core/Agents/IAgentUpdateService.cs:8-17`).
`AgentManagementService` implements both
(`src/CLIHub.Core/Agents/AgentManagementService.cs:11`), one container registration backs both
interfaces (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:127-129`), and the
ViewModel takes the two interfaces separately
(`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:41-42`). The capability names describe
what a consumer gets, not which team or layer owns it.

**Keep one registration when consumers want the whole surface, via a composite interface.**
`IUpdateService` is the model:
`IUpdateService : IUpdateVersionProvider, IUpdateChecker, IUpdateStateSource, IUpdateDownloader,
IUpdateInstaller` (`src/CLIHub.Core/Updates/IUpdateService.cs:6-10`). The container registers the
implementation once and forwards each capability interface to the same singleton
(`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:168-173`). A consumer that needs the
whole surface depends on `IUpdateService`; one that needs a slice depends on the slice. Splitting
interfaces therefore does not multiply instances or lifetimes.

**Split further only with evidence.**
The rule is consumer evidence: a consumer or a test subset that uses part of an interface. Deeper
agent-domain splits stay deferred with no date, because no such consumer exists yet
(`docs/backlog.md:52-53`).

**Internal boundary only.**
The split changed no behavior, no OpenSpec scenario, and no user-visible contract; the backlog
records it as zero behavior change with the same test count (`docs/backlog.md:44-46`).

Rejected alternatives:

- *One large interface plus extension methods* — extension methods keep the wide dependency and add
  a second, less discoverable call surface; the consumer still cannot depend on the narrow slice.
- *Split every service preemptively* — decomposition without a partial-use consumer produces
  interfaces nobody implements separately and tests nobody simplifies; the composite pattern above
  already covers the honest case.
- *A facade over the old services* — a facade adds a delegation hop without removing the wide
  dependency, so the test-stubbing problem survives it.

## Consequences

**Easier:** a test configures only the capability under test; a consumer's dependency states what it
uses; the composite registration means the split is invisible in object lifetimes.

**Harder:** there are now more interface names for the same service, so a reader must know that
`AgentManagementService` is one object behind two interfaces and that `IUpdateService` is a
composite; finding every consumer of, say, update execution means searching for
`IAgentUpdateService`, not for a class name.

**Committed to:** capability interfaces may be composed but not folded back into a wide interface
without a new ADR; a split is only justified by demonstrated partial use.

## Reopen triggers

When two capabilities always change together — every edit touches both, and no consumer or test ever
uses one without the other — merging them back into one interface is the honest move, and the
merger gets its own record. The same applies in reverse to the three-capability `IUpdateService`
composite if one of its five slices turns out to have a single, always-combined consumer.
