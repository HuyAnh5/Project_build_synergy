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

Mặc định hiện tại:
- skill gây damage chuẩn dùng công thức `damage gốc + Added Value`
- skill dùng `X` thì mặc định `X = Base Value + Added Value`
- skill hoặc passive về sau có thể cấp thêm Added Value theo điều kiện, và `Added Value` nên được xem là **ngôn ngữ thưởng chung** cho mọi output số học của skill
- nghĩa là không chỉ damage, mà cả `Burn`, `Bleed`, `Guard`, payoff từ `combat history`, payoff từ `board state` cũng mặc định cộng `Added Value` trừ khi text skill ghi rõ ngược lại
- nếu skill chiếm nhiều slot, `Added Value` mặc định là **tổng Added Value của toàn bộ dice trong local group**
- ngoại lệ rất quan trọng: nếu skill là **split-role / multi-branch**, mỗi branch output phải đọc `Added Value` của **đúng die source** mà branch đó chọn, không được lấy `Total Added Value của cả action` đổ vào mọi branch
- **Quy tắc chỉ số đầu tiên**: Nếu skill có **nhiều hơn 1 output** (ví dụ: vừa damage vừa Burn), Added Value mặc định chỉ cộng vào **output đầu tiên** được đề cập trong description của skill đó
  - Ví dụ: "deal 4 damage và apply 2 Burn" → Added Value + vào 4 damage
  - Ví dụ: "apply 4 Burn và deal 2 damage" → Added Value + vào 4 Burn

Ví dụ:
- Base Value = 4, Added Value = +3  
→ skill vẫn phải coi die này là **4**, không phải 7.

### 3.3 Crit / Fail

- **Crit**: roll ra mặt cao nhất của chính die đó
- **Fail**: roll ra mặt thấp nhất của chính die đó

Crit / Fail luôn được xác định theo **die riêng lẻ**, không theo các die khác đang equip.

Crit tạo Added Value theo rule của action:
- thường = `+20% Base Value`
- Physical = `+50% Base Value`

Fail không làm thay đổi Base Value và không trừ vào Added Value.
Fail chỉ làm giảm **base output của skill** xuống còn 50%, luôn floor.
`base output` ở đây không chỉ là damage; nó có thể là Guard, Burn, Bleed hoặc output số học khác nếu skill đó định nghĩa phần đó là output gốc chịu Fail.

Rule nhiều slot:
- Nếu local group có **ít nhất 1 Fail**, action đó chỉ ăn fail penalty **1 lần**
- `2 Fail` hoặc `3 Fail` trong cùng action không chém tiếp lần 2 hay lần 3

Rule nguồn Added Value:
- Dice có thể cung cấp Added Value từ **Crit** hoặc từ **enchant / customization / consumable effect** đã nằm trên mặt dice
- Skill / passive / relic / combat modifier có thể cấp Added Value nếu text ghi rõ
- Fail không phải nguồn Added Value âm

### 3.3A Skill Damage Formula Sheet

Sheet công thức chuẩn để author skill:

- `Base Value` = mặt thật của die, dùng để check condition.
- `Added Value` = phần cộng vào output cuối, không đổi bản chất của die.
- `Crit`:
  - non-Physical -> `Added Value += floor(Base Value x 0.2)`
  - Physical -> `Added Value += floor(Base Value x 0.5)`
- `Fail` -> làm `base output = floor(base output / 2)`, không trừ `Added Value`.
- Nếu skill nhiều slot có nhiều hơn 1 die Fail, fail penalty vẫn chỉ áp **1 lần cho cả action**.
- `Skill damage chuẩn` -> `Final Damage = damage gốc + Total Added Value`
- `Skill X` -> `X = Base Value + Added Value`
- `Skill fixed output khác` -> `Final Output = output gốc + Total Added Value`
- `Split-role skill` -> `Final Output của mỗi branch = output gốc của branch + Added Value của đúng die source của branch`
- `Skill 1 slot` -> `Total Added Value = Added Value của 1 die`
- `Skill 2 slot` -> `Total Added Value = Added Value die 1 + Added Value die 2`
- `Skill 3 slot` -> `Total Added Value = Added Value die 1 + Added Value die 2 + Added Value die 3`
- `Damage từ combat history counter` cũng tiếp tục cộng `Added Value` nếu text không nói ngược lại
- Toàn game dùng `floor`
- Nếu `base output > 0` nhưng sau Fail / floor tụt xuống dưới `1`, final output vẫn phải tối thiểu là `1`
- Nếu `base output = 0`, final output tiếp tục là `0`

