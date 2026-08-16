# DynamicUI24 Specification v0.11

**Status:** Architecture baseline / additive successor to v0.10
**Product:** DynamicUI24
**Primary platform:** .NET 9 + Avalonia, cross-platform
**Document engine policy:** TS24 licensed DevExpress Universal Subscription / Office & PDF File API

## 1. Purpose

DynamicUI24 v0.11 formalizes TS24's document-processing architecture. TS24 applications process many Office/PDF documents, and TS24 already licenses DevExpress Universal. DynamicUI24 should therefore reuse the mature DevExpress Office & PDF File API for supported formats while keeping all vendor APIs behind replaceable adapters.

**Long-term rule:** TS24 owns document semantics; DynamicUI24 owns vendor-neutral contracts and presentation integration; DevExpress owns low-level Office/PDF mechanics behind an adapter boundary.

v0.11 is additive to v0.10. Existing v0.10 requirements remain authoritative unless explicitly strengthened here.

## 2. Preferred document engine

For supported operations, prefer:

| Family | Preferred implementation |
|---|---|
| XLS/XLSX | DevExpress Spreadsheet Document API |
| DOC/DOCX/RTF | DevExpress Word Processing Document API |
| PDF | DevExpress PDF Document API |
| PPT/PPTX | DevExpress Presentation API |
| Barcode/archive/conversion | Appropriate licensed DevExpress non-visual API when useful |
| XML/JSON/CSV/TSV/fixed-width | Existing lightweight/native provider where more appropriate |
| Unsupported formats | Explicit independent adapter |

Do not force lightweight formats through a heavy Office engine.

## 3. Mandatory licensing policy

TS24 owns a valid DevExpress Universal Subscription.

All Codex/AI work MUST:
- use TS24-authorized DevExpress NuGet feed/package/license configuration;
- never download or activate a DevExpress trial;
- never create a trial account;
- never accept trial/evaluation watermark behavior;
- never commit feed credentials, license keys or tokens.

If authorized package/feed access is unavailable: **STOP and report the environment dependency.** Do not silently substitute trial packages.

## 4. Ports-and-adapters boundary

```text
TS24 App / DynamicUI24
        |
        v
Vendor-neutral Document Contracts
        |
        v
Document Adapter Registry
        |
        v
DynamicUI24.DevExpress.Documents
        |
        v
DevExpress Office & PDF File API
        |
  XLSX / DOCX / PDF / PPTX
```

`DynamicUI24.Core` MUST NOT reference DevExpress packages or expose DevExpress types.

DevExpress `Workbook`, RichEdit document types, PDF processors/documents, presentation types, enums and options remain implementation details of the DevExpress adapter.

## 5. Recommended project organization

```text
DynamicUI24.Core
  Documents/Contracts
  Documents/Models
  Documents/Operations
  Documents/Diagnostics

DynamicUI24.Shared
  Documents/Coordination
  Documents/Policies

Extensions/
  DynamicUI24.DevExpress.Documents/
    Spreadsheet/
    Word/
    Pdf/
    Presentation/
    Conversion/
    Infrastructure/

DynamicUI24.Avalonia
  Presentation/DocumentHost
  Presentation/Preview
  Presentation/Commands
```

Exact names may follow existing repository conventions. Dependency direction matters more than names.

## 6. Capability-focused contracts

Create contracts only as actual tasks require them. Prefer small capability interfaces such as:

```text
IDocumentReader
IDocumentWriter
IDocumentConverter
IDocumentMetadataReader
IDocumentPreviewProvider
ISpreadsheetDocumentService
IWordDocumentService
IPdfDocumentService
IPresentationDocumentService
```

Avoid one giant interface containing every Office feature.

## 7. Semantic document identity

Conceptually:

```text
DocumentReference
- DocumentId
- FileName?
- MediaType?
- Format
- SourceKind
- Length?
- ContentHash?
- Version?
- CompanyScope?
- SecurityClassification?
```

A local file path is not universal identity. A `DocumentReference` is never a DevExpress object.

## 8. Format model

Framework-safe identifiers may include:

`XLS`, `XLSX`, `CSV`, `TSV`, `DOC`, `DOCX`, `RTF`, `PDF`, `PPT`, `PPTX`, `XML`, `JSON`, `TXT`, `IMAGE`, `UNKNOWN`.

Do not infer format only from extension when stronger metadata/content detection exists.

## 9. Streams first

Prefer `Stream`, bounded buffers, and semantic content references. Use managed temporary files only when a selected operation truly requires them.

