# AGENTS.md

> Đây là **master context file** của project.
> File này hợp nhất và thay thế vai trò của cả `AGENTS.md` cũ và `GAME_DESIGN_DOCUMENT.md` ở mức handoff / AI context / design reference.
> 
> Mục tiêu của file này là để **bất kỳ người nào hoặc AI nào đọc vào cũng có thể hiểu đúng project mà không cần lục lại chat cũ**.
> 
> Khi có mâu thuẫn giữa suy đoán cá nhân và file này, **ưu tiên file này**.
> Khi một mục được ghi rõ là **đã chốt**, hãy xem nó là current spec.
> Khi một mục được ghi rõ là **chưa chốt**, hãy xem nó là context định hướng / content pool hiện tại, không được tự coi là final implementation nếu user chưa yêu cầu.

---

## 1. Mục đích của file này

File này tồn tại để làm 5 việc cùng lúc:

1. **Design context** — mô tả game là gì, chơi như thế nào, triết lý thiết kế ra sao.
2. **Gameplay spec** — ghi lại các rule combat, dice, status, resource, lane order, payoff đã chốt.
3. **Implementation context** — mô tả source of truth, class chính, cấu trúc thư mục, hướng refactor đã chọn.
4. **Handoff document** — để gửi sang cho người khác hoặc AI khác mà không cần kể lại toàn bộ lịch sử dự án.
5. **Guardrail document** — nói rõ vùng nào ổn, vùng nào không nên đụng vào, vùng nào còn mở.

Đây **không** phải bản tóm tắt ngắn. Đây là bản context đầy đủ.

---

## 2. Cách đọc và cách dùng file này

Khi làm việc với project, phải phân biệt 4 loại thông tin trong file:

### 2.1 Phần đã chốt

Đây là current spec.
Nếu sửa code hoặc viết design note, phải bám theo đúng rule ở đây.
Không được tự diễn giải khác đi chỉ vì nhớ nhầm từ chat cũ.

### 2.2 Phần đang dùng làm định hướng nhưng chưa chốt

Đây là pool ý tưởng / content direction / design frame hiện tại.
Được giữ lại để không mất context, nhưng **không được mặc định implement cứng** nếu user chưa xác nhận làm phần đó.

### 2.3 Phần mô tả hiện trạng code

Đây là trạng thái runtime / architecture ở thời điểm file được viết.
Mục tiêu là tránh đập phá nhầm những hệ đang ổn.

### 2.4 Phần quy tắc làm việc

Đây là guideline cho AI / người sửa project.
Nếu task mới không yêu cầu refactor lớn, ưu tiên patch nhỏ, đúng spec, dễ review, dễ test.

---

## 3. Tổng quan project

Đây là game roguelike turn-based trên mobile.
Thể loại đúng nhất hiện tại là:

**Dice-driven Tactical Combat Roguelike**

Các nguồn cảm hứng chính:

- **Balatro** — passive/joker thay đổi luật chơi, custom dice giống custom deck/card, consumable giống tarot/use item, endless mode, build pursuit.
- **Slay the Spire** — combat turn-based rõ nhịp, intent system, boss như bức tường cơ chế, build synergy.
- **Persona / Expedition 33** — bộ skill cố định theo loadout, focus/energy economy, emphasis vào lựa chọn action hơn là spam UI phức tạp.
- **D&D** — Crit/Fail, nhiều loại dice, exact value, mặt xúc xắc là identity thật của quyết định.

### 3.1 Bản sắc cốt lõi

Trọng tâm của game là:

- dice
- skill slot
- passive
- status / payoff
- lane order / action order
- setup -> detonate

Tất cả hệ thống phải quay về một ý chính:

**dice là trung tâm, mọi thứ xoay quanh dice.**

Game không đi theo hướng ép người chơi phải tính nhẩm quá nhiều lớp modifier.
Về lâu dài, tooltip / preview / runtime text phải hiển thị kết quả đã resolve, không bắt người chơi tự tính Base, Added và các bonus ẩn để mới chơi được.

### 3.2 Trải nghiệm mong muốn

Mỗi turn, người chơi nhìn dice vừa roll ra rồi quyết định:

1. dùng die nào cho skill nào,
2. dùng thứ tự nào,
3. setup trước hay nổ trước,
4. lock plan và chấp nhận kết quả.

Game thiên về:

- ra quyết định chiến thuật,
- đọc board,
- đọc trạng thái,
- tận dụng đúng die đúng thời điểm,
- build synergy theo run,

chứ không thiên về spam thao tác cơ học hoặc tính toán quá rối.

---

## 4. Core Loop

Vòng chơi combat cốt lõi:

```text
Roll dice -> Plan (assign dice vào skill slot, reorder) -> Lock -> Execute (trái sang phải) -> Enemy turn
```

Giải thích đúng ý:

- **Roll**: roll tất cả dice đang equip cho turn đó.
- **Plan**: gán dice vào skill slot, có thể reorder cặp dice/skill trong planning.
- **Lock**: xác nhận không đổi nữa.
- **Execute**: skill chạy từ trái sang phải theo lane hiện tại.
- **Enemy turn**: enemy hành động, status tick / cleanup, end turn logic.

Đây là khung xương sống của combat. Không được sửa tùy tiện nếu không có bug thật hoặc user yêu cầu đổi design.

---

## 5. Action Economy / Loadout Economy

### 5.1 Những gì player có trong combat

Player có:

- **6 skill slot**
- **1 passive slot**
- **1 đến 3 dice**

Đầu run player bắt đầu với **1 dice**.
Tối đa trong 1 turn chỉ có **3 dice**, tức tối đa **3 action groups** để plan.

### 5.2 Focus economy

Rule hiện tại đã chốt:

- **Max Focus = 9**
- **Start combat = 2 Focus**
- **Đầu mỗi turn = +1 Focus**
- **Không cho nợ Focus**
- Vì vậy **turn 1 thực tế = 3 Focus**

### 5.3 Basic actions

Basic actions luôn có sẵn, không nằm trong 6 skill slot chính:

- **Basic Attack**
  - 0 Focus
  - 4 damage gốc
  - cho **+1 Focus**

- **Basic Guard**
  - 0 Focus
  - Guard = **Base Value** của die dùng cho hành động đó

Basic actions là anchor của economy.
Chúng tồn tại để người chơi luôn còn lựa chọn hợp lệ kể cả khi roll xấu hoặc thiếu Focus.

### 5.4 Loadout ngoài combat

Loadout system hiện tại theo định hướng:

- **6 skill slot**: swap tự do ngoài combat, mua ở shop, bán ngoài combat.
- **1 passive slot**: swap tự do ngoài combat.
- **1-3 dice slot**: số dice equip = số action/turn.
- **Basic Attack + Basic Guard** luôn tồn tại, không thể bỏ khỏi loadout.

