## 1. Resizable splitter

- [x] 1.1 In `src/CLIHub/Windows/MainWindow.xaml`, change the content grid columns to: Projects `Width="250" MinWidth="180"`, a new `Width="Auto"` splitter column, and Agents `Width="*" MinWidth="300"`; move the Agents `GroupBox` to the new column index. Verify with `dotnet build CLIHub.sln`.
- [x] 1.2 Add a `GridSplitter` in the splitter column (`Width="5"`, stretch alignment, `ResizeBehavior="PreviousAndNext"`) and remove the Projects pane's right margin. Verify by running the app and dragging the splitter: both panes resize and content is not overlapped.

## 2. Layout alignment

- [x] 2.1 Give both panes the same outer margin value on all sides, and adjust the Agents action `WrapPanel`/button margins so the row's bottom edge matches the Projects "Add Project..." button bottom edge. Verify with `dotnet build CLIHub.sln`.
- [x] 2.2 Run the app at the default 800×600 size and confirm the two bottom action rows share a bottom edge and both panes have uniform outer margins.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`, then launch the app and confirm: the splitter drags, minimum widths hold (neither pane collapses), and the layout is aligned per the `main-window-layout` spec scenarios.