### 3.4 Local Group

Là cụm dice mà một skill đang chiếm.  
Ví dụ skill 2 slot chỉ đọc 2 die trong cụm của nó, không đọc toàn bộ 3 die trên board.

### 3.4A Condition là hệ quy chiếu, không phải payoff cố định

`Condition` không nên được hiểu là payoff cố định kiểu:

- `chẵn = luôn bonus Burn`
- `lẻ = luôn bonus damage`
- `highest = luôn Guard`
- `lowest = luôn Burn`

Thay vào đó, condition là **hệ quy chiếu để chọn / đọc / phân vai cho dice**.

Tức là:

- `chẵn / lẻ`
- `Crit / Fail`
- `Exact value`
- `highest / lowest`
- `die đầu / die cuối`

chỉ là cách xác định:

- die nào đang được đọc
- output nào được gán cho die nào
- branch nào đang mở
- reward nào đang được bật

Payoff phía sau có thể là:

- damage
- Burn
- Bleed
- Guard
- Mark
- Focus
- Added Value
- hoặc effect module khác

Ở tầng authoring / inspector, `Tab Condition` nên được hiểu là nơi khai báo:

- `Reference` nào đang được đọc
- `Comparison` nào đang được dùng
- nhiều clause kết hợp theo `All / Any`

ví dụ condition có thể đọc:

- `Any Base Value`
- `Highest Base Value In Group`
- `Lowest Base Value In Group`
- `Total Base Value In Group`
- `Total Resolved Value In Group`
- `Any Die Crit`
- `Any Die Fail`
- `Current Focus`
- `Occupied Slots`

Condition cũ kiểu `Parity / Threshold / Match / Straight` như một block riêng không còn là hướng authoring chính nữa.

### 3.4B Highest / Lowest là bộ chọn die, không phải output cố định

`highest` và `lowest` trong local group phải được hiểu là:

- `highest base value in local group`
- `lowest base value in local group`

Đây là **bộ chọn die**, không phải payoff cố định.

Ví dụ hợp lệ:

- `highest -> Burn`, `lowest -> Guard`
- `highest -> Guard`, `lowest -> Burn`
- `highest -> Mark spread`, `lowest -> self Focus`

Không được hardcode tư duy:

- `highest` luôn là Guard
- `lowest` luôn là Burn

### 3.4C Single-output và Split-role

#### Single-output skill

Skill tạo ra một output chính duy nhất cho cả action.

Ví dụ:

- `Deal 7 damage`
- `Apply 5 Burn`
- `Gain Guard = X`
- `Damage = số enemy đã từng bị Freeze trong combat này`

Kiểu này mặc định dùng:

- `Total Added Value của cả action`

#### Split-role / Multi-branch skill

Skill tách local group thành nhiều branch output khác nhau.

Ví dụ:

- `highest -> Guard`, `lowest -> Burn`
- `die đầu -> apply Mark`, `die cuối -> deal damage`
- `die chẵn -> Burn`, `die lẻ -> Bleed`

Kiểu này **không được** dùng `Total Added Value của cả action` cho mọi branch.
Thay vào đó:

- mỗi branch chỉ được đọc `Base Value` của die nó chọn
- mỗi branch chỉ được đọc `Added Value` của die nó chọn
- mỗi branch chỉ được đọc `Crit / Fail` của die nó chọn

Ví dụ:

- roll `10 crit` và `2 normal`
- skill là `highest -> Guard`, `lowest -> Burn`

thì:

- `Guard` dùng die `10` và ăn `Added Value` của die `10`
- `Burn` dùng die `2` và **không** được ăn `Added Value` từ die `10`

Tương tự:

- roll `10 crit` và `20 fail`
- skill là `lowest -> Burn`, `highest -> Guard`

thì:

- `Burn` lấy từ die `10 crit` -> `12 Burn`
- `Guard` lấy từ die `20 fail` -> `10 Guard`

không được trộn add/fail của hai die vào cùng một output.

### 3.4D Condition Standard cho 80-85% skill

Mục tiêu của condition system **không phải** là diễn tả mọi skill trong game.
Mục tiêu đúng là:

- cover được khoảng `80%-85%` skill bằng authoring chuẩn,
- chỉ những skill thật sự đặc biệt mới cần custom code,
- tránh việc mỗi skill lại phải code lại toàn bộ effect từ đầu.

#### Condition là gì trong game này

`Condition` là **hệ quy chiếu để đọc / chọn / phân vai cho dice, target, resource, board state**.

`Condition` **không phải payoff cố định**.

Điều này có nghĩa:

- `chẵn` không đồng nghĩa với `bonus Burn`,
- `lẻ` không đồng nghĩa với `bonus damage`,
- `highest` không đồng nghĩa cố định với `Guard`,
- `lowest` không đồng nghĩa cố định với `Burn`.

`Condition` chỉ trả lời:

- đang đọc trục nào,
- đang chọn die nào,
- đang nhìn target/state nào,
- khi nào skill được mở payoff phụ.

Còn payoff thực sự phải nằm ở `Effect / Outcome`.

#### Công thức authoring chuẩn cho đa số skill

Một skill thông thường nên được ghép từ 4 lớp:

1. `Core`
- element
- tag
- slot cost
- focus cost
- target rule
- base output như `flat damage`, `flat guard`, `flat status`, hoặc `X`

2. `Optional Condition`
- có thể không có
- nếu có thì thường chỉ nên có `1 condition chính`

3. `Effect / Outcome`
- deal damage
- apply Burn / Freeze / Mark / Bleed
- gain Guard / Focus
- consume status
- grant Added Value
- split outcome theo die được chọn

4. `Custom Hook`
- chỉ dùng khi skill thực sự vượt khỏi khung chuẩn

#### Condition system nên làm được những gì

Condition chuẩn nên cover tốt 5 nhóm sau:

1. `Dice Parity`
- any base odd
- any base even
- highest base is odd/even
- lowest base is odd/even

2. `Crit / Fail`
- any die crit
- any die fail
- selected die crit
- selected die fail

3. `Exact Value`
- any base value = N
- highest base = N
- lowest base = N
- all bases in group = N

4. `Resource / Action-space`
- current focus >= N
- occupied slots = N
- remaining slots = N
- current action is 1/2/3-slot

5. `Simple State / Board checks`
- target has Burn / Freeze / Chilled / Mark / Bleed
- enemies with status >= N
- combat history count >= N

Nếu condition system cover chắc 5 nhóm này, phần lớn skill thường sẽ không cần code riêng.

#### Condition system không nên ôm những gì

Không nên cố nhét vào condition system các case như:

- alternate mode qua từng lần dùng nếu skill có state machine riêng
- branch sequencing quá đặc thù
- rule rewrite làm đổi hẳn cách resolve của game
- effect có animation / propagation / timing cực riêng
- multi-step engine cần state lưu riêng cho skill

Các case này nên đi vào:

- `Effect / Outcome layer`
- hoặc `custom hook`

#### Quy tắc thực dụng khi author skill

Nếu một skill có thể mô tả bằng:

- `đọc 1-2 reference`
- `so sánh 1 điều kiện`
- `bật 1-2 outcome chuẩn`

thì **không được code riêng**.

Chỉ cho phép custom code khi skill cần ít nhất một trong các thứ sau:

- state riêng của chính skill qua nhiều lần dùng
- timing đặc biệt không khớp flow chuẩn
- rule rewrite lên die/action/board
- split-role quá đặc thù không map nổi vào branch chuẩn
- propagation / retrigger / copy logic đặc biệt

#### Rule authoring rất quan trọng

- `Condition` mặc định phải đọc từ `Base Value`.
- `Added Value` là lớp thưởng của output, không phải input mặc định của condition.
- `Single-output skill` được phép dùng `Total Added Value` của action.
- `Split-role skill` phải resolve theo `per-die Base/Added/Crit/Fail` của đúng die được chọn cho từng branch.
- Một skill thường chỉ nên có `1 condition chính`.
- Rare rất đặc biệt mới nên có `2 condition đáng kể`.

