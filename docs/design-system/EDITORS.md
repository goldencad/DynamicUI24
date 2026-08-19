# Editors

Normative authority: [DynamicUI24 Specification v0.16 §10](../specification/DynamicUI24-Spec-v0.16.md#10-universal-editors-and-forms). This file is implementation guidance; v0.16 wins on conflict.

Editor chrome is quiet and consistent: label, optional required marker, placeholder, helper or safe validation message, optional leading/trailing icon, embedded semantic actions, tooltip and shared contextual help. ReadOnly remains inspectable where policy permits; Disabled is non-interactive.

Text and memo use native OS input. Numeric/date/choice use native Avalonia controls. Date is presented as one compact `CalendarDatePicker`; Time is one native text-backed `HH:mm` field; DateTime is a clearly grouped compact date/time composition. DateRange is one semantic editor with localized Start/End groups that wrap on narrow surfaces. `DateOnly`, `TimeOnly`, `DateTime`, and `DateRangeValue.Start`/`.End` remain authoritative; formatted text is presentation only. The shared presenter uses `dd/MM/yyyy` for vi-VN and the approved culture short-date pattern for other cultures.

Universal Editor owns this presentation for Report parameters, forms, filters, Setup, and Authoring. Consumers must not create product-specific date editors. Calendar and clock composition remains lightweight and native-input based; no custom text engine may intercept OS IME composition.

Floating labels are metadata-supported but not forced where they would destabilize an active native input session. Error state must be perceivable through text/accessibility, not color alone. P1 may mask or omit value, helper, tooltip, clipboard and accessibility output.

Numeric stepping is metadata-owned. `EditorDefinition.Increment` controls the presenter step; when omitted, the native numeric presenter uses `1`. The Percentage Demo explicitly uses `.01` with fraction storage, so one step represents one percentage point. Consumers may select another increment without changing deterministic `Fraction` versus `WholeNumber` storage semantics.
