# COMBAT_CORE_SPEC.md

> Tài liệu này là **source of truth cho vòng combat cốt lõi**. Nó mô tả nhịp lượt, quy tắc dice, Focus, damage/Guard/Stagger, lane order và nguyên tắc resolve số trong combat.  
> File này không liệt kê full content skill/passive; phần đó nằm ở `SKILLS_PASSIVES_SPEC.md`.  
> Khi có mâu thuẫn giữa trực giác cá nhân và file này, ưu tiên file này.

---

## 1. Mục tiêu của hệ thống

Combat của game phải tạo ra cảm giác:

- mỗi lượt ngắn nhưng có ý nghĩa,
- xúc xắc là trung tâm thật sự của quyết định,
- sequencing và lane order là sức mạnh,
- player phải chọn giữa setup, payoff, defense, economy,
- độ sâu đến từ quyết định và tương tác hệ thống, không đến từ tính nhẩm quá nhiều lớp modifier.

Combat loop cốt lõi phải luôn giữ được tinh thần:

**Roll → Plan → Lock → Execute → Enemy Turn**

---

## 2. Phạm vi file này

File này bao gồm:

- dice system,
- Focus economy trong combat,
- Basic actions,
- turn flow 5 phase,
- damage / Guard / Stagger,
- skill tag nền,
- lane mapping / reorder / pair identity,
- tooltip / preview direction ở cấp combat,
- edge cases cốt lõi của combat.

File này không đi sâu vào:

- full content skill/passive pool,
- relic pool cụ thể,
- run economy ngoài combat,
- implementation / class code.

---



## 3. Logic Flow

Phần này mô tả **đường đi logic tổng quát của combat core**: hệ thống bắt đầu từ đâu, kiểm tra điều kiện gì ở mỗi bước, và kết quả nào được truyền sang bước kế tiếp.  
Phần này không thay thế rule chi tiết ở các section sau; nó là bản đồ vận hành cấp cao.

### 3.1 Flow của một combat encounter

**Combat Start**  
→ Initialize encounter, load player loadout, enemy roster, passive state và temporary combat state  
→ Hiển thị enemy intent / encounter information  
→ Giao tài nguyên khởi đầu theo current combat rule  
→ Chuyển sang `PlayerTurnStart`

**PlayerTurnStart**  
→ Resolve mọi effect “start of turn” trên player  
→ +1 Focus theo current rule  
→ Refresh các cờ temporary theo current turn timing  
→ Chuyển sang `Roll`

**Roll**  
→ Roll tất cả active dice  
→ Với từng die: xác định rolled face, Base Value, Added Value liên quan, Crit / Normal / Fail  
→ Preserve toàn bộ die-local context cho planning và execution  
→ Chuyển sang `Planning`

**Planning**  
→ Player gắn skill vào slot / nhóm slot  
→ Hệ thống kiểm tra: đủ slot, đủ contiguous span, đủ Focus, skill placement hợp lệ, condition / passive modifier hiện hành  
→ Nếu invalid: reject assignment hoặc giữ player ở Planning  
→ Nếu valid: đưa action vào planned queue  
→ Khi player lock plan: nếu không có action hợp lệ thì block; nếu có thì chuyển sang `Targeting`

**Targeting**  
→ Yêu cầu target cho từng action cần target  
→ Kiểm tra target rule, lane legality, range legality, self / ally / enemy legality  
→ Invalid target bị từ chối và giữ nguyên phase  
→ Khi mọi action đã có target hợp lệ: chuyển sang `Execution`

**Execution**  
→ Convert queue từ planning-facing positions sang runtime order  
→ Áp dụng lane mapping / reorder rule  
→ Khóa **reorder** và **skill assignment** của turn hiện tại  
→ Trong khi Execute đang diễn ra, player vẫn có thể dùng consumable hợp lệ để **edit dice state**  
→ Mọi thay đổi lên dice phải cập nhật lại die-local context / crit / fail / highest-lowest / exact-value access cho các phần **chưa resolve**  
→ Resolve từng action theo thứ tự cuối cùng: read die-local context → build runtime packet → damage / Guard → consume / payoff → apply status / secondary effect → clear action  
→ Sau action cuối cùng: chuyển sang `PlayerTurnEnd`