---

## 6. Dice System — Current Locked Rules

Đây là phần cực quan trọng. Rất nhiều logic combat, UI preview, passive và content layer phải bám đúng phần này.

### 6.1 Loại dice

- Bắt đầu run với **1 dice**, tối đa **3 dice**.
- Dice có thể là **d2, d4, d6...**
- Mặt dice có thể custom.
- Dice không chỉ là số damage; dice còn quyết định condition, crit/fail, exact value, parity, threshold, v.v.

### 6.2 Base Value và Added Value

Rule đã chốt:

- **Base Value** = mặt thật roll ra
- **Added Value** = phần cộng thêm vào output cuối
- **Mọi condition phải đọc từ Base Value**
- Added Value **không đổi bản chất** của die
- Với skill damage chuẩn, output cuối mặc định là **damage gốc + Added Value**
- Với skill `X`, mặc định **`X = Base Value + Added Value`**
- Nếu skill chiếm 2 hoặc 3 slot, Added Value mặc định là **tổng Added Value của toàn bộ dice trong nhóm skill đó**
- Dice có thể mang / sinh Added Value từ **Crit** hoặc từ **enchant / consumable / dice customization** đã gắn trên mặt; các nguồn ngoài dice có thể đến từ skill/passive/relic ghi rõ

Các condition phải đọc theo Base Value gồm:

- chẵn / lẻ
- crit / fail
- <= 3
- >= ngưỡng nào đó
- exact value
- highest / lowest

Không được để hệ thống condition đọc từ số đã cộng modifier xong, vì như vậy sẽ phá identity của dice.

### 6.3 Local die context

Nếu skill 2-3 slot có text kiểu:

- die cao nhất,
- die thấp nhất,
- exact value trong nhóm,

thì **chỉ xét trong chính nhóm dice của skill đó**, không xét cả bàn.

Đây là rule đã chốt.
Không được tự đổi thành global board context nếu không có chỉ đạo mới.

### 6.4 Crit / Fail

Rule đã chốt:

- **Crit** = roll đúng giá trị mặt cao nhất của die
- **Fail** = roll đúng giá trị mặt thấp nhất của die
- Crit / Fail **không đổi Base Value**
- Crit sinh Added Value / bonus output
- Fail chỉ cắt nửa **damage gốc của skill**, không trừ Added Value và không đổi Base Value

Các hệ số đã chốt hiện tại:

- **Crit thường = +20% Base**
- **Crit Physical = +50% Base**
- **Fail = 50% damage gốc của skill**

Ví dụ cộng dồn theo nhiều dice:

- Skill 2 slot có `5 damage gốc`, gắn vào `d10 crit` và `d20 crit`, không phải Physical -> `5 + floor(10 x 0.2) + floor(20 x 0.2) = 11`.
- Nếu skill đó là Physical -> `5 + floor(10 x 0.5) + floor(20 x 0.5) = 20`.
- Skill 3 slot cũng cộng theo đúng logic đó với cả 3 die trong nhóm.
- Nếu skill 2 hoặc 3 slot có `ít nhất 1 Fail`, action đó chỉ bị `cắt 50% damage gốc` đúng **1 lần**.
- `2 Fail` hoặc `3 Fail` trong cùng action không stack fail penalty thêm.

### 6.5 Trường hợp nhiều mặt cùng max / min

Rule đã chốt:

- Nhiều mặt cùng max -> tất cả là Crit
- Nhiều mặt cùng min -> tất cả là Fail
- Nếu **max == min** -> tất cả là Crit, không có Fail
- Trong case `max == min`, **Crit thắng Fail**

Điều này rất quan trọng khi dice bị custom mạnh và không còn là d6 bình thường.

### 6.6 Làm tròn

Rule đã chốt:

- Toàn game dùng **floor**
- Damage sau tính toán nếu < 1 thì vẫn là **minimum 1**
- Ngoại lệ: nếu Guard chặn hết thì có thể không mất HP

### 6.7 Pipeline dice math

Pipeline mong muốn và đang được dùng như source design:

```text
baseValue -> critAddedValue -> passiveSkillConditionalAddedValue -> totalAddedValue -> finalActionOutput
```

`DiceSlotRig` là **source of truth chính** cho dice math.

Mọi preview / execute / tooltip runtime về lâu dài phải bám cùng một nguồn số, không được mỗi nơi tự tính một kiểu.

### 6.7A Combat Formula Sheet

- `Base Value` = mặt thật của die, dùng để check mọi condition.
- `Added Value` = phần cộng vào output cuối, không đổi bản chất của die.
- `Crit`:
  - non-Physical -> `Added Value += floor(Base Value x 0.2)`
  - Physical -> `Added Value += floor(Base Value x 0.5)`
- `Fail` -> chỉ làm `damage gốc = floor(damage gốc / 2)`, không trừ `Added Value`.
- `Skill damage chuẩn` -> `Final Damage = damage gốc + Total Added Value`
- `Skill X` -> `X = Base Value + Added Value`
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
- Damage dương mà sau tính toán `< 1` thì thành `1` tối thiểu

### 6.8 Dice customization

Dice không phải hệ tĩnh.
Custom dice là một phần identity của game.

Current design direction:

- Mỗi mặt dice có thể tăng **Base Value** bằng consumable.
- Đơn vị cơ bản của progression là **+1 base cho 1 mặt**.
- Giá trị tối đa của 1 mặt hiện tại được hiểu là **99**.
- Kể cả d2 vẫn có thể bị đẩy thành những mặt cực lớn như `1 / 99` nếu run đầu tư đủ sâu.
- Việc mod dice làm thay đổi toàn bộ logic của Crit / Fail / Exact / Even / Odd / Threshold.

### 6.9 Reorder trong Planning

Rule / định hướng đã chốt:

- Player có thể kéo cặp **dice + skill** về bất kỳ vị trí nào trong **Planning phase**.
- Reorder ảnh hưởng cả:
  - **dice assignment**
  - **thứ tự execute**
- Khi bấm sang Execute phase, reorder bị khóa.

Ví dụ đúng tinh thần:

- roll được một die rất cao,
- kéo nó xuống cuối,
- dùng 1-2 lane trước để setup Mark / phá Guard / áp trạng thái,
- dùng lane cuối làm finisher.

### 6.10 Ý nghĩa thiết kế của dice

Dice không chỉ là “mana ngẫu nhiên”.
Dice là identity engine của combat.
Nó quyết định:

- output,
- điều kiện kích hoạt,
- thứ tự finisher,
- hướng build,
- và cả cách passive tương tác.

