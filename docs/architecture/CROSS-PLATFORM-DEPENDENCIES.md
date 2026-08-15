# Cross-platform dependencies

M1 reviews the platform-sensitive dependency closure used by `DynamicUI24.Demo`.

| Dependency | Package metadata/build-output finding | Five-RID publish status |
| --- | --- | --- |
| Avalonia Desktop 11.3.2 | Its package metadata declares Windows, macOS, and Linux desktop backends through Avalonia Native, Win32, X11, and Skia dependencies. RID publish resolves assets for `win-x64`, `win-arm64`, `osx-arm64`, `osx-x64`, and `linux-x64`. | Required gate |
| Avalonia Native / Skia / Win32 / X11 | Native/backend assets are selected by NuGet and the .NET RID graph during self-contained publish; each target is therefore verified by the five-RID publish gate. | Required gate |
| Avalonia Fluent resources | Managed XAML/resources; no additional native RID claim. They are included by each demo publish. | Required gate |
| Actipro Avalonia Pro 25.2.0 | Package metadata targets .NET 8 and references Avalonia 11.3.0+. Its Bars/Ribbon assemblies are managed assets and are present in the demo's publish closure for every required RID. | Required gate |

The review records package metadata and actual publish closure, not a claim of native GUI certification. Avalonia backends, renderers, GPU/drivers, display servers, and Actipro controls must still be exercised on the intended operating system and architecture. Actipro licensing remains the consumer's responsibility.

Before adding or upgrading a platform-sensitive dependency, maintainers must review its native assets and metadata against all five RIDs, add any necessary CI validation, update this document, and obtain a compatibility exception before reducing the matrix.
