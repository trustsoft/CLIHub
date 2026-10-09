# Agent Command Capability-Aware UI - Tasks

## Task Breakdown

### Task 1: Add Capability Properties to AgentItem
**Estimated:** 15 minutes  
**Files:** `src/CLIHub/ViewModels/AgentItem.cs`

**Description:**
Add three new boolean properties (`CanInit`, `CanUpdate`, `CanVersion`) that check for command presence in plugin.json, following the existing pattern of `CanLaunch` and `CanResume`.

**Steps:**
1. Open `src/CLIHub/ViewModels/AgentItem.cs`
2. Locate `CanResume` property (line ~73)
3. Add three new properties after `CanResume`:
   ```csharp
   /// <summary>
   ///   Whether the agent defines the init command.
   /// </summary>
   public bool CanInit => Plugin.Commands?.Get(AgentCommandKind.Init) != null;

   /// <summary>
   ///   Whether the agent defines the update command.
   /// </summary>
   public bool CanUpdate => Plugin.Commands?.Get(AgentCommandKind.Update) != null;

   /// <summary>
   ///   Whether the agent defines the version command.
   /// </summary>
   public bool CanVersion => Plugin.Commands?.Get(AgentCommandKind.Version) != null;
   ```
4. Build project to verify compilation
5. Run existing tests to ensure no regression

**Acceptance:**
- Properties compile without errors
- Properties match existing code style (XML docs, null-conditional operator)
- All existing tests still pass

---

### Task 2: Update WindowActionCoordinator Command Predicates
**Estimated:** 20 minutes  
**Files:** `src/CLIHub/ViewModels/WindowActionCoordinator.cs`

**Description:**
Change the `CanExecute` predicates for ResumeCommand, InitCommand, UpdateCommand, and VersionCommand to check agent capability instead of just checking if an agent is selected.

**Steps:**
1. Open `src/CLIHub/ViewModels/WindowActionCoordinator.cs`
2. Locate command construction in constructor (lines 59-70)
3. Update ResumeCommand predicate:
   ```csharp
   ResumeCommand = new RelayCommand(
       () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Resume),
       () => _getSelectedAgent()?.CanResume == true);
   ```
4. Update InitCommand predicate:
   ```csharp
   InitCommand = new RelayCommand(
       () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Init),
       () => _getSelectedAgent()?.CanInit == true);
   ```
5. Update UpdateCommand predicate:
   ```csharp
   UpdateCommand = new RelayCommand(
       () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Update),
       () => _getSelectedAgent()?.CanUpdate == true);
   ```
6. Update VersionCommand predicate:
   ```csharp
   VersionCommand = new RelayCommand(
       () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Version),
       () => _getSelectedAgent()?.CanVersion == true);
   ```
7. Leave LaunchCommand unchanged (keeps `HasSelectedAgent`)
8. Build project to verify compilation
9. Run existing tests

**Acceptance:**
- All four commands use capability-checking predicates
- LaunchCommand remains unchanged
- Code compiles without errors
- No test failures

---

### Task 3: Verify UpdateCommandStates Integration
**Estimated:** 10 minutes  
**Files:** `src/CLIHub/ViewModels/LaunchWindowViewModel.cs`

**Description:**
Verify that the LaunchWindowViewModel calls `UpdateCommandStates()` when the selected agent changes, ensuring WPF re-evaluates command predicates.

**Steps:**
1. Open `src/CLIHub/ViewModels/LaunchWindowViewModel.cs`
2. Locate `SelectedAgent` property setter
3. Verify it calls `UpdateCommandStates()` on value change
4. Verify `UpdateCommandStates()` method exists and calls `CommandManager.InvalidateRequerySuggested()`
5. If missing, add the call:
   ```csharp
   public AgentItem? SelectedAgent
   {
       get => _selectedAgent;
       set
       {
           if (SetProperty(ref _selectedAgent, value))
           {
               UpdateCommandStates();
           }
       }
   }
   ```
6. Build and test

