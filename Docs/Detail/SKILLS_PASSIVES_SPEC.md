# SKILLS_PASSIVES_SPEC.md

> Tài liệu này là **source of truth cho content layer hiện tại của skill, passive, combo engines và triết lý thiết kế content**.
> Rất quan trọng: không phải mọi skill dưới đây đều đã final implementation. File này phải luôn được đọc cùng với nhãn **đã chốt** / **revised đã chốt** / **pool chưa chốt** / **rarity pending**.

---

## 1. Mục tiêu của file

File này tồn tại để:

* giữ lại full content direction hiện tại,
* giúp người đọc hiểu build intent,
* phân biệt cái nào đã chốt với cái nào vẫn là pool,
* tránh mất anti-synergy có chủ đích,
* làm chuẩn cho việc thêm skill/passive mới sau này,
* và từ bản này trở đi, bắt đầu chuẩn hóa **grammar của skill** để sau này thêm skill mới không phải quay lại code một luật quá riêng lẻ từ đầu.

---

## 2. Nguyên tắc thiết kế content

### 2.1 Lenticular design là bắt buộc

Game không đi theo hướng:

* `1 skill = 1 build biệt lập`

Mà đi theo hướng:

* một skill có thể tự dùng được,
* nhưng khi đặt cạnh đúng passive / đúng status / đúng dice engine thì mở ra lớp giá trị mới,
* build nảy sinh từ **mạng lưới tương tác**, không phải từ bộ set cứng độc lập.

### 2.2 Phân lớp rarity / độ sâu

* **Common**: tự đủ dùng, hiểu nhanh, ít cần setup, mạnh ổn định ngay.
* **Uncommon**: mạnh hơn khi canh điều kiện / đúng nhịp.
* **Rare**: một mình vẫn hữu ích nhưng không gánh ngay; khi đúng engine thì cực mạnh.

### 2.3 Tag nền tảng

Các tag nền tảng hiện dùng:

* `Attack`
* `Guard`
* `Status`
* `Sunder`

Ngoài ra, với các skill có tính chất hỗ trợ trạng thái người chơi hoặc gây hiệu ứng lên đối thủ, cần ghi rõ nhãn phụ:

* `(Buff)`
* `(Debuff)`
* `(Utility)`

Mục tiêu là để sau này buff/debuff skill luôn được đọc rõ bên cạnh tag chính.

### 2.4 Intentional anti-synergy là hợp lệ

Không phải mọi đồ mạnh đều cần cộng hưởng với mọi thứ. Một số anti-synergy có chủ đích là tốt nếu nó:

* giữ build identity sắc hơn,
* buộc player commit hướng chơi,
* ngăn một engine ôm hết mọi payoff mạnh nhất.

### 2.5 Ưu tiên hoàn thiện content hiện tại

Ở giai đoạn hiện tại, file này ưu tiên khóa sâu hơn cho 3 hệ:

* **Fire**
* **Ice**
* **Lightning**

Physical và Bleed vẫn giữ nguyên trong file như content pool thật, nhưng chưa phải trục ưu tiên polish đầu tiên.

### 2.6 Grammar chuẩn hóa cho skill mới

Từ bản này trở đi, skill mới không nên được nghĩ như một “luật đặc biệt riêng” nếu không thật sự cần.
Một skill mới nên được mô tả theo cùng một form chuẩn:

* **Element**
* **Tag chính / phụ**
* **Slots / Focus**
* **Rarity**
* **Delivery Pattern**
* **Condition chính**
* **Effect Modules**
* **Text hiện tại / text chuẩn hóa**
* **Vai trò thiết kế**
* **Role trong build** (`Core / Support / Bridge / Utility`)

### 2.7 Các module hiệu ứng chuẩn nên dùng lại

#### A. Damage Modules

* Gây damage theo `X = Base Value + Added Value`
* Gây damage chuẩn với damage gốc rồi cộng Added Value
* Gây damage AoE
* Gây bonus damage theo condition

Rule chung từ current direction:

* Nếu skill dùng **fixed output**, output cuối mặc định luôn là `fixed output + Added Value`
* Rule này không chỉ áp cho `Attack`; `Guard`, status output và history payoff số học cũng đi cùng logic nếu text không nói ngược lại

#### B. Guard Modules

* Nhận Guard theo giá trị dice
* Nhận Guard cố định rồi cộng Added Value
* Nhận Guard theo trạng thái hiện có
* Giữ / chuyển hóa / khuếch đại Guard payoff

#### C. Status Apply Modules

* Áp Burn
* Áp Mark
* Áp Freeze
* Kéo dài Chilled
* Áp Bleed

#### D. Status Consume / Payoff Modules

* Consume Burn để tăng damage
* Consume Mark để lan / payoff board
* Dùng Freeze / Chilled làm cửa sổ payoff
* Convert Bleed thành tài nguyên khác

#### E. Propagation Modules

* Lan Mark sang board
* Chain control sang mục tiêu khác
* Nảy hit sang mục tiêu khác

#### F. Buff / Debuff Modules

* Buff Basic Attack
* Buff payoff của một hệ cụ thể
* Debuff khiến mục tiêu nhận thêm damage từ một cơ chế cụ thể

#### G. Positional Modules

* Slot trái làm một việc
* Slot phải làm một việc
* Skill nhiều slot nhưng từng vị trí có vai trò khác nhau

### 2.8 Rule thiết kế rất quan trọng

* Một skill thường chỉ nên có **1 condition chính**.
* Rare không nên chỉ là “damage to hơn”. Rare phải mở engine, sequencing hoặc build identity mới.
* Nếu một skill mới không map nổi vào grammar trên, phải kiểm tra lại:
  đó là module mới thật, hay chỉ là biến thể của module cũ.

### 2.9 Những trục chính của skill condition

Từ current spec này trở đi, skill condition phải đọc game theo 7 trục xương sống sau:

* `Crit / Fail`
* `Parity (chẵn/lẻ)`
* `Exact value`
* `Local group / slot relation`
* `Effect / Target State`
* `Resource axis (Focus / Guard)`
* `Board / encounter axis`

Ý nghĩa thực dụng:

