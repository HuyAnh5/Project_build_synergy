# TODO_IMPLEMENTATION_STATUS.md

> **CURRENT SOURCE OF TRUTH — IMPLEMENTATION / FINALITY STATUS**  
> File này nói rõ phần nào đã là hướng design hiện tại, phần nào chưa làm, phần nào chưa final.  
> Nó giúp Codex/dev không hiểu nhầm rằng mọi thứ trong spec đều đã implement xong.

---

## 1. Current locked direction

Các điểm sau là hướng hiện tại và không nên đổi nếu không có chỉ đạo mới:

- Combat flow: **Player Phase → End Phase → Enemy Phase**.
- Dice tự roll ở đầu **Player Phase**.
- Player reorder dice, sau đó click/drag skill vào target để cast ngay.
- Dice consume theo thứ tự hiện tại từ trái sang phải.
- Dice used visual: hạ Y + đổi background, không biến mất.
- Resource chính: **AP**.
- Basic Attack / Basic Guard mặc định tag `None`, có thể được skill/relic đổi tag.
- Blue value mới nhận Added Value / modifier hợp lệ; số thường không scale.
- Element payoff là **skill-text driven**.
- Mark không có thời hạn; tồn tại đến khi hit/skill hợp lệ phá. Baseline: 1 hit hợp lệ phá Mark nếu không có rule khác.
- Relic nhặt được nhiều, hiển thị ở góc trên UI kiểu relic bar.
- Consumable groups: **Fate / Seal / Rune**.
- Access Type: **Melee / Range**.

---

## 1.5 Playtest risks / validation notes

Các điểm dưới đây không phải lỗi cần sửa ngay, nhưng phải được kiểm chứng bằng prototype/playtest trước khi mở rộng thêm spec:

- **Dice reorder depth**: 1–3 dice không tự động tạo chiều sâu. Skill/relic/enemy content phải tạo tension quanh thứ tự dice, giá trị dice, high/low, even/odd, crit/fail, multi-dice consumption, slot cost và timing.
- **Burn duration / batch visibility**: hiện chưa bắt buộc UI phải bung từng batch expiration. Theo dõi playtest xem player có bị thiếu thông tin khi quyết định consume/chờ Burn không.
- **Mark no-duration**: giảm memory load cho prototype, nhưng có thể thiếu urgency nếu enemy intent/content không tạo áp lực khai thác đúng lúc.
- **Basic Attack + AP**: Basic Attack không miễn phí vì consume dice slot. Nếu playtest cho thấy player spam Basic để ramp AP quá nhiều, mới giới hạn hoặc đổi rule.

### Balance numbers location

Exact tuning numbers sống trong game data / implementation nếu chúng còn volatile. Spec files mô tả rule direction, constraint và interaction logic; không copy toàn bộ số balance vào docs trừ khi số đó đã được khóa làm source of truth.

---

## 2. Combat core status

### Design status

- Combat core đã đủ rõ để handoff logic.
- Flow current đã nằm ở `COMBAT_CORE_SPEC.md`.
- Prototype combat lab scope đã nằm ở `COMBAT_LAB_RANDOM_LOADOUT_LOGIC_SPEC.md`.

### Implementation / polish chưa final

- Full targeting UX cuối cùng.
- VFX/SFX cụ thể cho used dice, cast, invalid target, status payoff.
- Data table số cuối cùng cho toàn bộ skill/enemy.
- Full exception rule của mọi skill content pool.

---

## 3. UI / UX / preview status

### Design status

- Chỉ giữ `COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md` làm UI preview source.
- `UX_UI_FEEDBACK_SPEC.md` giữ interaction/feedback tổng quát.
- Dice used visual hiện tại: hạ Y + đổi background.

### Chưa final

- Animation timing cuối cùng.
- Full art polish.
- Full layout responsive / mobile.
- Icon/VFX/SFX cuối cùng.

---

## 4. Element / status status

### Design status

