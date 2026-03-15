# AGENTS.md

## 1. Mục đích

File này là context chính để agent làm việc với project Unity này.
Đọc hết file trước khi sửa code.

Mục tiêu:

* hiểu đúng combat spec hiện tại
* hiểu đúng cấu trúc code hiện tại
* tránh phá những phần đã ổn định
* ưu tiên patch gọn, dễ review, dễ test

---

## 2. Tổng quan project

Đây là game roguelike turn-based trên mobile, trọng tâm là:

* dice
* skill slot
* passive
* status / payoff
* lane order / action order

Vòng chơi combat có logic chính:

* roll dice
* gán skill vào lane
* khóa plan
* chọn target
* execute từ trái sang phải theo lane hiện tại

Game không đi theo hướng bắt người chơi phải tính nhẩm quá nhiều.
Tooltip / preview về sau phải hiện kết quả cuối cùng, không ép player tự quy đổi Base / Added mới chơi được.

---

## 3. Action economy

Player có:

* 6 skill slot
* 3 passive slot
* tối đa 3 dice

Đầu run chỉ có 1 dice.

Focus:

* reset sau mỗi combat
* đầu combat có 2 Focus
* đầu mỗi turn +1 Focus
* turn 1 thực tế là 3 Focus
* không cho nợ Focus

Basic action:

* Basic Attack: 0 Focus, 4 damage cố định, +1 Focus
* Basic Guard: 0 Focus, Guard bằng Base Value của die

---

## 4. Dice rules đã chốt

### 4.1 Base và Added

* Base Value = mặt thật roll ra
* Added Value = phần cộng trừ thêm vào output cuối
* Mọi condition phải đọc từ Base Value
* Added Value không đổi bản chất của die

Condition phải đọc theo Base:

* chẵn / lẻ
* crit / fail
* <= 3
* exact value
* highest / lowest

### 4.2 Local die context

Nếu skill 2-3 slot có text kiểu lấy die cao nhất / thấp nhất,
chỉ xét trong chính nhóm die của skill đó, không xét cả bàn.

### 4.3 Crit / Fail

* Crit = roll đúng giá trị mặt cao nhất của die
* Fail = roll đúng giá trị mặt thấp nhất của die
* Crit / Fail không đổi Base Value
* Crit / Fail chỉ sinh Added Value

Rule đã chốt:

* Crit thường = +20% Base
* Crit Physical = +50% Base
* Fail = -50% Base

### 4.4 Dice có nhiều mặt trùng max / min

* Nhiều mặt cùng max -> tất cả là crit
* Nhiều mặt cùng min -> tất cả là fail
* Nếu max == min -> tất cả là crit, không có fail
* Trong case max == min, crit thắng fail

### 4.5 Làm tròn

* Toàn game dùng floor
* Damage hợp lệ sau tính toán nếu < 1 thì vẫn là minimum 1
* Ngoại lệ: Guard chặn hết thì có thể không mất HP

### 4.6 Pipeline mong muốn

* `baseValue`
* `critFailAddedValue`
* `passiveAddedValue`
* `totalAddedValue`
* `resolvedValue`

`DiceSlotRig` là source of truth chính cho dice math.

---

## 5. Turn flow đã chốt

1. Start Phase

* +1 Focus
* passive đầu lượt
* enemy chốt intent

2. Roll Phase

* roll các dice hiện có

3. Planning Phase

* kéo dice vào skill slot
* được reorder dice trong phase này

4. Execution Phase

* skill chạy từ trái sang phải theo lane hiện tại
* vào execute thì khóa reorder

5. Enemy / End Phase

* enemy hành động
* xử lý status tick / cleanup
* player mất Guard cuối lượt trừ khi có rule giữ Guard

Phần reorder / lane mapping đang được xem là ổn ở mức nền tảng.
Đừng sửa vào đây nếu không thật sự cần.

---

## 6. Core combat rule đã chốt

### 6.1 Damage