Bất kỳ hệ mới nào thêm vào cũng phải tôn trọng vai trò trung tâm của dice.

---

## 7. Turn Flow — Current Locked Flow

Flow hiện tại đã chốt như sau:

1. **Start Phase**
   - +1 Focus
   - passive đầu lượt
   - enemy chốt intent

2. **Roll Phase**
   - roll các dice hiện có

3. **Planning Phase**
   - kéo dice vào skill slot
   - reorder được trong phase này

4. **Execution Phase**
   - skill chạy từ trái sang phải theo lane hiện tại
   - vào execute thì khóa reorder

5. **Enemy / End Phase**
   - enemy hành động
   - xử lý status tick / cleanup
   - player mất Guard cuối lượt trừ khi có rule giữ Guard

Phần reorder / lane mapping / phase lock hiện được xem là đã ổn ở mức nền tảng.
Không được tự ý đập lại nếu không có bug cụ thể.

---

## 8. Core Combat Rules — Current Locked Rules

### 8.1 Damage

- Damage trừ Guard trước
- Phần dư mới vào HP

### 8.2 Stagger

Stagger là rule đã chốt và đã được coi là core behavior.

Rule cụ thể:

- Khi Guard từ **> 0** về **0**, mục tiêu vào trạng thái **Stagger**
- Overflow của hit phá Guard vẫn vào HP **bình thường**
- Overflow đó **không** được nhân `x1.2`
- Chỉ **hit kế tiếp duy nhất** mới ăn `x1.2 tổng damage`
- Hết turn mà không có hit bồi thì **Stagger biến mất**

Chi tiết rất quan trọng:

- `Lightning shock` **không consume** Stagger
- `Bleed tick` **không consume** Stagger
- `Burn consume` nếu nằm trong **direct-hit** thì được tính vào tổng damage trước khi nhân `1.2`

### 8.3 Skill Tags

Nhóm tag nền tảng hiện tại:

- `Attack`
- `Guard`
- `Status`
- `Sunder`

Rule đã chốt:

- `Sunder` **không có bonus ẩn**
- Nếu có tương tác đặc biệt, phải được viết rõ trong rule hoặc text thật

Không được ngầm buff hoặc thêm hidden multiplier cho Sunder chỉ vì cảm giác “phải mạnh hơn”.

---

## 9. Elements / Status / Payoff Systems — Current Locked Rules

### 9.1 Physical

Vai trò:

- burst
- anti-Guard
- damage thẳng

Rule đáng nhớ:

- Crit Physical dùng **`+50% Base`**

### 9.2 Fire / Burn

Burn **không phải DoT chính**.
Burn là **resource để consume**.

Rule đã chốt:

- Burn có stack
- Mỗi lần apply Burn tạo ra **một batch Burn riêng** tồn tại **3 turn**
- Tổng Burn hiển thị = tổng mọi batch Burn còn sống
- Khi một batch hết hạn, chỉ batch đó biến mất; batch apply sau vẫn được giữ lại
- Consume mặc định = **`+2 damage mỗi stack Burn bị xóa`**
- Chỉ skill đặc biệt mới được override con số baseline này

Ví dụ:

- Turn 1 áp `7 Burn`
- Turn 3 áp thêm `9 Burn` -> mục tiêu hiện `16 Burn`
- Sang turn sau, batch `7 Burn` cũ hết hạn -> mục tiêu còn `9 Burn`

Ý nghĩa thiết kế:

- Fire là hệ thiên về setup tài nguyên rồi nổ burst
- Burn là nguyên liệu cho các Fire payoff, không phải poison clone
- Fire phải giữ nhịp add Burn rồi detonate sớm, không nên giữ Burn quá lâu

### 9.3 Ice / Freeze / Chilled

Rule đã chốt:

- **Freeze**: skip 1 turn
- Hết Freeze -> thành **Chilled**
- **Chilled tồn tại 2 turn**
- Đang Freeze hoặc Chilled thì **miễn Freeze mới**
- Ice damage hit vào target đang Freeze / Chilled -> player nhận **`+1 Focus +3 Guard`**

Ý nghĩa thiết kế:

- Ice không chỉ là CC
- Ice tạo cửa sổ tempo / khai thác tài nguyên
- Freeze -> Chilled là nhịp build-up rồi payoff

### 9.4 Lightning / Mark

Mark là điểm yếu để **direct-hit** khai thác.
Mark **không stack**.

Rule đã chốt:

- Mark không stack
- shock phụ của Lightning **không làm mất Mark**

#### Non-Lightning hit vào Mark

- gây thêm **`+4 direct damage`** lên chính mục tiêu đó

#### Lightning hit vào Mark

- hit chính gây damage bình thường
- sau đó proc **`4 damage all enemies`**

#### Rule của shock phụ Lightning

- không cộng Added Value
- không tiêu Mark
- không proc Mark
- không chain tiếp

#### Nếu AoE Lightning direct-hit nhiều mục tiêu có Mark

- mỗi mục tiêu có Mark tạo **1 shock proc**
- shock chạy **tuần tự**
- mỗi proc cách nhau **`0.2s`**

Ý nghĩa thiết kế:

- Mark là weak point / mồi nổ
- Lightning là hệ board-control / chain payoff
- Shock phụ phải rõ ràng, gọn, không tự cascade vô hạn

### 9.5 Bleed

Rule đã chốt:

- Bleed gây damage **đầu lượt**
- Bleed **bỏ qua Guard**
- Bleed **giảm dần theo lượt**
- current wording rõ nhất hiện tại là **`-1 stack mỗi cuối lượt`**

Ý nghĩa thiết kế:

- Bleed là áp lực tích lũy theo thời gian
- khác Burn ở chỗ nó là damage-over-time thật và có thể đổi thành resource ở build phù hợp

---

## 10. Ailment System

Ailment hiện được coi là **enemy-side system**.

Nghĩa là:

- không xem ailment là nhóm skill player cast lên enemy trong core design hiện tại
- enemy là bên chủ yếu dùng ailment lên player / combat state
- nếu trong code còn helper player -> enemy thì đó **không còn là gameplay ưu tiên**

Rule chance hiện tại trong code / context:

- **enemy -> player = 100% chance**

Khi mô tả system, tooltip, refactor plan hoặc content note, **không mô tả Ailment như bộ skill player-side tiêu chuẩn**.

---

## 11. Tooltip / Preview / Runtime Number Direction

Đây là section phải giữ riêng, không được bỏ qua.

### 11.1 Hướng design đã chốt

Trong shop / khi chưa có dice:

- hiện dạng tĩnh, ví dụ:
  - `Deal X damage`
  - `Gain X Guard`

