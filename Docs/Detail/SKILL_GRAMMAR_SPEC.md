# SKILL_GRAMMAR_SPEC.md

> Tài liệu này là **source of truth cho ngữ pháp thiết kế skill**.  
> Mục tiêu của file là chuẩn hóa cách định nghĩa **condition**, **scope**, **nguồn số**, **effect module** và **quy tắc ghép skill** để sau này thêm skill mới không phải tạo một luật riêng hoặc code tay lại từ đầu.

---

## 1. Mục tiêu của file

File này tồn tại để:

- chuẩn hóa các **condition** mà skill được phép đọc,
- chuẩn hóa các **effect module** mà skill được phép ghép,
- làm rõ skill đọc từ **Base Value** hay **Added Value**,
- chốt rõ **scope** của một condition: die đơn, local group, target, toàn board, player state,
- tạo chung một “grammar” để thiết kế skill, passive và tooltips,
- làm nền cho hệ code generic sau này.

---

## 2. Phạm vi file này

File này bao gồm:

- glossary dùng chung cho skill logic,
- source value model,
- scope model,
- condition taxonomy,
- effect module taxonomy,
- composition rules,
- execution order ở cấp skill,
- checklist khi tạo skill mới,
- ví dụ chuẩn hóa một số skill hiện có.

File này không đi sâu vào:

- full content pool của toàn bộ skill,
- rarity cuối cùng của mọi skill,
- balance number cuối cùng,
- class / code implementation cụ thể,
- UI polish cuối cùng.

---

## 3. Từ điển nền tảng

### 3.1 Base Value

Là giá trị thật của mặt dice vừa roll.  
Mọi condition của skill phải ưu tiên đọc từ **Base Value**, trừ khi text ghi rất rõ là đọc từ một giá trị khác.

### 3.2 Added Value

Là giá trị cộng thêm vào output của action.  
Added Value **không đổi bản chất của Base Value**.

Ví dụ:
- Base Value = 4, Added Value = +3  
→ skill vẫn phải coi die này là **4**, không phải 7.

### 3.3 Crit / Fail

- **Crit**: roll ra mặt cao nhất của chính die đó
- **Fail**: roll ra mặt thấp nhất của chính die đó

Crit / Fail luôn được xác định theo **die riêng lẻ**, không theo các die khác đang equip.

### 3.4 Local Group

Là cụm dice mà một skill đang chiếm.  
Ví dụ skill 2 slot chỉ đọc 2 die trong cụm của nó, không đọc toàn bộ 3 die trên board.

### 3.5 Runtime Packet

Là gói dữ liệu skill dùng ở lúc execution, gồm:

- skill definition,
- die context,
- player state,
- target state,
- lane/order context,
- passive modifiers,
- effect modules cần resolve.

### 3.6 Delivery Pattern

Là cách skill phân phối tác động:

- Single Target
- Multi Target / AoE
- Self
- Chain / Propagate
- Split by Slot Position
- Multi-step / Sequence

---

## 4. Source model — skill được phép đọc gì

Một skill chỉ nên đọc dữ liệu từ các nguồn hợp lệ sau:

### 4.1 Die Source

- Base Value của từng die
- Added Value của từng die hoặc của action
- Crit / Fail của từng die
- Giá trị cao nhất / thấp nhất trong local group
- parity: chẵn / lẻ
- threshold: <= / >= / = một giá trị cụ thể
- vị trí slot trong cụm: trái / giữa / phải

### 4.2 Player State Source

- Focus hiện tại
- Guard hiện tại
- HP hiện tại hoặc HP đã mất (nếu skill ghi rõ)
- Basic Attack / Basic Guard đã dùng hay chưa trong lượt/combat
- số action còn lại trong turn (chỉ khi skill thật sự cần)

### 4.3 Target State Source

- Target có Burn / Mark / Freeze / Chilled / Bleed hay không
- số stack hiện tại của status đó
- target có Guard hay không
- target có Stagger hay không
- target có ailment hay không

### 4.4 Board State Source

- số lượng enemy còn sống
- số lượng mục tiêu đang có một status cụ thể
- lane / vị trí mục tiêu
- toàn bộ board có bao nhiêu Mark, Bleed, Burn...

### 4.5 Combat Metadata Source

- số định mệnh / combat seed rule riêng
- turn index nếu một skill đặc biệt thật sự cần đọc
- encounter-specific law nếu boss law ghi rõ

> Guardrail: skill thường **không nên** đọc quá nhiều nguồn cùng lúc.  
> Một skill dễ đọc thường chỉ cần 1 nguồn chính + 1 nguồn phụ.

---

## 5. Scope model — condition đang nhìn ở phạm vi nào

Condition phải ghi rõ scope. Đây là phần rất quan trọng để tránh code sai.