This must support desktop, API, Docker, object storage, Konect24, signing/assembly nodes and in-memory workflows.

## 10. Resource ownership

Adapters must deterministically own/dispose streams they create, temporary files, buffers and disposable DevExpress objects. Ownership must be explicit at contract boundaries.

## 11. Async UI boundary

Document operations must not block the Avalonia UI thread. Application-facing coordination is async-ready with cancellation/progress where meaningful. Generation validation protects against stale results even when an underlying synchronous library call cannot be interrupted.

## 12. Spreadsheet adapter

DevExpress Spreadsheet Document API is the preferred XLS/XLSX implementation for:
- workbook/sheet load/save;
- range access;
- formatting;
- formulas as document content;
- validation/tables;
- metadata;
- conversion/export;
- printing/export capabilities where required.

DynamicUI24 DataEntry remains its own UI/data engine. A DevExpress workbook is NOT the runtime model of the DataEntry Grid.

## 13. Task 10D XLSX alignment

Task 10D generic import/export contracts remain authoritative.

Preferred physical path:

```text
IImportFormatProvider / IExportFormatProvider
              |
              v
          XLSX Adapter
              |
              v
DevExpress Spreadsheet Document API
```

If the existing 10D XLSX adapter is not DevExpress-based, do not reopen completed 10D merely to rewrite it. Audit during 10E. Migrate only if small, contract-preserving and fully tested; otherwise record a controlled migration backlog item.

Visual reorder/hide/pin must never alter semantic `VariableCode` XLSX mapping.

## 14. Word adapter

Use DevExpress Word Processing Document API for supported DOC/DOCX/RTF work: create/load/save, formatting, styles, tables, fields, headers/footers, images, protection, mail merge, comparison/conversion where required.

Do not leak RichEdit/DevExpress document types into application/domain contracts.

## 15. PDF adapter

Use the approved stable production-ready DevExpress PDF API for the repository version.

Potential capabilities include create/load/save, merge/split, pages, metadata, forms/annotations, protection, signatures, rendering/conversion and inspection.

**Preview/CTP rule:** never migrate mission-critical production code to a CTP/preview API merely because it is newer. Evaluation can occur behind the adapter. Production migration requires stable release, capability parity, cross-platform evidence, regression tests and explicit approval.

## 16. Presentation adapter

Use DevExpress Presentation API for supported PPT/PPTX load/generate/modify/inspect/export operations. Never introduce PowerPoint COM automation.

## 17. No Microsoft Office automation

Do not introduce:
- Office COM;
- Office Interop as document core;
- Word/Excel/PowerPoint UI automation;
- Windows-only Office installation dependencies.

## 18. Conversion contract

Conceptually:

```text
DocumentConversionRequest
- SourceFormat
- TargetFormat
- Options
- SecurityContext
- Culture?

DocumentConversionResult
- Format
- Content/Stream
- MediaType
- Diagnostics
```

Generic contracts expose generic options. Vendor-specific advanced options belong to extension-specific contracts.

## 19. Capability discovery

Use explicit document-processing capabilities such as `READ`, `WRITE`, `CONVERT`, `PREVIEW`, `PRINT`, `MERGE`, `SPLIT`, `EXTRACT`, `METADATA`, `PROTECT`.

`SIGN` is intentionally NOT a DevExpress document-adapter capability in TS24 architecture.

Digital signing belongs to the separate TS24 signing module.

Unknown/unsupported capability fails safely.

## 20. Adapter registry

Resolve adapters deterministically by format + capability. No untrusted assembly scanning, arbitrary reflection, scripts or dynamic executable loading.

## 21. Package/version isolation

Centralize DevExpress versions in repository package management (prefer existing `Directory.Packages.props` conventions). Do not scatter versions.

A normal DevExpress upgrade should affect:
1. central package configuration;
2. DevExpress adapter implementation where APIs changed;
3. adapter tests/compatibility evidence.

Business/domain code should normally remain unchanged.

## 22. Upgrade policy

Do not mix a major DevExpress upgrade into an unrelated feature.

Before upgrade review:
- release notes for used APIs;
- breaking/obsolete APIs;
- package/prerequisite changes;
- drawing/font changes;
- licensing changes;
- cross-platform changes.

After upgrade verify:
- focused adapter builds/tests;
- representative golden documents;
- Windows/macOS/Linux CI;
- relevant five-RID publish;
- no trial watermark/evaluation behavior;
- authorized licensing/package restore.

## 23. Package acquisition

Prefer modern NuGet references from TS24-authorized DevExpress infrastructure rather than manually copied DLLs. Secrets remain external to Git.

