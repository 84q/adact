---
name: adact-flaui-testgen
description: Use when ADACT observations from a Windows desktop app should be turned into executable xUnit / FlaUI C# UI tests, with selector rationale, assertions, uncertainty, and optional POM split recommendations.
---

# ADACT FlaUI Test Generation

This Skill turns a natural-language test scenario and observations gathered with
the `adact-cli` Skill into direct xUnit / FlaUI C# test code. It does not replace
ADACT-based exploration; use `adact-cli` first to launch or attach to the app,
capture snapshots / inspect output, perform trial actions, and record the UI
state changes that justify the generated test.

## Purpose

- Convert an observed Windows desktop workflow into an executable xUnit / FlaUI
  test.
- Explain why each operation, selector, wait, and assertion was chosen.
- Surface manual review points and remaining uncertainty without asking the user
  to make low-level selector or wait-condition decisions for the agent.
- Keep MVP output as direct FlaUI operation code. Page Object Model (POM) work is
  limited to candidate extraction and split recommendations.

## Preconditions

- The target app can run in an interactive Windows desktop session.
- A scenario, app launch / attach information, test data, and expected result are
  available or can be inferred from observation.
- ADACT observations include enough UIA details to identify stable elements:
  AutomationId when available, plus Name, ControlType, ClassName, hierarchy,
  values, state changes, screenshots, or inspect output as needed.
- The `adact inspect` command provides a `selector` section with stability
  ratings (High / Medium / Low) and FlaUI code examples. These serve as a
  starting point for selector decisions but reflect a single runtime snapshot;
  test context (dynamic content, locale, data variations) must still be
  considered.

## Input

Use the observation template when possible:
[`references/observation-template.md`](references/observation-template.md).
Free-form input is acceptable if it contains the same facts: scenario, app
startup, observed steps, element details, state changes, expected result,
selector candidates, and unstable areas.

## Workflow

1. Compare the scenario with the observation record and identify the test case,
   setup, operation sequence, assertion target, cleanup, and risk areas.
2. If essential facts are missing, request targeted additional observation with
   `adact-cli`; do not invent selectors, expected results, or destructive setup.
3. Draft the generation plan: launch / attach mode, selector strategy, waits,
   assertions, cleanup, and notable assumptions.
4. Confirm the plan once before writing code unless the user already instructed
   you to proceed through test generation.
5. Generate direct xUnit / FlaUI C# test code and the accompanying rationale in
   the required output order.

## User confirmation rules

Ask for confirmation only when it changes risk or meaning. Normally, confirm the
agent's understanding and generation plan once before code generation. If the
user explicitly says to proceed through generation, continue without that pause.

Always stop for confirmation when the workflow may cause irreversible side
effects, external sends / purchases / notifications, secret handling, destructive
test data changes, unknown launch requirements that cannot be explored, or two
business interpretations are equally plausible.

## Test generation rules overview

- Prefer stable UIA selectors: AutomationId first, then Name + ControlType +
  parent context. Avoid coordinate or display-order selectors unless explicitly
  justified as a last resort.
- Prefer state or element waits over fixed sleeps.
- Include reliable cleanup for launched processes, windows, and test data.
- Make assertions match the scenario's user-visible or state-based expectation.
- Make failures traceable by naming the element / state being waited for or
  asserted.
- Do not impose ADACT repository-specific test traits or layering conventions as
  a default for the user's generated test project.

Detailed coding guidance is in
[`references/flaui-xunit-guidelines.md`](references/flaui-xunit-guidelines.md).

## Output structure

Use the fixed output structure from
[`references/output-format.md`](references/output-format.md): generated test
code, generation rationale, manual review points, uncertainty / assumptions, and
POM candidates.

## When information is insufficient

State exactly what is missing and why it affects code generation. Prefer a small
additional ADACT observation request over broad questions. If partial generation
is still useful, mark placeholders and assumptions clearly rather than presenting
them as verified facts.

## References

- Observation input: [`references/observation-template.md`](references/observation-template.md)
- Output format: [`references/output-format.md`](references/output-format.md)
- FlaUI / xUnit guidance: [`references/flaui-xunit-guidelines.md`](references/flaui-xunit-guidelines.md)
- POM candidate guidance: [`references/pom-guidelines.md`](references/pom-guidelines.md)
