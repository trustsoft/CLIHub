# Design: native argument passing in the PowerShell runtime

## Context

The runtime script is built once from the plugin descriptor and encoded with
`-EncodedCommand`. Its argument tail is passed with the stop-parsing token `--%` so PowerShell
does not reinterpret descriptor-provided arguments (for example `$`, backticks, or redirection
characters). Empirically (PowerShell 5.1, `powershell.exe -EncodedCommand`):

| Invocation form | Agent-received arguments |
|---|---|
| `& 'where.exe' --% /r C:\Windows notepad.exe` | `/r C:\Windows notepad.exe` (token honored: native exe) |
| `& 'opencode' --% --continue` | `--% --continue` (token leaked: name resolves to `opencode.ps1`, an ExternalScript) |
| `$exe = (Get-Command 'opencode' -CommandType Application …).Source; & $exe --% --continue` | `--continue` (token honored: resolution selects `opencode.cmd`) |
| `& 'opencode'` (no arguments) | runs the shim as-is (TUI starts) |

## Decision

`QuotePowerShellExecutable` chooses the invocation form by shape:

1. **No arguments** — `& '<name>'`. The shim executes directly; nothing to pass through.
2. **Arguments + bare PATH name** — resolve to a native application first, then invoke it:
   ```powershell
   $exe = (Get-Command '<name>' -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
   & $exe --% <args>
   ```
   `-CommandType Application` skips ExternalScript shims, so the stop-parsing token is honored
   and the agent receives only the intended arguments.
3. **Arguments + executable path** — direct `& '<path>' --% <args>` when the path ends in a
   native extension (`.exe`/`.com`); otherwise the same resolution form with a wildcard-escaped
   literal path. A `.ps1` path with arguments fails loudly at resolution instead of silently
   leaking the token — script shims do not support `--%` by design.

The `--%` semantics are unchanged: the descriptor tail reaches the agent verbatim, shielded from
PowerShell interpretation.

## Alternatives considered

- Drop `--%` and let PowerShell parse the tail — rejected: descriptor strings containing `$`,
  backticks, or statement separators would be silently reinterpreted.
- Always delegate to `cmd.exe /d /c` from the PowerShell script — rejected: PowerShell-to-cmd
  argument requoting reintroduces exactly the quoting hazards the PowerShell runtime avoids.
- Parse the descriptor tail into PowerShell-quoted tokens — rejected: reliable raw-string token
  parsing duplicates the shell and risks new escaping bugs.

## Verification

- Unit: exact encoded-script shapes for the three invocation forms.
- Real process: a `probe-agent.cmd` shim placed on PATH, launched through the builder output;
  the shim reports the arguments it received; the report must contain only the intended flag.
- Live UI: resume for the OpenCode plugin starts the TUI (opencode log shows `args=["--continue"]`).
