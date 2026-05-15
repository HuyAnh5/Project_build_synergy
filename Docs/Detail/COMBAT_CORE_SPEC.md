# COMBAT_CORE_SPEC.md

> Tài liệu này là **source of truth cho vòng combat cốt lõi**. Nó mô tả nhịp lượt, quy tắc dice, AP, damage/Guard/Stagger, dice sequencing và nguyên tắc resolve số trong combat.  
> File này không liệt kê full content skill/relic; phần đó nằm ở `SKILLS_PASSIVES_SPEC.md`.  
> Khi có mâu thuẫn giữa trực giác cá nhân và file này, ưu tiên file này.


> **CURRENT SOURCE UPDATE:** Bản này đã nhập các rule hiện tại từ các draft cũ. Không dùng file `archived combat change draft` làm source nữa. Resource hiện tại là **AP**. Combat flow hiện tại là **Player Phase**: dice tự roll đầu phase → player reorder → click/drag skill vào target để cast ngay → dice chuyển used state → End Phase → Enemy Phase.

---
---

## 1. Mục tiêu của hệ thống

Combat của game phải tạo ra cảm giác:

- mỗi lượt ngắn nhưng có ý nghĩa,
- dice là trung tâm thật sự của quyết định,
- player phải đọc thứ tự dice hiện tại và reorder để quyết định skill tiếp theo sẽ dùng dice nào,
- player phải chọn giữa setup, payoff, defense và economy,
- độ sâu đến từ quyết định và tương tác hệ thống, không đến từ tính nhẩm quá nhiều lớp modifier.

Combat loop runtime hiện tại:

```text
Player Phase: dice tự roll đầu phase -> reorder nếu cần -> click/drag skill vào target để cast ngay -> dice chuyển used state
End Phase
Enemy Phase
Lặp lại
```

Không còn flow:

- một phase roll riêng tách khỏi Player Phase; dice roll là hành vi đầu Player Phase. Skill hợp lệ được cast và resolve ngay khi player click/drag vào target.

## 2. Phạm vi file này

File này bao gồm:

- dice system,
- AP economy trong combat,
- Basic actions,
- turn flow 3 phase,
- damage / Guard / Stagger,
- skill tag nền,
- dice reorder / consume order / die identity,
- tooltip / preview direction ở cấp combat,
- edge cases cốt lõi của combat.

File này không đi sâu vào:

- full content skill/relic pool,
- relic pool cụ thể,
- run economy ngoài combat,
- implementation / class code.

---



## 3. Logic Flow

Phần này mô tả **đường đi logic tổng quát của combat core**: hệ thống bắt đầu từ đâu, kiểm tra điều kiện gì ở mỗi bước, và kết quả nào được truyền sang bước kế tiếp.  
Phần này không thay thế rule chi tiết ở các section sau; nó là bản đồ vận hành cấp cao.

### 3.1 Flow của một combat encounter

**Combat Start**
→ Initialize encounter, load player loadout, relic collection, dice state, enemy roster và temporary combat state
→ Hiển thị enemy intent / encounter information
→ Giao AP khởi đầu theo current combat rule
→ Chuyển sang `PlayerPhase`

**PlayerPhase**
→ Resolve start-of-player-phase effects trên player / relic / status
→ +1 AP theo current rule
→ Refresh dice used state nếu không có rule đặc biệt ngăn refresh
→ Dice tự roll ngay đầu phase
→ Với từng die: xác định rolled face, Base Value, Added Value liên quan, Crit / Normal / Fail
→ Player có thể reorder dice nếu muốn
→ Player bấm chọn skill rồi chọn target, hoặc kéo skill icon vào target hợp lệ
→ Hệ thống kiểm tra: đủ AP, đủ dice available theo slot cost, target hợp lệ, condition / relic / modifier hiện hành
→ Nếu invalid: reject cast và giữ player ở `PlayerPhase`
→ Nếu valid: consume các dice available đầu tiên theo thứ tự hiện tại
→ Resolve skill ngay sau khi cast hợp lệ
→ Dice đã dùng chuyển sang **used state**: hạ nhẹ trục Y và đổi background; dice vẫn nhìn thấy nhưng không còn available
→ Player có thể tiếp tục cast nếu còn tài nguyên
→ Khi player không muốn cast thêm: bấm `End Turn` → chuyển sang `EndPhase`