* game **không thiếu trục**, mà đang hơi dư trục,
* skill/passive mới nên bám rõ `1-2 trục chính`,
* không thêm condition mới chỉ để làm text đẹp,
* và không quay lại tư duy “mọi skill chỉ scale thẳng theo số roll”.

Một rule content rất quan trọng:

* `condition` không phải payoff cố định
* nó là **hệ quy chiếu để chọn die / mở branch / gán vai trò output**
* nên `chẵn`, `lẻ`, `Crit`, `Fail`, `highest`, `lowest`, `exact` đều không được gắn cứng với một loại payoff duy nhất

#### 2.9A Chi tiết từng trục được phép đọc

Đây là file `detail`, nên từ đây không chỉ ghi tên trục mà phải ghi rõ **biến nào của game được phép trở thành condition / payoff / support hook**.

##### A. `Crit / Fail`

Skill condition chỉ giữ:

* `Crit`
* `Fail`

Cách đọc:

* `1 slot` = die này crit/fail
* `nhiều slot` = cả cụm đều crit/fail

##### B. `Parity (chẵn/lẻ)`

Skill condition chỉ giữ:

* `Even`
* `Odd`

Cách đọc:

* `1 slot` = die này chẵn/lẻ
* `nhiều slot` = cả cụm đều chẵn/lẻ

##### C. `Exact value`

Skill condition chỉ giữ:

* `Die Equals X`
* `Group Contains Pattern`
* `Random Exact Number Owned`
* `Random Exact Number Random`

Lưu ý:

* `Die Equals X` cho phép nhập tay số cụ thể
* `Group Contains Pattern` cho phép nhập dãy như `1-2-3-5`
* `Random Exact Number Owned` chỉ random trong pool số mà dice player thật sự sở hữu
* `Random Exact Number Random` random độc lập, không cần player sở hữu số đó

##### D. `Local group / slot relation`

Skill condition chỉ giữ:

* `Self-position`
* `Neighbor relation`
* `Split-role`

Chi tiết:

* `Self-position` chỉ dùng `Left / Right`
* `Neighbor relation` chỉ dùng `Left / Right`
* `Split-role` chỉ dùng `Highest / Lowest`

Ý nghĩa:

* `Self-position` = skill này tự được lợi nếu đang nằm trái/phải
* `Neighbor relation` = skill này tác động sang skill bên trái/phải
* `Split-role` = dùng `Highest / Lowest` để chia branch rõ ràng như `Cauterize`

##### E. `Effect / Target State`

Skill hoặc passive có thể đọc:

* target có `Burn` hay không
* target đang có bao nhiêu `Burn`
* target có `Mark` hay không
* target đang `Freeze` hay `Chilled`
* target có bao nhiêu `Bleed`
* target có `Stagger` hay không
* số enemy trên board đang có cùng một trạng thái

Lưu ý:

* Burn hiện là `batch-based`, nhưng UI có thể chỉ hiện tổng stack
* `Status History (TODO)` hiện mới là note để mở rộng sau, chưa có runtime logic condition

##### F. `Resource axis (Focus / Guard)`

Skill condition chỉ giữ:

* `Current Focus`
* `Player Guard`
* `Target Guard`

##### G. `Board / encounter axis`

Skill hoặc passive có thể đọc:

* số lượng enemy hiện còn sống
* số lượng enemy đang có status

---

## 3. 27 skill hiện tại

> Ghi chú đọc file:
>
> * **Đã chốt rõ** = current content đã có định danh rất mạnh.
> * **Revised đã chốt** = skill cũ nhưng vừa được cập nhật rule/role và nên xem version mới là source of truth.
> * **Pool chưa chốt** = current content direction, chưa nên coi là final implementation.
> * **Rarity pending** = chưa có rarity khóa cứng từ source hiện tại.

### 3.1 Skill đã chốt rõ / revised đã chốt

#### 1) Ember Weapon

* **Element:** Fire
* **Tag:** `Status (Buff)`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** **Uncommon**
* **Delivery Pattern:** Self Buff
* **Condition chính:** Không có
* **Effect Modules:** Buff Basic Attack + Apply Burn
* **Text hiện tại:** Trong **3 turn tiếp theo**, Basic Attack gây thêm **+1 damage** và áp **Burn = tổng damage gây ra**.
* **Ví dụ:** Basic Attack base 4 damage → với Ember Weapon = 5 damage, áp 5 Burn mỗi hit.
* **Vai trò thiết kế:** biến Basic Attack thành engine Burn, mở Fire loop từ action nền.
* **Role trong build:** Support / Bridge Piece
* **Trạng thái:** **Đã chốt rõ**

#### 2) Hellfire

* **Element:** Fire
* **Tag:** `Attack`
* **Slots / Focus:** **3 slot, 2 Focus**
* **Rarity:** **Rare**
* **Delivery Pattern:** Single Target
* **Condition chính:** Target đã có Burn; exact value `7` để loop
* **Effect Modules:** Consume Burn + Exact-value payoff
* **Text hiện tại:** yêu cầu mục tiêu đã có Burn sẵn; consume toàn bộ Burn, gây **3 damage mỗi stack Burn**; sau đó, nếu mỗi dice trong nhóm rơi đúng **7**, áp lại **7 Burn mới**.
* **Vai trò thiết kế:** win condition exact-value cực sâu của Fire build.
* **Role trong build:** Core Piece
* **Note cực quan trọng:** anti-synergy cứng với passive làm thay đổi mặt dice như `Crit Escalation` là **intentional**.
* **Trạng thái:** **Đã chốt rõ**

#### 3) Cinderbrand

* **Element:** Fire
* **Tag:** `Status (Debuff)`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** **Uncommon**
* **Delivery Pattern:** Single Target
* **Condition chính:** Target có Burn bị consume
* **Effect Modules:** Debuff payoff amplification
* **Text hiện tại:** Trong **3 turn**, mục tiêu nhận thêm **+1 damage cho mỗi Burn bị consume**.
* **Ví dụ:** Burn spender cơ bản từ **+2 mỗi Burn** thành **+3 mỗi Burn**; `Hellfire` từ **+3 mỗi Burn** thành **+4 mỗi Burn**.
* **Vai trò thiết kế:** không trực tiếp gây burst, mà tăng trần payoff cho Fire build nếu player đã setup đúng.
* **Role trong build:** Support Piece
* **Trạng thái:** **Đã chốt rõ**