Trong combat / khi đã roll:

- hiện **số thật đã resolve**

### 11.2 Nguyên tắc quan trọng

Tooltip / preview / execute về sau phải dùng **cùng một nguồn số**.

Không được để:

- tooltip nói một kiểu,
- preview nói một kiểu,
- execute ra kết quả kiểu khác.

### 11.3 Trạng thái hiện tại

- đã có preview nền tảng ở mức icon / runtime hiển thị cơ bản
- formatter runtime text chưa cần chốt ngay
- phần này nên làm sau khi skill data được chuẩn hóa ổn định hơn

Đây là vùng quan trọng về UX, nhưng chưa phải ưu tiên số 1 nếu combat core đang cần ổn định.

---

## 12. Lane Mapping / Reorder / Pair Identity Rule

Đây là section cực quan trọng và phải giữ riêng.

### 12.1 Rule cốt lõi đã chốt

**`1/2/3` là identity của cặp dice/icon, còn `A/B/C` là lane hiện tại. Logic phải đọc theo `A/B/C`, không đọc theo `1/2/3`.**

Giải thích ý nghĩa:

- `1/2/3` chỉ là danh tính ban đầu của pair / icon / entry
- khi reorder trong Planning, pair đó có thể di chuyển sang lane khác
- lúc Execute, logic phải đọc **lane hiện tại**, không bám vào identity cũ

### 12.2 Những phần đang được xem là ổn

Các vùng sau hiện được xem là ổn ở mức nền tảng:

- reorder trong Planning
- execution order theo lane mới
- damage order theo lane mới
- phase lock khi vào execute

### 12.3 Điều cấm kỵ khi sửa

Không được:

- đọc thứ tự execute theo `1/2/3` cố định
- để UI reorder chỉ đổi hình mà logic không đổi lane thật
- để damage order bám identity cũ thay vì lane mới
- đập lại reorder chỉ vì muốn “refactor cho sạch” nếu chưa có bug rõ ràng

### 12.4 Ghi nhớ thực dụng

Nếu có bug lane mapping, phải test lại toàn chuỗi:

- planning order
- target order
- execution order
- damage order
- preview / icon mapping
- phase lock khi chuyển Execute

Đây là hệ nhạy cảm. Nếu nó đang ổn, đừng chạm vào.

---

## 13. Relic / Consumable System

> Phần này là current framework. Chi tiết pool relic chưa hoàn toàn populate xong.

### 13.1 Khung chung

- Player có **3 relic slot**
- Relic là **one-shot consumable**
- Dùng xong mất khỏi slot ngay
- Relic có thể drop từ enemy hoặc đến từ system/passive khác
- Pool relic lớn là chủ đích — tìm đúng relic cần là chuyện khó, giống tinh thần Balatro tarot / utility item

### 13.2 Type A — dùng trong combat

- là quick action
- không chiếm dice slot
- có thể yêu cầu chọn target

### 13.3 Type B — chỉnh dice ngoài combat

- mở overlay chỉnh dice / face
- các thao tác kiểu:
  - forge `+/- pip`
  - move pip
  - copy face value
  - tăng base một mặt

### 13.4 Vai trò thiết kế

Relic tồn tại để:

- tạo progression run,
- bẻ hướng build,
- cho utility đột biến,
- hỗ trợ custom dice mà không làm hệ dice mất kiểm soát quá sớm.

Đơn vị tăng trưởng cơ bản hiện tại vẫn là:

**`+1 base cho 1 mặt`**

---

## 14. Boss Design Philosophy

Boss hiện được định nghĩa theo triết lý sau:

- Boss là **bức tường cơ chế**, không phải stat wall đơn thuần
- Boss **không hard counter hoàn toàn** bất kỳ build nào
- Boss tạo **friction**, không tạo “cấm chơi hệ này”
- Player luôn nên có ít nhất 3 hướng:
  - adapt,
  - chấp nhận đánh khó hơn,
  - brute force bằng build đủ mạnh

Ví dụ đúng tinh thần:

- Không nên làm kiểu: `miễn Burn hoàn toàn`
- Nên làm kiểu: `đầu lượt tự clear 2 Burn stack`
- Không nên làm kiểu: `reflect mọi damage`
- Nên làm kiểu: `mỗi khi nhận Burn thì +2 Guard`

Mục tiêu là tạo áp lực chiến thuật chứ không vô hiệu hóa công sức build của player.

---

## 15. Endless Mode và Hidden Boss

### 15.1 Endless Mode

Sau khi thắng boss cuối, player có thể tiếp tục đi Endless.

Mục tiêu của Endless:

- hoàn thiện build,
- test sức mạnh build,
- đạt trạng thái “full build hoàn hảo”,
- phục vụ kiểu chơi muốn optimize run xa hơn chiến thắng đầu tiên.

### 15.2 Hidden Boss

Current design direction:

- chỉ xuất hiện trong **Endless mode**
- nằm đủ sâu để không lộ quá sớm
- nhưng không quá sâu đến mức full build xong vẫn không gặp nổi
- cơ chế của hidden boss là **xoay vòng ability của các boss khác** theo chu kỳ, ví dụ mỗi `2-3 turn`
- hidden boss không dùng tất cả ability cùng lúc; nó lấy từ pool ability của boss toàn game

Đây là thử thách tối thượng, không phải phần bắt buộc của loop thường.

---

## 16. Lenticular Design / Build Philosophy

Đây là triết lý thiết kế rất quan trọng của project.

### 16.1 Nguyên tắc cốt lõi

Không làm game theo kiểu:

- 20 skill = 20 build rời nhau

Mà làm theo kiểu:

- một số skill / passive / resource tương tác được với nhau
- build nảy sinh từ **mạng lưới tương tác**, không phải từ “bộ set cứng độc lập”

Mục tiêu là:

- ít content hơn nhưng nhiều tổ hợp hơn,
- kỹ năng có nhiều lớp giá trị,
- build mới đến từ quan hệ giữa các phần tử.

### 16.2 Phân lớp rarity / độ sâu

- **Common**: tự đủ dùng, hiểu nhanh, ít cần setup, mạnh ổn định ngay.
- **Uncommon**: mạnh hơn khi canh điều kiện, đúng tình huống thì bùng rõ.
- **Rare**: một mình vẫn hữu ích nhưng không gánh ngay; khi đúng engine thì cực mạnh.

### 16.3 Trục tương tác chính hiện tại

#### Trục dice

- Crit / Fail
- Chẵn / Lẻ
- Cao / Thấp
- Exact value
- Highest / Lowest trong nhóm

#### Trục trạng thái trên target

- Burn stacks
- Mark
- Freeze / Chilled
- Bleed stacks
- Stagger

