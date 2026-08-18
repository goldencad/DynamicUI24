# Editor Validation

Generic rules cover required, parse/type, length, regex, range, ordered date range, typed synchronous application rules, asynchronous provider rules and a semantic cross-field contract. Results carry severity, message code, safe localized message and semantic target.

Validation reads semantic values, never visual controls. A parse failure stays in editor candidate state. No rule may place raw protected values in messages. Business truth remains application/provider authority. Regex masks validate completed input with a timeout; they do not intercept native IME composition.