### 3.2 Pool skill chưa chốt — Physical

#### 4) Precision Strike

* **Element:** Physical
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** **Uncommon**
* **Delivery Pattern:** Single Target
* **Condition chính:** Đòn này Crit
* **Effect Modules:** X Damage + Self Added Value payoff
* **Text hiện tại:** Gây X damage. Nếu đòn này Crit, nhận `+2 Added Value` cho chính đòn đó.
* **Vai trò:** X-skill signature của Physical; giữ liên kết với trục Crit nhưng không còn ép parity làm cửa vào mặc định.
* **Role trong build:** Core Signature / Support Piece

#### 5) Brutal Smash

* **Element:** Physical
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 1 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Target đã có Mark trước khi trúng
* **Effect Modules:** Fixed Damage + Focus Refund
* **Text hiện tại:** Gây 12 damage gốc. Nếu mục tiêu đã có Mark trước khi trúng, hồi ngay 1 Focus.
* **Vai trò:** direct payoff rõ, kết nối Physical với Mark economy.
* **Role trong build:** Support Piece

#### 6) Heavy Cleave

* **Element:** Physical
* **Tag:** `Attack`
* **Slots / Focus:** **2 slot, 3 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Giá trị cao nhất trong 2 dice của cụm
* **Effect Modules:** Damage + Highest-in-group payoff
* **Text hiện tại:** Gây damage chuẩn. Cộng thêm `Base Value` cao nhất trong 2 dice đang dùng.
* **Vai trò:** local group highest-value payoff.
* **Role trong build:** Support / Payoff Piece

#### 7) Execution

* **Element:** Physical
* **Tag:** `Attack`
* **Slots / Focus:** **3 slot, 4 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Mục tiêu chết bởi đòn này
* **Effect Modules:** Damage + Carry-over payoff
* **Text hiện tại:** Gây damage chuẩn rất cao. Nếu mục tiêu chết bởi đòn này, overkill damage cộng vào Added Value cho đòn `Attack` hoặc `Sunder` đầu tiên của lượt kế.
* **Vai trò:** finisher / carry-over payoff.
* **Role trong build:** Core / Finisher Piece

#### 8) Sunder

* **Element:** Physical
* **Tag:** `Sunder`
* **Slots / Focus:** **2 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Không có
* **Effect Modules:** Damage + Ignore Guard + Clear Guard
* **Text hiện tại:** Gây damage chuẩn. Bỏ qua hoàn toàn Guard và xóa toàn bộ Guard của mục tiêu.
* **Vai trò:** anti-Guard trực diện.
* **Guardrail:** Sunder không có hidden bonus ngoài text.
* **Role trong build:** Utility / Tech Piece

#### 9) Fated Sunder

* **Element:** Physical
* **Tag:** `Sunder`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Base Value đúng bằng “số định mệnh”
* **Effect Modules:** Exact-value tech + Guard clear
* **Text hiện tại:** Gây 2 damage. Nếu Base Value đúng bằng “số định mệnh” của combat đó, xóa sạch Guard trước khi gây sát thương. Không được hưởng `x1.2` từ Stagger.
* **Vai trò:** exact-value anti-Guard với downside rõ.
* **Role trong build:** Utility / Exact-value Tech

#### 10) no Name

* **Element:** Physical
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 4 Focus**
* **Rarity:** Rare
* **Delivery Pattern:** Single Target
* **Condition chính:** Base Value = 1
* **Effect Modules:** Exact-value payoff + High fixed damage
* **Text hiện tại:** Nếu dice = 1, skill chỉ tốn 2 focus gây 20 damage.
* **Vai trò:** exact-value jackpot / low-roll payoff
* **Role trong build:** Core Payoff hoặc Tech Finisher

#### 11) Adaptive Slot Attack (tên pending)

* **Element:** pending
* **Tag:** `Attack`
* **Slots / Focus:** `Adaptive slot`, Focus pending
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Đọc `số slot còn trống` tại thời điểm assign
* **Effect Modules:** Fixed Damage + Total Added Value + Slot-count multiplier
* **Text hiện tại:** Nếu còn `3 slot` thì skill chiếm `3`; nếu còn `2 slot` thì skill chiếm `2`; nếu còn `1 slot` thì skill chiếm `1`. Gây `4 damage gốc`, cộng `Total Added Value` của toàn bộ dice trong local group, rồi nhân với đúng `số slot thực tế đã chiếm`.
* **Ví dụ:** còn `3 slot`, skill kéo vào `3 die` thì damage = `(4 + Total Added Value của 3 die) x 3`.
* **Vai trò:** proof-of-concept rất rõ cho việc `slot occupancy / slot availability` là biến gameplay thật chứ không chỉ là presentation.
* **Role trong build:** Core Signature / Action-space Payoff

**Trạng thái authoring hiện tại:**

* Đây vẫn là case lớn chưa có builder chuẩn hoàn chỉnh trong inspector.
* Ngoài mechanic này ra, phần lớn skill thường đã bắt đầu map được vào schema mới.

### 3.3 Pool skill chưa chốt — Fire

#### 10) Ignite+

* **Element:** Fire
* **Tag:** `Status (Debuff)`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** **Uncommon**
* **Delivery Pattern:** Single Target
* **Condition chính:** Base Value lẻ
* **Effect Modules:** Apply Burn + Consume-vulnerability debuff
* **Text hiện tại:** Áp X Burn lên mục tiêu. Nếu Base Value lẻ, mục tiêu nhận debuff: Burn bị consume trên nó gây thêm `+1 damage` trong `2 turn`.
* **Vai trò:** X-skill signature của Fire; vừa setup Burn vừa mở một payoff phụ cho spender.
* **Role trong build:** Core Setup / Bridge Piece

#### 11) Cauterize

* **Element:** Fire
* **Tag:** `Status/Guard (Buff + Debuff)`
* **Slots / Focus:** **2 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Split by Value Role
* **Condition chính:** Dice thấp nhất / cao nhất trong cùng cụm
* **Effect Modules:** Apply Burn + Guard Gain
* **Text hiện tại:** Áp Burn = dice thấp nhất. Nhận Guard = dice cao nhất trong 2 dice đang dùng.
* **Vai trò:** mixed utility; dùng local low/high trong cùng cụm.
* **Role trong build:** Bridge / Utility Piece

