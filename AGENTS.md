# AGENTS.md

## Purpose

This is a Unity C# game project.

AI agents must make small, safe, maintainable changes that are easy to review, test, debug, extend, and clean up later.

Do not code randomly. Do not rewrite systems without permission. Do not modify unrelated files.

---

## Core Rules

1. Understand the task before coding.
2. If the task is unclear, ask the user before editing.
3. Only edit files directly related to the current task.
4. Prefer reusing existing code over creating new systems.
5. Make the smallest safe change that solves the task.
6. Do not mix unrelated refactors with a feature or bug fix.
7. Do not delete, rename, or rewrite large systems unless explicitly requested.
8. Keep gameplay runtime, preview, UI, and data behavior consistent.
9. Verify changes with tests, compile checks, or clear manual Unity test steps.
10. At the end, report changed files, checks, risks, and what the user should test.

---

## Before Coding

Before editing, determine:

* What exact behavior is requested?
* What behavior must stay unchanged?
* Which files/systems are in scope?
* Which files/systems are unrelated and should not be touched?
* Is this a bug fix, feature, refactor, cleanup, or investigation?
* Can the existing architecture support this without a new system?

Ask the user first if:

* the request is ambiguous;
* there are multiple valid designs;
* the change may affect many systems;
* the change may alter balance, saved data, prefabs, scenes, or public APIs;
* a quick patch and a proper refactor are both possible;
* the task conflicts with this file.

---

## Scope Control

Allowed changes:

* files that directly implement the requested behavior;
* tests/checks related to the changed behavior;
* small supporting changes required by the task;
* documentation only when it explains changed behavior.

Not allowed unless explicitly requested:

* unrelated UI;
* unrelated skills;
* unrelated enemies;
* unrelated balance numbers;
* unrelated prefab/scene changes;
* passive system;
* broad architecture rewrites;
* mass formatting;
* public API renames;
* new parallel systems that duplicate existing behavior.

If more than a few files must change, explain why before editing.

---

## Serena Workflow

Use Serena symbolic tools for structural code tasks.

Use Serena when:

* finding classes, methods, properties, interfaces, or symbols;
* finding references, callers, implementations, or inheritance;
* tracing runtime flow;
* editing shared methods;
* checking refactor impact;
* reading project memories.

Recommended order:

1. Activate the current repository as the Serena project if needed.
2. Read Serena memories.
3. Use symbol overview before opening large files.
4. Find the relevant symbol.
5. Find references and implementations before editing shared code.
6. Edit the smallest relevant symbol.
7. Re-check references/diagnostics after editing.

Use normal text search for:

* exact strings;
* config values;
* logs;
* asset names;
* serialized data;
* documentation;
* non-code files.

Do not force Serena for trivial one-line edits.

---

## Context7 Workflow

Use Context7 only when current external docs are needed.

Use Context7 for:

* Unity API uncertainty;
* package API uncertainty;
* C# library behavior;
* third-party package usage.

Do not use Context7 for local project code. Use Serena or local search instead.

---

## OOP and Architecture Style

Prefer simple, clear OOP.

Rules:

* One class should have one clear responsibility.
* Keep methods focused and readable.
* Prefer composition over inheritance unless the subtype relationship is stable.
* Prefer explicit dependencies over hidden global state.
* Prefer data-driven behavior when the project already uses ScriptableObjects/config data.
* Avoid god classes, god managers, duplicate managers, and vague helpers.
* Avoid wrappers that only forward calls.
* Avoid speculative abstractions for possible future needs.
* Keep public APIs stable unless the task requires changing them.

Before creating a new class, manager, helper, service, utility, pipeline, or abstraction:

1. Search for an existing implementation.
2. Check if an existing component can be extended safely.
3. Check nearby systems that already solve similar problems.
4. Explain why the new abstraction is necessary.

If an existing system can be extended cleanly, reuse it instead of creating a parallel path.

---

## Unity Rules

Prefer:

* small MonoBehaviours with clear responsibilities;
* ScriptableObjects for reusable data/config when the project already uses them;
* serialized fields for designer-tunable values;
* runtime classes for logic that should be testable;
* clear separation between data, runtime logic, UI, and visuals.

Avoid:

* putting all logic into one MonoBehaviour;
* hidden scene dependencies;
* hardcoded object names;
* unexplained magic numbers;
* changing prefab/scene behavior without mentioning it;
* moving assets/scripts unless requested;
* renaming classes/files unless necessary.

When editing Unity code:

* preserve serialized field names when possible;
* be careful with prefab/scene references;
* mention manual Unity test steps when runtime behavior changes.

---

## Gameplay System Rules

