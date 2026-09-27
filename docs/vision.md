# Vision: CLIHub

## Essence
A Windows system tray companion application that streamlines access to multiple AI agent CLI tools within project directories.

## Problem
Developers working with multiple AI agent CLI tools (such as OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code) need a convenient way to launch these tools in the context of their current project without manually navigating to project directories and typing commands. Windows 10/11 users lack a unified, accessible launcher for AI development tools that respects project context.

## Goals
- Provide quick, one-click access to AI agent CLI tools from the Windows system tray
- Maintain awareness of the current project directory context
- Reduce friction in switching between different AI agent tools
- Support multiple popular AI agent CLI tools in a single interface
- Integrate seamlessly with Windows 10/11 system tray conventions

## Non-goals
- Replace the CLI tools themselves
- Provide AI capabilities directly
- Support macOS or Linux (Windows-only focus)
- Install or manage the AI tools on the user's behalf (CLIHub can invoke a tool's own self-update command, but does not manage installations)
- Serve as a full IDE or code editor

## Audience
- Windows developers using AI-assisted coding tools
- Teams standardizing on multiple AI agent CLI workflows
- Solo developers juggling several AI coding assistants

## Direction & Status

The capability sketch below began as a draft. Delivered capabilities are tracked as durable specs under `openspec/specs/`; the rest remain directions.

**Delivered**
- Windows system tray icon with a context menu (current project, recent projects, add project, launch agent, exit) — `project-management`
- Project management with logos, recents, and a current-project launch context — `project-management`
- Dynamic agent plugins described by JSON descriptors, seeded on first run with logos — `plugin-seeding`
- Agent command set: launch, resume last session, version, update, initialize — `agent-commands`
- Agent availability detection (installed on host / used in project) — `agent-detection`
- Agent version display and availability dimming/filtering — `agent-version`, `agent-availability-display`
- Windows Terminal integration for spawning sessions — `agent-commands`
- Global keyboard shortcut (Ctrl+Shift+A) to toggle the window — `hotkey-support`
- File logging with configurable levels and rotation — `logging`
- Single-instance enforcement and dependency injection — `app-lifecycle`

**Remaining directions**
- A dedicated launch-window layout (split projects/agents pane per the UI mockup)
- Settings UI for preferences (terminal executable, log level, hotkey, filters)
- Application update checking and notification (Velopack)
- Notification support for background agent activities
- Per-agent project filtering refinements beyond dim/hide
