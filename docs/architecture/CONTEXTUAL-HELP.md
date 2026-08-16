# Contextual Help Architecture

`HelpContextCode` is a stable semantic identifier, never a control name or sensitive value. Resolution precedence is field → Context Panel section → workspace → template. `IContextualHelpProvider` receives the code, culture, semantic company/workspace identifiers, permission context, privacy mode, generation and cancellation token.

Results contain safe title/content and semantic related action/navigation codes. Core has no browser, network, crawler, prompt, LLM, RAG or AI dependency. The Demo uses only registered local content. A future assistant may consume `HelpContextCode`, but must define a separate privacy-reviewed contract; raw application values are not part of this request.
