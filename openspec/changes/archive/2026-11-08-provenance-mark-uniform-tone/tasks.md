# Tasks: provenance-mark-uniform-tone

## 1. Remove the DataTrigger from ProjectProvenanceMark

- [x] 1.1 In `src/CLIHub/Themes/Common/Controls.xaml`, replace the comment block and style at lines 62–76
  with the flat version below. The change removes lines 71–75 (`Style.Triggers` / `DataTrigger` block)
  and rewrites the comment to reflect the new single-tone rule.

  **Remove (lines 62–76 verbatim):**
  ```xaml
      <!-- Project row provenance mark: the compact "M"/"D" source indicator shared by the Settings
           Projects page and the launch window project list. Manual projects (CanRemove is true exactly
           for them) use the strong primary foreground so the mark stays readable on light surfaces;
           discovered projects keep the muted tone. The character remains the primary distinction. -->
      <Style x:Key="ProjectProvenanceMark" TargetType="TextBlock">
          <Setter Property="FontSize" Value="{StaticResource Sizing.FontSize.Section}"/>
          <Setter Property="FontWeight" Value="SemiBold"/>
          <Setter Property="VerticalAlignment" Value="Center"/>
          <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
          <Style.Triggers>
              <DataTrigger Binding="{Binding CanRemove}" Value="True">
                  <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}"/>
              </DataTrigger>
          </Style.Triggers>
      </Style>
  ```

  **Insert in its place:**
  ```xaml
      <!-- Project row provenance mark: the compact "M"/"D" source indicator shared by the Settings
           Projects page and the launch window project list. Both marks use TextMutedBrush; the
           character is the only distinction between manual and discovered projects. -->
      <Style x:Key="ProjectProvenanceMark" TargetType="TextBlock">
          <Setter Property="FontSize" Value="{StaticResource Sizing.FontSize.Section}"/>
          <Setter Property="FontWeight" Value="SemiBold"/>
          <Setter Property="VerticalAlignment" Value="Center"/>
          <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
      </Style>
  ```

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## 2. Update the architecture documentation

- [x] 2.1 In `docs/architecture/ui.md`, replace the rail-alignment paragraph at lines 114–121.
  Lines 119–121 were already updated by the initial implementation (uniform-tone task). Lines 114–118
  still describe right-alignment and the 12 DIP symmetry claim; update them to describe centering.
  Re-wrap the full paragraph to ≤100 columns.

  **Find (lines 114–121 verbatim, current on-disk state after task 1 implementation):**
  ```
  The launch-window project row uses a fixed-width right marker rail inside the text column: the favorite
  star occupies the name row and the provenance mark the path row. Both rail markers right-align to the
  rail's trailing edge, so the project row's leading and trailing content insets are symmetric (12 DIP
  each), and the rail stays fixed-width so hiding the star does not shift the path text. The name and path
  share the `*` text column, and the path display converter measures the name text's width so the rail
  never eats into the trimmed path. The `ProjectProvenanceMark` style lives in `Controls.xaml` (both marks use `TextMutedBrush`;
  the character is the only distinction), so the Settings registry and the launch window render
  the same mark; the mark's tooltip and accessible name carry the full `Manual`/`Discovered` label.
  ```

  **Replace with (re-wrapped to ≤100 columns, centering rule):**
  ```
  The launch-window project row uses a fixed-width right marker rail inside the text column: the
  favorite star occupies the name row and the provenance mark the path row. Both rail markers are
  centered within the fixed-width rail column so their horizontal midpoints coincide, and the
  rail stays fixed-width so hiding the star does not shift the path text. The name and path share
  the `*` text column, and the path display converter measures the name text's width so the rail
  never eats into the trimmed path. The `ProjectProvenanceMark` style lives in `Controls.xaml`
  (both marks use `TextMutedBrush`; the character is the only distinction), so the Settings registry
  and the launch window render the same mark; the mark's tooltip and accessible name carry the full
  `Manual`/`Discovered` label.
  ```

Gates:

```powershell
openspec validate --all
```

## 3. Add CHANGELOG entry

- [x] 3.1 In `CHANGELOG.md`, add the following line at the end of the `### Changed` list under
  `## Unreleased` (after the existing last bullet in that section):

  ```
  - The manual ("M") provenance mark uses the same muted foreground as the discovered ("D") mark in both the launch window and the Settings Projects page; the character remains the only distinction (`provenance-mark-uniform-tone`, `project-management`).
  ```

