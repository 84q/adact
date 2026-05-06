# FlaUI / xUnit Generation Guidelines

Generate direct FlaUI operation code that a developer can paste into or adapt for
an xUnit test project.

## Test shape

- Use xUnit facts or theories that describe one user-observable behavior.
- Put setup, action, assertion, and cleanup in a readable order.
- Prefer launching the app from the test when the user supplied a safe launch
  command. If attaching to an existing process is required, make that precondition
  explicit.
- Avoid applying repository-specific traits, categories, or fixture conventions
  unless the user asks for that project's style.

## FlaUI application and automation lifetime

- Create `Application` and `UIA3Automation` lifetimes explicitly.
- Dispose automation objects and close / kill launched processes in cleanup.
- Prefer graceful close when safe; use kill only as a fallback for test-owned
  processes.
- Keep cleanup idempotent so failures during setup or action do not leave windows
  behind.

## Selector strategy

Prefer selectors in this order:

1. `AutomationId` with an appropriate `ControlType` or parent context.
2. Stable `Name` + `ControlType` when AutomationId is absent.
3. Parent / child hierarchy that scopes otherwise duplicated controls.
4. ClassName or framework-specific hints as supporting evidence.
5. Coordinates, indexes, or display order only as a documented last resort.

When an element's Name is user data, localized text, a counter, or an error
message that can change, avoid treating it as the sole selector. Use it for an
assertion or fallback only when the observation supports that choice.

## Waiting

- Prefer window, element, enabled / disabled, text, value, selection, or existence
  waits over `Thread.Sleep`.
- Use bounded timeouts with meaningful failure messages.
- Wait for the result of an action before asserting, especially after navigation,
  validation, async loading, or dialog opening.
- If a short fixed delay is unavoidable for a specific app behavior, isolate and
  comment it as a last resort.

## Actions

- Use semantic FlaUI APIs where available: invoke, set text / value, select,
  toggle, focus, or keyboard input as appropriate.
- For text entry, clear existing state first when the scenario expects overwrite.
- Avoid relying on the mouse position unless the observed app only responds to
  pointer interaction.

## Assertions

- Assert the outcome that proves the scenario, not merely that a button was
  clicked.
- Prefer stable visible state, control value, selection, enabled state, dialog
  presence, or persisted state when observed.
- Include assertion messages or helper names that identify the expected UI state.
- Do not hide failed assertions behind broad exception handling.

## Traceability and diagnostics

- Name helper methods after the screen or control they locate.
- Include enough selector context in exception messages to identify the missing
  element.
- Keep generated rationale aligned with code so reviewers can trace each selector
  and assertion back to observation evidence.

## Data and side effects

- Use disposable or clearly named test data.
- Reset state before or after the test when the scenario changes settings or
  persistent records.
- Never generate code that sends external messages, purchases, deletes production
  data, or uses secrets without explicit user confirmation and safe test data.
