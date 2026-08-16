# Privacy Policy Resolution

`PrivacyPolicyResolver` receives authorization, metadata, requested mode, mandatory policy, company/workspace identity, reveal state, capture capability, and generation. It returns authorization status, requested/effective mode, effective sensitivity/presentation, separate reveal/copy/export/search/notification/accessibility decisions, capture request/availability, fallback state, and an invariant reason code.

The precedence is authorization → mandatory policy → sensitivity minimum → company/workspace policy → requested mode → reveal eligibility → capture capability → presentation. The strictest outcome wins. Requested `OFF` may coexist with effective `ON` when restricted policy remains mandatory; UI must show both honestly.

Resolution is pure and does not cache raw values. Async policy providers must capture company/workspace/generation and discard late results. A failure or unknown sensitive input never returns raw content. Consumers must not reinterpret or loosen the result.