**EndPhase**
→ Resolve end-of-player-phase effects
→ Cleanup state cần chuyển sang enemy
→ Chuyển sang `EnemyPhase`

**EnemyPhase**
→ Enemy hành động theo intent / pattern / condition
→ Resolve death / status / ailment / Guard checks trong quá trình action chạy
→ Resolve end-of-enemy-phase effects
→ Check victory / defeat
→ Nếu combat tiếp tục: quay lại `PlayerPhase`; nếu không: chuyển sang `CombatEnd`

### 3.2 Flow của một player phase

`PlayerPhase Start`
→ Gain / refresh AP đầu phase
→ Dice tự roll
→ Player đọc roll, reorder nếu cần, cân giữa setup / payoff / defense / economy
→ `Click hoặc drag skill vào target` để cast ngay
→ `Validation`: hệ thống check AP, dice available, target rule, condition và modifier
→ `Consume dice`: theo thứ tự hiện tại từ trái sang phải
→ `Resolve`: skill resolve ngay sau khi cast hợp lệ
→ Dice đã dùng chuyển used state
→ Player có thể tiếp tục cast
→ `End Turn` để sang `EndPhase`

### 3.3 Flow của một die

`Roll Die`
→ Xác định mặt được roll
→ Gán rolled number thành Base Value
→ Xác định Crit / Normal / Fail từ chính die đó
→ Tính Added Value liên quan nhưng **không thay đổi identity của Base Value**
→ Lưu die-local context để dùng cho preview, validation và cast resolution

### 3.4 Flow dùng skill trực tiếp

`Player chọn skill`
→ hệ thống đọc slot cost, AP cost, target rule, tag, element, blue value, special condition
→ player chọn target bằng click hoặc drag/drop
→ hệ thống xác định dice sẽ bị consume theo thứ tự hiện tại
→ check đủ AP, đủ dice available và target hợp lệ
→ nếu hợp lệ: cast ngay, resolve skill, chuyển dice đã dùng sang used state
→ nếu không hợp lệ: reject cast và báo lý do invalid qua UI/tooltip

Không còn:

- xếp skill vào một hàng hành động trước khi resolve. Skill hợp lệ cast ngay tại thời điểm player click/drag vào target.

### 3.5 Flow resolve damage

`Action có target hợp lệ`  
→ Build final action context từ skill definition + die-local data + relic + attacker state + target state  
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

- **4 skill slot**,
- **Relic collection**: nhiều relic có thể được nhặt trong run và hiển thị dạng icon ở góc trên UI,
- **1 đến 3 dice**,
- **Basic Attack**,
- **Basic Guard**,
- **Consumables** thuộc 3 nhóm: **Fate / Seal / Rune**.

Rule quan trọng:

- mỗi skill chiếm **1 loadout slot** khi mang vào combat,
- nhưng khi cast, skill có thể consume **1 / 2 / 3 dice** tùy slot cost,
- dice cost quyết định action economy trong turn,
- skill nhiều dice là đánh đổi trực tiếp với số action còn lại trong turn.

### 4.2 AP economy

Current locked rules:

- **Max AP = 9**
- **Start combat = 2 AP**
- **Đầu mỗi Player Phase = +1 AP**
- **Không được nợ AP**
- Vì vậy **turn 1 thực tế = 3 AP**

Ý nghĩa thiết kế:

- AP phải thiếu vừa đủ để buộc player lựa chọn.
- Player phải có cảm giác có thể “xoay lượt” bằng sequencing tốt, build tốt hoặc basic action hợp lý.
- AP không được trở thành cái khóa khiến combat chết cứng.

### 4.3 Basic actions

Basic actions luôn có sẵn và **không chiếm 4 skill slot chính**.

#### Basic Attack

- **Tag mặc định:** `None`
- **Element mặc định:** `None`
- **0 AP**
- gây blue value **`"4 damage"`** nếu data skill đánh dấu số này là blue value
- cho **+1 AP**

Rule resolve:

- Basic Attack mặc định không phải Physical và không có element/tag hệ.
- Skill hoặc relic có thể đổi tag/element của Basic Attack nếu text ghi rõ.
- Nếu Basic Attack đang ở trạng thái không tag, Crit dùng hệ số thường.
- Nếu skill/relic biến Basic Attack thành Physical, Fire, Ice... thì nó đọc rule của tag/element được cấp theo text đó.

#### Basic Guard