- Fire hiện chủ yếu tương tác Burn.
- Ice hiện chủ yếu tương tác Freeze/Chilled.
- Lightning hiện chủ yếu tương tác Mark.
- Cross-element reaction là future design space, chưa phải locked rule.

### Chưa final

- Full catalog ailment.
- Full boss/enemy exception.
- Cross-element reaction như Ice + Burn, Fire + Freeze.
- Final icon/VFX/SFX/status tooltip.

---

## 5. Skill / relic content status

### Design status

- `SKILL_GRAMMAR_SPEC.md` giữ grammar chung.
- `SKILLS_PASSIVES_SPEC.md` giữ content list và content philosophy.
- Relic là passive-effect nhặt được nhiều trong run.

### Chưa final

- Không phải mọi skill trong content list đều đã balance xong.
- Rarity cuối cùng của một số skill/relic còn có thể đổi.
- Passive cũ cần được giảm sức mạnh nếu chuyển sang hệ nhặt nhiều.
- Một số wording skill cần rewrite theo blue value và skill-text payoff rule.

---

## 6. Consumable / Fate / Seal / Rune status

### Design status

- `Fate`: dice / roll / reload / reroll / face manipulation.
- `Seal`: combat direct aid / damage / status / cứu nguy.
- `Rune`: utility / buff / resource support.
- Consumable liên quan dice cần selected consumable + selected dice/face hợp lệ thì mới dùng được.

### Chưa implement / chưa final

- Data model thật cho consumable.
- Runtime inventory cho 3 shared consumable slot.
- Full use flow trong combat.
- Full Fate dice-edit flow ngoài combat / shop / loadout.
- Link cuối cùng giữa selected logical face và từng Fate cụ thể.
- Final pool tên gọi của toàn bộ consumable.

---

## 7. Map / Boss Preparation status

### Design status

- `MAP_STRUCTURE_AND_NAVIGATION_SPEC.md` giữ map graph, node, movement, backtrack.
- Boss Preparation clock nằm ở tầng map.
- Map quyết định player đi đâu, mất gì về route/time, và Preparation tăng như thế nào.

### Chưa implement / chưa final

- Full map UI.
- Full node art/icon.
- Final số Preparation cho từng node.
- Final rule backtrack / path cost nếu playtest yêu cầu đổi.
- Full tuning của Boss Preparation threshold.

---

## 8. Reward / run economy / event status

### Design status

- `RUN_ECONOMY_REWARD_EVENT_SPEC.md` giữ reward, shop, event, economy.
- Reward có thể gồm skill, relic, dice, consumable, gold/ore hoặc event package.
- Shop/economy là run-level system, không nằm trong combat core.

### Chưa implement / chưa final

- Full reward pool.
- Full shop UI và reroll tuning.
- Final economy numbers.
- Full event catalog.
- Final relation giữa Ore / dice progression / map node nếu playtest yêu cầu đổi.

---

## 9. Enemy / boss status

### Design status

- `ENEMIES_BOSSES_ENCOUNTERS_SPEC.md` giữ enemy roles, intent, encounter pressure, boss philosophy.
- Boss-specific modifier dùng Boss Preparation nằm ở enemy/boss spec, không nằm trong run economy.

### Chưa implement / chưa final

- Full enemy roster.
- Final boss kit.
- Final hidden boss direction.
- Boss-specific Preparation modifier tuning.
- Full encounter table.

---

## 10. Final package rule

Bộ docs này là **current design source package**, không phải bằng chứng rằng toàn bộ game đã code xong.

Khi Codex/dev cập nhật:

1. Đọc `CODEX_UPDATE_GUARDRAILS.md` trước.
2. Đọc `PROJECT_META.md` để biết file nào giữ source of truth.
3. Sửa file chuyên trách, không copy rule chi tiết sang nhiều file.
4. Nếu phát hiện rule cũ, xóa hoặc chuyển vào mục legacy rõ ràng.
5. Nếu một feature chưa implement, cập nhật file này thay vì giả vờ nó đã hoàn thành.
