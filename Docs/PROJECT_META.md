# PROJECT_META.md

> **CURRENT SOURCE OF TRUTH — DOCUMENT MAP / HANDOFF**  
> File này không phải gameplay spec chi tiết. Nó chỉ cho biết bộ tài liệu hiện tại được chia như thế nào, file nào giữ quyền quyết định cho mảng nào, và Codex/dev nên đọc/sửa theo thứ tự nào.

---

## 1. Rule đọc quan trọng

Khi có mâu thuẫn:

1. File spec chuyên trách của đúng mảng thắng.
2. `GDD_VISION.md` thắng nếu câu hỏi là pillar / hướng đi / anti-pillar.
3. `PROJECT_META.md` chỉ thắng ở chuyện file map, ownership và cách đọc tài liệu.
4. `CODEX_UPDATE_GUARDRAILS.md` thắng khi câu hỏi là rule cũ nào **không được hồi sinh**.

Không dùng file draft / change note cũ làm source hiện tại.

---

## 2. Core direction hiện tại

Game hiện là **Dice-driven Tactical Combat Roguelike**.

Combat flow hiện tại:

```text
Player Phase → End Phase → Enemy Phase
```

Trong `Player Phase`:

```text
Dice tự roll đầu phase
→ player reorder dice nếu muốn
→ player click skill rồi chọn target hoặc drag skill vào target
→ nếu hợp lệ, skill cast ngay
→ dice được dùng chuyển sang used visual state
→ player tiếp tục cast nếu còn AP / dice / target hợp lệ
→ player bấm End Turn
```

Resource chính là **AP**.

Dice used visual hiện tại:

```text
Dice không biến mất.
Dice hạ Y xuống một chút + đổi background.
Dice đó không còn available cho cast tiếp theo.
Khi refresh, dice nâng Y lên lại + background active.
```

Relic hiện tại:

```text
Relic là passive-effect nhặt được trong run.
Player có thể nhặt nhiều relic.
Relic hiển thị ở góc trên UI kiểu STS-like relic bar.
Vì có thể có nhiều relic, từng relic phải nhỏ / hẹp hơn passive cũ.
```

Consumable group hiện tại:

```text
Fate = dice / roll / reload / reroll / face manipulation
Seal = combat aid / damage / status / cứu nguy trực tiếp
Rune = utility / buff / resource support
```

Element payoff hiện tại:

```text
Payoff là skill-text driven.
Skill nào consume Burn, khai thác Freeze/Chilled, phá Mark, spread Mark hoặc tạo payoff từ status thì phải ghi rõ trong skill text.
Không có rule nền tự động áp cho mọi skill cùng hệ.
```

---

## 3. Source of truth theo file

### Direction / vision

- `GDD_VISION.md`
  - Pillars, anti-pillars, core identity, high-level loop.
  - Không giữ combat rule chi tiết.

### Combat / runtime rule

- `COMBAT_CORE_SPEC.md`
  - Turn flow, Player Phase, End Phase, Enemy Phase.
  - AP, dice pipeline, dice consume, blue value rule, damage/guard/stagger.
  - Click/drag skill cast ngay.

- `ROW_POSITIONING_SPEC.md`
  - Front row / back row.
  - Access Type: Melee / Range.
  - Target legality, row AoE, cross-row exception.

### Element / status

- `ELEMENTS_STATUS_SPEC.md`
  - Fire/Burn, Ice/Freeze/Chilled, Lightning/Mark, Bleed, status identity.
  - Element payoff theo skill text.
  - Mark tồn tại vô hạn cho đến khi hit/skill hợp lệ phá.

### Skill / content grammar

- `SKILL_GRAMMAR_SPEC.md`
  - Grammar chung: condition, scope, blue value, Added Value, effect module.
  - Đây là nơi định nghĩa cách viết skill mới.

- `SKILLS_PASSIVES_SPEC.md`
  - Content list: skill, relic, combo engine, rarity/content direction.
  - Không giữ grammar chung nếu grammar đó đã nằm ở `SKILL_GRAMMAR_SPEC.md`.

