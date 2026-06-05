# AGENTS.md

## Goal
Keep changes small, correct, maintainable, and easy to review.
Prefer reusing existing code over creating parallel implementations.

## Default workflow
1. Understand the requested behavior and what must remain unchanged.
2. Inspect only the relevant files, docs, tests, and call sites.
3. Search for existing classes, methods, utilities, and extension points before creating new code.
4. Make the smallest safe change that fully solves the task.
5. Review affected references and run the most relevant checks.
6. Report modified files, verification, and regression risks.

## Serena workflow
Use Serena symbolic tools before broad grep or opening many code files when the task involves code structure.

Prefer Serena for:
- finding classes, methods, interfaces, properties, and symbols;
- finding references, callers, implementations, and inheritance;
- tracing a code path;
- renaming symbols;
- editing shared methods;
- checking the impact of a refactor.

Recommended order:
1. Activate the current repository as the Serena project if needed.
2. Use symbol overview before reading large files.
3. Find the relevant symbol.
4. Find references and implementations before modifying shared code.
5. Edit the smallest relevant symbol.
6. Re-check references and diagnostics after editing.

Use normal text search for:
- config values;
- logs;
- documentation;
- asset names;
- serialized data;
- exact strings;
- non-code files.

Do not force Serena for trivial one-line edits.

## Reuse before creating
Before adding a new class, method, manager, helper, utility, service, or pipeline:
1. Search for an existing implementation.
2. Check whether an existing component can be extended safely.
3. Avoid creating a second path for behavior already handled elsewhere.
4. Explain why a new abstraction is necessary.

Do not create:
- duplicate helpers with slightly different names;
- generic Manager, Helper, or Utility classes without a clear responsibility;
- wrappers that only forward calls;
- speculative abstractions for possible future needs;
- hidden global state when explicit dependencies are practical.

## Code quality
- Keep each class focused on one responsibility.
- Keep source files under 500 lines when practical.
- Keep functions focused and readable.
- Prefer clear control flow and guard clauses over deep nesting.
- Prefer composition over inheritance unless the subtype relationship is stable.
- Avoid duplicated logic, god objects, and unnecessary coupling.
- Preserve existing behavior unless the request explicitly changes it.
- Do not mix unrelated refactors with a scoped feature or bug fix.

## Feature changes
When adding or changing a feature:
1. Identify the existing entry point and data flow.
2. Reuse existing extension points.
3. Handle normal cases, invalid input, empty input, repeated calls, and boundary values.
4. Update tests and docs when behavior changes.
5. Check compatibility when public APIs, saved data, schemas, or external contracts are affected.

## Bug fixes
Fix root causes, not only symptoms.

For every bug:
1. Reproduce the issue or state why reproduction is not possible.
2. Trace the execution flow.
3. Gather evidence from code, logs, diagnostics, stack traces, or tests.
4. Apply the smallest safe fix.
5. Add a regression test or concrete manual test steps.
6. Check nearby code for the same pattern.

For intermittent crashes, freezes, or memory growth, inspect:
- event subscribe and unsubscribe;
- object lifecycle and cleanup;
- coroutine, thread, task, and async cancellation;
- repeated initialization;
- collection growth;
- pooling;
- repeated allocation;
- null or destroyed-object access;
- race conditions.

Do not claim a bug is fixed without verification evidence.

## Refactoring
Refactor only for a clear reason:
- reduce duplication;
- simplify responsibilities;
- improve readability or testing;
- remove dead code;
- reduce coupling;
- prepare an explicitly requested feature.

Prefer incremental, reviewable changes.
Do not rewrite a working system only to impose a preferred style.

## Verification
Use the repository's existing tools and conventions.

When available, run the relevant subset of:
- build or compile checks;
- unit tests;
- integration tests;
- lint;
- formatting checks;
- static analysis;
- diagnostics;
- targeted manual verification.

Do not remove or weaken tests merely to make changes pass.
If a check cannot be run, explain why and provide the exact command.

## Git safety
Protect existing work.

Do not run destructive commands unless explicitly requested:
- git reset --hard
- git clean -fd
- force push
- destructive rebase
- deleting branches
- discarding uncommitted changes
- overwriting unrelated files

Do not commit or push unless explicitly requested.
Do not revert user changes merely because they complicate the task.

## Dependencies and security
- Do not add a production dependency unless it provides clear value.
- Prefer existing dependencies and standard-library features when suitable.
- Check maintenance status, license compatibility, security, and runtime cost before adding dependencies.
- Validate external input at system boundaries.
- Never expose secrets in source code, logs, tests, or commits.

## Completion report
At the end of a coding task, report:
- what changed and why;
- modified files;
- checks that were run and results;
- checks that could not be run;
- regression risks;
- manual test steps when relevant;
- existing code reused;
- any new abstraction and why it was necessary.

Keep the report concise and specific.

## Project-specific notes
Add only the information that is useful for this repository:
- project purpose;
- important directories;
- build, run, and test commands;
- architecture notes;
- generated files;
- third-party or read-only directories;
- project-specific constraints.