When changing a gameplay system, identify:

1. Data source.
2. Runtime owner.
3. Execution entry point.
4. Preview/UI path.
5. Visual/audio feedback path.
6. Test or manual verification path.

Runtime and preview behavior must not silently diverge.

If a skill, buff, debuff, enemy intent, dice result, crit/fail rule, or status effect changes, check both:

* actual gameplay execution;
* player-facing preview/UI text.

---

## Skill, Dice, Buff, and Debuff Rules

Current priority: core skill, dice, buff, debuff, preview, and status systems.

A skill may include:

1. Effect: guaranteed result.
2. Condition: optional result if condition is met.
3. Required: prerequisite to use.

Rules:

* Do not create one hardcoded file per skill unless there is no clean data-driven alternative.
* Reuse existing skill, condition, requirement, buff/debuff, preview, and status systems first.
* Preview and runtime should use the same data/calculation path when possible.
* If preview must differ from runtime, explain why.
* Do not touch passive system unless explicitly requested.

Known combat rules:

* Basic Attack fail halves damage before flat add values.
* Example: base 4 damage + fail + Ember Weapon = 4 / 2 + 1 = 3 final damage.
* Ember Weapon: +1 flat damage to Basic Attack; if Basic Attack crits, apply Burn equal to final damage dealt.

When changing damage rules, check:

* normal hit;
* fail;
* crit;
* flat add values;
* status application;
* preview text;
* final dealt damage.

---

## Bug Fix Rules

Fix root causes, not only symptoms.

For every bug:

1. Reproduce the issue or explain why reproduction is not possible.
2. Trace the execution flow.
3. Gather evidence from code, logs, diagnostics, stack traces, tests, or references.
4. Apply the smallest safe fix.
5. Add a regression test if practical.
6. Provide manual Unity test steps.
7. Check nearby code for the same pattern.

For intermittent crashes, freezes, or memory growth, inspect:

* event subscribe/unsubscribe;
* object lifecycle and cleanup;
* coroutines;
* async/task cancellation;
* repeated initialization;
* collection growth;
* pooling;
* null or destroyed-object access;
* race conditions;
* Update loops and repeated allocation.

Do not say "fixed" without verification evidence.

---

## Refactor and Cleanup Rules

Refactor only for a clear reason:

* reduce duplication;
* simplify responsibilities;
* improve readability;
* improve testability;
* remove verified dead code;
* reduce coupling;
* prepare an explicitly requested feature.

Good cleanup:

* remove unused local code near the changed area;
* simplify duplicated logic directly related to the task;
* improve private/local names;
* delete dead code only after checking references.

Bad cleanup:

* mass formatting unrelated files;
* renaming public APIs without need;
* moving folders;
* rewriting systems for style preference;
* deleting code because it looks unused without checking references;
* mixing cleanup with unrelated feature work.

If cleanup touches many files, ask first.

---

## Testing and Verification

Use existing project tools and conventions.

When available, run the relevant subset of:

* Unity EditMode tests;
* Unity PlayMode tests;
* compile/build checks;
* unit/integration tests;
* lint/format checks;
* targeted manual verification.

Do not remove or weaken tests to make changes pass.

If checks cannot be run, explain why and provide exact manual Unity test steps.

---

## Git Safety

Protect existing work.

Do not run destructive commands unless explicitly requested:

* `git reset --hard`
* `git clean -fd`
* force push
* destructive rebase
* deleting branches;
* discarding uncommitted changes;
* overwriting unrelated files.

Do not commit or push unless explicitly requested.

Do not revert user changes merely because they complicate the task.

Before large edits, recommend that the user commit or stash current work.

---

## Dependencies and Security

Do not add production dependencies unless clearly necessary.

Before adding a dependency:

1. Check if the project already has a solution.
2. Check maintenance status.
3. Check license compatibility.
4. Check runtime cost.
5. Explain why it is necessary.

Never expose secrets in source code, logs, tests, screenshots, or commits.

Do not add network calls, telemetry, file system writes, or editor automation unless requested.

---

## Completion Report

At the end of a coding task, report concisely:

* what changed and why;
* modified files;
* existing code reused;
* tests/checks run and results;
* checks that could not be run;
* regression risks;
* manual Unity test steps;
* any new abstraction and why it was necessary.

Keep the report short and specific.

---

## Default Contract

For every coding task:

1. Read this AGENTS.md.
2. Use Serena first for structural code tasks.
3. Inspect only relevant files.
4. Ask if unclear.
5. Make the smallest safe patch.
6. Verify with tests/checks/manual steps.
7. Report concise results.

The user should be able to test the game in Unity after each change without dealing with a large rewrite.
