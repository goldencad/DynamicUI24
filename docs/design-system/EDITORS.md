# Editors

Normative authority: [DynamicUI24 Specification v0.16 §10](../specification/DynamicUI24-Spec-v0.16.md#10-universal-editors-and-forms). This file is implementation guidance; v0.16 wins on conflict.

Editor chrome is quiet and consistent: label, optional required marker, placeholder, helper or safe validation message, optional leading/trailing icon, embedded semantic actions, tooltip and shared contextual help. ReadOnly remains inspectable where policy permits; Disabled is non-interactive.

Text and memo use native OS input. Numeric/date/choice use native Avalonia controls. Date is presented as one compact `CalendarDatePicker`; Time is one native text-backed `HH:mm` field; DateTime is a clearly grouped compact date/time composition. DateRange is one semantic editor with localized Start/End groups that wrap on narrow surfaces. `DateOnly`, `TimeOnly`, `DateTime`, and `DateRangeValue.Start`/`.End` remain authoritative; formatted text is presentation only. The shared presenter uses `dd/MM/yyyy` for vi-VN and the approved culture short-date pattern for other cultures.

Universal Editor owns this presentation for Report parameters, forms, filters, Setup, and Authoring. Consumers must not create product-specific date editors. Calendar and clock composition remains lightweight and native-input based; no custom text engine may intercept OS IME composition.

## Shared checkbox presentation

Boolean and MultiChoice use native Avalonia `CheckBox` semantics for checked state, pointer/Space input, Tab focus, automation, accessibility, and disabled policy. Their visual template is the shared `DuiCheckBoxTheme`; the Actipro default checkbox chrome is not used for Universal Editor checkbox anatomy.

The theme renders exactly one `BoxSurface` and conditionally shows catalog-backed `CHECK` or `INDETERMINATE` SVG icons. `DuiCheckBox*` resources own box size, border, radius, backgrounds, hover/focus/disabled treatments, and icon size/color. Option rows only provide `LeadingCheckSlot | Label` geometry and never draw another box or glyph.

Floating labels are metadata-supported but not forced where they would destabilize an active native input session. Error state must be perceivable through text/accessibility, not color alone. P1 may mask or omit value, helper, tooltip, clipboard and accessibility output.

Numeric stepping is metadata-owned. `EditorDefinition.Increment` controls the presenter step; when omitted, the native numeric presenter uses `1`. The Percentage Demo explicitly uses `.01` with fraction storage, so one step represents one percentage point. Consumers may select another increment without changing deterministic `Fraction` versus `WholeNumber` storage semantics.

## Theme-authorable editor geometry

The Standard owns editor anatomy, semantic identity, keyboard/focus behavior, popup ownership, IME boundaries, accessibility floors, and responsive relationships. Theme owns only physical mappings, resolved by the presentation adapter from the semantic resources below. Application metadata supplies none of these values. Task 11F may author these mappings; this task does not add Theme Studio UI.

| Property | Owner | Semantic role | Runtime consumer | Theme Studio editable | Validation |
| --- | --- | --- | --- | --- | --- |
| Control height | Theme | `Editor.ControlHeight.Standard` | editor surface/slot | Yes | >= hit-target floor |
| Widths | Theme | `Editor.Width.*` | geometry resolver | Yes | positive |
| Padding and gaps | Theme | `Editor.ContentPadding`, `Editor.InlineGap` | editor/popup layout | Yes | non-negative |
| Icon and slots | Theme | `Editor.Icon.Size`, `Editor.*Slot.Width` | affordance slot | Yes | slot >= icon |
| Border/radius | Theme | `Editor.Border.Thickness`, `Editor.Radius` | editor chrome | Yes | valid/non-negative |
| Popup height/padding | Theme | `Popup.MaxHeight`, `Popup.Padding` | owned dropdown | Yes | max > option height |
| Popup option/elevation | Theme | `Popup.OptionHeight`, `Popup.Elevation` | popup list surface | Yes | positive/valid recipe |
| Typography, colors, focus, motion | Theme | existing `Typography.*`, `Color.*`, `Motion.*` | shared theme adapter | Yes | existing theme policy |
| Popup ownership/lifecycle | Standard | Universal Editor anatomy | presenter | No | n/a |
| Semantic identity/selection | Standard | Universal Editor contracts | Core state | No | n/a |