### 5.1 Die Scope

Đọc đúng 1 die đang gắn cho action đó.

Ví dụ:
- `Base Value = 7`
- `Base Value <= 3`
- `die này Crit`

### 5.2 Local Group Scope

Đọc trong cụm die mà skill đang chiếm.

Ví dụ:
- `giá trị cao nhất trong 2 die của skill này`
- `giá trị thấp nhất trong 3 die của skill này`
- `mọi die trong cụm đều = 7`

### 5.3 Slot Position Scope

Đọc theo vị trí trái / giữa / phải trong cụm.

Ví dụ:
- `slot trái gây damage`
- `slot phải cho Guard`
- `slot giữa quyết định AoE scalar`

### 5.4 Target Scope

Đọc từ mục tiêu chính của skill.

Ví dụ:
- `target có Burn`
- `target đang Chilled`
- `target đã có Mark trước khi hit`

### 5.5 Board Scope

Đọc trên toàn bộ board.

Ví dụ:
- `mỗi enemy có Mark`
- `tổng Bleed trên toàn bộ kẻ địch`
- `combat có từ 3 enemy trở lên`

### 5.6 Player Scope

Đọc từ state của player.

Ví dụ:
- `Guard hiện tại`
- `Focus hiện tại`
- `đòn Basic Attack đầu tiên mỗi combat`

---

## 6. Condition taxonomy — các condition chuẩn được phép dùng

Mỗi skill thường chỉ nên có **1 condition chính**. Rare rất đặc biệt mới nên có 2 condition đáng kể.

### 6.1 Identity Conditions (đọc bản chất của die)

- `Crit`
- `Fail`
- `Base Value is Even`
- `Base Value is Odd`
- `Base Value = N`
- `Base Value <= N`
- `Base Value >= N`
- `Base Value is highest face of that die`
- `Base Value is lowest face of that die`

### 6.2 Local Group Conditions

- `Highest in Local Group`
- `Lowest in Local Group`
- `All Dice in Group = N`
- `Any Die in Group = N`
- `Sum of Group >= N`
- `Exactly 2 dice in group are even`
- `highest - lowest >= N`

### 6.3 Positional Conditions

- `Left Slot`
- `Middle Slot`
- `Right Slot`
- `Left die > Right die`
- `Right die is even`
- `center die determines payoff`

### 6.4 Target Status Conditions

- `Target has Burn`
- `Target has Mark`
- `Target has Freeze`
- `Target has Chilled`
- `Target has Bleed`
- `Target has Guard`
- `Target is Staggered`
- `Target has any status`

### 6.5 Player State Conditions

- `Player Guard >= N`
- `Player Focus >= N`
- `This is the first Basic Attack this combat`
- `Player lost HP last turn`
- `Player has no Guard`

### 6.6 Board Conditions

- `Combat has 3 or more enemies`
- `At least 2 enemies have Mark`
- `All enemies have Mark`
- `Total Bleed on board >= N`
- `At least 1 enemy is Chilled`

### 6.7 Meta / Encounter Conditions

- `Base Value = Fate Number`
- `Boss law active`
- `This is turn 1`

> Guardrail:  
> - Common nên ưu tiên dùng **Identity Condition** hoặc **Target Status Condition**.  
> - Uncommon có thể thêm **Local Group** hoặc **Player State**.  
> - Rare mới nên dùng các condition phức tạp hơn như **Board Condition**, **Positional**, hoặc condition kép có chủ đích.

---

## 7. Effect module taxonomy — các khối hiệu ứng chuẩn

Skill mới nên được ghép từ 1–3 module dưới đây.

### 7.1 Damage Modules

- `Deal Damage = X`
- `Deal Fixed Damage = N`
- `Deal AoE Damage = X to all`
- `Deal Splash Damage`
- `Deal Bonus Damage if condition met`
- `Deal Overkill Carryover`

### 7.2 Guard Modules

- `Gain Guard = X`
- `Gain Guard from Highest/Lowest in Group`
- `Gain Guard from Player Resource`
- `Retain Guard`
- `Convert Status into Guard`

### 7.3 Status Apply Modules

- `Apply Burn = X`
- `Apply Mark`
- `Apply Freeze`
- `Apply Chilled / extend Chilled`
- `Apply Bleed = X`
- `Apply generic ailment`

### 7.4 Status Consume / Payoff Modules

- `Consume Burn -> bonus damage`
- `Consume Mark -> propagation / payoff`
- `Break Freeze / exploit Chilled`
- `Consume Bleed -> create resource`
- `Consume Guard -> deal damage` (nếu sau này cần)

### 7.5 Propagation Modules

- `Spread Mark to all other enemies`
- `Bounce hit to another target`
- `Chain status when current one ends`
- `Reapply status to original target`

### 7.6 Buff Modules

