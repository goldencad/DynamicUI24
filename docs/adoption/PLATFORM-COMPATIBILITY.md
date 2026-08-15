# Platform compatibility

Consumer applications that use the DynamicUI24 shell inherit its five-RID publish baseline: `win-x64`, `win-arm64`, `osx-arm64`, `osx-x64`, and `linux-x64`. They must preserve it unless an approved compatibility exception says otherwise. `linux-arm64` is P2/future and is not part of the M1 mandatory gate.

For each consumer UI change, run its self-contained five-RID publish validation. Treat a successful publish as a packaging check—not native GUI certification. Consumer milestones/releases must perform real GUI smoke on the P0 targets (Windows x64, Ubuntu LTS x64, macOS Apple Silicon); P1 smoke is performed when suitable Windows ARM64 or macOS Intel hardware is available.

## Compatibility exceptions

An exception must be recorded before a target is removed, degraded, or made conditional. The record must identify the affected RID/tier, dependency or platform cause, impact, mitigation/alternative, owner, expiry/review date, and release decision. It requires architecture and release-owner approval, plus updates to the consumer's support documentation and CI. An exception does not silently change the DynamicUI24 framework baseline.

PayCalc24 is a future/reference consumer and should adopt this contract when integrated; it is not a DynamicUI24 dependency.
