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

* Gây damage theo giá trị dice
* Gây damage cố định
* Gây damage AoE
* Gây bonus damage theo condition

#### B. Guard Modules

* Nhận Guard theo giá trị dice
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
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Base Value chẵn
* **Effect Modules:** Damage + Guaranteed Crit
* **Text hiện tại:** Gây X damage. Nếu Base Value chẵn, đòn này luôn Crit.
* **Vai trò:** entry-point đơn giản cho parity + crit engine.
* **Role trong build:** Support / Entry Piece

#### 5) Brutal Smash

* **Element:** Physical
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 1 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Target đã có Mark trước khi trúng
* **Effect Modules:** Fixed Damage + Focus Refund
* **Text hiện tại:** Gây 12 damage cố định. Nếu mục tiêu đã có Mark trước khi trúng, hồi ngay 1 Focus.
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
* **Text hiện tại:** Gây damage = X + Base Value cao nhất trong 2 dice đang dùng.
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
* **Text hiện tại:** Gây tổng X damage. Nếu mục tiêu chết bởi đòn này, overkill damage cộng vào Added Value cho đòn `Attack` hoặc `Sunder` đầu tiên của lượt kế.
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
* **Text hiện tại:** Gây X damage. Bỏ qua hoàn toàn Guard và xóa toàn bộ Guard của mục tiêu.
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

### 3.3 Pool skill chưa chốt — Fire

#### 10) Ignite

* **Element:** Fire
* **Tag:** `Status (Debuff)`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Base Value lẻ
* **Effect Modules:** Apply Burn
* **Text hiện tại:** Áp X Burn. Nếu Base Value lẻ, áp thêm 2 Burn.
* **Vai trò:** setup Burn đơn giản, parity hook.
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
* **Text hiện tại:** Gây X damage. Consume toàn bộ Burn → `+2 damage mỗi stack Burn`.
* **Vai trò:** Burn spender baseline, rõ và dễ hiểu.
* **Role trong build:** Support / Payoff Piece

> `Ember Weapon`, `Hellfire` và `Cinderbrand` đã được liệt kê ở phần skill chốt phía trên.

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

#### 14) Shatter

* **Element:** Ice
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Target đang Chilled
* **Effect Modules:** Damage + Guard-based payoff
* **Text hiện tại:** Gây X damage. Nếu mục tiêu đang Chilled → cộng thêm 50% Guard hiện có của player, tối đa +20.
* **Vai trò:** chuyển control thành payoff.
* **Role trong build:** Core Payoff Piece

#### 15) Frost Shield

* **Element:** Ice
* **Tag:** `Guard (Buff)`
* **Slots / Focus:** **2 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Self
* **Condition chính:** Không có
* **Effect Modules:** Guard Gain
* **Text hiện tại:** Nhận X Guard.
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

#### 21) Spark Barrage

* **Element:** Lightning
* **Tag:** `Attack`
* **Slots / Focus:** **1 slot, 2 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Chain Hit
* **Condition chính:** Base Value chẵn
* **Effect Modules:** Damage + Bounce
* **Text hiện tại:** Gây X damage. Nếu Base Value chẵn → hit này nảy sang 1 mục tiêu khác.
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
* **Text hiện tại:** Gây X damage cộng thêm **4 damage cho mỗi Mark đang có trên toàn sân**.
* **Vai trò:** board-count payoff rõ ràng.
* **Role trong build:** Support / Payoff Piece

#### 23) Thunderclap

* **Element:** Lightning
* **Tag:** `Attack`
* **Slots / Focus:** **3 slot, 4 Focus**
* **Rarity:** pending
* **Delivery Pattern:** AoE
* **Condition chính:** Giá trị cao nhất trong cụm + số kẻ địch đang có Mark
* **Effect Modules:** AoE Damage + Mark payoff
* **Text hiện tại:** X = dice cao nhất trong cụm. Gây X damage lên tất cả. Cộng thêm 4 damage cho mỗi kẻ địch đang có Mark.
* **Vai trò:** AoE payoff đỉnh của Lightning board control.
* **Role trong build:** Core Finisher Piece

### 3.6 Pool skill chưa chốt — Bleed

#### 24) Lacerate

* **Element:** Bleed
* **Tag:** `Attack/Status (Debuff)`
* **Slots / Focus:** **1 slot, 3 Focus**
* **Rarity:** pending
* **Delivery Pattern:** Single Target
* **Condition chính:** Crit
* **Effect Modules:** Apply Bleed
* **Text hiện tại:** Áp X Bleed. Nếu Crit → áp thêm X Bleed nữa.
* **Vai trò:** Crit-based Bleed injector.
* **Role trong build:** Core Setup Piece

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

### 7.1 Skill mới nên bám ít nhất một trục sau

#### Trục dice

* Crit / Fail
* Chẵn / Lẻ
* Cao / Thấp
* Exact value
* Highest / Lowest trong nhóm
* **Vị trí slot trong cụm**

#### Trục trạng thái trên target

* Burn stacks
* Mark
* Freeze / Chilled
* Bleed stacks
* Stagger

#### Trục tài nguyên player

* Focus hiện tại
* Guard hiện tại
* lane đã dùng / còn trống
* số dice đang equip hoặc đang gắn vào một cụm

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