**PlayerTurnEnd**  
→ Resolve end-of-player-turn effects  
→ Update state cần mang sang enemy turn  
→ Chuyển sang `EnemyTurnStart`

**EnemyTurnStart → EnemyTurnEnd**  
→ Resolve start-of-turn effects của enemy  
→ Chọn và execute enemy actions theo intent / pattern / condition  
→ Resolve death / status / ailment / Guard checks trong quá trình action chạy  
→ Resolve end-of-turn effects của enemy  
→ Check victory / defeat  
→ Nếu combat tiếp tục: quay lại `PlayerTurnStart`; nếu không: chuyển sang `CombatEnd`

### 3.2 Flow của một player turn

`Start Turn`  
→ Gain / refresh tài nguyên đầu lượt  
→ `Roll` active dice  
→ `Planning`: player đọc roll, chọn skill, chọn slot, cân giữa setup / payoff / defense / economy  
→ `Validation`: hệ thống check slot, Focus, condition, conflict với plan hiện tại  
→ `Lock Plan`  
→ `Targeting`  
→ `Execution` theo runtime order  
→ `End Turn`

### 3.3 Flow của một die

`Roll Die`  
→ Xác định mặt được roll  
→ Gán rolled number thành Base Value  
→ Xác định Crit / Normal / Fail từ chính die đó  
→ Tính Added Value liên quan nhưng **không thay đổi identity của Base Value**  
→ Lưu die-local context để dùng cho planning, preview và execution

### 3.4 Flow gán skill vào slot

`Player chọn skill`  
→ Hệ thống đọc slot cost, Focus cost, target rule, tag, element, special condition  
→ `Player chọn slot / nhóm slot`  
→ Hệ thống check: slot trống hay không, span đủ hay không, contiguous hay không, current modifier có làm đổi cost / slot size hay không  
→ `Check tài nguyên`  
→ Đủ Focus và hợp lệ thì cho skill vào planned queue; không đủ thì reject và giữ nguyên Planning

### 3.5 Flow resolve damage

`Action có target hợp lệ`  
→ Build final action context từ skill definition + die-local data + passive + attacker state + target state  
→ Check pre-damage legality  
→ Resolve raw output  
→ Resolve Guard interaction / bypass / anti-Guard nếu có  
→ Resolve damage lên HP nếu còn  
→ Resolve on-hit / on-damage / on-crit / on-fail / on-consume hooks  
→ Resolve status application theo đúng thứ tự timing của combat core  
→ Update board state cho action tiếp theo


## 4. Action economy trong combat

### 4.1 Công cụ hiện có của player

Trong combat, player hiện có:

- **6 skill slot**,
- **3 passive slot**,
- **1 đến 3 dice**,
- **Basic Attack**,
- **Basic Guard**.

Rule rất quan trọng ở cấp action economy:

- mỗi skill chỉ chiếm **1 loadout slot** khi mang vào combat,
- nhưng trong **Planning phase**, skill có thể chiếm **1 / 2 / 3 dice** tùy slot cost của chính skill đó,
- skill nhiều dice là đánh đổi trực tiếp với số action groups còn lại trong turn.

Ví dụ: `Hellfire` chỉ chiếm **1 slot loadout**, nhưng khi assign sẽ chiếm **3 dice**, nên turn đó gần như tiêu toàn bộ action economy cho một payoff lớn.

Đầu run bắt đầu với **1 dice**.  
Tối đa trong combat là **3 dice**, đồng nghĩa tối đa khoảng **3 action groups** mỗi lượt.

### 4.2 Focus economy

Current locked rules:

- **Max Focus = 9**
- **Start combat = 2 Focus**
- **Đầu mỗi lượt = +1 Focus**
- **Không được nợ Focus**
- Vì vậy **turn 1 thực tế = 3 Focus**

Ý nghĩa thiết kế:

- Focus phải thiếu vừa đủ để buộc player lựa chọn.
- Player phải có cảm giác có thể “xoay lượt” bằng sequencing tốt, build tốt hoặc basic action hợp lý.
- Focus không được trở thành cái khóa khiến combat chết cứng.

