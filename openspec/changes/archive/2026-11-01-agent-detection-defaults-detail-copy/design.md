# Design: agent-detection-defaults-detail-copy

## Context

One-string copy change to the DEFAULTS summary card's detail line on Settings →
Agent Detection, plus its tooltip and the two durable specs that pin the old wording.
No behavior, binding, layout, or control change.

## Verified current state

- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:368-377`:
  ```csharp
  /// <summary>
  ///   The built-in default detection policy shown in the DEFAULTS summary card, e.g.
  ///   "15 min · 10 sec".
  /// </summary>
  public string ProbePolicyDefaults => $"{this.ProbeTtlWatermark} min · {this.ProbeTimeoutWatermark} sec";

  /// <summary>
  ///   The DEFAULTS summary-card detail: when the built-in values apply.
  /// </summary>
  public string ProbePolicyDefaultsDetail => "Used when a field is empty";
  ```
  `ProbePolicyDefaults` (the value line) is untouched by this change.
  `ProbePolicyDefaultsDetail` (the detail line) and its doc comment are the edit target.

- `src/CLIHub/Views/SettingsWindow.xaml:267-271` — the DEFAULTS `controls:SummaryCard`:
  ```xml
  <controls:SummaryCard Detail="{Binding ProbePolicyDefaultsDetail}"
                        DetailToolTip="Applied when a detection field is empty"
                        Icon="{x:Static themes:IconGlyphs.Diagnostic}"
                        Label="DEFAULTS"
                        Value="{Binding ProbePolicyDefaults}"/>
  ```
  `Detail` is bound (edited in the VM, not here). `DetailToolTip` is a literal that
  currently restates the old detail's "when" framing — it needs to change so it no
  longer contradicts the new detail line. `Icon`, `Label`, and `Value` are untouched.

- `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:897-905`:
  ```csharp
  [Fact]
  public void ResetProbePolicy_ClearsBothFields()
  {
      var fixture = new SettingsViewModelFixture();
      fixture.ViewModel.Load();

      Assert.Equal("15 min · 10 sec", fixture.ViewModel.ProbePolicyDefaults);
      Assert.Equal("Used when a field is empty", fixture.ViewModel.ProbePolicyDefaultsDetail);
      Assert.False(fixture.ViewModel.ResetProbePolicyCommand.CanExecute(null));
      ...
  ```
  Line 903 (`ProbePolicyDefaults`) stays `"15 min · 10 sec"` — untouched. Line 904 is
  the assertion to update.

- Middle-dot convention precedent:
  `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:661-668`:
  ```csharp
  this.WatcherDetailText = current.State switch
  {
      ...
      _ => current.LastScanUtc is { } last
          ? $"Last scan {last.ToLocalTime():HH:mm:ss}{this.ScannedCoverageSuffix}"
          : "Not scanned yet"
  };
  ```
  where `ScannedCoverageSuffix` (`:317`) yields strings like `" · 27 folders"`,
  producing `Last scan 19:54:57 · 27 folders` on the DAEMON card
  (`SettingsWindow.xaml:196-199`, `Label="DAEMON"`). This is a *value* `·` *value*
  shape (timestamp · folder count), not label · label, but it establishes the repo's
  `·` (U+00B7) separator with a single space on each side as the house style for
  joining two short facts on one detail line. The new `ProbePolicyDefaultsDetail`
  literal reuses that separator to join two *labels* instead (`Cache TTL · Checking
  timeout`), which is a new but consistent use of the same convention — there is no
  prior "label · label" detail line elsewhere in this codebase to cite verbatim, so
  this is treated as an extension of the convention, not a literal precedent match.

- Durable spec text quoting the old copy, both under requirements that otherwise stay
  untouched:
  - `openspec/specs/settings-theme/spec.md:453-502`, requirement
    `Agent Detection page summary cards`; the target scenario is at `:499-502`:
    ```
    #### Scenario: Defaults card detail

    - **WHEN** the defaults card is visible
    - **THEN** the detail line shows "Used when a field is empty" or equivalent
    ```
  - `openspec/specs/preferences-ui/spec.md:695-786`, requirement
    `Agent Detection page content`; the target scenario is at `:752-755`:
    ```
    #### Scenario: DEFAULTS card detail

    - **WHEN** the DEFAULTS card is visible
    - **THEN** the detail line shows "Used when a field is empty" or equivalent
    ```
  Both scenarios use the hedge "or equivalent", so the exact new literal
  (`Cache TTL · Checking timeout`) is itself a valid instance of "equivalent" wording
  without even needing a MODIFIED requirement strictly — but since the user wants the
  exact string pinned and both specs currently anchor on the *old* literal as their
  illustrative example, this change still updates the THEN text so the spec's example
  matches the real string, rather than leaving a stale example next to a relaxed hedge.

- **Requirement-prose contradiction found and fixed (this revision).** The
  `settings-theme` requirement sentence itself (`openspec/specs/settings-theme/spec.md:455`,
  reproduced verbatim in this change's delta at `specs/settings-theme/spec.md:5`) ends
  "...and SHALL state that these values are used when a field is empty." — the *old*
  detail copy's own "when" framing, stated as a requirement, not just illustrated by a
  scenario. Once the `Defaults card detail` scenario's THEN changes to the new "what"
  framing (`Cache TTL · Checking timeout`), that trailing clause of the requirement
  sentence contradicts its own scenario and would also contradict the shipped behavior
  after implementation. The `preferences-ui` requirement sentence
  (`openspec/specs/preferences-ui/spec.md:697`) has no equivalent clause — it says only
  "...and the built-in default values," with no "when a field is empty" wording — so
  it was re-scanned and found not to need a change.

## Decisions

- **D1 — New detail literal: `Cache TTL · Checking timeout`.** Matches the brief's
  required exact string. "Checking timeout" (not "Check timeout" or "Version check
  timeout") mirrors the field's own label used elsewhere on the page
  (`preferences-ui:714`: `"Version check timeout" field`) closely enough while staying
  short; the brief fixes this exact wording, so no alternative was explored for this
  piece.
- **D2 — `DetailToolTip` becomes `Built-in defaults for cache TTL and version check timeout`.**
  The old tooltip (`"Applied when a detection field is empty"`) restates the *old*
  detail's "when" framing and would now contradict the new detail line, which states
  *what* (field names), not *when*. The replacement names both fields, stays a single
  short sentence (matching the sibling CACHE TTL/TIMEOUT card tooltips at
  `SettingsWindow.xaml:256,262`, which are also one-sentence literals), and reuses
  "version check timeout" — the field's full name from the `preferences-ui` spec
  (`:714`) — rather than the shortened "Checking timeout" used in the detail line
  itself, since the tooltip has room to be precise where the card's narrow detail line
  does not.
- **D3 — Alternatives considered and rejected:**
  - *Keep the old copy.* Rejected — it is the brief's whole purpose; the generic hint
    doesn't say what the two numbers on the value line are for.
  - *Label+value form, e.g. `Cache TTL 15 min · Checking timeout 10 sec`.* Rejected —
    the value line directly above already shows `15 min · 10 sec`; repeating the
    numbers on the detail line is redundant and the DEFAULTS card is one of three
    equal-width cards in a `StretchChildren="True"` horizontal `SpacedPanel`
    (`SettingsWindow.xaml:254`), so a long detail string is more likely to wrap or
    truncate than the two sibling cards' shorter details (`"Custom (default: N
    min)"`-style, per `settings-theme:479-482`).
  - *Leave `DetailToolTip` unchanged.* Rejected — leaving `"Applied when a detection
    field is empty"` next to a detail line that no longer talks about "when" would
    read as inconsistent on hover; the brief's own rationale (card clarity) extends to
    the tooltip.
