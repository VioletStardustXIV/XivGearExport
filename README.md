# XivGearExport

A simple Dalamud plugin to export your in-game gearsets to xivgear.app.

## Supported functionality

Use `/xivgearexport` (or `/xge`) to export your gear set, and `/xivgearexportconfig` (or `/xgeconfig`, or the config button) to configure the plugin.

Supports:
- Export to edit mode
- Export to view only mode
- Optionally opening in browser
- Optionally putting the URL in chat
- Default sims in xivgear.app are included in the export
- Right-click menu export for character gearset list

Limitations:
- Will assume gearset is for level 100 character (or 80 for BLU), though this is easy to fix via Save As
- Does not export food
- Does not export relic stats for relic weapons with selectable stats