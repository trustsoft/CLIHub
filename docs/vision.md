# Vision: CLIHub

## Essence
A Windows system tray companion application that streamlines access to multiple AI agent CLI tools within project directories.

## Problem
Developers working with multiple AI agent CLI tools (like OpenCode, Aider, Cursor, etc.) need a convenient way to launch these tools in the context of their current project without manually navigating to project directories and typing commands. Windows 10/11 users lack a unified, accessible launcher for AI development tools that respects project context.

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
- Manage AI tool installation or updates
- Serve as a full IDE or code editor

## Audience
- Windows developers using AI-assisted coding tools
- Teams standardizing on multiple AI agent CLI workflows
- Solo developers juggling several AI coding assistants

## Directions (draft — subject to review)
- System tray icon with context menu showing available AI agent CLIs
- Project directory detection and management (recent projects, favorites)
- Quick launch for configured AI tools in selected project context
- Configuration UI for adding/removing AI CLI tools and their launch parameters
- Windows Terminal integration for spawning CLI sessions
- Keyboard shortcuts for power users
- Notification support for background AI agent activities
- Settings persistence across sessions
