# Navigation Search

Navigation Search filters the already authorization-resolved semantic Tree. It searches the current hierarchy only; it is not record search. Matching descendants retain ancestor context, collapsed descendants participate, and hidden permission nodes never enter the source tree.

`NavigationSearchDefinition` controls enablement, placeholder, auto-show threshold seam, prefix/contains mode, collapsed descendants, and hierarchy preservation. While filtering, overflow is bypassed so nodes behind See more are discoverable without duplication. Clearing restores the original resolved hierarchy and existing overflow controller state. Selection, disabled, hover, and focus semantics remain owned by `DynamicTreeHost`.

Applications should resolve permission/company state first, then filter, then render. Never create visual nodes merely to search them.
