# Output Format

Return results in this order so a human can review code and rationale together.

## 1. Generated test code

- Provide one or more complete C# code blocks for xUnit / FlaUI direct operation
  tests.
- Include required `using` directives and fixture / cleanup code needed to make
  the sample coherent.
- Keep project-specific integration notes outside the code block unless the user
  supplied an existing project pattern.
- Mark any unavoidable placeholder with a clear name such as
  `TODO_ConfirmExecutablePath` and explain it in section 4.

## 2. Generation rationale

Explain the mapping from observation to code.

- Scenario step -> generated operation
- Observed element -> selector used
- Observed state change -> wait or assertion
- Cleanup need -> cleanup implementation

Prefer concise bullets. Include enough detail for the reviewer to find the
source observation behind each important selector and assertion.

## 3. Manual review points

List items that a human should confirm, without outsourcing routine technical
choices the agent can already make.

- Selector stability concerns such as localizable Name text or duplicate labels
- Assertion validity against the user's actual business expectation
- Test data safety, persistence, or environment dependence
- Launch / close behavior and side effects
- Whether more cases are needed for error paths or boundary values

## 4. Uncertainty and assumptions

Separate verified facts from assumptions.

- Assumed app path, arguments, account, locale, data state, or timing behavior
- Missing observations that may affect selector or assertion choice
- Places where generated code is intentionally conservative
- Partial generation limits if the input was incomplete

## 5. POM candidates

Do not generate POM code in the MVP unless the user explicitly asks for a
follow-up refactor. Instead, summarize candidate pages / components / actions and
why extracting them may help.

Use `references/pom-guidelines.md` for deciding whether to recommend extraction.