### 3.4E Bộ condition preset nên có trên Inspector

Để author dễ hình dung, Inspector không nên bắt đầu bằng raw clause.
Bề mặt authoring nên bắt đầu bằng `preset theo hệ`.

Ví dụ preset đủ dùng:

#### Fire
- Any Base Odd
- Any Base Even
- Exact All Bases = N
- Highest Base = N
- Lowest Base = N
- Occupied Slots = N
- Remaining Slots = N
- Target Has Burn

#### Ice
- Any Base Odd
- Any Base Even
- Exact All Bases = N
- Highest Base = N
- Lowest Base = N
- Occupied Slots = N
- Target Is Frozen
- Target Is Chilled

#### Lightning
- Any Base Odd
- Any Base Even
- Any Die Crit
- Any Die Fail
- Highest Base = N
- Lowest Base = N
- Target Has Mark
- Marked Enemies >= N

#### Physical
- Any Base Odd
- Any Base Even
- Any Die Crit
- Any Die Fail
- Highest Base = N
- Lowest Base = N
- Current Focus >= N
- Occupied Slots = N
- Remaining Slots = N

#### Bleed
- Any Base Odd
- Any Base Even
- Any Die Crit
- Any Die Fail
- Highest Base = N
- Lowest Base = N
- Target Has Bleed
- Total Bleed On Board >= N

### 3.4F Hiện tại author được gì bằng Inspector

Với khung hiện tại, `SkillDamageSO` đã author được các loại rất thường gặp sau mà không cần code riêng:

- `Base Effect`
  - `Deal Flat Damage`
  - `Deal X Damage`
  - `Apply Flat Burn`
  - `Apply X Burn`
- `Condition`
  - `Dice Parity`
  - `Crit / Fail`
  - `Exact Value`
  - `Resource`
  - `Slot Space`
  - `Target State`
  - `Board State`
- `If Condition Met`
  - `Deal Damage`
  - `Apply Burn`
  - `Gain Guard`
  - `Gain Added Value`
- `Split Role`
  - `Lowest selected -> Burn / Guard`
  - `Highest selected -> Burn / Guard`

Nói ngắn:

- `Ignite+` đã map được bằng inspector
- `Fire Slash` đã map được bằng inspector
- `Hellfire` đã map được phần exact-value bằng inspector, phần consume/reapply vẫn dùng effect đã có
- `Cauterize` đã map được bằng inspector qua `Split Role`

### 3.4G Những gì vẫn chưa author hoàn toàn bằng Inspector

Ngoài case `slot = x / chiếm toàn bộ số slot còn trống`, hiện tại còn các nhóm sau vẫn chưa được coi là hoàn toàn solved bằng inspector:

1. `Adaptive Slot Occupancy`
- skill đang chiếm `3/2/1` slot theo số slot còn trống
- đây vẫn là mechanic riêng, chưa có builder chuẩn

2. `Split-role output ngoài Burn / Guard`
- hiện `Split Role` mới cover:
  - `Burn`
  - `Guard`
- chưa cover trực tiếp:
  - `Added Value`
  - `Mark`
  - `Freeze`
  - `Bleed`

3. `Một skill cần nhiều condition độc lập rồi mỗi condition mở một outcome khác nhau`
- hiện khung chuẩn tốt nhất khi:
  - có `1 condition chính`
  - và `1 conditional outcome chính`

4. `Rule rewrite / state machine`
- alternate mode theo lần dùng
- copy/move base theo logic nhiều bước
- retrigger / propagation nhiều tầng
- custom timing riêng

5. `Target-state condition ở planning preview sâu`
- model condition đã có chỗ cho `Target Has Burn/Mark/...`
- nhưng nếu muốn preview/planning bám sát target động ở mọi ngữ cảnh, còn cần nối sâu hơn vào target-selection flow
- Any Base Even
- Any Die Crit
- Any Die Fail
- Highest Base = N
- Lowest Base = N