#### 12) Fire Slash

* **Element:** Fire
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Consume Burn
* **Effect Modules:** Damage + Burn spender
* **Text hiện tại:** Gây damage chuẩn. Consume toàn bộ Burn → `+2 damage mỗi stack Burn`.
* **Vai trò:** Burn spender baseline, rõ và dễ hiểu.
* **Role trong build:** Support / Payoff Piece

> `Ember Weapon`, `Hellfire` và `Cinderbrand` đã được liệt kê ở phần skill chốt phía trên.

### 3.3A Fire skill đã setup theo inspector mới

Những skill Fire đã được setup lại theo schema inspector hiện tại:

* `Ignite+`
  * `Base Effect`: `Apply X Burn`
  * `Condition`: `Dice Parity -> Any Base Odd`
  * `If Condition Met`: `Apply Burn +2`

* `Fire Slash`
  * `Base Effect`: `Deal X Damage`
  * `Base Effect phụ`: `Consume Burn +2 damage / stack`
  * Không cần condition

* `Hellfire`
  * `Base Effect`: Burn spender
  * `Condition`: `Exact Value -> All Bases = 7`
  * phần reapply exact-value vẫn đang dùng effect/runtime đã có

* `Cauterize`
  * `Split Role`
  * `Lowest selected -> Burn`
  * `Highest selected -> Guard`

### 3.3B Inspector schema hiện đã cover gì

Schema hiện tại đã bắt đầu cover các khối sau:

* `Base Effect`
  * `Deal Flat Damage`
  * `Deal X Damage`
  * `Apply Flat Burn`
  * `Apply X Burn`

* `If Condition Met`
  * `Deal Damage`
  * `Apply Burn`
  * `Gain Guard`
  * `Gain Added Value`

* `Split Role`
  * `Lowest selected -> Burn / Guard`
  * `Highest selected -> Burn / Guard`

Điều này có nghĩa:

* mọi hệ đều có thể dùng `Gain Guard`
* mọi hệ đều có thể dùng `Gain Added Value`

### 3.3C Những case vẫn chưa cover hoàn toàn bằng inspector

Ngoài `Adaptive Slot Attack / slot = x`, hiện tại còn các nhóm sau chưa được coi là solved hoàn toàn:

* `Split Role` với output ngoài `Burn / Guard`
  * ví dụ `Added Value`, `Mark`, `Freeze`, `Bleed`

* `Nhiều condition độc lập trên cùng 1 skill`, mỗi condition mở một outcome khác nhau

* `Rule rewrite / state machine`
  * alternate mode theo lần dùng
  * retrigger / propagation nhiều tầng
  * timing đặc biệt

* `Target-state condition` nếu muốn planning/preview bám cực sát theo target động trong mọi ngữ cảnh

### 3.4 Pool skill chưa chốt — Ice

#### 13) Deep Freeze

* **Element:** Ice
* **Tag:** `Status (Debuff)`
* **Slots / Focus:** **1 slot, 3 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Target chưa có Freeze hoặc Chilled
* **Effect Modules:** Apply Freeze
* **Text hiện tại:** Áp Freeze lên mục tiêu. Không tác dụng nếu mục tiêu đã có Freeze hoặc Chilled.
* **Vai trò:** CC entry-point thẳng, tôn trọng immunity rule.
* **Role trong build:** Core Setup Piece

#### 14) Shatter Guard

* **Element:** Ice
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** **Uncommon**
* **Delivery Pattern:** Single Target
* **Condition chính:** Không có
* **Effect Modules:** X Damage + Mirror Guard gain
* **Text hiện tại:** Gây damage = `Base Value + Added Value`. Nhận Guard bằng đúng lượng damage đã gây.
* **Vai trò:** X-skill signature của Ice; biến cùng một roll thành vừa payoff vừa phòng thủ.
* **Role trong build:** Core Signature / Bridge Piece

#### 15) Frost Shield

* **Element:** Ice
* **Tag:** `Guard (Buff)`
* **Slots / Focus:** **2 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Self
* **Condition chính:** Không có
* **Effect Modules:** Guard Gain
* **Text hiện tại:** Nhận Guard chuẩn.
* **Vai trò:** anchor phòng thủ của Ice branch.
* **Role trong build:** Bridge / Anchor Piece

#### 16) Winter's Bite

* **Element:** Ice
* **Tag:** `Attack (Utility)`
* **Slots / Focus:** **1 slot, 1 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Target đang Chilled
* **Effect Modules:** Fixed Damage + Extend Chilled
* **Text hiện tại:** Gây 6 damage cố định. Kéo dài Chilled thêm 1 turn.
* **Vai trò:** cheap payoff / maintenance cho Chilled window.
* **Role trong build:** Support / Utility Piece

#### 17) Permafrost Chain

* **Element:** Ice
* **Tag:** `Status (Debuff)`
* **Slots / Focus:** **2 slot, 6 Focus**
* **Rarity:** **Rare**
* **Delivery Pattern:** Chain Control
* **Condition chính:** Freeze vừa hết trên mục tiêu hiện tại
* **Effect Modules:** Apply Freeze + Chain Control
* **Text hiện tại:** Áp Freeze lên 1 địch. Khi Freeze hết → nảy sang 1 địch khác, ưu tiên mục tiêu chưa bị khống chế.
* **Vai trò:** Rare control engine của Ice.
* **Role trong build:** Core Engine Piece

#### 18) Cold Snap

* **Element:** Ice
* **Tag:** `Attack/Guard (Buff)`
* **Slots / Focus:** **2 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Split by Slot Position
* **Condition chính:** **Vị trí slot trong cụm**
* **Effect Modules:** Positional Split + Damage + Guard
* **Text hiện tại:** **Slot bên trái** gây damage bằng giá trị die gắn ở slot trái. **Slot bên phải** cho Guard bằng giá trị die gắn ở slot phải.
* **Vai trò:** mở grammar positional cho Ice, khiến player không chỉ đọc cao/thấp mà còn đọc đúng vị trí đặt skill.
* **Role trong build:** Support / Bridge Piece
* **Trạng thái:** **Revised đã chốt**
* **Note:** phiên bản cũ dùng `dice thấp nhất trong cụm` + Freeze ngẫu nhiên 1 địch không còn là source of truth chính.