### 4.3 Basic actions

Basic actions luôn có sẵn và **không chiếm 6 skill slot chính**.

#### Basic Attack

- **0 Focus**
- **4 damage gốc**
- cho **+1 Focus**

Rule resolve:

- Basic Attack vẫn nhận **Added Value** như các action gây damage khác.
- Vì Basic Attack không mang tag `Physical`, Crit của nó dùng hệ số **Crit thường = +20% Base Value** để tạo Added Value.
- Ví dụ: d20 crit ở mặt 20 -> `floor(20 x 0.2) = +4 Added Value` -> Basic Attack gây `4 + 4 = 8 damage`.
- Ví dụ: d8 crit ở mặt 8 -> `floor(8 x 0.2) = +1 Added Value` -> Basic Attack gây `4 + 1 = 5 damage`.
- Nếu die là Fail, Basic Attack chỉ còn `floor(4 / 2) = 2 damage`; Fail không đọc Added Value và không đổi Base Value.

#### Basic Guard

- **0 Focus**
- tạo Guard bằng **Base Value** của die dùng cho action đó

Vai trò thiết kế:

- là anchor của economy,
- luôn cho player một lựa chọn hợp lệ khi roll xấu,
- giúp build mạnh vẫn còn lý do đọc và dùng action nền,
- ngăn cảm giác “không làm được gì”.

---

## 5. Dice system — current locked rules

Đây là phần quan trọng nhất của toàn bộ combat. Mọi skill logic, passive logic, preview, tooltip và hướng build đều phải tôn trọng phần này.

### 5.1 Dice không chỉ là damage source

Dice có thể là:

- d2, d4, d6, hoặc các biến thể khác,
- có mặt số không đều,
- có thể được custom trong run,
- có thể ảnh hưởng tới crit/fail, exact value, parity, threshold, highest/lowest.

Dice không phải “mana ngẫu nhiên”.  
Dice là **identity engine** của combat.

### 5.2 Base Value và Added Value

Current locked rules:

- **Base Value** = mặt thật roll ra
- **Added Value** = phần cộng thêm vào output cuối
- **Mọi condition phải đọc từ Base Value**
- Added Value **không đổi bản chất** của die
- Với phần lớn skill gây damage chuẩn, damage cuối được hiểu là: **damage gốc của skill + tổng Added Value áp dụng cho action đó**
- Với skill dùng `X`, mặc định hiện tại: **`X = Base Value + Added Value`**, trừ khi text skill ghi rõ công thức khác
- `Added Value` từ current direction không chỉ là bonus damage; nó là **lớp thưởng chung của toàn hệ thống**
- Nghĩa là nếu skill tạo ra một **output số học** như damage, Burn, Bleed, Guard hoặc payoff từ combat history / board state, output cuối mặc định đều tiếp tục cộng `Total Added Value` trừ khi text skill ghi rõ ngược lại
- Rule chuẩn hóa từ current direction: nếu skill có **fixed output**, output cuối mặc định luôn là `fixed output + Added Value`, bất kể đó là `Attack`, `Guard` hay output số học khác
- Nếu text skill ghi `lowest base` / `highest base`, phần chọn giá trị vẫn đọc từ **Base Value**
- Nếu đó là **single-output skill**, output cuối vẫn cộng `Total Added Value`
- Nếu đó là **split-role / multi-branch skill**, mỗi output branch chỉ được dùng `Added Value` của **đúng die source** mà branch đó chọn

Rule cộng dồn theo số slot:

- Skill **1 slot** cộng Added Value của **1 die** đang gắn cho skill đó.
- Skill **2 slot** cộng **tổng Added Value của cả 2 die** trong local group của skill đó.
- Skill **3 slot** cộng **tổng Added Value của cả 3 die** trong local group của skill đó.
- Ví dụ: skill 2 slot có `5 damage gốc`, gắn vào `d10 crit` và `d20 crit`, không phải `Physical` -> damage cuối là `5 + floor(10 x 0.2) + floor(20 x 0.2) = 5 + 2 + 4 = 11`.
- Nếu chính skill 2 slot đó là `Physical` -> damage cuối là `5 + floor(10 x 0.5) + floor(20 x 0.5) = 5 + 5 + 10 = 20`.
- Skill 3 slot cũng theo đúng rule cộng dồn này với cả 3 die trong nhóm.