#### Trục tài nguyên player

- Focus hiện tại
- Guard hiện tại
- lane đã dùng / còn trống

Mọi skill / passive mới nên bám một hoặc nhiều trục trên thay vì tạo mechanic lạc hệ.

---

## 17. Skills — Current Content Context

> **Rất quan trọng:** Section này giữ lại để không mất context content.
> 
> Không phải toàn bộ skill ở đây đều đã chốt final.
> Hiện tại chỉ có một số cái được xem là chốt rõ ràng, còn nhiều skill vẫn là pool thiết kế đang mở.

### 17.1 Skill đã chốt rõ

#### Ember Weapon [Status]
- **1 slot, 2 Focus**
- Trong **3 turn tiếp theo**, Basic Attack gây thêm **+1 damage** và áp **Burn = tổng damage gây ra**

Ví dụ:
- Basic Attack base là 4 damage gốc
- Có Ember Weapon -> thành 5 damage gốc trước Added Value
- áp 5 Burn mỗi hit
- nếu đi kèm passive tăng stack như `Elemental Catalyst`, lượng Burn applied có thể tăng thêm theo rule passive

#### Hellfire [Attack]
- **3 slot, 2 Focus**
- Điều kiện kích hoạt là mục tiêu đã có Burn sẵn
- Khi kích hoạt:
  - consume toàn bộ Burn
  - gây **3 damage mỗi stack Burn**
  - sau đó, nếu mỗi dice trong nhóm rơi đúng **7**, áp lại **7 Burn mới**

Ý nghĩa thiết kế:

- `exact 7` là điều kiện cố ý để gắn với custom dice
- skill này là win condition cực sâu của Fire build
- anti-synergy cứng với passive làm thay đổi mặt dice như `Crit Escalation` là **intentional**

### 17.2 Pool skill chưa chốt — hệ Physical

#### Precision Strike [Attack]
- **1 slot, 2 Focus**
- Gây X damage
- Nếu Base Value chẵn, đòn này luôn Crit
- Ý nghĩa: skill dễ hiểu, tận dụng parity + crit

#### Brutal Smash [Attack]
- **1 slot, 1 Focus**
- Gây 12 damage gốc
- Nếu mục tiêu đã có Mark trước khi trúng, hồi ngay 1 Focus

#### Heavy Cleave [Attack]
- **2 slot, 3 Focus**
- Gây damage = X + Base Value cao nhất trong 2 dice đang dùng

#### Execution [Attack]
- **3 slot, 4 Focus**
- Gây tổng X damage
- Nếu mục tiêu chết bởi đòn này, overkill damage cộng vào Added Value cho đòn `Attack` hoặc `Sunder` đầu tiên của lượt kế

#### Sunder [Sunder]
- **2 slot, 2 Focus**
- Gây X damage
- Bỏ qua hoàn toàn Guard và xóa toàn bộ Guard của mục tiêu

#### Fated Sunder [Sunder]
- **1 slot, 2 Focus**
- Gây 2 damage
- Nếu Base Value đúng bằng “số định mệnh” của combat đó, xóa sạch Guard trước khi gây sát thương
- Không được hưởng `x1.2` từ Stagger

### 17.3 Pool skill chưa chốt — hệ Fire

#### Ignite [Status]
- **1 slot, 2 Focus**
- Áp X Burn
- Nếu Base Value lẻ, áp thêm 2 Burn

#### Cauterize [Status/Guard]
- **2 slot, 2 Focus**
- Áp Burn = dice thấp nhất
- Nhận Guard = dice cao nhất trong 2 dice đang dùng

#### Fire Slash [Attack]
- **1 slot, 2 Focus**
- Gây X damage
- Consume toàn bộ Burn -> `+2 damage mỗi stack Burn`

> `Ember Weapon` và `Hellfire` đã nằm ở phần skill chốt phía trên.

### 17.4 Pool skill chưa chốt — hệ Ice

#### Deep Freeze [Status]
- **1 slot, 3 Focus**
- Áp Freeze lên mục tiêu
- Không tác dụng nếu mục tiêu đã có Freeze hoặc Chilled

#### Shatter [Attack]
- **1 slot, 2 Focus**
- Gây X damage
- Nếu mục tiêu đang Chilled -> cộng thêm 50% Guard hiện có của player, tối đa +20

#### Frost Shield [Guard]
- **2 slot, 2 Focus**
- Nhận X Guard

#### Winter's Bite [Attack]
- **1 slot, 1 Focus**
- Gây 6 damage cố định
- Kéo dài Chilled thêm 1 turn

#### Permafrost Chain [Status]
- **2 slot, 6 Focus**
- Rare
- Áp Freeze lên 1 địch
- Khi Freeze hết -> nảy sang 1 địch khác, ưu tiên mục tiêu chưa bị khống chế

#### Cold Snap [Attack]
- **2 slot, 2 Focus**
- X = dice thấp nhất trong cụm
- Gây X damage và Freeze ngẫu nhiên 1 địch

### 17.5 Pool skill chưa chốt — hệ Lightning

#### Static Conduit [Status]
- **2 slot, 2 Focus**
- Gây 4 damage
- Nếu mục tiêu có Mark -> áp Mark lên toàn bộ kẻ địch còn lại

#### Flash Step [Status]
- **1 slot, 1 Focus**
- Áp Mark lên mục tiêu
- Ghi đè giá trị Base Value của dice tiếp theo được kéo vào bằng giá trị dice hiện tại

#### Spark Barrage [Attack]
- **1 slot, 2 Focus**
- Gây X damage
- Nếu Base Value chẵn -> hit này nảy sang 1 mục tiêu khác

#### Overload [Attack]
- **1 slot, 2 Focus**
- Gây X damage cộng thêm **4 damage cho mỗi Mark đang có trên toàn sân**

#### Thunderclap [Attack]
- **3 slot, 4 Focus**
- X = dice cao nhất trong cụm
- Gây X damage lên tất cả
- cộng thêm 4 damage cho mỗi kẻ địch đang có Mark

### 17.6 Pool skill chưa chốt — hệ Bleed

#### Lacerate [Attack]
- **1 slot, 3 Focus**
- Áp X Bleed
- Nếu Crit -> áp thêm X Bleed nữa

#### Blood Ward [Guard]
- **1 slot, 2 Focus**
- Nhận Guard = tổng số Bleed stack đang có trên tất cả kẻ địch

#### Siphon [Status]
- **2 slot, 3 Focus**
- Consume toàn bộ Bleed trên 1 mục tiêu
- Cứ 5 Bleed tiêu thụ -> tạo 1 Consumable ngẫu nhiên, tối đa 3

