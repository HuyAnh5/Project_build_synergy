# CODEX_UPDATE_GUARDRAILS

> File này là **guardrail cho Codex / AI / dev** khi cập nhật tài liệu hoặc code của project.  
> Nó không phải gameplay spec chi tiết.  
> Mục tiêu của file này là nói rõ: **được cập nhật như thế nào, không được hồi sinh rule cũ nào, và khi gặp mâu thuẫn thì phải sửa ở đâu.**

---

## 0. Quy tắc đọc đầu tiên

Trước khi sửa bất kỳ file nào, Codex phải hiểu:

```text
Docs hiện tại đang được dọn về một bộ source of truth mới.
Không được coi các file draft / legacy / change note cũ là rule hiện tại.
```

Nếu có mâu thuẫn giữa file guardrail này và một file spec cũ, **ưu tiên guardrail này để biết rule nào đã bị loại bỏ**.  
Sau đó cập nhật lại file spec chuyên trách cho đúng.

---

## 1. Mục tiêu cập nhật docs

Mục tiêu không phải là xóa mọi câu lặp lại 100%.

Mục tiêu đúng là:

```text
Không có 2 file cùng giữ source of truth cho cùng 1 rule chi tiết.
Không có rule cũ và rule mới cùng tồn tại như đều hợp lệ.
Không có thuật ngữ cũ làm dev hiểu sai gameplay hiện tại.
```

Một số file được phép nhắc cùng một ý ở cấp độ khác nhau:

- `GDD_VISION.md` nói ở cấp vision / pillar.
- `COMBAT_CORE_SPEC.md` nói rule combat chi tiết.
- `PROJECT_META.md` nói file nào là source of truth.
- `UX_UI_FEEDBACK_SPEC.md` nói UX tổng quát.
- `COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md` nói UI preview behavior / handoff cụ thể.

Nhưng chỉ **một file chuyên trách** được giữ rule chi tiết cuối cùng.

---

## 1.5 Guardrail chống over-documentation

Sau khi sửa contradiction / terminology / source-of-truth conflict, ưu tiên tiếp theo là **build prototype và playtest**, không phải mở rộng thêm hệ thống mới trên giấy.

Codex không được tự ý thêm system mới như cross-element reaction, relic economy mới, map layer mới, boss subsystem mới, hoặc UI subsystem mới nếu user không yêu cầu rõ. Nếu gặp chỗ chưa final, hãy ghi là `Playtest Risk` hoặc `Future Design Space`, không biến nó thành rule locked.

### Balance numbers

Nếu thiếu số cụ thể trong docs nhưng số đó đang nằm trong game data / implementation, không tự bịa bảng số mới. Chỉ thêm note:

```text
Exact tuning numbers live in game data / implementation unless explicitly locked in docs.
```

### Relic / Consumable terminology

- `Relic` = passive-effect collection, nhặt được nhiều, hiển thị ở góc trên UI.
- `Consumable` = Fate / Seal / Rune.
- Không được dùng `Relic` để chỉ one-shot consumable.

---

## 2. Các file không được hồi sinh

Không được đưa lại các file sau vào bộ source chính:

```text
COMBAT_CHANGES_2026.md
COMBAT_UI_PREVIEW_FINAL_ORDERED_SPEC.md
```

Lý do:

- `COMBAT_CHANGES_2026.md` chỉ là draft / transition note cũ.
- Nội dung đúng trong đó phải được nhập vào spec chuyên trách.
- Nội dung sai / lỗi thời trong đó phải bị bỏ.
- `COMBAT_UI_PREVIEW_FINAL_ORDERED_SPEC.md` đã bị loại vì chỉ giữ một file UI preview chính.

File UI preview chính hiện tại là:

```text
COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md
```

Nếu cần giữ checklist/test case tốt từ file UI cũ, hãy nhập vào `COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md`, không tạo lại file phụ.

---

## 3. Combat flow hiện tại

Flow hiện tại phải là:

```text
Player Phase → End Phase → Enemy Phase
```

Trong đó:

```text
Đầu Player Phase:
- dice tự roll
- player đọc kết quả roll
- player reorder dice nếu muốn
- player bấm chọn skill hoặc kéo thả skill vào target
- nếu hợp lệ, skill cast ngay
- dice được dùng chuyển sang used state
- player có thể tiếp tục dùng skill nếu còn tài nguyên / dice hợp lệ
- player bấm End Turn để sang Enemy Phase
```

Không còn `Roll Phase` riêng.  
Dice roll là hành vi đầu `Player Phase`, không phải một phase độc lập trong docs design.

Không được viết lại flow theo kiểu:

```text
Roll Phase → Planning Phase → Execution Phase
```

Không được viết:

```text
Roll Dice → Equip skill vào planning board → Lock plan → Execute
```

---

## 4. Những rule combat cũ bị cấm hồi sinh

Các khái niệm sau là legacy và không được mô tả như rule hiện tại:

```text
Planning Phase
Execution Phase
Roll Phase riêng
Planning board
Lock planning
Assign die thủ công vào skill
Equip skill vào board trước khi cast
Skill planned queue
Dice biến mất khỏi hàng dice sau khi dùng
Dice bị dim 50% như visual chính thức
1 passive slot
Focus
Mana
Strike
Zodiac
```

Nếu cần nhắc đến chúng để giải thích lịch sử, phải đặt trong mục:

```text
Legacy / Removed Behavior
```

và ghi rõ:

```text
Không còn là rule hiện tại.
```

---

## 5. Dice used visual state hiện tại

Dice sau khi dùng **không biến mất**.

Rule hiện tại:

```text
Khi dice được dùng:
- dice vẫn còn hiển thị trên UI
- dice hạ Y xuống một chút
- background / visual state đổi sang used
- dice đó không còn available để skill consume tiếp
```

Khi dice được refresh bởi turn mới hoặc consumable/effect:

```text
- dice nâng Y về vị trí active
- background / visual state trở lại bình thường
- dice trở lại available nếu rule refresh cho phép
```

Không được viết:

```text
Dice đã dùng biến mất khỏi hàng dice.
Turn mới nạp lại dice bằng cách spawn lại toàn bộ hàng dice.
```

Nếu có animation “reload”, nó chỉ là presentation của việc refresh dice, không có nghĩa dice đã biến mất khỏi UI.

---

## 6. Resource naming hiện tại

Tên tài nguyên hiện tại là:

```text
AP
```

Không dùng các tên cũ trong rule chính:

```text
Focus
Mana
AP/Focus
Focus/Mana
```

Nếu code vẫn còn biến / class tên `Focus`, phải ghi rõ đó là tên implementation legacy.  
Trong docs player-facing và gameplay spec hiện tại, dùng `AP`.

Ví dụ sửa đúng:

```text
Skill cost: 2 AP
Start combat: 2 AP
Turn start: +1 AP
Max AP: 9
```

Không viết:

```text
Skill cost: 2 Focus
Focus/Mana cost
```

---

## 7. Relic rule hiện tại

Passive cũ đã chuyển thành hướng relic-like.

Rule hiện tại:

```text
Relic có thể nhặt nhiều cái.
Relic hiển thị dạng icon ở góc trên UI, giống Slay the Spire và các game cùng genre.
Relic là passive effect của run.
Vì có thể nhặt nhiều, mỗi relic phải yếu / hẹp hơn passive cũ.
```

Không còn rule:

```text
1 passive slot
chỉ được mang 1 passive vào combat
passive là một slot loadout duy nhất
```

Nếu gặp cụm:

```text
1 passive slot
passive slot
passive random
passive reward
```

thì phải cập nhật thành:

```text
Relic collection
Relic reward
Relic effect
Relic pool
```

Tuy nhiên, nếu code hiện còn tên `PassiveSystem`, có thể giữ tên code tạm thời nhưng phải ghi rõ:

```text
Implementation legacy name. Gameplay-facing system is Relic.
```

---

## 8. Consumable groups hiện tại

Ba nhóm consumable hiện tại là:

```text
Fate
Seal
Rune
```

### Fate

Fate là nhóm can thiệp dice / roll / face / outcome.

Bao gồm các hành vi như:

```text
reroll dice
reload dice
refresh used dice
choose face
adjust face value
copy / paste face
enchant face
temporary dice mutation
permanent dice sculpt
```

Không dùng tên cũ `Zodiac` trong rule hiện tại.

### Seal

Seal là nhóm combat aid trực tiếp:

```text
damage
status interaction
combat rescue
status spread
single-use tactical effect
```

### Rune

Rune là nhóm utility / support:

```text
AP restore
buff
cleanse
reroll support
resource utility
combat smoothing
```

Nếu gặp `Zodiac`, hãy đổi thành `Fate`, trừ khi đang nói về lịch sử cũ.

---

## 9. Melee / Range naming

Access type hiện tại chỉ dùng:

```text
Melee
Range
```

Không dùng `Strike` làm thuật ngữ chính nữa.

Rule row hiện tại:

```text
Melee bị front row chặn.
Range có thể target theo rule range của skill.
```

Nếu file cũ dùng `Strike`, hãy đổi sang `Melee`.

Nếu cần ghi chú migration:

```text
Legacy term: Strike. Current term: Melee.
```

---

## 10. Blue value / fixed number rule

Đây là rule số học hiện tại:

```text
Số thường = fixed number.
Số xanh = scalable output / blue value.
```

### Số thường

Số thường không bị ảnh hưởng bởi:

```text
dice
crit
fail
Added Value
position
condition modifier
relic
skill buff
face enchant
```

Trừ khi text skill ghi rất rõ một ngoại lệ.

### Số xanh

Số xanh có thể bị tác động bởi Added Value và modifier hợp lệ.

Số xanh có thể là:

```text
damage
guard
burn
bleed
heal
AP
status amount
other numeric output
```

Miễn là skill định nghĩa output đó là blue value.

Ví dụ:

```text
Deal 4 damage.
```

`4` là số thường, không cộng Added Value.

```text
Deal "4 damage".
```

`"4 damage"` là blue value, có thể cộng Added Value.

Không được dùng rule cũ:

```text
Mọi output số học mặc định cộng Added Value.
```

Không được dùng rule cũ:

```text
Nếu skill có nhiều output, Added Value mặc định cộng vào output đầu tiên.
```

Rule hiện tại là:

```text
Added Value chỉ cộng vào output được đánh dấu là blue value.
```

---

## 11. Crit / Fail và Added Value

Crit có thể tạo Added Value.

Fail không phải là Added Value âm.

Rule đúng:

```text
Crit tạo Added Value theo rule của action.
Fail là penalty / modifier lên blue output hoặc base output theo rule skill.
Fail không được mô tả như nguồn Added Value âm.
```

Nếu docs cũ nói Fail “đọc Added Value” hoặc “trừ Added Value”, phải sửa lại.

---

## 12. Basic Attack / Basic Guard hiện tại

Basic Attack và Basic Guard mặc định:

```text
Element: None
Tag: None
```

Chúng có thể được skill / relic / effect đổi tag hoặc element tạm thời.

Không được mặc định viết:

```text
Basic Attack là Physical.
Basic Guard là Neutral/Guard tag cố định.
```

Nếu một skill/relic muốn biến Basic Attack thành Fire/Physical/etc., phải ghi trong text của skill/relic đó.

---

## 13. Element payoff hiện tại

Không còn auto elemental payoff ở rule nền.

Rule hiện tại:

```text
Element payoff phải nằm trong skill text.
```

Ví dụ:

```text
Fire không tự động consume Burn chỉ vì là Fire hit.
Ice không tự động +AP / +Guard chỉ vì đánh vào Freeze/Chilled.
Lightning không tự động tạo mọi payoff nếu skill không ghi.
```

Tuy nhiên identity hiện tại vẫn là:

```text
Fire chủ yếu tương tác với Burn.
Ice chủ yếu tương tác với Freeze / Chilled.
Lightning chủ yếu tương tác với Mark.
Bleed chủ yếu tương tác với Bleed.
Physical chủ yếu là direct hit / anti-Guard / crit.
```

Hiện tại chưa có cross-element reaction chính thức.

Không được thêm rule kiểu:

```text
Ice + Burn = Bỏng Lạnh
Fire + Freeze = reaction mới
```

