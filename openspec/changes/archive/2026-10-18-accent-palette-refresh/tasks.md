## 1. Palette

- [x] 1.1 Add Teal, Indigo, and Tangerine to the accent presets and rename Windows Blue to Blue
- [x] 1.2 Keep Sky as the default and verify every preset is a normalized, unique color

## 2. Picker

- [x] 2.1 Replace the segmented accent selector with preview cards showing the dot, name, and hex
- [x] 2.2 Mark the selected card with the 1px accent ring and a check inside the dot
- [x] 2.3 Show the active accent name in the card badge
- [x] 2.4 Show the custom field only while the custom entry is selected

## 3. Custom memory

- [x] 3.1 Keep the last custom accent independent of the selected preset
- [x] 3.2 Restore it when Custom is selected, including after the Settings window is reopened

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`
- [x] 4.2 Check the picker visually at the default and minimum window widths