### 3.5 Pool skill chưa chốt — Lightning

#### 19) Static Conduit

* **Element:** Lightning
* **Tag:** `Status (Debuff)`
* **Slots / Focus:** **2 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target -> Board Propagation
* **Condition chính:** Target có Mark
* **Effect Modules:** Damage + Mark Propagation
* **Text hiện tại:** Gây 4 damage. Nếu mục tiêu có Mark → áp Mark lên toàn bộ kẻ địch còn lại.
* **Vai trò:** board-wide Mark propagation.
* **Role trong build:** Core Spread Piece

#### 20) Flash Step

* **Element:** Lightning
* **Tag:** `Status (Utility)`
* **Slots / Focus:** **1 slot, 1 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target + Sequencing Utility
* **Condition chính:** Không có
* **Effect Modules:** Apply Mark + Base Value transfer
* **Text hiện tại:** Áp Mark lên mục tiêu. Ghi đè giá trị Base Value của dice tiếp theo được kéo vào bằng giá trị dice hiện tại.
* **Vai trò:** setup Mark + sequencing / value transfer.
* **Role trong build:** Core Setup / Utility Piece

#### 20A) Marked Bolt

* **Element:** Lightning
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** **Uncommon**
* **Delivery Pattern:** Single Target
* **Condition chính:** Mục tiêu đã có Mark
* **Effect Modules:** X Damage + Mark spread
* **Text hiện tại:** Gây X damage lên mục tiêu. Nếu mục tiêu có Mark, áp Mark lên tất cả kẻ địch còn lại.
* **Vai trò:** X-skill signature của Lightning; dùng Mark làm bridge từ single-target sang board control.
* **Role trong build:** Core Signature / Bridge Piece

#### 21) Spark Barrage

* **Element:** Lightning
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Chain Hit
* **Condition chính:** Base Value chẵn
* **Effect Modules:** Damage + Bounce
* **Text hiện tại:** Gây damage chuẩn. Nếu Base Value chẵn → hit này nảy sang 1 mục tiêu khác.
* **Vai trò:** parity-driven chain hit.
* **Role trong build:** Support / Bridge Piece

#### 22) Overload

* **Element:** Lightning
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Số Mark đang có trên toàn sân
* **Effect Modules:** Damage + Board-count payoff
* **Text hiện tại:** Gây 2 damage chuẩn cộng thêm **5 damage cho mỗi Mark đang có trên toàn sân**.
* **Vai trò:** board-count payoff rõ ràng.
* **Role trong build:** Support / Payoff Piece

#### 23) Thunderclap

* **Element:** Lightning
* **Tag:** `Attack`
* **Slots / Focus:** **3 slot, 4 Focus**
* **Rarity:** pending
* **Delivery Pattern:** AoE
* **Condition chính:** không có
* **Effect Modules:** AoE Damage + Mark payoff
* **Text hiện tại:** Gây 5 damage AOE.
* **Vai trò:** AoE payoff đỉnh của Lightning board control.
* **Role trong build:** Core Finisher Piece

### 3.6 Pool skill chưa chốt — Bleed

#### 24) Lacerate

* **Element:** Bleed
* **Tag:** `Attack/Status (Debuff)`
* **Slots / Focus:** **1 slot, 3 Focus**
* **Rarity:** **Uncommon**
* **Delivery Pattern:** Single Target
* **Condition chính:** Crit
* **Effect Modules:** Apply Bleed
* **Text hiện tại:** Áp X Bleed. Nếu Crit → áp thêm X Bleed nữa.
* **Vai trò:** X-skill signature của Bleed; crit vào đúng die sẽ mở bùng nổ stack rất rõ.
* **Role trong build:** Core Signature / Setup Piece

#### 25) Blood Ward

* **Element:** Bleed
* **Tag:** `Guard (Buff)`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Self
* **Condition chính:** Tổng Bleed trên toàn bộ kẻ địch
* **Effect Modules:** Guard Gain via Enemy Status
* **Text hiện tại:** Nhận Guard = tổng số Bleed stack đang có trên tất cả kẻ địch.
* **Vai trò:** chuyển DoT pressure thành phòng thủ.
* **Role trong build:** Support / Conversion Piece

#### 26) Siphon

* **Element:** Bleed
* **Tag:** `Status (Utility)`
* **Slots / Focus:** **2 slot, 3 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Consume Bleed theo ngưỡng 5
* **Effect Modules:** Bleed Conversion -> Consumable
* **Text hiện tại:** Consume toàn bộ Bleed trên 1 mục tiêu. Cứ 5 Bleed tiêu thụ → tạo 1 Consumable ngẫu nhiên, tối đa 3.
* **Vai trò:** resource conversion engine.
* **Role trong build:** Core Engine Piece

#### 27) Hemorrhage

* **Element:** Bleed
* **Tag:** `Attack/Status (Debuff)`
* **Slots / Focus:** **2 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** HP player đã mất ở lượt trước
* **Effect Modules:** Reflect Pressure -> Bleed
* **Text hiện tại:** Áp Bleed lên mục tiêu bằng đúng số HP mà player đã mất ở lượt trước đó.
* **Vai trò:** phản chiếu áp lực nhận vào thành Bleed payoff.
* **Role trong build:** Support / Reactive Piece

---

## 3.7 Brainstorm skill candidates - history / custom-state

> Nhóm này là **brainstorm pool**, chưa phải implementation target gần.
> Điểm chung của cả 3 skill là đều cần đọc `history`, `self memory` hoặc `custom-state` của chính skill trong combat.
> Vì vậy hiện tại nên xem đây là nhóm skill đặc biệt, chưa thuộc lớp `1 condition chuẩn + 1-2 effect module` thông thường.

#### 28) Momentum Strike

