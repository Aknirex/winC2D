# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade winC2D.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|

### Project upgrade details

#### winC2D.csproj modifications

Project properties changes:
  - Target framework should be changed from `net6.0-windows` to `net8.0-windows`