trừ khi user chốt rõ trong tương lai.

Nếu muốn ghi hướng mở, chỉ ghi:

```text
Cross-element reactions are future design space, not current locked rule.
```

---

## 14. Burn hiện tại

Burn là status/resource của Fire, nhưng không tự động bị consume bởi mọi Fire hit.

Rule hiện tại:

```text
Skill nào consume Burn thì phải ghi rõ trong skill text.
Skill consume Burn có thể deal 0 / 1 / 2 / 3 damage mỗi Burn tùy skill.
Burn không có baseline consume damage chung áp cho mọi skill.
```

Không được viết:

```text
Fire direct hit vào target có Burn luôn consume Burn theo baseline.
Consume baseline = +2 damage mỗi Burn cho toàn hệ.
```

trừ khi đó là text của một skill cụ thể.

---

## 15. Ice hiện tại

Ice không còn auto payoff chung.

Rule hiện tại:

```text
Skill nào cho +AP thì ghi +AP.
Skill nào cho +Guard thì ghi +Guard.
Skill nào khai thác Freeze / Chilled thì ghi rõ payoff.
```

Không được viết:

```text
Ice damage hit vào target Freeze/Chilled luôn cho +1 AP +3 Guard.
```

hoặc:

```text
Ice hit Freeze/Chilled luôn cho reward chung.
```

trừ khi đó là text của skill cụ thể.

---

## 16. Mark hiện tại

Mark hiện tại:

```text
Mark không stack.
Mark không có thời hạn.
Mark không tự hết theo turn.
Mark tồn tại mãi cho đến khi bị phá.
Baseline hiện tại: 1 hit hợp lệ phá Mark, trừ khi skill/relic ghi khác.
```

Không được viết mơ hồ:

```text
Remove / preserve Mark theo timing rule chưa rõ.
```

Phải ghi rõ khi nào Mark bị phá và bởi loại hit / skill nào.

Nếu một skill muốn giữ Mark lại, text skill phải ghi.

Nếu một relic muốn Mark cần 2 hit mới phá, text relic phải ghi.

---

## 17. UI Preview source of truth

Chỉ một file UI preview chính:

```text
COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md
```

File này giữ:

```text
HP preview
Guard preview
Stagger preview
status preview
AP preview
dice preview
skill hover preview
target preview
invalid preview
test cases
implementation checklist
```

Không tạo lại file UI preview thứ hai.

Nếu cần thêm test case, thêm vào `COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md`.

---

## 18. UX vs logic split

Không để UX file trở thành gameplay source of truth.

### `RELICS_AND_DICE_PROGRESSION_SPEC.md`

Giữ logic:

```text
Consumable groups: Fate / Seal / Rune
Relic system
Dice progression
Dice edit effect logic
Target requirement của Fate
Permanent vs temporary dice change
```

### `UX_UI_FEEDBACK_SPEC.md`

Giữ UX / interaction:

```text
hover
selected state
Use button gating
Confirm gating
dice + consumable must both satisfy selection context
why button disabled
visual feedback for selected object
```

Không copy toàn bộ effect logic của Fate sang UX file.

---

## 19. Map / Economy / Boss split

### `MAP_STRUCTURE_AND_NAVIGATION_SPEC.md`

Giữ:

```text
map graph
node type
movement
backtrack
Boss Preparation clock
which node increases Preparation
Preparation threshold chung
Boss Intel ở map layer nếu liên quan route/node
```

### `RUN_ECONOMY_REWARD_EVENT_SPEC.md`

Giữ:

```text
reward gacha
shop economy
event reward package
gold
ore
relic reward
skill reward
consumable reward
Fate / Seal / Rune reward purpose
```

### `ENEMIES_BOSSES_ENCOUNTERS_SPEC.md`

Giữ:

```text
boss-specific Preparation modifier
Dragon / Knight / boss behavior cụ thể
enemy pressure
boss mechanic wall
hidden boss / endless combat pressure
```

Không để cùng một boss-specific modifier vừa nằm ở Map vừa nằm ở Enemy/Boss như source truth ngang nhau.

---

## 20. Khi cập nhật file, phải làm theo quy trình này