- **Tag mặc định:** `None`
- **Element mặc định:** `None`
- **0 AP**
- tạo Guard theo blue value / Base Value tùy data hiện tại của Basic Guard

Rule resolve:

- Basic Guard mặc định không phải Neutral/Guard tag cứng.
- Skill hoặc relic có thể đổi tag/element/cách scale của Basic Guard nếu text ghi rõ.
- Basic Guard là anchor phòng thủ và vẫn phải là lựa chọn có ý nghĩa khi roll xấu.

## 5. Dice system — current locked rules

Đây là phần quan trọng nhất của toàn bộ combat. Mọi skill logic, relic logic, preview, tooltip và hướng build đều phải tôn trọng phần này.

### 5.1 Dice không chỉ là damage source

Dice có thể là:

- d2, d4, d6, hoặc các biến thể khác,
- có mặt số không đều,
- có thể được custom trong run,
- có thể ảnh hưởng tới crit/fail, exact value, parity, threshold, highest/lowest.

Dice không phải “resource ngẫu nhiên” hay AP ngẫu nhiên.  
Dice là **identity engine** của combat.

### 5.2 Base Value, Added Value và Blue Value

Current locked rules:

- **Base Value** = mặt thật roll ra.
- **Added Value** = phần cộng thêm vào output cuối, nhưng không đổi bản chất của Base Value.
- **Số thường** trong skill text = fixed / rule number, không bị dice, Crit, Fail, vị trí, Added Value hoặc modifier tác động.
- **Số xanh / blue value** = output được phép nhận Added Value và modifier hợp lệ.
- Skill chỉ nhận Added Value trên những output được đánh dấu là blue value.
- Không còn rule “mọi output số học mặc định cộng Added Value”.
- Không còn rule “Added Value mặc định cộng vào output đầu tiên”.

Ví dụ:

```text
Deal 4 damage.
```

`4` là số thường nếu không được đánh dấu blue value; nó không scale theo dice.

```text
Deal "4 damage".
```

`"4"` là blue value; nó có thể nhận Added Value từ Crit, dice face, condition, relic, skill buff hoặc modifier hợp lệ.

Blue value có thể là:

- damage,
- Guard,
- Burn,
- Bleed,
- AP,
- heal,
- status amount,
- hoặc output số học khác nếu skill data đánh dấu nó là blue.

Condition vẫn phải đọc từ **Base Value** trừ khi text ghi rõ ngoại lệ:

- chẵn / lẻ,
- crit / fail,
- `<= 3`,
- exact value,
- highest / lowest.

Fail không phải là “Added Value âm”. Fail là penalty / modifier lên output hợp lệ theo rule của skill.

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
- Fail không làm mất Added Value do die Crit khác hoặc do relic/skill đã cấp cho action đó.

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
baseValue -> critAddedValue -> relicSkillConditionalAddedValue -> totalAddedValue -> finalActionOutput
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
- Về sau có thể tồn tại skill hoặc relic cho thêm Added Value nếu đạt điều kiện; các bonus này vẫn chỉ cộng vào output cuối, không đổi bản chất Base Value của die.
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
- `Non-dice sources of Added Value` -> `skill`, `relic`, `relic`, `modifier` ghi rõ
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

### 5.10 Reorder trong Player Phase

Current locked rules:

- Player có thể reorder dice trong **Player Phase** trước khi cast.
- Reorder quyết định skill tiếp theo sẽ consume dice nào.
- Skill consume dice available từ trái sang phải theo thứ tự hiện tại.
- Sau khi một skill cast hợp lệ, các dice đã consume chuyển sang used state và không còn available cho cast tiếp theo.

Ví dụ đúng tinh thần:

- roll được một die rất cao,
- kéo nó về vị trí mà skill payoff sắp dùng,
- dùng skill đầu để setup Mark / phá Guard / áp trạng thái,
- dùng skill sau để payoff bằng dice đã được đặt đúng thứ tự.

### 5.11 Ý nghĩa thiết kế của dice

Dice quyết định:

- output,
- điều kiện kích hoạt,
- thứ tự finisher,
- hướng build,
- nhịp economy,
- và cả cách relic tương tác.

Bất kỳ hệ mới nào thêm vào cũng phải tôn trọng vai trò trung tâm của dice.

---

## 6. Turn flow — current locked flow

### 6.1 Tổng quan 3 phase

Combat turn hiện tại chỉ cần đọc qua 3 phase chính:

```text
Player Phase -> End Phase -> Enemy Phase
```

Dice tự roll ở đầu **Player Phase**. Không còn `Player Phase start` riêng.

### 6.2 Player Phase

Trong Player Phase:

1. start-of-player-phase effect resolve,
2. player nhận AP đầu phase,
3. dice được refresh nếu hợp lệ,
4. dice tự roll,
5. player reorder dice nếu muốn,
6. player click hoặc drag skill vào target để cast ngay,
7. dice bị consume theo slot cost, đọc từ trái sang phải theo thứ tự hiện tại,
8. dice đã dùng chuyển sang used state: hạ nhẹ trục Y và đổi background,
9. player có thể cast thêm nếu còn AP/dice,
10. player bấm End Turn để sang End Phase.

### 6.3 End Phase

End Phase là cleanup của lượt player:

- resolve end-of-player-phase effect,
- dọn preview / temporary state,
- khóa input player,
- chuyển sang Enemy Phase.

### 6.4 Enemy Phase

Enemy Phase:

- enemy resolve intent,
- enemy action chạy theo pattern / condition,
- status / death / stagger / guard được update,
- nếu combat chưa kết thúc thì quay lại Player Phase.

### 6.5 Rule lock quan trọng

Rule hiện tại:

- Dice tự roll đầu Player Phase.
- Player có thể reorder dice trong Player Phase.
- Player click/drag skill vào target để cast ngay nếu hợp lệ.
- Skill consume dice theo thứ tự hiện tại từ trái sang phải.
- Reorder phục vụ sequencing: quyết định skill tiếp theo sẽ consume dice nào.

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

- thứ tự dice hiện tại,
- dice nào còn available / dice nào đã used,
- skill nào đang được chọn hoặc kéo,
- target nào hợp lệ,
- mục tiêu nào có Guard,
- mục tiêu nào đang Stagger,
- player còn bao nhiêu AP,
- cast hiện tại hợp lệ hay bị chặn vì thiếu AP / thiếu dice / target sai.

---

---

## 8. Dice reorder / consume order / die identity rule

Đây là section rất quan trọng và phải giữ riêng.

### 8.1 Rule cốt lõi đã chốt

Player không gán die thủ công vào từng skill. Thay vào đó, player reorder hàng dice để quyết định skill tiếp theo sẽ dùng dice nào.

Rule consume hiện tại:

- skill 1 slot consume 1 dice available đầu tiên,
- skill 2 slot consume 2 dice available đầu tiên,
- skill 3 slot consume 3 dice available đầu tiên,
- đọc từ trái sang phải theo thứ tự dice hiện tại,
- dice đã used vẫn hiện trên UI nhưng không còn available cho cast tiếp theo.

### 8.2 Ý nghĩa thiết kế

Reorder tồn tại để player làm sequencing như:

- đặt dice tốt cho payoff sau,
- dùng dice thấp cho setup trước,
- giữ dice crit / exact / parity phù hợp cho skill quan trọng,
- quyết định dùng skill nào trước để còn đúng dice cho skill sau.

Nếu dice order mất giá trị chiến thuật, combat sẽ mất nhiều chiều sâu.

### 8.3 Điều cấm kỵ khi sửa

Không được:

- để preview highlight dice khác với dice thực sự bị consume,
- để UI reorder nhưng logic vẫn đọc thứ tự cũ,
- để used dice bị consume lại khi chưa được refresh,
- để consumable refresh dice nhưng UI không cập nhật available state.

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

### 10.2 Turn / reorder edge cases

- Dice tự roll đầu Player Phase.
- Reorder hợp lệ trong Player Phase trước mỗi cast nếu dice còn movable theo UI rule.
- Preview và cast phải đọc cùng một dice order hiện tại.
- Used dice không được tính là available cho skill tiếp theo nếu chưa được refresh.

### 10.3 Damage edge cases

- Damage sau floor mà `< 1` vẫn phải tối thiểu là `1`, trừ khi Guard chặn toàn bộ.
- Stagger chỉ buff **hit direct kế tiếp**.
- Shock phụ / Bleed tick không được ăn Stagger thay hit chính.

### 10.4 Economy edge cases

- Player không được nợ AP.
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
- turn flow 3 phase,
- Damage / Guard / Stagger,
- dice reorder / consume order,
- used dice visual state,
- tooltip static ngoài combat / resolved trong combat.

---