#### Hemorrhage [Attack]
- **2 slot, 2 Focus**
- Áp Bleed lên mục tiêu bằng đúng số HP mà player đã mất ở lượt trước đó

### 17.7 Vai trò của section skill này

Section này được giữ lại để:

- không mất content direction,
- giữ pool thiết kế hiện tại,
- để AI / người khác hiểu build intent,
- nhưng vẫn phân biệt rõ cái nào đã chốt và cái nào chưa chốt.

Không được đọc section này theo kiểu “mọi thứ đều final”.

---

## 18. Passives — Current Content Context

> Section này cũng là context content, không phải toàn bộ đều final implementation.

### 18.1 Danh sách passive hiện tại

| Passive | Rarity | Text hiện tại |
|---|---|---|
| **Crit Escalation** | Rare | Mỗi khi roll Crit -> toàn bộ mặt của các dice nhận +1 base cho đến hết combat. |
| **Dice Forging** | Rare | Lần đầu tiên mỗi trận dùng Basic Attack -> mặt dice vừa dùng nhận +1 base vĩnh viễn cho toàn bộ Run. |
| **Clear Mind** | Rare | Bắt đầu mỗi lượt, hồi thêm 1 Focus. |
| **Iron Stance** | Rare | Guard không biến mất vào cuối lượt của Player. |
| **Even Resonance** | Uncommon | Mỗi dice có Base Value chẵn nhận thêm +3 Added Value cho đòn đó. |
| **Elemental Catalyst** | Uncommon | Khi địch nhận Burn hoặc Bleed, cộng thêm 1 stack khuyến mãi. |
| **Spiked Armor** | Uncommon | Khi địch đánh vào Guard của bạn, chúng nhận lại Physical damage bằng lượng Guard bị phá vỡ. |
| **Mitigation (Desperate Guard)** | Common | Dùng Basic Guard bằng dice có Base Value <= 3 -> tạo 1 Consumable. |
| **Fail Forward** | Common | Mỗi khi roll ra mặt thấp nhất / Fail -> nhận ngay 3 Guard. |
| **Alchemist** | Common | Bắt đầu mỗi combat, nhận 1 Consumable ngẫu nhiên. |

### 18.2 Notes rất quan trọng về passive

- `Crit Escalation` anti-synergy cứng với `Hellfire` vì Hellfire cần exact value 7, còn Crit Escalation đẩy mặt dice lên.
- `Clear Mind` kết hợp với `Hellfire` có thể tạo loop focus tự duy trì.
- `Dice Forging` và `Crit Escalation` có thể dùng chung, nhưng một cái là tăng trưởng vĩnh viễn theo run, một cái là tăng trưởng trong combat.

### 18.3 Vai trò thiết kế của passive

Passive không chỉ là stat stick.
Passive là nơi game thay đổi luật chơi ở cấp macro:

- đổi cách đọc crit/fail,
- đổi tốc độ economy,
- đổi giá trị của Basic actions,
- mở engine build dài hơi.

---

## 19. Combo Engines đã identify

Phần này giữ lại để AI / người khác hiểu các build backbone mà design hiện đang nhắm tới.

### 19.1 Fire Loop Engine

Ví dụ flow:

```text
Ember Weapon -> Basic Attack x2 -> tích Burn -> Hellfire detonate
```

Nếu đi kèm passive tăng stack như `Elemental Catalyst`, lượng Burn và damage nổ tăng mạnh.
Nếu custom dice để mặt 7 xuất hiện đúng ý, Hellfire có thể trở thành loop win condition của cả run.

### 19.2 Lightning Board Control

Ví dụ flow:

```text
Flash Step -> Static Conduit -> Overload
```

hoặc:

```text
Static Conduit -> Thunderclap
```

Ý tưởng là dùng Mark để biến direct-hit thành board-wide payoff.

### 19.3 Bleed Resource Engine

Ví dụ flow:

```text
Lacerate -> Blood Ward -> Siphon
```

Bleed không chỉ là DoT mà còn là tài nguyên có thể đổi thành Guard hoặc Consumable.

### 19.4 Crit Snowball

Ví dụ flow:

```text
Crit Escalation -> Precision Strike -> crit nhiều hơn -> face tăng -> build lớn dần
```

Đây là engine snowball gắn chặt với custom dice.

### 19.5 Dice Customization Engine

Ví dụ flow:

```text
Dice Forging -> tăng base mặt trọng yếu -> passive đọc parity/exact value -> skill payoff bùng nổ
```

Đây là trục build dài hạn theo run thay vì chỉ combat tức thời.

---

## 20. Hệ thống chính trong code

Những class / system quan trọng hiện tại:

- `TurnManager`
- `SkillPlanBoard`
- `SkillExecutor`
- `CombatActor`
- `StatusController`
- `BattlePartyManager2D`
- `DiceSlotRig`
- `DiceSpinnerGeneric`
- `RunInventoryManager`
- `PassiveSystem`
- `DamagePopupSystem`
- `CombatHUD`
- `ActorWorldUI`
- `TargetClickable2D`
- `EnemyBrainController`

### 20.1 Source of truth

Đây là rule kiến trúc phải giữ:

- `DiceSlotRig` = source of truth của **dice math**
- `SkillPlanBoard` = source of truth của **lane planning**
- **UI không được trở thành source of truth thứ hai**

### 20.2 Hướng tách data skill

Project hiện đi theo hướng tách skill thành:

- `SkillDamageSO`
- `SkillBuffDebuffSO`
- `SkillPassiveSO`

Legacy pipeline đã bị loại bỏ:

- `SkillSO_Legacy`
- `SkillConditionalOverrides`
- pipeline `SkillSO` cũ không còn là hướng phát triển chính

Khi sửa code mới:

- không revive pipeline cũ trừ khi user yêu cầu rõ ràng
- cẩn thận compatibility với scene / prefab cũ nếu còn đang tồn tại trong project

---

## 21. Cấu trúc thư mục hiện tại

Code chính hiện nằm dưới `Assets/Scripts`.
Không coi `Assets/Scripts/Demo` là nơi chứa code chính nữa.

Cấu trúc domain hiện tại:

- `Assets/Scripts/Combat/Actors`
- `Assets/Scripts/Combat/Execution`
- `Assets/Scripts/Combat/Status`
- `Assets/Scripts/Combat/Turn`
- `Assets/Scripts/Dice`
- `Assets/Scripts/Enemies`
- `Assets/Scripts/Inventory`
- `Assets/Scripts/Skills/Basic`
- `Assets/Scripts/Skills/Buff`
- `Assets/Scripts/Skills/Damage`
- `Assets/Scripts/Skills/Debuff`
- `Assets/Scripts/Skills/Definitions`
- `Assets/Scripts/Skills/Effect`
- `Assets/Scripts/Skills/Legacy`
- `Assets/Scripts/Skills/Passive`
- `Assets/Scripts/Skills/Planning`
- `Assets/Scripts/Skills/Runtime`
- `Assets/Scripts/UI/Combat`
- `Assets/Scripts/UI/Loadout/Dice`
- `Assets/Scripts/UI/Loadout/Passive`
- `Assets/Scripts/UI/Planning`

