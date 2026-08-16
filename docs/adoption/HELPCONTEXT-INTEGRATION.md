# HelpContext Integration

Assign stable, non-sensitive `HelpContextCode` values to template, workspace, section and field metadata. Resolve the current code with `HelpContextResolver`, then call a registered `IContextualHelpProvider` using current culture and semantic scope. Treat unknown codes as an empty help state and provider failure as a safe local error.

Do not put record labels, user-entered text, secrets or prompts in a help code. Local content is the recommended default. Help actions navigate or dispatch through existing registries.