Nguồn sinh Added Value hiện tại:

- Dice có thể mang / sinh Added Value từ:
  - **Crit** của chính die đó trong action hiện tại
  - **enchant / consumable / dice customization** đã gắn sẵn lên mặt dice theo spec progression
- Ngoài dice, Added Value cũng có thể đến từ **skill / passive / relic / modifier condition** ghi rõ trong text.
- Fail không tự sinh Added Value âm và không xóa Added Value đã có.

Các condition phải đọc theo Base Value gồm:

- chẵn / lẻ,
- crit / fail,
- `<= 3`,
- `>= ngưỡng`,
- exact value,
- highest / lowest.

Không được để hệ condition đọc từ số đã cộng modifier xong. Nếu làm vậy, dice sẽ mất identity.

### 5.3 Local die context

Nếu skill 2–3 slot có text như:

- die cao nhất,
- die thấp nhất,
- exact value trong nhóm,

thì **chỉ xét trong chính nhóm dice của skill đó**, không xét cả bàn.

Đây là rule đã chốt.  
Không được tự đổi thành global board context nếu chưa có chỉ đạo mới.

### 5.4 Crit / Fail

Current locked rules:

- **Crit** = roll đúng giá trị mặt cao nhất của die
- **Fail** = roll đúng giá trị mặt thấp nhất của die
- Crit / Fail **không đổi Base Value**
- Crit sinh **Added Value / bonus output**
- Fail không làm đổi Base Value hay trừ Added Value
- Fail chỉ cắt nửa **base output của chính skill đó**, luôn floor
- `base output` ở đây không chỉ là damage; nếu skill tạo Guard/Burn/Bleed/output lịch sử và phần đó được định nghĩa là output gốc của skill, Fail cũng phải đọc trên phần đó

Hệ số đang dùng ở mức current spec:

- **Crit thường = +20% Base**
- **Crit Physical = +50% Base**
- **Fail = 50% damage gốc của skill**

Rule áp dụng với skill nhiều slot:

- Mỗi die trong local group tự check Crit / Fail theo chính die đó.
- Nếu nhiều die cùng Crit, Added Value từ các die đó **cộng dồn** vào cùng action.
- Nếu action là `Physical`, mỗi die Crit đóng góp Added Value theo hệ số `+50% Base` của chính nó.
- Nếu action không phải `Physical`, mỗi die Crit đóng góp Added Value theo hệ số `+20% Base` của chính nó.
- Nếu local group có **ít nhất 1 Fail**, action đó chỉ ăn **1 lần** fail penalty: `damage gốc / 2`.
- `2 Fail` hoặc `3 Fail` trong cùng một action **không stack thêm** fail penalty.
- Fail không làm mất Added Value do die Crit khác hoặc do passive/skill đã cấp cho action đó.

### 5.5 Trường hợp nhiều mặt cùng max / min

Current locked rules:

- Nhiều mặt cùng max → tất cả là Crit
- Nhiều mặt cùng min → tất cả là Fail
- Nếu **max == min** → tất cả là Crit, không có Fail
- Trong case `max == min`, **Crit thắng Fail**

Rule này đặc biệt quan trọng khi dice bị custom rất mạnh và không còn giống d6 bình thường.

### 5.6 Làm tròn

Current locked rules:

- Toàn game dùng **floor**
- Nếu một output có **giá trị gốc dương** nhưng sau Fail / floor / penalty mà rơi xuống dưới `1`, output cuối vẫn là **minimum 1**
- Rule này áp dụng cho mọi **output số học dương** của action, không chỉ direct damage
- Nếu output gốc thực sự là `0`, kết quả vẫn giữ `0`, không tự nhảy lên `1`
- Ngoại lệ: nếu Guard chặn hết thì có thể không mất HP

### 5.7 Pipeline dice math

Pipeline nguồn thiết kế hiện tại:

```text
baseValue -> critAddedValue -> passiveSkillConditionalAddedValue -> totalAddedValue -> finalActionOutput
```