**Acceptance:**
- `UpdateCommandStates()` is called on agent selection change
- Method invokes `CommandManager.InvalidateRequerySuggested()`
- No compilation errors

---

### Task 4: Create AgentItem Unit Tests
**Estimated:** 30 minutes  
**Files:** `tests/CLIHub.Tests/ViewModels/AgentItemTests.cs` (new file)

**Description:**
Create comprehensive unit tests for the three new capability properties to ensure they correctly detect command presence.

**Steps:**
1. Create new file `tests/CLIHub.Tests/ViewModels/AgentItemTests.cs`
2. Add test class structure:
   ```csharp
   namespace CLIHub.Tests.ViewModels;

   using CLIHub.Core.Models;
   using CLIHub.ViewModels;
   using Xunit;

   public sealed class AgentItemTests
   {
       // Tests here
   }
   ```
3. Add helper method to create test plugins:
   ```csharp
   private static Plugin CreatePluginWithCommands(params AgentCommandKind[] kinds)
   {
       var commands = new AgentCommands();
       foreach (var kind in kinds)
       {
           commands.Add(kind, new AgentCommand 
           { 
               Kind = kind, 
               CommandLine = "test" 
           });
       }
       return new Plugin 
       { 
           Id = "test", 
           Name = "Test", 
           Commands = commands 
       };
   }
   ```
4. Add tests for CanInit:
   - `CanInit_ReturnsTrueWhenInitCommandExists`
   - `CanInit_ReturnsFalseWhenInitCommandMissing`
   - `CanInit_ReturnsFalseWhenCommandsNull`
5. Add tests for CanUpdate:
   - `CanUpdate_ReturnsTrueWhenUpdateCommandExists`
   - `CanUpdate_ReturnsFalseWhenUpdateCommandMissing`
   - `CanUpdate_ReturnsFalseWhenCommandsNull`
6. Add tests for CanVersion:
   - `CanVersion_ReturnsTrueWhenVersionCommandExists`
   - `CanVersion_ReturnsFalseWhenVersionCommandMissing`
   - `CanVersion_ReturnsFalseWhenCommandsNull`
7. Run tests: `dotnet test --filter "FullyQualifiedName~AgentItemTests"`
8. Verify all tests pass

**Acceptance:**
- 9 new tests created
- All tests pass
- Tests cover positive cases, negative cases, and null handling
- Test code follows project conventions

---

### Task 5: Create WindowActionCoordinator Unit Tests
**Estimated:** 45 minutes  
**Files:** `tests/CLIHub.Tests/ViewModels/WindowActionCoordinatorTests.cs` (new file)

**Description:**
Create unit tests for command CanExecute behavior to verify predicates correctly check agent capabilities.

**Steps:**
1. Create new file `tests/CLIHub.Tests/ViewModels/WindowActionCoordinatorTests.cs`
2. Add test class with setup:
   ```csharp
   namespace CLIHub.Tests.ViewModels;

   using CLIHub.Core.Models;
   using CLIHub.ViewModels;
   using Xunit;

   public sealed class WindowActionCoordinatorTests
   {
       // Helper to create coordinator with controlled agent selection
       private WindowActionCoordinator CreateCoordinator(
           Func<AgentItem?> getSelectedAgent = null,
           Func<Project?> getCurrentProject = null)
       {
           // Create minimal coordinator with test doubles
       }
   }
   ```
3. Add tests for InitCommand:
   - `InitCommand_CanExecute_ReturnsFalseWhenNoAgentSelected`
   - `InitCommand_CanExecute_ReturnsFalseWhenAgentCannotInit`
   - `InitCommand_CanExecute_ReturnsTrueWhenAgentCanInit`
4. Add tests for UpdateCommand:
   - `UpdateCommand_CanExecute_ReturnsFalseWhenNoAgentSelected`
   - `UpdateCommand_CanExecute_ReturnsFalseWhenAgentCannotUpdate`
   - `UpdateCommand_CanExecute_ReturnsTrueWhenAgentCanUpdate`