Only reference packages actually required by adapter projects; do not make every project restore the full DevExpress stack.

## 24. Cross-platform

Implemented document capabilities must be evaluated on relevant targets:
- Windows;
- macOS ARM64;
- macOS x64 where published;
- Linux x64;
- Docker/server where relevant.

Core contracts remain platform-neutral. Do not claim cross-platform support without representative tests.

## 25. Drawing/rendering

Keep DevExpress cross-platform drawing/rendering packages inside adapter/presentation infrastructure. Do not introduce `System.Drawing` assumptions into Core.

## 26. Fonts

Provide a font-resolution policy suitable for cross-platform/server use. Do not hard-code Windows font directories. Do not commit proprietary fonts without authorization. Detect/fallback safely and document fidelity limitations.

## 27. Temporary files

Prefer streams. When temp files are required:
- random safe names;
- minimum lifetime;
- cleanup on success/failure/cancellation;
- no sensitive business identifiers in names;
- framework-managed temp abstraction.

## 28. Large-document safety

Adapters must consider file size, decompression expansion, worksheet dimensions, page/slide count, image sizes, memory pressure, bounded preview and cancellation. Never load unbounded external content blindly.

## 29. Untrusted documents

Validate formats and bound resource use. Reject malformed files safely. Never execute macros, embedded executables or external links automatically. Parser errors must not crash the Shell.

## 30. Macro policy

DynamicUI24 does not execute VBA/macros. Preserve only when explicitly required/supported; otherwise report limitations. Never introduce a VBA runtime.

## 31. Privacy

P1 remains authoritative. Sensitive content must not leak through preview, thumbnail, metadata, clipboard, export, diagnostics, temp files, recent items or accessibility.

Document adapters perform file mechanics; they do not decide authorization.

## 32. Export security

Before protected export resolve permission, capability and privacy/redaction policy. No adapter may bypass P1 to emit raw protected content.

## 33. Digital signing boundary

`Document Processing != Digital Signing`.

DevExpress Office & PDF File API is used only for document processing, rendering, conversion, inspection, generation, preview support, and related file mechanics.

TS24 digital signing is implemented in a separate TS24 signing module.

DynamicUI24 DevExpress document adapters MUST NOT:
- perform digital signing;
- own or access private keys;
- select signing certificates;
- request or retain certificate/token PINs;
- communicate directly with USB tokens;
- communicate directly with HSM/YubiHSM;
- implement PKCS#11 signing;
- implement remote signing;
- own signing authorization;
- own signing audit;
- own signing workflow state.

The TS24 signing module owns all cryptographic signing responsibilities.

DevExpress may be used before or after signing only for non-signing document processing where required.

Even if a DevExpress PDF API exposes signature-generation functionality, TS24 architecture MUST NOT use DevExpress as the digital-signing engine.

## 34. TS24 signing-module integration

The integration boundary is document bytes / hashes / signature results, not DevExpress signing objects.

Conceptually:

```text
DynamicUI24 / Document Adapter
        |
        | document bytes / prepared document
        v
TS24 Signing Module
        |
        +--> USB Token
        +--> YubiHSM
        +--> Remote Signing
        +--> PKCS#11
        +--> Signing Node
        |
        v
signature result / signed document
        |
        v
Document processing/finalization if required
```

The separate signing module may use its own PDF/signature assembly implementation.

DevExpress is not required for cryptographic signing.

## 36. Hash integrity

Signing workflows hash the exact canonical bytes required by the workflow. Do not silently mutate bytes between approved hash/sign stages. Conversion produces a new representation and normally a new hash.

## 36. Preview architecture

```text
DocumentReference
      |
      v
Preview Provider
      |
      +--> DevExpress/native renderer adapter
      +--> safe fallback
      |
      v
DynamicUI24 Preview Host
```

Core never returns Avalonia controls.

## 37. Bounded preview

Large documents use lazy/bounded page or slide rendering, bounded thumbnails/cache, cancellation and stale-generation protection. Never render every page merely to open a document.

## 38. Preview privacy

P1 applies to preview. If safe masking inside rendered content is impossible, policy may hide the preview, show a protected placeholder or require authorized reveal. Restricted content fails closed.

## 39. Metadata/text extraction

Metadata and text extraction are explicit capabilities. Extracted content remains privacy/permission/company scoped and is never automatically sent to AI/cloud.

OCR is separate and not required by v0.11.

## 40. S1 Search integration

Document indexing/search occurs only through explicit application search providers. The adapter does not automatically index documents.

