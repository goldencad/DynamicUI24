# Master Catalog Designer

`MASTER_CATALOG` defines catalog metadata, not catalog records. The Setup editor supports draft creation, edit, clone, validation, publish, retire and cancel/revert through the existing lifecycle and Dynamic Action Bars.

Catalogs are ordered by `DisplayOrder` then `CatalogCode`. `ParentCatalogId` may form a hierarchy of any reasonable depth; missing parents, duplicate codes and cycles are rejected. The ten catalogs in the Demo prove extensibility and are not a framework limit. `CompanyScope` is `GLOBAL` or `COMPANY`, with fail-closed permission presentation inherited from Setup.

Published definitions are immutable. Clone a published definition to create a draft/version candidate.
