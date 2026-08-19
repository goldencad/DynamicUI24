# States and notifications guidance

Normative authority: [v0.16 §§8, 14](../specification/DynamicUI24-Spec-v0.16.md#8-standard-component-state-model).

Use `ComponentState` for control interaction and `ContentState` for surface readiness. Reuse the notification coordinator for Info, Success, Warning, and Critical outcomes. Application-specific content is allowed; duplicate state cards and notification engines are not. Keep error/help association accessible and avoid color-only meaning.