Khi user chốt một rule mới:

1. Xác định file chuyên trách.
2. Cập nhật rule chi tiết vào file chuyên trách.
3. Nếu file khác cần nhắc, chỉ nhắc ở cấp summary và trỏ về file chuyên trách.
4. Xóa hoặc sửa rule cũ mâu thuẫn.
5. Không để legacy rule tồn tại như một lựa chọn hợp lệ.
6. Nếu thuật ngữ code cũ vẫn cần giữ vì class name, ghi rõ đó là implementation legacy name.
7. Chạy tìm kiếm các cụm cũ dễ gây lỗi.

Các cụm phải search sau mỗi pass cleanup:

```text
COMBAT_CHANGES_2026
COMBAT_UI_PREVIEW_FINAL_ORDERED
2026
Focus
Mana
1 passive slot
passive slot
Roll Phase
Planning Phase
Execution Phase
planning board
lock planning
assign die
Strike
Zodiac
dice disappear
dice biến mất
biến mất khỏi hàng dice
```

Nếu các cụm này xuất hiện trong file current source, phải kiểm tra:

- nó nằm trong mục legacy/removed hay không;
- nếu không, phải sửa.

---

## 21. Validation report nếu có

Nếu tạo validation report, không được ghi kiểu mơ hồ:

```text
Focus: OK
Mana: OK
```

Vì dễ hiểu nhầm là thuật ngữ đó vẫn hợp lệ.

Phải ghi rõ:

```text
Focus: not found in current specs; replaced by AP.
Mana: not found in current specs; replaced by AP.
1 passive slot: not found; replaced by relic collection.
Roll Phase: not found as standalone phase; dice now auto-roll at Player Phase start.
```

Hoặc tốt hơn: không đưa validation report vào docs package cuối nếu nó chỉ phục vụ kiểm tra tạm thời.

---

## 22. Cách sửa khi gặp lỗi replace máy móc

Nếu gặp các câu kiểu:

```text
skill / relic / dice / relic
relic / relic
chỉnh skill/relic/dice/relic
```

Phải sửa lại bằng tay.

Cách viết đúng thường là:

```text
skill / relic / dice / consumable
```

hoặc:

```text
skill / relic / dice / Fate / Seal / Rune
```

Không dùng replace hàng loạt mà không đọc lại câu.

---

## 23. Không tự đóng những vùng chưa final

Nếu user chưa chốt, không được tự biến thành final rule.

Các vùng vẫn có thể mở:

```text
full enemy roster
full boss roster
exact reward rate final
final relic pool
final Fate / Seal / Rune pool
cross-element reactions
full unlock tree
mobile layout
final art / VFX / SFX
```

Nếu cần ghi, dùng nhãn:

```text
Current direction
Open design space
Not locked yet
Prototype-only
```

Không ghi như “Rule đã chốt” nếu user chưa chốt.

---

## 24. File header nên dùng

Mỗi file source chính nên có header rõ:

```text
> CURRENT SOURCE OF TRUTH for: [scope]
> This file owns: [rules]
> This file does not own: [out of scope]
```

File không còn source chính phải có header:

```text
> ARCHIVED / LEGACY NOTE
> Do not use as current rule source.
```

Nếu file bị loại khỏi zip/source package thì tốt hơn không đưa vào.

---

## 25. Tóm tắt cực ngắn cho Codex

Nếu chỉ nhớ 10 điều:

1. Không hồi sinh `COMBAT_CHANGES_2026.md`.
2. Combat flow hiện tại là `Player Phase → End Phase → Enemy Phase`.
3. Dice tự roll đầu `Player Phase`; không còn `Roll Phase` riêng.
4. Không còn planning/lock/execute/assign die thủ công.
5. Dice used không biến mất; nó hạ Y + đổi background.
6. Resource là `AP`, không dùng Focus/Mana.
7. Relic nhặt được nhiều; không còn 1 passive slot.
8. Consumable groups là `Fate / Seal / Rune`, không dùng Zodiac.
9. Added Value chỉ cộng vào blue value, không cộng mọi output và không mặc định output đầu tiên.
10. Element payoff nằm trong skill text; không còn auto payoff nền.
