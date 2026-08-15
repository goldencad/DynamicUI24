# Formula Definition Boundary

Formula definitions are declarative metadata only. DynamicUI24 Setup validates identifiers and references but never calculates a result.

Metadata must not contain or launch C#, compiled assemblies, SQL, shell commands, JavaScript or arbitrary scripts. A future Calculation Engine sits below the UI boundary, consumes validated/versioned definitions through an explicit contract and owns dependency evaluation and execution safety.

Local AI may propose expression metadata and reference selections only as a draft. It must use known `VariableCode` values, preserve the no-execution boundary and pass validation before a user publishes the definition.