## 41. S2 Context integration

Context Panel may consume safe semantic document metadata/preview context. It never receives raw DevExpress runtime objects.

## 42. N1 notifications

Use existing notifications for meaningful completion/failure/progress only. Do not emit a toast for every internal document event.

## 43. Report direction — Task 11

Task 11 semantic report definitions remain vendor-neutral:

```text
Dynamic Report Semantic Model
        |
        v
Report Runtime
   |            |
screen       export
                |
                v
       Document Adapter
                |
                v
        DevExpress APIs
```

DevExpress is an output/render engine, not the authoritative report metadata model.

## 44. History/Document direction — Task 12

Task 12 should become the first major consumer of `DocumentReference`, capability discovery, metadata, preview, versions, privacy and document actions. It must still support non-DevExpress providers/formats.

## 45. Dashboard direction — Task 13

Dashboard runtime is independent of Office APIs. Only export/snapshot paths use document adapters where appropriate.

## 46. Signing/Approval direction — Task 14

Use document preview/preparation adapters while preserving the separate TS24 signing module. No DevExpress adapter performs cryptographic signing or owns signing workflow state.

## 47. Designer direction — Task 15

Visual Document/XML Layout Designer stores vendor-neutral semantic layout metadata. DevExpress may render/export it but must never become the authoritative stored template object graph.

## 48. Batch direction — Task 16

Batch document processing must bound concurrency, support cancellation/progress, isolate per-document failures and avoid holding all outputs in memory.

## 49. Governance direction — Task 17

Application/document metadata versions are independent of DevExpress library versions. A library upgrade does not automatically require metadata migration.

## 50. Finalization direction — Task 18

Produce a document compatibility matrix covering adapter, formats, operations, OS/RID/Docker support, approved DevExpress version, fonts, fidelity limitations, licensing/deployment requirements and upgrade procedure.

## 51. Error abstraction

Core-facing error categories may include:

`UNSUPPORTED_FORMAT`, `UNSUPPORTED_CAPABILITY`, `MALFORMED_DOCUMENT`, `PASSWORD_REQUIRED`, `ACCESS_DENIED`, `PRIVACY_BLOCKED`, `RESOURCE_LIMIT`, `CANCELLED`, `PROVIDER_UNAVAILABLE`, `CONVERSION_FAILED`, `UNKNOWN_SAFE_FAILURE`.

Do not expose raw DevExpress exception types across Core contracts.

## 52. Password-protected documents

Passwords use an explicit secure flow with minimal lifetime. Never log/store in metadata/preferences/command lines.

## 53. Digital signature semantics

Keep separate:
- cryptographic signature state;
- certificate/trust state;
- TS24 workflow/approval state.

A DevExpress validation result alone is not application approval state.

## 54. Templates

Templates are semantic, versioned application resources supplied as streams/references. Local file paths are not authoritative template identity. DevExpress version and template version are independent.

## 55. Localization/time

Adapters accept culture where relevant. Do not hard-code vi-VN/en-US. Test Vietnamese document fidelity. Business timezone semantics remain application-owned.

## 56. Lightweight formats

XML/JSON use suitable .NET serializers/parsers. CSV/TSV/fixed-width/custom 10D providers may remain streaming/native. DevExpress preference applies where it provides clear value, not universally.

## 57. Escape hatch

Advanced DevExpress-only capability may live in a DevExpress extension-specific interface/project with documented coupling and capability detection. Never pollute generic Core contracts for one vendor feature.

## 58. Legacy migration

When migrating old .NET Framework document code:
1. identify business capability;
2. map it to vendor-neutral contract;
3. implement via current adapter;
4. write semantic regression tests;
5. remove COM/UI/platform coupling;
6. reuse business rules, not accidental legacy architecture.

## 59. Test strategy

Document adapters need focused:
- contract tests;
- adapter tests;
- format round-trip tests;
- conversion tests;
- malformed-input tests;
- privacy/security tests;
- large-document tests;
- cross-platform tests;
- upgrade regression tests.

Use test-safe golden documents, never customer production files.

## 60. Round-trip semantics

Test `load -> inspect -> save -> reload -> verify semantics`. Do not require byte equality when legitimate Office package metadata is rewritten.

## 61. Stale results / Company Context

Preview/conversion uses generation validation. Late document A result cannot overwrite current B. Company A artifacts must never appear after switching to Company B.

## 62. Cache

Cache keys include document identity/version, transformation/options, Company scope and privacy-sensitive presentation state where needed. Caches are bounded and evictable. Never key only by file name.

## 63. Diagnostics