* Damage trừ Guard trước
* Dư mới vào HP

### 6.2 Stagger

Rule đã chốt:

* khi Guard từ > 0 về 0, mục tiêu vào `Stagger`
* overflow của hit phá Guard vẫn vào HP bình thường
* overflow đó không được x1.2
* chỉ hit kế tiếp duy nhất mới ăn `x1.2 tổng damage`
* hết turn mà không có hit bồi thì `Stagger` biến mất

Chi tiết:

* `Lightning shock` không consume `Stagger`
* `Bleed tick` không consume `Stagger`
* `Burn consume` nằm trong direct-hit thì được tính vào tổng damage trước khi nhân `1.2`

### 6.3 Tag skill

Tag chính:

* `Attack`
* `Guard`
* `Status`
* `Sunder`

`Sunder` không có bonus ẩn. Nếu có tương tác đặc biệt thì phải viết rõ trong rule / text thật.

---

## 7. Element / effect đã chốt

### 7.1 Physical

* burst / anti-Guard / damage thẳng
* Crit Physical dùng `+50% Base`

### 7.2 Fire / Burn

Burn không phải DoT chính. Burn là resource để consume.

Rule:

* Burn có stack
* consume mặc định = `+2 damage mỗi stack Burn bị xóa`
* chỉ skill đặc biệt mới override

### 7.3 Ice / Freeze / Chilled

Rule:

* Freeze: skip 1 turn
* hết Freeze -> thành Chilled
* Chilled tồn tại 2 turn
* đang Freeze hoặc Chilled thì miễn Freeze mới
* Ice damage hit vào target Freeze / Chilled -> player `+1 Focus +3 Guard`

### 7.4 Lightning / Mark

Mark:

* không stack
* là điểm yếu để direct-hit xử lý
* shock phụ của Lightning không làm mất Mark

Non-Lightning hit vào Mark:

* `+4 direct damage` lên chính mục tiêu đó

Lightning hit vào Mark:

* hit chính gây damage bình thường
* sau đó proc `4 damage all enemies`

Rule shock phụ:

* không cộng Added Value
* không tiêu Mark
* không proc Mark
* không chain tiếp

Nếu AoE Lightning direct-hit nhiều mục tiêu có Mark:

* mỗi mục tiêu có Mark tạo 1 shock proc
* shock chạy tuần tự
* mỗi proc cách nhau `0.2s`

### 7.5 Bleed

* gây damage đầu lượt
* bỏ qua Guard
* giảm dần theo lượt

---

## 8. Ailment

Ailment hiện được coi là enemy-side system.

Nghĩa là:

* không xem ailment như nhóm skill player cast lên enemy
* enemy là bên sử dụng ailment lên player / combat state

Rule chance hiện tại trong code:

* enemy -> player = 100%

Nếu trong code còn helper player -> enemy thì đó không còn là gameplay ưu tiên.

---

## 9. Tooltip / preview direction

Hướng design đã chốt:

Trong shop / khi chưa có dice:

* hiện dạng tĩnh: `Deal X damage`, `Gain X Guard`

Trong combat / khi đã roll:

* hiện số thật đã resolve

Tooltip / preview / execute về sau phải dùng cùng một nguồn số.

Trạng thái hiện tại:

* đã có icon preview nền tảng
* formatter runtime text chưa cần chốt ngay
* phần này để sau khi chuẩn hóa skill data

---

## 10. Lane mapping rule cần giữ

Rule cốt lõi:

`1/2/3 là identity của cặp dice/icon, A/B/C là lane hiện tại. Logic phải đọc theo A/B/C, không đọc theo 1/2/3.`

Phần này đang được xem là đã ổn:

* reorder trong Planning
* execution order theo lane mới
* damage order theo lane mới
* phase lock khi vào execute

---

## 11. Hệ thống chính trong code

Những class quan trọng:

* `TurnManager`
* `SkillPlanBoard`
* `SkillExecutor`
* `CombatActor`
* `StatusController`
* `BattlePartyManager2D`
* `DiceSlotRig`
* `DiceSpinnerGeneric`
* `RunInventoryManager`
* `PassiveSystem`
* `DamagePopupSystem`
* `CombatHUD`
* `ActorWorldUI`
* `TargetClickable2D`

---

## 12. Kiến trúc skill data

Project đang dùng hướng tách skill thành các nhóm:

* `SkillDamageSO`
* `SkillBuffDebuffSO`
* `SkillPassiveSO`

Legacy `SkillSO_Legacy` và `SkillConditionalOverrides` đã bị bỏ.
Không dùng lại pipeline `SkillSO` cũ nữa.

Khi sửa, cẩn thận compatibility với scene / prefab cũ.
User ưu tiên chuẩn hóa skill data sau.
Không cần cố gắng động vào content layer nếu task hiện tại là combat / refactor code runtime.

---

## 13. Cấu trúc `Assets/Scripts` hiện tại

Code chính hiện nằm dưới `Assets/Scripts`, không còn để trong `Assets/Scripts/Demo`.

Cấu trúc domain hiện tại:

* `Assets/Scripts/Combat/Actors`
* `Assets/Scripts/Combat/Execution`
* `Assets/Scripts/Combat/Status`
* `Assets/Scripts/Combat/Turn`
* `Assets/Scripts/Dice`
* `Assets/Scripts/Enemies`
* `Assets/Scripts/Inventory`
* `Assets/Scripts/Skills/Basic`
* `Assets/Scripts/Skills/Buff`
* `Assets/Scripts/Skills/Damage`
* `Assets/Scripts/Skills/Debuff`
* `Assets/Scripts/Skills/Definitions`
* `Assets/Scripts/Skills/Effect`
* `Assets/Scripts/Skills/Legacy`
* `Assets/Scripts/Skills/Passive`
* `Assets/Scripts/Skills/Planning`
* `Assets/Scripts/Skills/Runtime`
* `Assets/Scripts/UI/Combat`
* `Assets/Scripts/UI/Loadout/Dice`
* `Assets/Scripts/UI/Loadout/Passive`
* `Assets/Scripts/UI/Planning`

Khi mở file hoặc viết note mới:

* không dùng lại path cũ kiểu `Assets/Scripts/Demo/...`
* không mặc định `Assets/Scripts/Combat/Core/...` nữa

---

## 14. Tình hình hiện tại của project

### 14.1 Đã xong ở combat core

Những rule / hệ thống đã cập nhật theo spec mới:

* Focus đầu combat = 2, turn 1 thực tế = 3
* Burn consume baseline = `+2 / stack`
* Freeze / Chilled immunity và Ice reward = `+1 Focus +3 Guard`
* Bleed tick đầu lượt bỏ qua Guard
* Mark / Lightning theo rule direct-hit + shock proc
* shock Lightning chạy tuần tự với delay `0.2s`
* `Stagger` đã được implement và hiển thị như một status thật
* ailment direction hiện là enemy-side, enemy -> player = 100%

### 14.2 Tình hình refactor file lớn

Refactor đã làm theo hướng:

* giữ class Unity gốc
* tách logic nặng ra utility / helper mới
* giảm rủi ro mất reference scene / prefab

Đã tách:

`TurnManager`

* `Assets/Scripts/Combat/Turn/TurnManagerCombatUtility.cs`
* `Assets/Scripts/Combat/Turn/TurnManagerLifecycleUtility.cs`
* `Assets/Scripts/Combat/Turn/TurnManagerPlanningUtility.cs`
* `Assets/Scripts/Combat/Turn/TurnManagerTargetingUtility.cs`
* `Assets/Scripts/Combat/Turn/TurnManagerViewUtility.cs`

`SkillExecutor`

* `Assets/Scripts/Combat/Execution/AttackPreviewCalculator.cs`
* `Assets/Scripts/Combat/Execution/SkillAttackResolutionUtility.cs`
* `Assets/Scripts/Combat/Execution/SkillTargetResolver.cs`