Gates:

```powershell
openspec validate --all
```

## 4. Validate the OpenSpec change

- [x] 4.1 Run `openspec validate --all` and confirm it reports 0 failed items. The `.openspec.yaml`
  for this change carries `skip_specs: true`, so no spec delta file is required.

Gates:

```powershell
openspec validate --all
```

## 5. Fix marker alignment in the launch window

- [x] 5.1 In `src/CLIHub/Views/LaunchWindow.xaml` line 245, change the star `TextBlock`'s
  `HorizontalAlignment` attribute from `"Right"` to `"Center"`.

  **Find (line 245 verbatim):**
  ```xaml
                                                       HorizontalAlignment="Right"
  ```
  *(this is the `HorizontalAlignment` attribute of the star `TextBlock` starting at line 243)*

  **Replace with:**
  ```xaml
                                                       HorizontalAlignment="Center"
  ```

- [x] 5.2 In `src/CLIHub/Views/LaunchWindow.xaml` line 254, change the provenance mark `TextBlock`'s
  `HorizontalAlignment` attribute from `"Right"` to `"Center"`.

  **Find (line 254 verbatim):**
  ```xaml
                                                       HorizontalAlignment="Right"
  ```
  *(this is the `HorizontalAlignment` attribute of the provenance mark `TextBlock` starting at line 252)*

  **Replace with:**
  ```xaml
                                                       HorizontalAlignment="Center"
  ```

- [x] 5.3 In `src/CLIHub/Views/LaunchWindow.xaml` lines 199–202, update the rail comment to describe
  centering instead of right-alignment.

  **Find (lines 199–202 verbatim):**
  ```xaml
                                                 <!-- Fixed right rail: the star sits on the name line and the
                                                      provenance mark on the path line, both right-aligned to the
                                                      rail's trailing edge, so the two markers never compete on
                                                      one line and hiding the star shifts nothing. -->
  ```

  **Replace with:**
  ```xaml
                                                 <!-- Fixed right rail: the star sits on the name line and the
                                                      provenance mark on the path line, both centered within the
                                                      rail column, so their horizontal midpoints coincide and
                                                      hiding the star shifts nothing. -->
  ```

- [x] 5.4 Close out the remaining stale alignment comments found during verification
  (`src/CLIHub/Views/LaunchWindow.xaml:173–175` "the markers right-align to the rail's trailing
  edge"; `src/CLIHub/Themes/Common/Sizing.xaml:94–96` "right-aligned marker rail";
  `Sizing.xaml:101–105` "both right-align … 12px trailing inset"). Each now describes centering
  within the fixed rail. Comment-only edits; every `Sizing` token value is unchanged. Rationale and
  the scope decision: `design.md` Decision 6.

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
```

## 6. Smoke test

- [x] 6.1 Run `dotnet run --project src/CLIHub/CLIHub.csproj`. For each of the three themes
  (Dark, Light, Blue), switch the theme via Settings → Theme & Accent, then:

  **Launch window — tone:**
  - Open the launch window.
  - Verify that a **manual project** ("M") renders in the same muted tone as a **discovered project**
    ("D") — not bright or high-contrast.
  - Verify both marks are legible (not invisible) against the window background.

  **Launch window — alignment:**
  - With both an "M" row and a "D" row visible, verify that the provenance mark (path line) is
    horizontally centered in the rail column and visually coaxial with the favorite star (★, name
    line) when both appear in the same row.
  - Verify the star and the mark do not appear offset left or right relative to each other.

  **Settings Projects page:**
  - Open Settings → Projects.
  - Verify the same uniform muted tone for both "M" and "D" marks.
  - Verify the tooltip shows "Manual" or "Discovered" respectively.
  - No alignment fix applies to this surface (it uses a `StackPanel`, not the fixed-width rail).

  Expected result in all six observations (3 themes × launch window): "M" and "D" are visually
  indistinguishable except for the character; neither is as bright as the project name; the mark
  and the star share the same vertical center axis in rows that show both.

## Workflow follow-up

- Run `graphify update .` after implementation is complete to refresh the knowledge graph.
- Archive the change only after all gates pass and the smoke test is confirmed by the user:
  `openspec archive provenance-mark-uniform-tone`.
- After archiving, run the post-archive integrity checks:
  - `git status --porcelain` — must be clean
  - `openspec list` — change must be absent
  - `openspec validate --all` — must report 0 errors
