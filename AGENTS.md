# AGENTS.md — Codex Rules for This Unity Project

## 0. Prime directive

This is a **dice-driven tactical combat roguelike**. Protect the core identity: dice-first tactics, setup -> payoff, build expression, and readable complexity.

Codex must:

1. Read this file first.
2. Search and read current project docs/specs relevant to the task by **topic/content**, not by a fixed filename list.
3. Use Serena/symbol search to inspect existing classes, methods, references, assets, prefabs, scenes, and ScriptableObjects before editing.
4. Make the smallest safe change that solves the task.
5. Do not create duplicate managers, duplicate systems, or parallel architecture.
6. Keep runtime execution, preview, tooltip, and UI feedback consistent.
7. Report changed files, risk, validation, and Unity manual tests after editing.

Prefer Vietnamese when explaining design decisions to the user. Code, class names, method names, comments, and asset names should follow the repository's existing style/language.

---

## 1. Documentation and source-of-truth rules

Do not guess gameplay, UX, economy, combat, status, skill, relic, map, enemy, or progression rules. For each task, find the newest/current relevant docs/specs by searching concepts such as combat loop, AP, dice math, preview, skill grammar, status, reward, relic, map, boss, or economy.

Do **not** depend on fixed documentation filenames. Docs may be renamed, split, merged, archived, or replaced.

Conflict priority:

1. User's latest explicit instruction.
2. The most specific current spec/doc for the touched system.
3. Current repository implementation, assets, prefabs, and ScriptableObjects.
4. Broader design-vision docs.
5. This AGENTS.md default guidance.

If docs conflict or seem stale, state the conflict and make the smallest safe implementation consistent with the latest user instruction.

---

## 2. Game identity guardrails

The game should feel like:

- Dice are the center, not cosmetic RNG.
- Each turn asks: which die, which skill, which row/lane/order, which target, setup or payoff now?
- Builds change decisions, not only numbers.
- Complexity is readable through preview, tooltip, UI, animation, and feedback.
- PC combat clarity comes before mobile simplification.

Avoid:

- draw/discard deckbuilder structure;
- dice becoming irrelevant under stacked modifiers;
- heavy mental math every action;
- every build becoming generic damage scaling;
- hard counters that fully shut off a build;
- relics/passives becoming the only highlight while dice/enchant identity becomes secondary.

---

## 3. Architecture rules

Before adding code, locate the existing source of truth. Search symbols/references first; do not read unrelated files manually.

Rules:

- UI must not become a second gameplay-math source of truth.
- Preview, tooltip, and execution must not compute separate results.
- Prefer existing runtime, dice, skill, status, relic, AP, preview, inventory, reward, and UI systems over new managers.
- Prefer reusable data modules and stable ids/hooks over display-name checks.
- Do not revive deprecated/legacy systems unless explicitly asked.
- Keep new core code in real runtime folders, not demo/test folders.
- Do not rename/move scene, prefab, ScriptableObject, or asset references unless required and risk is explained.

Logic placement:

- Data/config -> existing ScriptableObject/data definitions.
- Runtime calculation -> combat/runtime systems.
- Preview -> preview/result-model systems.
- UI -> render state and feedback only.
- Exception mechanics -> existing behavior hooks/polymorphism if available.

---

## 4. Unity/C# coding rules

Follow existing repository conventions first. For new/refactored code, use these defaults.

Style and OOP:

- Keep self-written `.cs` files under **500 lines** when possible.
- Do not use `#region` to hide an overgrown class.
- One class = one main responsibility.
- Prefer composition over deep inheritance.
- Do not create abstractions for hypothetical future needs.
- Prefer clear Unity C# over clever architecture.
- Do not mix unrelated style cleanup into feature/bugfix tasks.

Naming:

- Class/file/method/property: `PascalCase`.
- Private field: `m_` + `camelCase`.
- Serialized field: `[SerializeField] private` + `m_` + `camelCase`.
- Constant/static readonly: `UPPER_SNAKE_CASE`.
- Local/parameter: `camelCase`.
- Interface: `IName`.
- Coroutine: `IE...`, e.g. `IEFadeIn()`.
- Button callback: `OnButton...`.
- Event callback: `OnNounVerbed()`.
- Event name: happened-state without `On`, e.g. `LoadingCompleted`.
- Unity refs: `m_btnConfirm`, `m_txtTitle`, `m_imgIcon`, `m_animPanel`, `m_objContainer`, `m_transformContent`, `m_canvasGroupPopup`.

Formatting and Unity safety:

- Always use braces for `if`, `else`, loops, and control statements.
- Member order: constants -> static fields -> serialized fields -> private fields -> properties -> events -> Unity lifecycle -> public methods -> private helpers -> callbacks -> cleanup.
- Do not rename/delete serialized fields casually.
- If renaming serialized fields, use `FormerlySerializedAs` and explain migration risk.
- Runtime code must not import `UnityEditor`.
- Editor code must live in an `Editor` folder or editor-only assembly.
- Pair event subscription/unsubscription with lifecycle, usually `OnEnable` / `OnDisable`.

---

## 5. Combat, dice, and value rules

Combat direction:

- Player phase starts, dice auto-roll, player may reorder dice, then casts skills immediately by clicking/dragging to valid targets.
- Skills consume available dice by true slot cost.
- Used dice stay visible but enter used state.
- End turn leads into enemy phase.
- Do not reintroduce old planning-lock/manual-assign flow unless explicitly asked.
- Basic actions remain available outside the main skill slots and must remain useful fallback actions.

Dice/value definitions:

- **Base Value** = actual rolled face identity.
- **Added Value** = bonus to final output; it does not change Base Value identity.
- Fixed numbers in skill text stay fixed unless marked as scalable/blue value by current data/spec.
- **Blue Value** = output allowed to receive valid Added Value/modifiers.
- Conditions normally read Base Value, not Added Value, unless text/spec says otherwise.
- Multi-dice checks use the skill's local consumed dice group.

Crit/Fail direction:

- Crit/Fail are determined from the die's actual rolled face identity.
- If max == min, Crit wins and there is no Fail.
- Crit adds bonus output/Added Value and does not change Base Value.
- Fail penalty applies once per skill action if at least one local consumed die fails.
- Multiple Fails do not stack unless a current spec explicitly says so.
- Fail should not delete unrelated Added Value sources.

Damage/Guard/Stagger direction:

- Guard blocks damage before HP and is not extra HP.
- Stagger is a real combat state, not only preview color.
- Stagger caused by the current hit must not boost that same hit unless the target was already Staggered before the action.

---

## 6. Preview, tooltip, and feedback rules

Preview must be produced by combat/preview systems, not improvised inside UI widgets.

Preview must account for relevant current modifiers: consumed dice, AP/dice cost, Base Value, Added Value, blue value, Crit, Fail, target state, HP, Guard, Stagger, statuses, buffs/debuffs, relics/passives, conditions, invalid reasons, and self/player effects.

Required behavior:

- Hover skill = resource + skill/dice output preview.
- Drag/target skill = final post-action state preview.
- Dice cost preview must show exactly which dice would be consumed.
- Condition highlight must check the exact dice consumed now.
- Invalid preview must explain why: missing AP, missing dice, invalid target, unmet condition, or blocked state.
- Status preview should show final stack/state and visually distinguish add/remove/consume.
- HP/Guard preview should show final post-action HP/Guard; Guard stays in shield/value layer.
- Value changes should show `old -> new` where useful.

Dice visuals:

- Active = usable.
- Used = visible but lowered/background-changed.
- Crit = visibly highlighted.
- Fail = visibly distinct.
- Value/enchant changes should be clear enough that the player understands why output changed.

---

## 7. Skill, status, relic, and reward guardrails

Skill grammar direction:

`Cost + Target + Effect + Optional Condition + Optional Payoff`

Every skill condition must declare scope: die, local dice group, slot position, target, board/combat state, or player state.

Prefer existing modules/data paths for damage, Guard, status apply/remove/consume/payoff, propagation/chaining, buff/debuff, utility/reorder/dice effects, and positional/row/lane logic.

Content rules:

- Rare should not just mean bigger numbers.
- New skills must create decisions about dice choice, order, row/lane, target priority, timing, resource, setup/payoff, or build direction.
- Intentional anti-synergy is allowed; not every skill should synergize with everything.
- Do not hard-code one new class per skill if existing data-driven modules can support it.
- Polish current content/system clarity before expanding huge pools.

Status identity:

- Physical: direct combat power and stronger Crit identity.
- Fire/Burn: stack then consume/payoff; not generic poison.
- Ice/Freeze/Chilled: tempo, delay, control, defensive reward.
- Lightning/Mark: direct-hit plus Mark payoff/chaining.
- Bleed: pressure, ticking, resource/tempo engine.
- Stagger: intermediate payoff state tied to Guard break/vulnerability.

Relic/dice/reward direction:

- Relics/passives may change rules, but dice and face/enchant identity should remain the highlight.
- Dice progression can include face value edits, face enchant edits, and whole-die identity/type if supported by current design.
- A face should not hold multiple different enchants unless current design explicitly allows it.
- Enchant preview/feedback must show player-facing changes when relevant.
- Run flow should support combat -> reward/shop/event/progression -> build edit -> harder combat.
- Rewards should shape builds, not just give generic power.
- Do not finalize open economy, pool size, boss, unlock, shop, event, or pacing details without explicit direction.

---

## 8. Refactor rules

Refactor only when it solves the task or reduces immediate risk.

- Preserve behavior unless the task asks to change behavior.
- Do not mix feature work with unrelated style cleanup.
- Do not move/rename prefab-facing assets casually.
- Do not break serialized schemas.
- Keep original MonoBehaviours if needed to preserve scene/prefab references.
- Extract helper/utility logic only around the responsibility being changed.
- If a file is already over 500 lines, avoid adding large unrelated logic and report that it needs a separate refactor.

Avoid touching stable flow unless the bug/task is clearly there: player phase flow, dice roll/reorder/used state, skill execution/consume path, row/lane/order mapping, existing preview result model, and scene/prefab references.

---

## 9. Workflow checklist and response format

Before editing:

- Read this AGENTS.md.
- Search relevant docs/specs by topic/content.
- Use Serena/symbol tools to find existing implementation and references.
- Identify source of truth for runtime, preview, UI, data, and assets.
- Explain the smallest safe plan if the task is non-trivial.

While editing:

- Edit only relevant files.
- Keep runtime and preview consistent.
- Preserve serialized fields and prefab/scene references.
- Prefer data-driven extension over duplicate systems.
- Avoid unrelated cleanup.

After editing, respond with:

```md
## Root Cause / Intent
- ...

## Files Changed
- `path/File.cs`: ...

## Implementation Summary
- ...

## Coding & OOP Check
- File size / responsibility / naming / serialization notes.

## Unity Risk
- Serialized fields, prefabs, scenes, ScriptableObjects affected or not.

## Validation
- Checked: ...
- Needs manual Unity test: ...
```