- `Buff Basic Attack for N turns`
- `Buff a system rule for N turns`
- `Reduce Focus Cost under condition`
- `Add temporary Added Value`
- `Grant Focus now or each turn`

### 7.7 Debuff Modules

- `Target takes +N damage from specific source`
- `Target loses Guard`
- `Target's next action weakened`
- `Target becomes vulnerable to one mechanic`

### 7.8 Utility Modules

- `Refund Focus`
- `Create Consumable`
- `Move / copy Base Value`
- `Record a number for later payoff`
- `Prevent a trigger`

### 7.9 Positional Modules

- `Left slot does A, right slot does B`
- `Center slot decides payoff`
- `Resolve per slot in order`

---

## 8. Composition rules — cách ghép một skill hợp lệ

### 8.1 Công thức khuyến nghị

Một skill thường nên được tạo từ:

- **1 Delivery Pattern**
- **1 Condition chính**
- **1–2 Effect Modules chính**
- **0–1 secondary module** nếu thật sự cần

### 8.2 Công thức an toàn theo rarity

#### Common
- 1 condition đơn giản hoặc không condition
- 1 hiệu ứng chính
- 0–1 hiệu ứng phụ nhỏ

#### Uncommon
- 1 condition rõ + 1 payoff rõ
- hoặc 1 utility layer làm sequencing thú vị hơn

#### Rare
- cho phép mở engine mới,
- cho phép local-group / board / positional gameplay,
- nhưng vẫn nên có **một fantasy rõ ràng**, không phải nhồi mọi thứ vào cùng lúc.

### 8.3 Những tổ hợp nên tránh

- 2 condition khó + 3 module chính trên cùng một skill
- vừa đọc board-wide vừa đọc exact-value vừa đọc player state
- vừa buff, vừa AoE, vừa control, vừa finisher trên cùng 1 skill 1 slot

---

## 9. Resolution order — thứ tự resolve bên trong một skill

Trừ khi text của skill ghi ngoại lệ rõ ràng, resolve theo thứ tự này:

1. **Read source values**  
   Đọc Base Value / local group / player state / target state.

2. **Check condition**  
   Xác định condition có thỏa không.

3. **Build primary output**  
   Tính damage, Guard, apply amount hoặc payoff packet.

4. **Resolve direct hit / Guard interaction**  
   Áp dụng damage, Sunder, bypass Guard, Stagger interaction.

5. **Resolve consume / payoff**  
   Ví dụ consume Burn, payoff trên Mark, convert Bleed.

6. **Resolve apply / reapply status**  
   Áp lại Burn, Mark, Freeze, Chilled, Bleed nếu skill có phần này.

7. **Resolve refund / utility hậu xử lý**

> Mục tiêu của thứ tự này là giữ nhất quán với `COMBAT_CORE_SPEC` và `ELEMENTS_STATUS_SPEC`:  
> **hit/damage trước → consume/payoff → apply status sau**, trừ khi text skill nói khác.

---

## 10. Data template chuẩn cho skill mới

Mỗi skill mới nên viết theo block này:

```md
#### [Skill Name]
- **Element:**
- **Tag:**
- **Slots / Focus:**
- **Rarity:**
- **Delivery Pattern:**
- **Condition chính:**
- **Condition Scope:**
- **Effect Modules:**
- **Text hiện tại:**
- **Vai trò thiết kế:**
- **Role trong build:**
- **Note anti-synergy / edge case:**
```

Nếu một skill không điền được block trên, skill đó chưa đủ rõ để thêm vào game.

---

## 11. Ví dụ chuẩn hóa một số skill hiện có

### 11.1 Hellfire

- **Element:** Fire
- **Tag:** `Attack`
- **Slots / Focus:** `3 slot, 2 Focus`
- **Rarity:** `Rare`
- **Delivery Pattern:** Single Target
- **Condition chính:** `Target has Burn` + `All Dice in Group = 7` cho loop
- **Condition Scope:** Target Scope + Local Group Scope
- **Effect Modules:** Consume Burn → bonus damage; reapply Burn if exact-value loop met
- **Vai trò thiết kế:** exact-value fire engine / finisher
- **Note:** anti-synergy có chủ đích với passive tăng mặt dice

### 11.2 Cold Snap (revised)

- **Element:** Ice
- **Tag:** `Attack/Guard (Buff)`
- **Slots / Focus:** `2 slot, 2 Focus`
- **Rarity:** `Uncommon` nghiêng hướng này
- **Delivery Pattern:** Split by Slot Position
- **Condition chính:** `Left Slot / Right Slot`
- **Condition Scope:** Slot Position Scope
- **Effect Modules:** Left slot deals damage; right slot gains Guard
- **Vai trò thiết kế:** mở grammar positional cho Ice