* **Element:** pending
* **Tag:** `Attack`
* **Slots / Focus:** pending
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Số lần chính skill này đã được dùng trong combat hiện tại
* **Effect Modules:** Self usage scaling + Damage
* **Text hiện tại:** Bắt đầu combat với 0 damage nền rất thấp. Mỗi lần chính skill này được dùng, damage nền của nó tăng cố định cho những lần dùng sau trong cùng combat. Fantasy ví dụ: lần 1 gây `1 damage`, lần 2 gây `4 damage`, lần 3 gây `7 damage`.
* **Vai trò:** payoff tăng trưởng thuần theo số lần commit dùng chính skill đó (hết combat reset về 0).
* **Role trong build:** Core Scaling Piece
* **Trạng thái:** Brainstorm - custom-state candidate

#### 29) Frozen Ledger

* **Element:** Ice
* **Tag:** `Attack`
* **Slots / Focus:** pending
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Kết hợp `enemy đang Freeze` và `enemy đã từng bị Freeze trong combat này`
* **Effect Modules:** Board-state payoff + Status-history payoff
* **Text hiện tại:** Bắt đầu với `0 damage`. Skill này nhận thêm damage theo mỗi instance Freeze hợp lệ. Fantasy ví dụ: mỗi enemy đang Freeze hoặc mỗi enemy đã từng bị Freeze trong combat đóng góp `+4 damage`; về sau nếu combat đã có tổng cộng 6 instance hợp lệ thì skill có thể lên `24 damage`.
* **Vai trò:** biến Ice từ control thuần thành nhánh có payoff theo dấu vết Freeze đã tạo ra trong suốt combat (hết combat reset về 0).
* **Role trong build:** Bridge / History Payoff Piece
* **Trạng thái:** Brainstorm - status-history candidate
* **Note:** cần chốt rõ sau này skill đếm theo `số enemy unique từng bị Freeze`, `số instance Freeze từng xảy ra`, hay `đang Freeze + ever Frozen` như hai bucket riêng.

#### 30) Addvalue Hoard

* **Element:** pending
* **Tag:** `Attack`
* **Slots / Focus:** pending
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** `Total Added Value` của action hiện tại
* **Effect Modules:** Added Value payoff + Self memory scaling
* **Text hiện tại:** Bắt đầu combat với `0 damage nền`. Mỗi lần dùng, skill gây damage = `damage nền đang tích` + `Total Added Value` của action hiện tại. Sau khi resolve, phần Added Value vừa dùng sẽ được cộng vĩnh viễn vào damage nền của chính skill cho các lượt sau trong cùng combat. Ví dụ: turn đầu nhận `+2 Added Value` thì gây `2 damage` và từ đó skill có nền `2`; turn sau nhận `+9 Added Value` thì gây `11 damage` và damage nền mới của skill trở thành `11`.
* **Vai trò:** biến Added Value từ lớp thưởng tức thời thành tài nguyên tích lũy dài hơi cho riêng một skill.
* **Role trong build:** Core Scaling / Engine Payoff
* **Trạng thái:** Brainstorm - custom-state candidate

---

## 4. 11 passive hiện tại

| Passive                          | Rarity       | Text hiện tại                                                                                                          | Vai trò thiết kế                                                                                                            | Trạng thái          |
| -------------------------------- | ------------ | ---------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| **Crit Escalation**              | **Rare**     | Mỗi khi một die roll Crit → **chỉ die đó** nhận `+1 Base` lên toàn bộ mặt của nó cho đến hết combat. Reset sau combat. | Combat snowball có kiểm soát hơn bản cũ, giữ cá tính riêng cho từng die, vẫn anti-synergy có chủ đích với exact-value setup | **Revised đã chốt** |
| **Dice Forging**                 | **Rare**     | **Lần đầu tiên mỗi combat** dùng Basic Attack → **mặt dice vừa dùng** nhận `+1 Base` vĩnh viễn cho toàn bộ Run.        | Run growth / dice customization engine gắn trực tiếp với Basic Attack                                                       | **Revised đã chốt** |
| **Clear Mind**                   | **Uncommon** | Bắt đầu mỗi lượt, hồi thêm 1 Focus.                                                                                    | Economy engine universal, giúp xoay lượt và làm cầu nối cho nhiều build                                                     | **Revised đã chốt** |
| **Iron Stance**                  | Rare         | Guard không biến mất vào cuối lượt của Player.                                                                         | Guard-retention engine                                                                                                      | Pending             |
| **Even Resonance**               | Uncommon     | Mỗi dice có Base Value chẵn nhận thêm +3 Added Value cho đòn đó.                                                       | Parity payoff rõ ràng                                                                                                       | Pending             |
| **Elemental Catalyst**           | Uncommon     | Khi địch nhận Burn hoặc Bleed, cộng thêm 1 stack khuyến mãi.                                                           | Status amplification                                                                                                        | Pending             |
| **Spiked Armor**                 | Uncommon     | Khi địch đánh vào Guard của bạn, chúng nhận lại Physical damage bằng lượng Guard bị phá vỡ.                            | Defensive retaliation                                                                                                       | Pending             |
| **Mitigation (Desperate Guard)** | Common       | Dùng Basic Guard bằng dice có Base Value <= 3 → tạo 1 Consumable.                                                      | Basic Guard payoff / utility generation                                                                                     | Pending             |
| **Fail Forward**                 | Common       | Mỗi khi roll ra mặt thấp nhất / Fail → nhận ngay 3 Guard.                                                              | Bù đắp roll xấu, soften variance                                                                                            | Pending             |
| **Alchemist**                    | UnCommon     | Bắt đầu mỗi combat, nhận 1 Consumable ngẫu nhiên.                                                                      | Utility baseline                                                                                                            | Pending             |
| **Lingering Mark**               | Uncommon     | Mark trên mục tiêu giờ cần **2 hit hợp lệ** để bị xóa thay vì 1.                                                       | Mark payoff extension                                                                                                       | Pending             |

### 5.0 Rule resolve damage của content layer

Từ current spec này trở đi:

* Phần lớn skill gây damage không còn đọc `X`; chúng có **damage gốc/chuẩn** và khi resolve sẽ cộng thêm **Added Value** áp dụng cho action đó.
* Nhóm skill signature dùng `X` vẫn giữ nguyên; mặc định **`X = Base Value + Added Value`**.
* `Basic Attack` cũng đi theo rule trên: có `4 damage gốc`, rồi cộng Added Value nếu có.
* `Fail` chỉ cắt nửa **base output của skill**, luôn floor; không làm giảm Added Value và không thay đổi Base Value.
* `base output` ở đây không chỉ là damage; nếu một skill tạo Guard/Burn/Bleed/output lịch sử và phần đó được xem là output gốc của chính skill, Fail cũng đọc trên phần đó.
* Về sau có thể tồn tại skill hoặc passive cho thêm Added Value nếu đạt điều kiện, kể cả theo từng skill riêng lẻ hoặc tăng tiến trong combat/run.
* Nếu skill chiếm 2 hoặc 3 slot, Added Value mặc định là **tổng Added Value của toàn bộ dice trong local group** của skill đó.
* Dice có thể mang / sinh Added Value từ **Crit** hoặc từ **enchant / consumable / dice customization** đã nằm trên mặt dice; ngoài ra Added Value cũng có thể do skill/passive/relic/modifier cấp.
* Nếu skill là `split-role / multi-branch`, mỗi output phải dùng `Added Value` của đúng die source của branch đó, không được lấy crit/add/fail của die này để cộng sang output do die khác tạo ra.

### 5.0A Skill Damage Formula Sheet

* `Base Value` = mặt thật của die, dùng để check condition.
* `Added Value` = phần cộng vào output cuối, không đổi bản chất của die.
* `Crit`:
  * non-Physical -> `Added Value += floor(Base Value x 0.2)`
  * Physical -> `Added Value += floor(Base Value x 0.5)`
* `Fail` -> làm `base output = floor(base output / 2)`, không trừ `Added Value`.
* `Skill damage chuẩn` -> `Final Damage = damage gốc + Total Added Value`
* `Skill X` -> `X = Base Value + Added Value`
* `Skill fixed Guard / status / history output` -> `Final Output = output gốc + Total Added Value`
* `Split-role skill` -> mỗi output branch resolve bằng `Base/Added/Crit/Fail` của đúng die source của branch
* `Skill 1 slot` -> `Total Added Value = Added Value của 1 die`
* `Skill 2 slot` -> `Total Added Value = Added Value die 1 + Added Value die 2`
* `Skill 3 slot` -> `Total Added Value = Added Value die 1 + Added Value die 2 + Added Value die 3`
* `Damage từ combat history counter` cũng tiếp tục cộng `Added Value` nếu text không nói ngược lại
* Toàn game dùng `floor`
* Nếu `base output > 0` nhưng sau Fail / floor nhỏ hơn `1`, final output vẫn phải tối thiểu là `1`
* Nếu `base output = 0`, final output tiếp tục là `0`
* Damage dương mà sau tính toán `< 1` thì thành `1` tối thiểu
---

## 5. Anti-synergy và note rất quan trọng

### 5.1 Hellfire vs Crit Escalation

* `Hellfire` cần exact value `7` để loop đúng fantasy.
* `Crit Escalation` tăng mặt của die vừa Crit trong combat và có thể phá exact-value setup.
* Đây là **anti-synergy có chủ đích**, không phải bug design.

### 5.2 Clear Mind + Hellfire

* `Clear Mind` tăng nhịp hồi Focus mỗi lượt.
* Kết hợp với `Hellfire` có thể tạo loop gần hoặc hoàn toàn tự duy trì.
* Đây là interaction mạnh nhưng là loại sức mạnh phải đánh đổi passive flexibility.

### 5.3 Dice Forging vs Crit Escalation

* Cả hai đều tương tác với dice growth,
* nhưng một cái là **tăng trưởng vĩnh viễn theo run**,
* một cái là **tăng trưởng tạm trong combat**.

### 5.4 Basic actions phải còn chỗ đứng

Nhiều passive và một phần skill hiện tại cố tình làm Basic Attack hoặc Basic Guard có giá trị build-level.
Đây là định hướng cần giữ, không phải filler tạm thời.

### 5.5 Build thuần mạnh hơn, nhưng utility ngoài hệ vẫn hợp lệ

Game cho phép giữ 1–2 skill hoặc passive trái hệ / universal để vá nhịp, giữ sống sót hoặc nối engine.
Nhưng các món này không được mạnh tới mức build nào cũng dùng như nhau.

---

## 6. Combo engines đã identify

### 6.1 Fire Loop Engine

Ví dụ flow:

```text
Ember Weapon -> Basic Attack x2 -> tích Burn -> Cinderbrand -> Hellfire detonate
```

Nếu đi kèm `Elemental Catalyst`, lượng Burn và damage nổ tăng mạnh.
Nếu custom dice để mặt `7` xuất hiện đúng ý, `Hellfire` có thể trở thành loop win condition của cả run.

### 6.2 Lightning Board Control

Ví dụ flow:

```text
Flash Step -> Static Conduit -> reapply Mark / giữ board -> Thunderclap
```

Hoặc line đơn giản hơn:

```text
Flash Step -> Static Conduit -> Overload
```

Ý tưởng là dùng Mark để biến direct-hit thành board-wide payoff.

### 6.3 Ice Freeze Loop / Guard Tempo

Ví dụ flow:

```text
Deep Freeze -> giảm áp lực còn 2 địch hoạt động -> Cold Snap / Frost Shield -> Shatter / Winter's Bite
```

Fantasy của build này là:

* control đầu combat,
* tạo cửa sổ an toàn,
* đổi control thành Guard và payoff,
* thắng bằng tempo dài hơi thay vì burst ngay.

Build này mạnh nhất khi combat có **3 kẻ địch**, nhưng vẫn cần còn giá trị ở combat ít địch hơn nhờ nhánh Guard / Focus / Chilled payoff.

### 6.4 Bleed Resource Engine

Ví dụ flow:

```text
Lacerate -> Blood Ward -> Siphon
```

Bleed không chỉ là DoT mà còn là tài nguyên có thể đổi thành Guard hoặc Consumable.

### 6.5 Crit Snowball

Ví dụ flow:

```text
Crit Escalation -> Precision Strike -> crit nhiều hơn -> die đó lớn dần trong combat
```

Đây là engine snowball gắn với custom dice, nhưng không còn buff cả bộ dice như version cũ.

### 6.6 Dice Customization Engine