Safe diagnostics may include adapter, operation, format, capability, elapsed time and error category. Never log document body, extracted sensitive text, passwords, license secrets or private keys.

## 64. Required implementation documentation

When document adapters are implemented, maintain:

```text
docs/architecture/DOCUMENT-PROCESSING.md
docs/architecture/DEVEXPRESS-DOCUMENT-ADAPTERS.md
docs/architecture/DOCUMENT-SECURITY.md
docs/architecture/DOCUMENT-PREVIEW.md
docs/adoption/DOCUMENT-PROCESSING-INTEGRATION.md
docs/adoption/DEVEXPRESS-OFFICE-FILE-API.md
docs/backlog/DOCUMENT-PROCESSING-BACKLOG.md
```

## 65. Local-AI maintainability

Docs must provide concise sections:

`WHAT DOCUMENT CORE OWNS`, `WHAT DEVEXPRESS ADAPTER OWNS`, `WHAT APPLICATION OWNS`, `SUPPORTED FORMAT MAP`, `CAPABILITY MAP`, `STREAM OWNERSHIP`, `PRIVACY RULE`, `SIGNING BOUNDARY`, `PACKAGE POLICY`, `LICENSE POLICY`, `UPGRADE PROCEDURE`, `FOCUSED TEST COMMANDS`, `COMMON FAILURE MODES`.

## 66. Architecture guards

Future implementation should prove:
1. Core has no DevExpress dependency.
2. DevExpress packages stay in adapter/extension projects.
3. Generic contracts expose no DevExpress types.
4. No Office COM/Interop.
5. No trial/evaluation configuration.
6. No license secrets committed.
7. No digital-signing implementation in DevExpress document adapters.
8. No private-key ownership in document adapters.
9. No macro/script execution.
10. No automatic external-link execution.
11. Streams are first-class.
12. Temp files are bounded/cleaned.
13. Company stale protection exists.
14. P1 privacy is reused.
15. Preview is bounded.
16. No customer production docs are fixtures.

## 67. Task 10E addendum

10E remains DataEntry Advanced UX / Finalization.

Audit current 10D XLSX adapter:
- if already DevExpress Spreadsheet Document API: preserve/document it;
- if not: migrate only when small, contract-preserving, fully tested and scope-safe;
- otherwise add controlled backlog item.

10E must not be destabilized by a large 10D rewrite.

Never use trial packages.

## 68. DevExpress upgrade seam

Desired future upgrade:

```text
Core contracts [unchanged]
      |
DevExpress adapter v26.x
      |
upgrade
      v
DevExpress adapter v27.x
```

Core changes only when semantic capability truly changes.

## 69. Mandatory Codex header for document tasks

Every future task touching Office/PDF documents must include:

```text
TS24 owns a valid DevExpress Universal Subscription.

Use the licensed DevExpress Office & PDF File API as the preferred
processing engine for supported Office/PDF formats.

Use TS24-authorized package/feed/license configuration.

DO NOT obtain or activate a DevExpress trial.

Keep DevExpress behind DynamicUI24 vendor-neutral document adapters.

Do not expose DevExpress types in DynamicUI24.Core/domain contracts.

Do not introduce Microsoft Office COM/Interop.

Do not use DevExpress as the TS24 digital-signing engine.
Digital signing belongs to the separate TS24 signing module.

If licensed package/feed access is unavailable:
STOP and report the environment dependency.
```

## 70. v0.11 adoption acceptance

Architecture is adopted when:
- document contracts are vendor-neutral;
- DevExpress is isolated behind adapters;
- authorized Universal packages are used;
- no trial dependency exists;
- XLSX is aligned or explicitly backlogged;
- Word/PDF/Presentation can be added without Core vendor coupling;
- streams are first-class;
- privacy/permission/Company Context remain outside vendor objects;
- signing boundary remains intact;
- preview is bounded/stale-safe;
- CI restores licensed packages securely;
- implemented capabilities have cross-platform evidence;
- DevExpress upgrades are primarily adapter/package changes.

## 71. Non-goals

v0.11 does not require immediate implementation of all Office formats, full Office editors, OCR, AI document understanding, collaboration, workflow, VBA/macros, Office Interop, a new signing engine, a new storage system or replacement of 10D generic contracts.

## 72. Relationship to v0.10

v0.11 preserves completed P1, S1, S2 and Task 10A–10D. Task 10E continues under v0.11 with Section 66. All v0.10 requirements remain authoritative unless explicitly superseded here.

---

**End of DynamicUI24 Specification v0.11**
