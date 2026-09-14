# Tasks: add-logo-resolver

## 1. Core

- [x] 1.1 Implement `LogoResolver` with the unified chain
  (`logo.png` -> `logo.ico` -> `icon.png` -> `icon.ico` -> `favicon.ico`,
  flat, first hit wins, `null` when nothing found). Verify: unit tests —
  logo beats icon, png beats ico within a pair, favicon fallback, empty
  root.
- [x] 1.2 Add project override precedence to the resolver: stored `logo`
  wins when the file exists (relative resolved against the project
  folder, absolute as-is), otherwise silent fall-through to the chain.
  Verify: unit tests — absolute override, relative override, broken
  override falls back.
- [x] 1.3 Make `PluginLoader` folder-aware: results carry
  `(manifest, folder)`; update `AgentDetector`/`PopupViewModel` call
  sites. Verify: loader tests pass with folder assertions; full test run
  green.

## 2. App

- [x] 2.1 Add an image-loading service: per-path cached `BitmapImage`
  with bounded decode width and `OnLoad` caching; bundled default logo
  asset; decode failure falls back to the default. Verify: build and a
  smoke run — popup opens without exceptions on an empty-asset setup.
- [x] 2.2 Wire logos into popup item templates: Projects pane
  (logo + name + path) and Agents pane (logo + name + version); default
  logo when resolution returns nothing. Verify: manual checklist —
  project with `logo.png` shows it, agent plugin asset shows it, bare
  folders show the default.

## 3. Verification

- [x] 3.1 Run `dotnet test CLIHub.sln` and `dotnet build CLIHub.sln` —
  everything green, no new warnings.
- [x] 3.2 Manual end-to-end pass: add a project with a `logo.png` in its
  root, open the popup — logo in Projects, agents with/without plugin
  assets show logo/default respectively; switch projects — agent logos
  unaffected.
