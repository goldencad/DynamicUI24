# DataEntry filtering

## WHAT DATAENTRY OWNS
DataEntry owns typed filter descriptors, validation, active-filter presentation, clear actions, and query-generation changes.

## WHAT IT DOES NOT OWN
It does not implement SQL, a database query designer, business rules, or sensitive-value enumeration.

## COLUMN IDENTITY
`GridFilterDescriptor.VariableCode` is the only column key.

## FILTER CONTRACT
Text supports contains/equals/starts-with/empty; number supports equals/comparison/between/empty; date supports equals/before/after/between/empty; boolean supports true/false/any. A descriptor carries its data type and up to two typed operands, with no UI control in Core.

## PRIVACY RULE
The generic UX accepts policy-approved manual values. It never generates distinct-value suggestions, particularly for restricted columns. Raw sensitive filter values should not be persisted by applications unless their privacy policy explicitly permits it.

## 100K+ RULE
Sort/filter are provider requests and increment the runtime generation. The viewport remains bounded; late prior-generation or prior-company results are ignored.

## FOCUSED TEST COMMANDS
`/usr/local/share/dotnet/dotnet test tests/DynamicUI24.Tests --no-restore --filter "GridPersonalizationTests|SelectionSurvivesSortAndFilter|CompanySwitch"`

## COMMON FAILURE MODES
Avoid client-side visual-row filtering, untyped operators, automatic restricted-value suggestions, synchronous counts, or applying late responses.