Ví dụ flow:

```text
Dice Forging -> tăng base mặt trọng yếu -> passive đọc parity/exact value -> skill payoff bùng nổ
```

Đây là trục build dài hạn theo run thay vì chỉ combat tức thời.

---

## 7. Guardrail khi thêm skill/passive mới

### 7.1 Skill mới nên bám rõ trục nào trong 8 trục chính

* `Crit / Fail`
* `Parity (chẵn/lẻ)`
* `Exact value`
* `Local group / slot relation`
* `Effect / Target State`
* `Resource axis (Focus / Guard)`
* `Rule-bending axis (retrigger, copy/move base, add value)`
* `Board / encounter axis`

Khi thêm skill mới, đừng bắt đầu bằng câu hỏi `skill này gây bao nhiêu damage?`.
Hãy bắt đầu bằng:

* nó đọc trục nào,
* nó đổi quyết định gì,
* nó payoff cái gì,
* và nó thuộc vai trò gì trong build.

Vì đây là file `detail`, nếu một skill đọc một trục nào đó thì text spec nên chỉ ra càng cụ thể càng tốt là nó đang đọc biến nào:

* `Focus hiện tại` hay `Focus còn lại sau cost`
* `Guard của player` hay `Guard của target`
* `slot còn trống` hay `slot đã chiếm`
* `die đầu / die cuối trong local group`
* `tổng Added Value của action`
* `số enemy đã từng bị áp trạng thái trong combat này`

Không nên chỉ ghi kiểu:

* “đọc trục tài nguyên”
* “đọc vị trí slot”
* “đọc board state”

Nếu không nói rõ biến đang đọc, skill sẽ dễ đúng trên giấy nhưng mơ hồ khi implement và khó đọc khi balance.

### 7.2 Rare không được chỉ là “số to hơn”

Rare nên mở ra:

* engine mới,
* payoff mới,
* sequencing mới,
* hoặc build identity mới.

Nếu rare chỉ là common với damage lớn hơn, rarity đang bị dùng sai.

### 7.3 Không mọi thứ đều phải cộng hưởng với mọi thứ

Intentional anti-synergy là hợp lệ nếu nó:

* giữ build sắc,
* buộc player chọn hướng,
* ngăn một build ôm trọn mọi payoff mạnh.

### 7.4 Checklist bắt buộc khi tạo skill mới

Từ giờ, mỗi skill mới nên được viết dưới đúng form này:

* **Tên skill**
* **Element**
* **Tag chính / phụ**
* **Slots / Focus**
* **Rarity**
* **Delivery Pattern**
* **Condition chính**
* **Effect Modules**
* **Text hiện tại / text chuẩn hóa**
* **Vai trò thiết kế**
* **Role trong build**
* **Note anti-synergy / guardrail nếu có**

Nếu một skill không điền được theo form này, nghĩa là skill đó chưa đủ rõ để thêm vào game.

---

## 8. Những gì đã khóa mạnh hơn sau bản cập nhật này

Các điểm sau hiện nên được xem là direction đã khóa đủ mạnh:

* `Ember Weapon` là **Uncommon Buff** của Fire,
* `Hellfire` là **Rare Core Piece** của Fire,
* `Cinderbrand` là **Uncommon Debuff** của Fire,
* `Cold Snap` dùng version mới: **slot trái = attack / slot phải = guard**,
* `Crit Escalation` chỉ buff **die vừa Crit**, không còn buff toàn bộ dice equip,
* `Dice Forging` chỉ kích hoạt ở **lần Basic Attack đầu tiên mỗi combat**,
* `Clear Mind` là **Uncommon**,
* ba hệ ưu tiên polish trước là **Fire / Ice / Lightning**,
* về sau thêm skill mới nên ghép từ module chuẩn, không tạo exception thủ công nếu không cần.

---

## 9. Những gì chưa final trong file này

Các điểm sau **chưa nên coi là khóa cứng** nếu source hiện tại chưa xác nhận:

* rarity của phần lớn skill trong pool,
* data số cuối cùng của từng skill,
* thứ tự ra mắt / unlock order của từng skill,
* full text runtime cuối cùng cho UI,
* toàn bộ balance tuning,
* một phần Physical / Bleed hiện vẫn là content pool nhiều hơn là content đã khóa.

Nhưng các điểm sau là **content direction rất mạnh và nên được giữ**:

* 27 skill hiện tại như trên,
* 11 passive hiện tại như trên,
* Fire loop, Lightning board control, Ice freeze-tempo, Bleed resource, Crit snowball, Dice customization là các combo engine thật,
* anti-synergy như `Hellfire` vs `Crit Escalation` là intentional,
* grammar skill chuẩn hóa là hướng đúng để mở rộng content sau này mà không phải code lại từ đầu mỗi lần thêm skill mới.
---

## 10. Progress note - 2026-03-22

Doan nay la note tien do de chat sau biet duoc dang lam toi dau. Khong thay the content spec ben tren.

### 10.1 Skill/passive dang duoc dua vao runtime theo tung slice

Hien tai chua phai toan bo pool da chay that. Slice dau tien da duoc uu tien la:

- Fire skill: Ignite, Hellfire, Ember Weapon, Cinderbrand
- Passive: Clear Mind, Even Resonance, Elemental Catalyst, Fail Forward, Iron Stance, Crit Escalation, Dice Forging

### 10.2 Hellfire da duoc lam ro hon

Rule can nho cho content va runtime:

- Hellfire consume Burn de gay damage
- chi neu target da co Burn san truoc hit thi moi co phan ap lai Burn
- moi die trong local group co Base Value = 7 se cong them 7 Burn
- day khong phai rule "tat ca dice deu phai bang 7"

### 10.3 Huong authoring dang chot

Khong theo huong:

- hardcode skill/passive theo display name
- hoac ep 100% moi mechanic vao inspector bang cac field roi rac

Huong dang dung:

- engine = code
- content identity = data / asset / inspector
- exception = custom behavior hook

### 10.4 Viec can lam tiep

- migrate them skill/passive con lai vao runtime theo huong tren
- bo sung generic effect/condition modules de giam so mechanic phai viet custom
- giu anti-synergy intentional neu da la mot phan cua build identity
