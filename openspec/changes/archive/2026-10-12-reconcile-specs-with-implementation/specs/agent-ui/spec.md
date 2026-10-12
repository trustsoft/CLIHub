## MODIFIED Requirements

### Requirement: REQ-AGENT-CAP-008: Dynamic Command State Updates

The system SHALL update command enabled/disabled states immediately when agent selection changes.

#### Scenario: Update Commands on Selection Change
```
GIVEN agent A is selected with Resume and Update enabled and Version disabled
WHEN the user selects agent B that declares Version but neither Resume nor Update
THEN Resume SHALL become disabled
AND Update SHALL become disabled
AND Version SHALL become enabled
AND the change SHALL occur within 50ms
```

#### Scenario: Maintain Correct States After Rapid Switching
```
GIVEN the user rapidly switches between multiple agents
WHEN the user settles on a final agent selection
THEN the menu commands SHALL reflect that agent's capabilities accurately
```
