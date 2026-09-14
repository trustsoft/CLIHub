# Tasks: add-app-update

## 1. Core contracts and config

- [x] 1.1 Add the `IUpdateClient` abstraction (`IsInstalled`,
  `CheckAsync`, `DownloadAsync`, `ApplyAndRestart`) and the small result
  model it needs. Verify: solution builds.
- [x] 1.2 Add the `update` config section (`UpdateConfig.CheckOnStartup`,
  effective default true) to `Config`. Verify: ConfigStore tests — section
  round-trips; missing section and missing key both yield true.

## 2. UpdateService (Core)

- [x] 2.1 Implement the startup check: skip silently when the build is not
  installed or `checkOnStartup` is disabled; otherwise ask the client and
  return whether an update is available. Verify: unit tests with a fake
  client — disabled, not installed, no update, update available.
- [x] 2.2 Implement download and readiness: download an available update,
  raise an `UpdateReady` event only on success, and swallow check/download
  failures. Verify: unit tests — success raises once; failure raises
  nothing and does not throw.
- [x] 2.3 Implement apply: delegate to the client's apply-and-restart.
  Verify: unit test — the fake client records the apply call.

## 3. App integration

- [x] 3.1 Add the Velopack package and a `VelopackUpdateClient` adapter
  (`IsInstalled`, check, download, apply-and-restart) holding the pending
  update between calls. Verify: build succeeds with the package.
- [x] 3.2 Add the Velopack bootstrap as a custom `Main` (`App.xaml` build
  action `Page` + `StartupObject`), before WPF starts. Verify: `dotnet run`
  still starts the tray app normally.
- [x] 3.3 Wire startup: run the check fire-and-forget when enabled, marshal
  `UpdateReady` to the UI thread, show a tray notification, and apply the
  update when the notification is acted on. Verify: manual run confirms no
  popup/dialog and a normal tray startup; apply path checked in 4.2.

## 4. Verification

- [x] 4.1 Run `dotnet test CLIHub.sln` and `dotnet build CLIHub.sln` —
  everything green, no new warnings.
- [x] 4.2 Manual end-to-end: build a local release with `vpk pack`, install
  it, point the feed at a local folder containing a newer release, and
  verify check -> download -> tray notification -> apply-and-restart.
  Also verify `dotnet run` performs no check and shows no error.