`DiceSlotRig` là source of truth chính cho dice math trong code, nhưng ở cấp design điều quan trọng là:

- preview,
- tooltip runtime,
- execute result

về lâu dài phải đọc từ **cùng một nguồn số**, không được mỗi nơi tự tính một kiểu.

### 5.8 Exact value và ký hiệu X

`X` trong text skill là chỗ đọc giá trị đã resolve theo đúng rule của skill đó.  
Ở cấp design hiện tại, cần phân biệt rõ:

- có skill đọc **Base Value**,
- có skill đọc **resolved value**,
- có skill đọc **giá trị cao nhất/thấp nhất trong local group**, 
- có skill dùng X như một ô biến phụ thuộc die đang gắn vào slot.

Rule hiện tại cần khóa rõ:

- Với skill `X damage`, mặc định `X = Base Value + Added Value`.
- Với skill damage chuẩn không dùng `X`, mặc định output là `damage gốc + Added Value`.
- Về sau có thể tồn tại skill hoặc passive cho thêm Added Value nếu đạt điều kiện; các bonus này vẫn chỉ cộng vào output cuối, không đổi bản chất Base Value của die.
- Nếu skill chiếm 2 hoặc 3 slot, `Added Value` ở đây mặc định là **tổng Added Value của toàn bộ dice trong local group**, cộng với mọi bonus Added Value khác mà action đó đang nhận.
- Một số skill có thể đọc từ **combat history** thay vì Base Value, ví dụ: `damage gốc = số kẻ địch đã từng bị Freeze trong combat này`; khi resolve, output cuối của chúng vẫn tiếp tục cộng Added Value nếu action đó đang có.

Exact value là trục identity quan trọng của game, không phải gimmick.  
Các engine như `Hellfire` phải tiếp tục được xem là đại diện cho hướng exact-value build.

### 5.8A Combat Formula Sheet

Sheet công thức chuẩn hiện tại:

- `Base Value` = mặt thật của die, dùng để check mọi condition.
- `Added Value` = phần cộng vào output cuối, không đổi bản chất của die.
- `Crit`:
  - non-Physical -> `Added Value += floor(Base Value x 0.2)`
  - Physical -> `Added Value += floor(Base Value x 0.5)`
- `Fail` -> chỉ làm `base output = floor(base output / 2)`, không trừ `Added Value`.
- Nếu skill nhiều slot có nhiều hơn 1 die Fail, fail penalty vẫn chỉ áp **1 lần cho cả action**.
- `Skill damage chuẩn` -> `Final Damage = damage gốc + Total Added Value`
- `Skill X` -> `X = Base Value + Added Value`
- `Fixed status / guard / history output` -> `Final Output = output gốc + Total Added Value`
- `Split-role skill` -> `Final Output của mỗi branch = output gốc của branch + Added Value của đúng die source của branch`
- `Skill 1 slot` -> `Total Added Value = Added Value của 1 die`
- `Skill 2 slot` -> `Total Added Value = Added Value die 1 + Added Value die 2`
- `Skill 3 slot` -> `Total Added Value = Added Value die 1 + Added Value die 2 + Added Value die 3`
- `Dice sources of Added Value` -> `Crit`, `enchant`, `consumable`, `dice customization`
- `Non-dice sources of Added Value` -> `skill`, `passive`, `relic`, `modifier` ghi rõ
- `Burn apply` -> mỗi lần apply tạo `1 Burn batch`, mỗi batch sống `3 turn`
- `Visible Burn` -> tổng mọi batch Burn còn sống
- `Burn expire` -> batch nào hết hạn thì chỉ batch đó biến mất
- `Burn consume` -> consume toàn bộ Burn còn sống tại thời điểm đó
- `Burn consume baseline` -> `+2 damage x Burn consumed`, trừ khi skill override
- Toàn game dùng `floor`
- Nếu `base output > 0` nhưng sau Fail / floor nhỏ hơn `1`, final output vẫn phải tối thiểu là `1`
- Nếu `base output = 0`, final output tiếp tục là `0`

### 5.9 Dice customization