### 11.3 Cinderbrand

- **Element:** Fire
- **Tag:** `Status (Debuff)`
- **Slots / Focus:** `1 slot, 2 Focus`
- **Rarity:** `Uncommon`
- **Delivery Pattern:** Single Target
- **Condition chính:** `Burn is consumed on target`
- **Condition Scope:** Target Scope
- **Effect Modules:** Debuff target to take +1 damage per Burn consumed for 3 turns
- **Vai trò thiết kế:** amplify Burn payoff thay vì tự gây burst

### 11.4 Physical rare exact-value skill mẫu

Ví dụ một skill Physical hiếm kiểu “số 1” nên được ghi như sau:

- **Element:** Physical
- **Tag:** `Attack`
- **Slots / Focus:** `1 slot, 4 Focus`
- **Rarity:** `Rare`
- **Delivery Pattern:** Single Target
- **Condition chính:** `Base Value = 1`
- **Condition Scope:** Die Scope
- **Effect Modules:** High fixed damage + Focus Cost Override
- **Text mẫu:** Tốn 4 Focus. Nếu Base Value = 1, skill này chỉ tốn 2 Focus và gây 20 damage.
- **Vai trò thiết kế:** exact-value jackpot payoff cho build cố tình sculpt số 1

---

## 12. Guardrails rất quan trọng

### 12.1 Condition phải rõ scope

Không được viết mơ hồ kiểu:
- `highest die`
- `lowest dice`
- `if enemy has status`

Phải ghi rõ:
- `highest in local group`
- `lowest die in this skill's group`
- `if target has Burn before hit`

### 12.2 Đừng dùng Added Value cho condition nếu không thật sự muốn

Nếu text chỉ nói `dice = 7`, hệ thống phải hiểu là **Base Value = 7**.

### 12.3 Rare không đồng nghĩa với “nhiều chữ”

Rare nên sâu hơn về engine, không phải dài hơn về text.

### 12.4 Một skill mới phải trả lời được nó đổi quyết định gì

Ít nhất một trong các thứ sau phải đổi:

- cách đọc dice
- cách chọn target
- cách xếp slot
- cách cân Focus
- cách dùng Basic Attack/Guard
- cách đánh giá reward / build direction

Nếu không đổi quyết định nào, đó có thể chỉ là một phiên bản khác màu của skill cũ.

---

## 13. Gợi ý hướng code sau này

Sau khi file này đủ ổn, nên code theo hướng:

- **generic Condition Evaluator**
- **generic Effect Resolver**
- **runtime packet builder**
- **data-driven skill definitions**

Không nên code từng skill như một class riêng nếu skill đó chỉ là tổ hợp của các module chuẩn.

Chỉ những skill thật sự exotic hoặc rare engine đặc biệt mới nên cần custom node riêng.

---

## 14. Current Locked Direction

Những điểm sau nên coi là đã khóa ở cấp grammar:

- condition mặc định phải đọc từ **Base Value**,
- highest/lowest mặc định phải ghi rõ scope, ưu tiên **local group**,
- **slot position** là một condition hợp lệ,
- `Delivery Pattern + 1 condition chính + 1–2 module chính` là công thức mặc định,
- skill mới phải map được vào grammar này trước khi thêm vào content pool,
- file này là nơi nên mở rộng khi muốn thêm rule condition mới hoặc effect module mới trong tương lai.
---

## 15. Progress note - 2026-03-22

Doan nay ghi lai huong runtime/authoring dang duoc ap dung.

### 15.1 Hien tai grammar dang duoc dua vao runtime theo cach nao

Project dang di theo lop:

- engine code
- content data
- custom behavior cho truong hop exception

Day la buoc chuyen tiep giua:

- hardcode theo ten skill
- va mot he module data-driven day du hon trong tuong lai

### 15.2 Behavior id la buoc trung gian dang dung

Hien tai mot so skill/passive da co `behaviorId` de runtime nhan ra mechanic identity ma khong can string match theo ten.

Y nghia:

- inspector/asset bat dau tro thanh source of truth cho "skill nay la mechanic nao"
- runtime van resolve bang code
- cac skill dac thu van duoc cho phep co custom hook

### 15.3 Huong grammar tiep theo nen la gi

Can mo rong dan them:

- condition modules
- effect modules
- scope modules
- timing hooks

Muc tieu:

- phan lon skill/passive thuong co the lap tu grammar/module
- skill/passive hiem hoac qua dac biet moi can custom resolver

### 15.4 Guardrail

Khong nen co gang ep 100% moi skill/passive vao inspector neu dieu do bien inspector thanh mot ngon ngu lap trinh te hon code.

Muc tieu tot hon la:

- da so content lam bang data
- engine va resolver nam trong code
- exception co hook ro rang, co ten ro rang