- **D4 — Both specs get a MODIFIED requirement, not left alone under the "or
  equivalent" hedge.** The hedge technically tolerates the new string without a spec
  edit, but both scenarios currently show the *old* literal as their worked example;
  updating the THEN keeps the spec's own example synchronized with the shipped string,
  consistent with how `2026-10-26-settings-tooltip-copy` treated similar literal/hedge
  text (it still edited exact-value scenarios rather than relying on hedges). Only the
  THEN line of each named scenario changes in `preferences-ui`; in `settings-theme` the
  THEN line changes **and** the requirement sentence's trailing clause changes (D4a,
  below) — every other scenario and all scenario names in both blocks stay
  byte-identical.
- **D4a — `settings-theme` requirement sentence: one trailing clause rewritten, not
  just the scenario.** Old: "...and SHALL state that these values are used when a
  field is empty." New: "...and SHALL name the two detection parameters those default
  values apply to." This is a deliberate, scoped edit to the requirement prose itself
  — not a MODIFIED-block authoring mistake — made because leaving the old clause in
  place would pin a requirement that contradicts its own `Defaults card detail`
  scenario and the shipped detail string. Everything else in the sentence (the cache
  TTL and timeout cards' behavior, the "N min · M sec" format clause, the Diagnostic
  icon clause, the live-refresh clause) is byte-identical to
  `openspec/specs/settings-theme/spec.md:455`. The `preferences-ui` requirement
  sentence (`openspec/specs/preferences-ui/spec.md:697`) carries no equivalent "when a
  field is empty" clause, so it needed no change — confirmed by re-reading it against
  the new detail copy.
- **D4b — The `or equivalent` hedge is dropped from both scenarios deliberately.** The
  main specs read `...shows "Used when a field is empty" or equivalent`; the deltas pin
  the exact literal `"Cache TTL · Checking timeout"` with no hedge. This is intended:
  the user request names the target copy exactly, so the scenario becomes an exact-value
  pin and a future copy change must edit the spec (as this change does) instead of
  sliding under a loose hedge. Recorded here because the hedge removal tightens the
  contract and should be visible to whoever reviews the archive diff.
- **D5 — No control, layout, or binding change.** `Detail="{Binding
  ProbePolicyDefaultsDetail}"` already binds to the VM property being edited; no XAML
  structural change is needed beyond the one `DetailToolTip` literal.

## Deviations from the brief

None identified. The brief's example tooltip wording
(`Built-in defaults for cache TTL and version check timeout`) was checked against the
card and adopted verbatim (D2) — it does not contradict the new detail line or the
value line.

This revision (requirement-prose fix) matches its own follow-up brief exactly: only the
`settings-theme` requirement sentence's trailing clause changed (D4a), the
`preferences-ui` requirement sentence needed no change after re-scanning (confirmed, no
deviation), and no scenario name changed in either delta.

## Open questions

None.