Dice không phải hệ tĩnh. Custom dice là một phần identity của game.

Current design direction:

- mỗi mặt dice có thể tăng **Base Value** bằng consumable / relic / progression,
- đơn vị cơ bản của progression là **`+1 base cho 1 mặt`**,
- giá trị tối đa của 1 mặt hiện được hiểu là **99**,
- kể cả d2 vẫn có thể bị đẩy thành những mặt rất lớn ở late run,
- mod dice làm thay đổi toàn bộ logic Crit / Fail / Exact / Even / Odd / Threshold.

Dice customization phải ảnh hưởng thật đến:

- lựa chọn trong turn,
- lựa chọn xây build trong run,
- hướng unlock / progression dài hạn.

### 5.10 Reorder trong Planning

Current locked rules:

- Player có thể kéo cặp **dice + skill** về bất kỳ vị trí nào trong **Planning phase**.
- Reorder ảnh hưởng cả:
  - **dice assignment**
  - **thứ tự execute**
- Khi bấm sang Execute phase, reorder bị khóa.

Ví dụ đúng tinh thần:

- roll được die rất cao,
- kéo nó xuống cuối,
- dùng 1–2 lane trước để setup Mark / phá Guard / áp trạng thái,
- dùng lane cuối làm finisher.

### 5.11 Ý nghĩa thiết kế của dice

Dice quyết định:

- output,
- điều kiện kích hoạt,
- thứ tự finisher,
- hướng build,
- nhịp economy,
- và cả cách passive tương tác.

Bất kỳ hệ mới nào thêm vào cũng phải tôn trọng vai trò trung tâm của dice.

---

## 6. Turn flow — current locked flow

### 6.1 Tổng quan 5 phase

Flow hiện tại đã chốt như sau:

1. **Start Phase**
2. **Roll Phase**
3. **Planning Phase**
4. **Execution Phase**
5. **Enemy / End Phase**

### 6.2 Start Phase

Ở đầu lượt:

- player nhận **+1 Focus**,
- passive đầu lượt xử lý,
- enemy chốt / giữ intent cho turn hiện tại.

Mục tiêu của phase này là mở lượt mới rõ ràng, tránh nhập nhằng giữa cleanup turn cũ và quyền hành động turn mới.

### 6.3 Roll Phase

- roll toàn bộ dice hiện đang equip cho lượt đó,
- giá trị roll sẽ quyết định không chỉ damage mà còn điều kiện, payoff, parity, crit/fail, exact value,
- UI phải chuyển người chơi sang mode đọc tình huống.

### 6.4 Planning Phase

Player có thể:

- kéo dice vào skill slot,
- quyết định die nào cho skill nào,
- reorder các cặp hành động,
- chọn setup trước hay nổ trước,
- cân giữa payoff, defense, economy.

Planning là nơi chiến thuật thật sự diễn ra.

Planning phase cũng là thời điểm hợp lệ để player dùng các consumable can thiệp dice nếu consumable đó được phép dùng trong combat.

Rule:
- flow chọn target là **state-based**, không khóa cứng thứ tự thao tác
- player có thể select consumable trước hoặc chọn target trước tùy context
- action **Use** chỉ hợp lệ khi target requirement của effect đã được thỏa
- nếu consumable chỉnh die làm thay đổi Base / Added / face / enchant của die đang roll, thay đổi đó phải cập nhật vào resolved dice state theo đúng source of truth
- nếu consumable được dùng trước khi lock plan, planning / preview phải phản ánh state mới

### 6.5 Execution Phase

- Skill resolve **từ trái sang phải** theo lane hiện tại.
- Khi vào Execute phase thì **khóa reorder** và **khóa skill đã đưa vào turn hiện tại**.
- Execute **không khóa dice edit** nếu consumable hiện hành cho phép can thiệp dice.
- Trong Execute, mọi edit dice phải cập nhật ngay:
  - Base / Added nếu effect tác động vào đó
  - rolled face / face data nếu effect tác động vào đó
  - Crit / Fail
  - highest / lowest
  - exact-value access liên quan