Raw clause editor chỉ nên là `advanced mode`, không phải mặt authoring chính.

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
- combat history counters, ví dụ:
  - `đã có bao nhiêu kẻ địch từng bị Freeze trong combat này`
  - `skill này đã được dùng bao nhiêu lần trong combat này`
  - `đã có bao nhiêu lần Crit với chính skill này trong run`

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

### 6.1 Parity

- `Even`
- `Odd`
- Cách đọc:
- `1 slot` = die này chẵn/lẻ
- `nhiều slot` = cả cụm đều chẵn/lẻ

### 6.2 Crit / Fail

- `Crit`
- `Fail`
- Cách đọc:
- `1 slot` = die này crit/fail
- `nhiều slot` = cả cụm đều crit/fail

### 6.3 Exact value

- `Die Equals X`
- `Group Contains Pattern`
- `Random Exact Number Owned`
- `Random Exact Number Random`
- `Die Equals X` cho nhập số cụ thể
- `Group Contains Pattern` cho nhập pattern kiểu `1-2-3-5`
- `Random Exact Number Owned` chỉ random trong pool face value mà dice player đang sở hữu
- `Random Exact Number Random` random độc lập, không cần quan tâm player có số đó hay không

### 6.4 Local group / slot relation

- `Self-position`
- `Neighbor relation`
- `Split-role`
- `Self-position` chỉ có `Left / Right`
- `Neighbor relation` chỉ có `Left / Right`
- `Split-role` chỉ có `Highest / Lowest`

### 6.5 Target status

- `Target has Burn`
- `Target has Mark`
- `Target has Freeze`
- `Target has Chilled`
- `Target has Bleed`
- `Target has Stagger`
- `Status History (TODO)` hiện chỉ là note mở rộng sau, chưa có runtime logic

### 6.6 Resource

- `Current Focus >= N`
- `Player Guard >= N`
- `Target Guard >= N`

### 6.7 Board / encounter

- `Alive Enemies Count >= N`
- `Enemies With Status Count >= N`

> Guardrail:
> - Skill condition phải bị cắt gọn theo 7 trục trên.
> - Không mở lại `slot space`, `middle slot`, `count crit/fail`, hay exact theo vị trí nếu chưa có build thật sự cần.

---

## 7. Effect module taxonomy — các khối hiệu ứng chuẩn

Skill mới nên được ghép từ 1–3 module dưới đây.

### 7.1 Damage Modules

- `Deal Damage = X`
- `Deal Standard Damage = N (+ Added Value unless stated otherwise)`
- `Deal AoE Damage = X to all`
- `Deal Splash Damage`
- `Deal Bonus Damage if condition met`
- `Deal Overkill Carryover`
- `Deal Damage from Combat History Counter + Added Value`

Rule viết text:

- Nếu skill là damage chuẩn, wording chuẩn nên hiểu là `deal N damage + Added Value`.
- Nếu skill là skill `X`, wording chuẩn nên hiểu là `X = Base Value + Added Value`.
- Nếu skill nhiều slot, `Added Value` mặc định là tổng Added Value của toàn bộ dice trong local group.

### 7.2 Guard Modules

- `Gain Guard = X`
- `Gain Fixed Guard + Added Value`
- `Gain Guard from Highest/Lowest in Group`
- `Gain Guard from Player Resource`
- `Retain Guard`
- `Convert Status into Guard`

Rule chuẩn hóa rất quan trọng:

- `Guard` không nên đi một logic riêng tách khỏi `Damage` nếu bản chất chỉ khác loại output.
- Nếu skill là `fixed Guard output`, wording chuẩn nên hiểu là `gain N Guard + Added Value` trừ khi text ghi rõ ngược lại.
- Nói ngắn gọn: **`fixed output + Added Value` là rule chung cho cả Attack lẫn Guard**.

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
- `Add permanent Added Value to a specific skill for this run`
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

Nếu muốn skill đọc output cuối, phải ghi rất rõ kiểu:
- `X = Base Value + Added Value`
- `deal N damage + Added Value`

Nếu skill nhiều slot, nên ghi rõ hoặc ngầm hiểu theo grammar chuẩn:
- `Added Value` = tổng Added Value của toàn bộ dice trong local group

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
