# Supported platforms

DynamicUI24 maintains the following runtime identifier (RID) contract.

| Tier | Platform | RID | M1 status |
| --- | --- | --- | --- |
| P0 | Windows x64 | `win-x64` | Official |
| P0 | Ubuntu LTS x64 | `linux-x64` | Official Linux scope |
| P0 | macOS Apple Silicon | `osx-arm64` | Official |
| P1 | Windows ARM64 | `win-arm64` | Compatibility |
| P1 | macOS Intel | `osx-x64` | Compatibility |
| P2 | Linux ARM64 | `linux-arm64` | Future; not certified or gated in M1 |

`SupportedPublishRids` in `Directory.Build.props` is the central five-RID publish contract. The demo inherits it as `RuntimeIdentifiers`; architecture tests protect both declarations.

Every UI task that reaches publish validation must publish the demo self-contained for all five contract RIDs. CI validates this matrix on the matching Windows, macOS, and Ubuntu runners. Publish success proves packaging and dependency resolution only; it does **not** prove native GUI behavior.

Native GUI certification occurs at milestone/release scope: Windows x64, Ubuntu LTS x64, and macOS Apple Silicon are required P0 smokes. P1 native smokes run when matching hardware/runtime is available. Ubuntu support is deliberately limited to Ubuntu LTS x64; no generic all-Linux claim is implied.