- Kết quả đã resolve trước đó **không bị viết lại**; thay đổi chỉ ảnh hưởng phần còn lại chưa resolve.
- Thứ tự resolve là một phần của sức mạnh build, không phải chỉ là chi tiết trình diễn.

### 6.6 Enemy / End Phase
### 6.6 Enemy / End Phase

- enemy hành động,
- status tick / cleanup diễn ra,
- Guard của player biến mất cuối lượt nếu không có rule giữ Guard,
- chuẩn bị chuyển sang Start Phase tiếp theo.

### 6.7 Các rule lock cực kỳ quan trọng

Không được tự ý phá các nguyên tắc sau nếu không có bug cụ thể:

- Planning mới cho reorder,
- Execute đọc lane hiện tại, không đọc identity cũ,
- phase transition phải rõ ràng,
- khi vào Execute thì **reorder** và **skill assignment của turn hiện tại** đã bị khóa,
- dice-edit consumable trong combat được phép tồn tại ở cả Planning và Execute nếu spec của effect cho phép,
- trong Execute, dice mutation không được retroactive viết lại action đã resolve,
- UI và logic phải cùng truyền đi một thông điệp:
  - **line hành động đã khóa**
  - nhưng **dice state vẫn có thể mutate** nếu consumable hợp lệ

## 7. Core combat rules — current locked rules
## 7. Core combat rules — current locked rules

### 7.1 Damage và Guard

Current locked rules:

- Damage trừ Guard trước
- Phần dư mới vào HP

Ý nghĩa:

- Guard là lớp đệm thật,
- các hệ như Physical / Sunder / Stagger phải có lý do tồn tại,
- sequencing để phá Guard rồi mới đánh HP phải luôn có giá trị.

### 7.2 Stagger

Stagger là core behavior đã chốt.

Rule cụ thể:

- Khi Guard từ **`> 0`** về **`0`**, mục tiêu vào trạng thái **Stagger**
- Overflow của hit phá Guard vẫn vào HP **bình thường**
- Overflow đó **không** được nhân `x1.2`
- Chỉ **hit kế tiếp duy nhất** mới ăn `x1.2 tổng damage`
- Hết turn mà không có hit bồi thì **Stagger biến mất**

Chi tiết rất quan trọng:

- `Lightning shock` **không consume** Stagger
- `Bleed tick` **không consume** Stagger
- `Burn consume` nếu nằm trong **direct-hit** thì được tính vào tổng damage trước khi nhân `1.2`

Ý nghĩa thiết kế:

- Guard break phải tạo ra **một khoảnh khắc chiến thuật**,
- Stagger thưởng cho sequencing tốt,
- Stagger không được trở thành một nguồn nhân damage vô hạn cho effect phụ.

### 7.3 Skill tags nền tảng

Nhóm tag hiện tại:

- `Attack`
- `Guard`
- `Status`
- `Sunder`

Rule đã chốt:

- `Sunder` **không có bonus ẩn**
- Nếu có tương tác đặc biệt, phải được viết rõ trong text hoặc rule thật

Không được ngầm buff Sunder vì “cảm giác phải mạnh”.

### 7.4 Trạng thái combat-level phải đọc rõ

Ở cấp combat core, người chơi luôn cần đọc được tối thiểu:

- lane hiện tại,
- die đang gắn vào lane nào,
- skill nào sẽ resolve trước,
- mục tiêu nào có Guard,
- mục tiêu nào đang Stagger,
- player còn bao nhiêu Focus,
- action đã lock hay chưa.

---

## 8. Lane mapping / reorder / pair identity rule

Đây là section rất quan trọng và phải giữ riêng.

### 8.1 Rule cốt lõi đã chốt

Phải phân biệt rõ hai lớp identity:

- **`1 / 2 / 3`** = identity gốc / pair identity ban đầu
- **`A / B / C`** = vị trí lane thực thi hiện tại sau reorder

Quy tắc thực thi đúng:

`Lane order` ở đây phải được hiểu là **thứ tự resolve của cặp dice + skill trong turn**, không phải vị trí hình học của enemy. Đây là trục sequencing thật của combat, ví dụ: buff đòn sau, phá Guard trước rồi finisher sau.

