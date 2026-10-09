# CLIHub Vision

## Product

CLIHub is a Windows system-tray companion that reduces the friction of launching multiple AI coding-agent
CLIs in the correct project directory.

## Goals

- Make project-aware agent launch a quick, repeatable action.
- Keep several agent CLIs available through one small Windows-native surface.
- Preserve the user's existing terminal, agent installation, and project files.
- Provide enough availability, version, update, and release-note state to make repeated use predictable.

## Non-goals

- Replacing or embedding the agent CLIs.
- Providing AI capabilities, an IDE, or an in-app terminal.
- Installing or managing agent tools on the user's behalf.
- Supporting macOS or Linux.

## Current Direction

The shipped behavior is defined by the specifications under [`openspec/specs/`](../openspec/specs/), not by
a duplicated feature inventory in this document. User-facing behavior is summarized in [`README.md`](../README.md).

Remaining product directions are deliberately small and exploratory:

- richer per-agent project filtering and configuration;
- notifications for background agent activity;
- graceful handling of running agents during operations that currently require all matching processes to be closed.
