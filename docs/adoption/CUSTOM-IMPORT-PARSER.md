# Custom import parser

Implement `IImportParserProvider` with a stable technical `ParserCode`, extension hints, bounded `InspectAsync`, and cancellation-aware `ParseAsync`. Return source semantic fields and `ImportSourceRecord`; do not map to application fields or commit data.

Register the instance in `ImportExportRegistry`, then reference its code from an `ImportDefinition`. The generic mapping, preview, diagnostics and commit host require no modification. Validate sizes, reject malformed input safely, never load an assembly named by a file, and do not expose internal exception text.

Test registration, schema inspection, cancellation, malformed input, bounded samples and use through `ImportEngine`. The demo `CUSTOM_DEMO` format shows `@record`, `key=value`, `@end` records.