`StatusController`

* `Assets/Scripts/Combat/Status/StatusRuntimeEntries.cs`
* `Assets/Scripts/Combat/Status/StatusBuffDebuffUtility.cs`
* `Assets/Scripts/Combat/Status/StatusAilmentUtility.cs`
* `Assets/Scripts/Combat/Status/StatusStateUtility.cs`

`SkillPlanBoard`

* `Assets/Scripts/Skills/Planning/SkillPlanBoardStateUtility.cs`
* `Assets/Scripts/Skills/Planning/SkillPlanRuntimeUtility.cs`

`RunInventoryManager`

* `Assets/Scripts/Inventory/RunInventoryBindingUtility.cs`
* `Assets/Scripts/Inventory/RunInventorySetupUtility.cs`
* `Assets/Scripts/Inventory/RunInventoryLoadoutUtility.cs`

`EnemyBrainController`

* `Assets/Scripts/Enemies/EnemyIntentSelectionUtility.cs`
* `Assets/Scripts/Enemies/EnemyIntentPreviewUtility.cs`

Loadout UI - Dice:

* `Assets/Scripts/UI/Loadout/Dice/DiceEquipLayoutUtility.cs`
* `Assets/Scripts/UI/Loadout/Dice/DiceEquipWorldSyncUtility.cs`
* `Assets/Scripts/UI/Loadout/Dice/DiceEquipStateUtility.cs`
* `Assets/Scripts/UI/Loadout/Dice/DiceEquipPresentationUtility.cs`
* `Assets/Scripts/UI/Loadout/Dice/DiceEquipWorldFollowUtility.cs`

Loadout UI - Passive:

* `Assets/Scripts/UI/Loadout/Passive/PassiveEquipLayoutUtility.cs`
* `Assets/Scripts/UI/Loadout/Passive/PassiveEquipWorldSyncUtility.cs`
* `Assets/Scripts/UI/Loadout/Passive/PassiveEquipStateUtility.cs`
* `Assets/Scripts/UI/Loadout/Passive/PassiveEquipPresentationUtility.cs`

### 14.3 Những phần đang ổn

Những vùng này được xem là khá ổn và không nên động vào nếu không cần:

* planning -> await target -> executing -> enemy turn
* reorder dice trong planning
* execution order / damage order theo lane hiện tại
* lane mapping giữa pair identity và lane
* consume rule nền tảng

### 14.4 Những việc còn lại hợp lý cho chat sau

Nếu tiếp tục ở đoạn chat sau, ưu tiên hợp lý là:

* tiếp tục tách file nếu có file nào thật sự vẫn ôm quá nhiều logic
* chuẩn hóa skill data / content layer
* tooltip / runtime preview formatter sau khi skill data ổn
* polish dice feedback UI

Nếu combat core đã ổn và file đã ở mức chấp nhận được, không cần cố gắng tách thêm chỉ vì số dòng.

---

## 15. Quy tắc khi agent sửa code

Bắt buộc:

* đừng phá những phần reorder / lane / execution đang ổn
* execute, preview, tooltip không được nói ba kiểu nếu sửa dice math
* effect phải bám theo spec trong file này, không bám logic cũ nếu logic cũ lệch
* ưu tiên patch nhỏ, dễ review
* nếu refactor lớn, phải nói rõ vì sao
* luôn nêu file nào sửa và vì sao
* nếu có thể, đưa checklist test thủ công

Khi sửa combat:

* ưu tiên bảo toàn behavior đúng trước
* utility mới chỉ nên tách khi giúp code dễ đọc hơn thật sự
* giữ `SkillPlanBoard` là source of truth cho lane planning
* giữ `DiceSlotRig` là source of truth cho resolved dice math
* không để UI trở thành source of truth thứ hai

---

## 16. Cách trả lời mong muốn

