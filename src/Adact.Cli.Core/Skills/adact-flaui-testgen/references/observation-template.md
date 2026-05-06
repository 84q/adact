# Observation Template

Use this template to collect the facts needed before generating xUnit / FlaUI
code. The format is recommended, not a strict schema; keep enough detail for the
generated selectors, waits, and assertions to be reviewed.

## Scenario

- User goal:
- Preconditions:
- Test data:
- Expected result:
- Out-of-scope or unsafe actions:

## App startup / attachment

- App type / technology if known:
- Launch command, executable path, or existing window title:
- Required arguments / environment:
- Initial data or settings:
- Safe shutdown / cleanup method:

## Environment

- OS / desktop session notes:
- App version / build:
- Display, locale, account, or data dependencies:

## Screens and flow

For each relevant screen or dialog:

- Window title:
- Screen purpose:
- Key regions:
- Transition into this screen:
- Transition out of this screen:

## Observed operation steps

For each step:

1. Action performed:
2. Target element:
3. Input value or selection:
4. ADACT ref used during observation, if any:
5. Resulting UI state:
6. Snapshot / inspect / screenshot evidence:

## Element details

Record candidate elements used for actions and assertions.

| Purpose | AutomationId | Name | ControlType | ClassName | Parent / context | Value / state | Patterns | Stability notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |

## State changes and assertions

- Before state:
- Trigger:
- After state:
- Assertion candidate:
- Why this assertion proves the scenario:
- Alternative assertions considered:

## Selector candidates

For each important target:

- Preferred selector:
- Fallback selector:
- Context needed to avoid ambiguity:
- Why coordinate / index selection is or is not avoided:

## Unstable or missing information

- Dynamic text, localization, generated ids, virtualization, timing, or async work:
- Fixed wait that appeared necessary during observation:
- Ambiguous business meaning:
- Missing launch, cleanup, data, selector, or assertion facts:

## Artifacts

- Snapshot paths:
- Inspect output paths:
- Screenshots:
- Logs or failure messages:
