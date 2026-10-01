# Spec Delta

## REMOVED Requirements

### Requirement: Theme scope

**Reason**: The requirement limited the dark theme to the launch window and mandated that the Settings window keep its light styling. The Settings window now receives its own dark theme through the `settings-theme` capability, so the scope limitation and its "Settings window unchanged" scenario no longer hold.

**Migration**: The Settings window's dark appearance is specified by the `settings-theme` capability; this capability continues to define the launch window's theme.