5. Add tests for VersionCommand:
   - `VersionCommand_CanExecute_ReturnsFalseWhenNoAgentSelected`
   - `VersionCommand_CanExecute_ReturnsFalseWhenAgentCannotShowVersion`
   - `VersionCommand_CanExecute_ReturnsTrueWhenAgentCanShowVersion`
6. Add tests for ResumeCommand:
   - `ResumeCommand_CanExecute_ReturnsFalseWhenNoAgentSelected`
   - `ResumeCommand_CanExecute_ReturnsFalseWhenAgentCannotResume`
   - `ResumeCommand_CanExecute_ReturnsTrueWhenAgentCanResume`
7. Add test for LaunchCommand (unchanged behavior):
   - `LaunchCommand_CanExecute_ReturnsTrueWhenAgentSelected`
8. Run tests: `dotnet test --filter "FullyQualifiedName~WindowActionCoordinatorTests"`
9. Verify all tests pass

**Acceptance:**
- 13 new tests created
- All tests pass
- Tests verify both capability presence and absence
- Test doubles properly isolate coordinator logic

---

### Task 6: Manual Testing - Single Agent Verification
**Estimated:** 20 minutes  
**Files:** None (manual testing)

**Description:**
Manually test command availability for each agent type to verify correct behavior in the UI.

**Steps:**
1. Launch CLIHub application
2. Open a project in the Projects pane
3. **Test github-copilot:**
   - Select agent
   - Open Actions menu
   - Verify: Launch ✓, Version ✓, Update ✓, Init ✓, Resume ✗
   - Try clicking disabled Resume → should be grayed out and unclickable
4. **Test openclaude:**
   - Select agent
   - Open Actions menu
   - Verify: Launch ✓, Resume ✓, Version ✓, Update ✗, Init ✗
5. **Test opencode:**
   - Select agent
   - Open Actions menu
   - Verify: Launch ✓, Resume ✓, Version ✓, Update ✓, Init ✗
6. **Test cline-cli:**
   - Select agent
   - Open Actions menu
   - Verify: Launch ✓, Version ✓, Resume ✗, Update ✗, Init ✗
7. **Test qwen-code:**
   - Select agent
   - Open Actions menu
   - Verify: Launch ✓, Version ✓, Update ✓, Resume ✗, Init ✗
8. Document any discrepancies

**Acceptance:**
- All agents show correct command availability
- Disabled commands are visually distinct (grayed out)
- No exceptions or errors occur

---

### Task 7: Manual Testing - Selection Change Responsiveness
**Estimated:** 10 minutes  
**Files:** None (manual testing)

**Description:**
Test that command states update immediately when switching between agents.

**Steps:**
1. Launch CLIHub
2. Select openclaude (has Resume, no Init)
3. Open Actions menu → verify Resume enabled, Init disabled
4. **Without closing menu**, select github-copilot (no Resume, has Init)
5. Verify menu updates: Resume disables, Init enables
6. Close and reopen menu → verify same states
7. Rapidly switch between 3-4 agents
8. Open menu after each switch → verify correct states
9. Measure subjective responsiveness (should feel instant)

**Acceptance:**
- Commands enable/disable immediately on selection change
- No visual lag or flicker
- Correct states after rapid switching
- No exceptions

---

### Task 8: Manual Testing - Edge Cases
**Estimated:** 15 minutes  
**Files:** None (manual testing)

**Description:**
Test edge cases like no selection, empty project, and rapid interactions.

**Steps:**
1. **No agent selected:**
   - Deselect all agents (click empty space)
   - Open Actions menu
   - Verify all agent commands disabled
2. **No project selected:**
   - Close project
   - Select agent
   - Open Actions menu
   - Verify commands reflect agent capabilities
3. **Empty project (no plugin.json files):**
   - Create test project with no .ai folders
   - Verify UI handles gracefully
4. **Rapid menu open/close:**
   - Select agent
   - Open/close Actions menu 10 times rapidly
   - Verify no exceptions or freezing
5. **Filter toggle with menu open:**
   - Open menu
   - Toggle "Only agents available in project"
   - Verify menu remains functional