### Loadout / build / relic / consumable

- `LOADOUT_AND_BUILD_SPEC.md`
  - Skill slots, basic actions, relic collection, build identity.

- `RELICS_AND_DICE_PROGRESSION_SPEC.md`
  - Relic framework, dice customization, dice progression.
  - Consumable groups: Fate / Seal / Rune.
  - Fate/dice-edit selection flow ở mức logic/progression.

### UX / UI preview

- `UX_UI_FEEDBACK_SPEC.md`
  - UX interaction, selected state, Use/Confirm, feedback priority.

- `COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md`
  - Combat preview behavior cụ thể.
  - Chỉ giữ file này làm UI preview source; không dùng thêm file UI preview phụ.

### Run / map / economy / enemy

- `MAP_STRUCTURE_AND_NAVIGATION_SPEC.md`
  - Map graph, node, movement, backtrack, Boss Preparation clock ở tầng map.

- `RUN_ECONOMY_REWARD_EVENT_SPEC.md`
  - Reward, shop, event, economy, run-level progression.

- `ENEMIES_BOSSES_ENCOUNTERS_SPEC.md`
  - Enemy roles, intent, encounter pressure, boss philosophy.
  - Boss-specific modifier dùng Boss Preparation.

### Prototype / implementation status / guardrail

- `COMBAT_LAB_RANDOM_LOADOUT_LOGIC_SPEC.md`
  - Prototype combat lab scope.

- `TODO_IMPLEMENTATION_STATUS.md`
  - Cái gì đã nằm trong design, cái gì chưa làm/chưa final.

- `CODEX_UPDATE_GUARDRAILS.md`
  - Rule cập nhật cho Codex/dev.
  - Những thuật ngữ cũ không được hồi sinh.

---

## 4. Các file đã loại khỏi source package

Không đưa các file sau quay lại package hiện tại:

```text
COMBAT_CHANGES_2026.md
COMBAT_UI_PREVIEW_FINAL_ORDERED_SPEC.md
VALIDATION_REPORT.md
```

Lý do:

- `COMBAT_CHANGES_2026.md` là draft chuyển hướng, nội dung đúng đã nhập vào spec chính.
- `COMBAT_UI_PREVIEW_FINAL_ORDERED_SPEC.md` đã được merge phần hữu ích vào `COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md`.
- `VALIDATION_REPORT.md` dễ gây hiểu nhầm vì ghi `OK` cạnh các thuật ngữ legacy.

---

## 5. Không lặp source of truth

Được phép nhắc lại cùng một ý ở các cấp khác nhau:

- `GDD_VISION` nói hướng đi.
- `COMBAT_CORE` nói rule combat.
- `UX_UI` nói cảm nhận/interaction.
- `COMBAT_UI_PREVIEW` nói preview behavior.

Không được để hai file cùng giữ rule chi tiết mâu thuẫn nhau.

Nếu một rule chi tiết đang bị lặp ở nhiều file, hãy:

1. chọn file chuyên trách,
2. giữ rule đầy đủ ở đó,
3. ở file còn lại chỉ để link/nhắc ngắn,
4. không copy lại toàn bộ rule.

---

## 6. Package này dùng để làm gì

Bộ file này đủ để:

- hiểu hướng game,
- hiểu combat flow hiện tại,
- hiểu status/payoff direction,
- hiểu loadout/relic/consumable direction,
- hiểu map/reward/boss preparation ở mức design,
- giao Codex/dev tiếp tục cập nhật docs/code mà không hồi sinh rule cũ.

Bộ file này **không khẳng định** mọi hệ thống đã implement xong. Những phần chưa làm/chưa final nằm trong `TODO_IMPLEMENTATION_STATUS.md`.

Exact tuning numbers có thể nằm trong game data / implementation nếu chúng còn thay đổi liên tục. Không tự copy hoặc bịa số balance vào docs trừ khi user chốt chúng là source of truth.
