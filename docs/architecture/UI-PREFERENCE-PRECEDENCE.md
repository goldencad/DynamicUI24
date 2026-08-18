# UI Preference Precedence

Resolution is Framework Default → Published Definition → User Preference → Authorization/Privacy/Policy ceiling → Platform capability → rendered UI.

Preferences can hide, reorder, resize, pin, save views or collapse only where the definition permits. They never grant access. A saved “show SALARY” preference remains stored but cannot make an unauthorized column visible; if authorization later returns, it can become effective again. Removed semantic IDs are ignored, invalid dimensions reset or clamp deterministically, and hidden items cannot remain pinned.

Existing Grid preferences and Quick Access stores remain authoritative; Task 10H does not create replacements.
