# plugin-installation Spec Delta: Remove Agent Origin Badge

## REMOVED Requirements

### Requirement: Plugin origin display

**Reason:** The origin badge has been removed from the agent card UI by this change. The recorded origin (Built-in, Installed, or Unknown) is no longer displayed as a separate chip on the agent card; instead, it appears as the first line of the agent name's tooltip, along with the descriptor's version, publisher, and homepage when declared.

**Migration:** The behavior has moved to the `agent-ui` capability's `Agent card displayed content` requirement. The agent name's tooltip now presents "Origin: Built-in", "Origin: Installed", or "Origin: Unknown" as a labelled line, followed by "Version: ...", "Publisher: ...", and "Homepage: ..." when the descriptor declares them. No separate badge is rendered.