- Player có thể kéo pair từ vị trí gốc sang lane khác trong Planning.
- Khi Execute bắt đầu, game phải đọc theo **lane hiện tại** (`A/B/C`), không đọc theo “pair ban đầu từng là số 1/2/3”.
- Effect resolve trái sang phải theo lane thực thi.

### 8.2 Những phần đang được xem là ổn

- Kéo cả cặp `dice + skill` như một đơn vị trong planning
- Reorder tác động cả assignment và execution order
- Lock plan rồi mới execute
- Lane order là một phần gameplay thật

### 8.3 Điều cấm kỵ khi sửa

Không được:

- để UI nhìn như đã reorder nhưng logic vẫn resolve theo vị trí gốc,
- để preview / execute đọc khác lane,
- để một pair “vừa ở lane mới vừa giữ logic lane cũ”,
- để sau khi lock plan mà vẫn đổi được order ngầm.

### 8.4 Ghi nhớ thực dụng

Lane order tồn tại để player làm sequencing như:

- phá Guard trước,
- setup status sau,
- finisher cuối,

hoặc một thứ tự ngược lại tùy build. Nếu lane order mất giá trị chiến thuật, combat sẽ mất rất nhiều chiều sâu.

---

## 9. Tooltip / preview / runtime number direction

### 9.1 Hướng design đã chốt

Ngoài combat / trong shop / khi chưa roll:

- hiện dạng tĩnh như `Deal X damage`, `Gain X Guard`

Trong combat / khi đã roll:

- hiện **số thật đã resolve** hoặc gần-resolve nhất có thể

### 9.2 Nguyên tắc quan trọng

Tooltip / preview / execute về lâu dài phải dùng **cùng một nguồn số**.

Không được để:

- tooltip nói một kiểu,
- preview nói một kiểu,
- execute ra kết quả kiểu khác.

### 9.3 Ý nghĩa UX

Game có thể sâu ở backend, nhưng frontend phải ngày càng rõ hơn.  
Readable Complexity là pillar thật, không phải phần trang trí.

### 9.4 Trạng thái hiện tại

- đã có preview nền tảng,
- formatter runtime text chưa cần coi là final,
- vùng này quan trọng nhưng không nên override combat core đang ổn chỉ để “đẹp hơn” quá sớm.

---

## 10. Edge cases cốt lõi cần luôn nhớ

### 10.1 Dice edge cases

- Dice có thể có nhiều mặt cùng max hoặc cùng min.
- Dice có thể bị custom tới mức `max == min`.
- Die custom mạnh vẫn phải giữ rule Crit/Fail đúng như spec.
- Exact value phải tiếp tục đọc từ Base Value, không từ resolved value đã cộng thêm.

### 10.2 Turn / lane edge cases

- Reorder chỉ hợp lệ trong Planning.
- Sau khi lock, UI và logic đều phải coi order đã cố định.
- Execute phải đọc lane hiện tại, không đọc slot origin.

### 10.3 Damage edge cases

- Damage sau floor mà `< 1` vẫn phải tối thiểu là `1`, trừ khi Guard chặn toàn bộ.
- Stagger chỉ buff **hit direct kế tiếp**.
- Shock phụ / Bleed tick không được ăn Stagger thay hit chính.

### 10.4 Economy edge cases

- Player không được nợ Focus.
- Basic Attack / Basic Guard luôn phải là fallback hợp lệ.
- Build mạnh về late run vẫn không nên xóa hoàn toàn vai trò chiến thuật của basic actions.

---

## 11. Những gì chưa final trong combat core

Các vùng sau **không nên coi là fully final** nếu chưa có chỉ đạo mới:

- formatter text cuối cùng,
- full targeting UX,
- VFX/SFX cụ thể cho từng state,
- data table số cho toàn bộ content,
- toàn bộ exception rule của mọi skill trong content pool.

Nhưng những vùng sau phải được xem là **locked current spec**:

- Base vs Added,
- local die context,
- Crit/Fail logic,
- floor + minimum damage,
- turn flow 5 phase,
- Damage / Guard / Stagger,
- lane mapping `1/2/3` vs `A/B/C`,
- reorder chỉ trong Planning,
- tooltip static ngoài combat / resolved trong combat.