Ghi nhớ:

- không dùng lại path cũ kiểu `Assets/Scripts/Demo/...`
- không mặc định `Assets/Scripts/Combat/Core/...` nữa nếu cây thư mục hiện tại đã khác

---

## 22. Tình hình hiện tại của project

### 22.1 Những rule / hệ thống combat core đã được xem là cập nhật theo spec mới

Các phần sau hiện được xem là đã cập nhật và không nên tiếp tục mô tả như “còn thiếu core behavior” nữa:

- Focus đầu combat = 2, turn 1 thực tế = 3
- Burn consume baseline = `+2 / stack`
- Freeze / Chilled immunity và Ice reward = `+1 Focus +3 Guard`
- Bleed tick đầu lượt bỏ qua Guard
- Mark / Lightning theo rule direct-hit + shock proc
- shock Lightning chạy tuần tự với delay `0.2s`
- `Stagger` đã được implement và hiển thị như một status thật
- Ailment direction hiện là enemy-side, enemy -> player = 100%
- reorder + lane mapping + phase lock khi execute được xem là ổn ở mức nền tảng

### 22.2 Tình hình refactor file lớn

Refactor hiện tại đi theo hướng:

- giữ class Unity gốc để tránh vỡ reference scene / prefab
- tách logic nặng ra utility / helper mới
- giảm rủi ro khi chạm vào file lớn

Các cụm đã tách:

#### `TurnManager`
- `Assets/Scripts/Combat/Turn/TurnManagerCombatUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnManagerLifecycleUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnManagerPlanningUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnManagerTargetingUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnManagerViewUtility.cs`

#### `SkillExecutor`
- `Assets/Scripts/Combat/Execution/AttackPreviewCalculator.cs`
- `Assets/Scripts/Combat/Execution/SkillAttackResolutionUtility.cs`
- `Assets/Scripts/Combat/Execution/SkillTargetResolver.cs`

#### `StatusController`
- `Assets/Scripts/Combat/Status/StatusRuntimeEntries.cs`
- `Assets/Scripts/Combat/Status/StatusBuffDebuffUtility.cs`
- `Assets/Scripts/Combat/Status/StatusAilmentUtility.cs`
- `Assets/Scripts/Combat/Status/StatusStateUtility.cs`

#### `SkillPlanBoard`
- `Assets/Scripts/Skills/Planning/SkillPlanBoardStateUtility.cs`
- `Assets/Scripts/Skills/Planning/SkillPlanRuntimeUtility.cs`

#### `RunInventoryManager`
- `Assets/Scripts/Inventory/RunInventoryBindingUtility.cs`
- `Assets/Scripts/Inventory/RunInventorySetupUtility.cs`
- `Assets/Scripts/Inventory/RunInventoryLoadoutUtility.cs`

#### `EnemyBrainController`
- `Assets/Scripts/Enemies/EnemyIntentSelectionUtility.cs`
- `Assets/Scripts/Enemies/EnemyIntentPreviewUtility.cs`

