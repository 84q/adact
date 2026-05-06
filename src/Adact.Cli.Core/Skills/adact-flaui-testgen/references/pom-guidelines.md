# POM Candidate Guidelines

MVP output is direct FlaUI test code. Use this guide only to decide whether to
recommend Page Object Model extraction and how to describe candidate splits.

## Recommend POM extraction when

- Multiple tests will reuse the same screen, dialog, selector, or operation.
- A workflow spans several screens or has branching dialogs.
- Selectors are verbose and need a single maintenance point.
- The team expects long-term UI test maintenance.
- Setup, navigation, or cleanup code would obscure the assertion in direct tests.

## Usually keep direct code when

- The scenario is a one-off smoke or characterization test.
- Only one or two controls are involved.
- The app UI is still changing quickly and abstraction would be premature.
- The user asked for the smallest runnable example.

## Candidate split dimensions

- Page or window: main window, settings dialog, login screen.
- Component: toolbar, navigation tree, grid, editor, notification area.
- Operation: login, save settings, create record, search, confirm dialog.
- Assertion helper: verify success banner, verify validation error, verify grid
  row contents.

## What to output

In the POM candidates section, list:

- Candidate page / component name
- Repeated selectors or operations it would own
- Tests that would benefit
- Reason to defer or prioritize extraction

Do not output full POM classes during MVP generation unless the user asks for a
separate refactoring step.
