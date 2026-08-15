# Ribbon Definition

`RibbonDefinition` owns ordered immutable tabs; each `RibbonTabDefinition` owns groups; each `RibbonGroupDefinition` owns commands. Labels are `LocalizationKey` values and images are semantic `IconKey` values. Stable technical IDs/codes are never translated.

`RibbonCommandType` classifies Navigate, Refresh, Search, Filter, Import, Export, Preview, ApplicationCommand, BatchAction, and CustomRegistered commands. Task 5 dispatches Navigate, Refresh, ApplicationCommand, and CustomRegistered; other types resolve safely as unavailable.

Definitions sort by `DisplayOrder` and then technical code. `RibbonDefinitionValidator` reports deterministic codes for duplicate tabs/groups/commands, malformed rules, invalid command shapes, and unknown workspace targets. Invalid definitions produce no visible Ribbon instead of crashing the shell.