**Acceptance:**
- No crashes or exceptions
- Graceful handling of null/empty states
- UI remains responsive under rapid interaction

---

### Task 9: Update Documentation
**Estimated:** 10 minutes  
**Files:** `docs/improvements.md`

**Description:**
Mark Phase 13 (Agent Subsystem Enhancement) as completed in improvements.md.

**Steps:**
1. Open `docs/improvements.md`
2. Locate Phase 13 entry
3. Update status from "Pending" to "✅ Completed"
4. Add completion date
5. Add note about what was implemented:
   ```markdown
   ## Phase 13: Agent Subsystem Enhancement ✅ Completed 2024-01-XX
   
   **Implemented:**
   - Capability-aware UI for agent commands
   - Dynamic enable/disable of menu items based on plugin.json
   - AgentItem properties: CanInit, CanUpdate, CanVersion
   - Updated WindowActionCoordinator command predicates
   
   **Remaining (future):**
   - Plugin metadata extension
   - Command output parsing
   - Agent health monitoring
   ```
6. Commit changes

**Acceptance:**
- improvements.md accurately reflects completed work
- Clear distinction between implemented and future work
- Documentation is up to date

---

### Task 10: Create Commit and Archive Change
**Estimated:** 10 minutes  
**Files:** Git repository

**Description:**
Commit all changes with appropriate commit messages and archive the OpenSpec change.

**Steps:**
1. Review all changes: `git status`
2. Stage production code:
   ```bash
   git add src/CLIHub/ViewModels/AgentItem.cs
   git add src/CLIHub/ViewModels/WindowActionCoordinator.cs
   git add src/CLIHub/ViewModels/LaunchWindowViewModel.cs  # if changed
   ```
3. Stage tests:
   ```bash
   git add tests/CLIHub.Tests/ViewModels/AgentItemTests.cs
   git add tests/CLIHub.Tests/ViewModels/WindowActionCoordinatorTests.cs
   ```
4. Create production commit:
   ```bash
   git commit -m "feat(ui): add capability-aware agent command menu

   - Add CanInit, CanUpdate, CanVersion properties to AgentItem
   - Update WindowActionCoordinator command predicates to check capabilities
   - Commands now enable/disable based on plugin.json support
   - Add comprehensive unit tests for capability detection

   Closes Phase 13 (Agent Subsystem Enhancement - capability UI)"
   ```
5. Stage documentation:
   ```bash
   git add docs/improvements.md
   ```
6. Create docs commit:
   ```bash
   git commit -m "docs: mark Phase 13 as completed"
   ```
7. Archive OpenSpec change:
   ```bash
   openspec archive agent-command-capability-aware-ui
   ```
8. Push commits

**Acceptance:**
- Two commits created (production + docs)
- Commit messages follow conventional commits format
- OpenSpec change successfully archived
- All changes pushed to remote

---

## Summary

**Total Estimated Time:** 3-4 hours

**Task Order:**
1. Add capability properties (15 min)
2. Update command predicates (20 min)
3. Verify UpdateCommandStates (10 min)
4. Build and verify compilation (5 min)
5. Create AgentItem tests (30 min)
6. Create WindowActionCoordinator tests (45 min)
7. Manual testing - agents (20 min)
8. Manual testing - selection (10 min)
9. Manual testing - edge cases (15 min)
10. Update documentation (10 min)
11. Commit and archive (10 min)

**Dependencies:**
- Tasks 1-3 must complete before Task 4-5 (tests depend on implementation)
- Tasks 1-5 must complete before Task 6-8 (manual testing requires working code)
- All tasks must complete before Task 9-10 (docs and commit)

**Parallel Opportunities:**
- Tasks 4 and 5 (unit tests) can be written in parallel by different developers
- Task 3 (verification) can overlap with Task 1-2 if separate developer

**Risk Mitigation:**
- Frequent builds after each task catch issues early
- Unit tests before manual testing ensure basic functionality
- Manual testing catches UI issues unit tests might miss