User thường viết bằng tiếng Việt.
Ưu tiên trả lời bằng tiếng Việt, ngắn, rõ, thẳng vào vấn đề.

Nếu sửa code, nên nêu:

1. Nguyên nhân
2. File nào sửa
3. Vì sao sửa như vậy
4. Checklist test

---

## 17. Tóm tắt cực ngắn

Nếu chỉ nhớ vài điểm, hãy nhớ:

1. Đây là game dice-slot tactical combat trên mobile
2. Đừng phá flow combat, reorder và lane mapping đang ổn
3. Player có 6 skill slot, 3 passive slot, tối đa 3 dice
4. `DiceSlotRig` là source of truth của dice math
5. `SkillPlanBoard` là source of truth của lane planning
6. `Ailment` hiện là enemy-side system
7. Combat core đã cập nhật Burn, Mark/Lightning, Freeze/Chilled, Bleed, Stagger; việc lớn tiếp theo nghiêng về skill data và tiếp tục dọn code khi cần

---

## 18. Ghi chú bổ sung từ chat gần đây

### 18.1 Reorder / lane mapping

Theo trạng thái mới nhất mà user chốt:

* reorder trong Planning đang được xem là ổn ở mức nền tảng
* execution order theo lane mới đang ổn
* damage order theo lane mới đang ổn
* phase lock khi vào execute đang ổn

Chỉ sửa vùng này nếu có bug rõ ràng được tái hiện lại.
Không tự ý refactor lại reorder nếu không có lý do mạnh.

### 18.2 Combat core đã cập nhật theo spec mới

Những phần được xem là đã cập nhật và không nên coi là "còn thiếu" nữa:

* Burn consume baseline
* Mark / Lightning proc rule
* Freeze / Chilled immunity và Ice reward
* Bleed tick bỏ qua Guard
* Stagger core behavior

### 18.3 Ailment là enemy-side system

Khi viết mô tả, tooltip, plan refactor hoặc content note:

* không mô tả Ailment như một bộ player skill
* coi nó là enemy-side debuff / combat control system

### 18.4 Hướng ưu tiên hiện tại

Theo user, hướng hợp lý cho chat sau là:

* tiếp tục tách file nếu thật sự cần để dễ đọc / dễ quản lý
* chuẩn hóa skill data / content layer
* tooltip / runtime preview formatter sau khi skill data ổn
* polish thêm dice feedback UI

Nếu combat core đang ổn và không có bug cần sửa ngay, không cần tiếp tục đập lớn vào core combat chỉ để refactor.

### 18.5 Trạng thái skill legacy

Những file / pipeline sau đã bị loại bỏ:

* `SkillSO_Legacy`
* `SkillConditionalOverrides`

Không đưa lại reference tới `SkillSO` cũ khi sửa code mới.

### 18.6 Ghi chú thực tế cho agent ở đoạn chat sau

Nếu tiếp tục làm việc ở đoạn chat khác, hãy giả định đúng các điểm sau:

* `Assets/Scripts/Demo` không còn là nơi chứa code chính
* `Assets/Scripts/Combat/Core` không còn là cây thư mục dùng
* combat core đã được cập nhật theo spec mới cho Burn, Mark / Lightning, Freeze / Chilled, Bleed, Stagger
* `Ailment` hiện được coi là enemy-side system
* ưu tiên hiện tại là giữ combat rule ổn định
* chỉ refactor tiếp khi nó thực sự làm code dễ đọc hơn
* không cần cố tách thêm nếu file hiện tại đã ở mức chấp nhận được

---

## 19. Hiện trạng trước khi commit

Nếu combat đã được test qua trong Unity và console sạch, có thể xem scope chat này là xong ở mức runtime / refactor.

Trước khi commit:

* kiểm tra console Unity không còn lỗi compile
* test nhanh roll -> planning -> target -> execute -> enemy turn
* test nhanh reorder dice
* test nhanh reorder passive

Nếu không có vấn đề, có thể commit.