#### Loadout UI - Dice
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipLayoutUtility.cs`
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipWorldSyncUtility.cs`
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipStateUtility.cs`
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipPresentationUtility.cs`
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipWorldFollowUtility.cs`

#### Loadout UI - Passive
- `Assets/Scripts/UI/Loadout/Passive/PassiveEquipLayoutUtility.cs`
- `Assets/Scripts/UI/Loadout/Passive/PassiveEquipWorldSyncUtility.cs`
- `Assets/Scripts/UI/Loadout/Passive/PassiveEquipStateUtility.cs`
- `Assets/Scripts/UI/Loadout/Passive/PassiveEquipPresentationUtility.cs`

### 22.3 Những vùng đang ổn và không nên đụng nếu không cần

Các vùng sau hiện được xem là khá ổn:

- flow runtime moi `roll -> reorder neu can -> drag skill cast ngay -> End Turn -> enemy turn`
- reorder dice trong player phase
- execution order / damage order theo lane hiện tại
- lane mapping giữa pair identity và lane hiện tại
- consume rule nền tảng
- state spent / consume sau khi cast

Nếu task không nhắm đúng bug ở các vùng này, ưu tiên **không đụng**.

### 22.4 Những việc còn lại hợp lý cho chat sau / bước sau

Nếu tiếp tục làm việc sau này, hướng hợp lý là:

- tiếp tục tách file nếu có file nào thật sự còn ôm quá nhiều logic
- chuẩn hóa skill data / content layer
- làm tooltip / runtime preview formatter sau khi skill data ổn
- polish thêm dice feedback UI

Nếu combat core đã ổn và file đã ở mức chấp nhận được, **không cần tách thêm chỉ vì số dòng**.

---

## 23. Những gì chưa thiết kế hoặc chưa nên coi là final

Phần này phải được giữ lại để tránh AI khác tưởng mọi thứ đã chốt hết.

Các vùng hiện vẫn mở hoặc chưa populate đầy đủ:

- run structure chi tiết (map, node types, shop placement) nếu chưa có tài liệu mới hơn thay thế
- regular enemy design chi tiết
- consumable pool chi tiết ngoài framework cơ bản
- tooltip / runtime preview formatter final
- hidden boss placement cụ thể trong endless
- skill content populate hoàn chỉnh vào toàn bộ SO thực tế

Nếu user không yêu cầu chốt các phần này, không được tự “đóng spec” thay user.

---

## 24. Quy tắc khi agent / AI sửa code

Đây là guardrail quan trọng.

### 24.1 Nguyên tắc bắt buộc

- đừng phá những phần reorder / lane / execution đang ổn
- execute, preview, tooltip không được nói ba kiểu nếu có sửa dice math
- effect phải bám spec trong file này, không bám logic cũ nếu logic cũ lệch spec
- ưu tiên patch nhỏ, dễ review
- nếu refactor lớn, phải nói rõ vì sao
- luôn nêu file nào sửa và vì sao
- nếu có thể, đưa checklist test thủ công

### 24.2 Khi sửa combat

- ưu tiên bảo toàn behavior đúng trước, đẹp code tính sau
- utility mới chỉ nên tách khi thực sự giúp code dễ đọc / dễ bảo trì hơn
- giữ `SkillPlanBoard` là source of truth cho lane planning
- giữ `DiceSlotRig` là source of truth cho resolved dice math
- không để UI thành source of truth thứ hai

### 24.3 Điều không nên làm

- không đập lại lane mapping chỉ vì muốn code “sạch” hơn
- không tự revive `SkillSO` cũ nếu pipeline mới đang là hướng chính
- không coi toàn bộ content pool chưa chốt là final spec
- không sửa combat core chỉ để refactor nếu không có bug hoặc yêu cầu rõ

---

## 25. Cách trả lời / cách handoff mong muốn

Nếu đây là file được AI khác đọc để tiếp tục hỗ trợ project, style mong muốn là:

- ưu tiên tiếng Việt
- ngắn, rõ, thẳng vào vấn đề trong chat trả lời
- nhưng khi viết tài liệu / patch note / handoff thì phải đủ ngữ cảnh

Nếu sửa code, nên trả lời theo cấu trúc:

1. nguyên nhân
2. file nào sửa
3. vì sao sửa như vậy
4. checklist test

Nếu không chắc một rule đã chốt hay chưa, phải tra lại file này trước khi kết luận.

---

## 26. Tóm tắt cực ngắn

Nếu chỉ nhớ vài điểm thì phải nhớ:

1. Đây là game dice-driven tactical roguelike trên mobile.
2. Dice là trung tâm; Base Value và Added Value là hai khái niệm khác nhau.
3. Player có 6 skill slot, 1 passive slot, tối đa 3 dice.
4. `DiceSlotRig` là source of truth của dice math.
5. `SkillPlanBoard` là source of truth của lane planning.
6. Rule `1/2/3` vs `A/B/C` là cực quan trọng: logic phải đọc lane hiện tại.
7. Đừng phá reorder / lane mapping / execution order nếu chúng đang ổn.
8. Combat core đã cập nhật Burn, Mark/Lightning, Freeze/Chilled, Bleed, Stagger.
9. `Ailment` hiện là enemy-side system.
10. Hướng lớn tiếp theo nghiêng về skill data, content layer, preview formatter và polish UI, không phải đập lại core combat nếu không cần.

---

## 27. Ghi chú thực tế cho người / AI đọc file này ở chat sau

Nếu bạn tiếp quản project từ file này, hãy giả định đúng các điểm sau:

- `Assets/Scripts/Demo` không còn là nơi chứa code chính.
- `Assets/Scripts/Combat/Core` không còn là cây thư mục mặc định để viện dẫn nữa nếu project đã được tách domain mới.
- Combat core đã được cập nhật theo spec mới cho Burn, Mark / Lightning, Freeze / Chilled, Bleed, Stagger.
- `Ailment` hiện được coi là enemy-side system.
- Reorder trong Planning, lane mapping, execution order, damage order theo lane hiện tại đang được xem là ổn ở mức nền tảng.
- Ưu tiên hiện tại là **giữ combat rule ổn định**.
- Chỉ refactor tiếp khi nó thực sự giúp code dễ đọc / dễ maintain hơn.
- Không cần cố tách thêm nếu file hiện tại đã ở mức chấp nhận được.
- Không được đánh đồng “content pool hiện tại” với “mọi thứ đã final”.

---

## 28. Hiện trạng trước khi commit / trước khi bàn giao

Nếu combat đã được test qua trong Unity và console sạch, có thể xem scope hiện tại là xong ở mức runtime / refactor tương ứng.

Trước khi commit hoặc trước khi kết thúc một nhánh chỉnh sửa combat, nên test tối thiểu:

- console Unity không còn lỗi compile
- roll -> planning -> target -> execute -> enemy turn
- reorder dice trong Planning
- reorder passive / loadout UI nếu có chạm vào phần đó
- kiểm tra preview / execute không lệch nhau ở logic vừa sửa

Nếu không có vấn đề, có thể xem nhánh đó ở mức chấp nhận được.

---

## 29. Kết luận sử dụng

Nếu cần một câu ngắn để hiểu file này:

**Đây là file nguồn chân lý thực dụng của project ở mức design + runtime context + handoff guidance.**

Đọc file này trước khi sửa code, thêm mechanic, đánh giá design, hay chuyển project sang một chat khác.
---

## 30. Progress Note - 2026-03-22

Doan nay de giu nhip handoff cho chat sau. Khong thay the rule da chot phia tren.

### 30.1 Skill/passive authoring direction hien tai

Huong dang dung:

- code for engine
- data for content
- custom hooks for exceptions

Khong di theo 2 cuc doan:

- khong hardcode tung skill/passive bang ten text mai mai
- khong ep 100% moi mechanic vao inspector neu data model chua dien ta noi

### 30.2 Nhung gi da tien trien

- da co `behaviorId` tren cac skill/passive SO de data xac dinh mechanic identity
- da migrate mot runtime slice dau tien cho nhom Fire skill va mot so passive
- da chuyen inspector cua skill sang huong de nhin va de author hon
- da sua passive slot UI de reorder / drag / adaptive layout giong dice o muc UI
- da sap xep lai folder asset skill damage theo he de giup handoff va quan ly content

### 30.3 Fire slice dang la vertical slice dau tien

Nhom skill dang duoc uu tien de chay that:

- Ignite
- Hellfire
- Ember Weapon
- Cinderbrand

Hellfire rule hien tai phai nho:

- target phai co Burn san truoc hit moi duoc reapply Burn
- moi die trong local group co Base Value = 7 se cong them 7 Burn

### 30.4 Passive slice dang hoat dong truoc

- Clear Mind
- Even Resonance
- Elemental Catalyst
- Fail Forward
- Iron Stance
- Crit Escalation
- Dice Forging

### 30.5 Huong di tiep theo

- tiep tuc migrate passive/skill theo behavior id + runtime hook
- sau do mo rong generic module layer cho phan skill/passive pho thong
- chi de custom resolver cho mechanic that su dac biet

Muc tieu dai hon:

- phan lon content author duoc bang data
- engine van nam trong code
- preview / execute / tooltip cung doc chung tu mot grammar/runtime model

---

## Runtime Update Note (2026-04)

- Build state runtime hien tai chi con `1 passive slot`.
- Combat runtime hien tai dang dung grammar:
  - `roll dice`
  - `reorder neu can`
  - `drag skill tu skill slot vao target de cast truc tiep`
  - `bam End Turn`
- Roll dice hien tai van giu nguyen.
- Consume dice kieu bien mat that chua duoc dua vao runtime; hien tai van tam bieu dien bang dice spent / dim 50%.
- Dinh huong design day du hon nam o [COMBAT_CHANGES_2026.md](/C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Docs/Detail/COMBAT_CHANGES_2026.md).
