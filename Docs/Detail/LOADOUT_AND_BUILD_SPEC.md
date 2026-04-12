# LOADOUT_AND_BUILD_SPEC.md

> Tài liệu này mô tả **cấu trúc build**, **loadout system**, vai trò của **Basic Attack / Basic Guard**, quan hệ giữa **skill / passive / dice**, và cách game muốn các build khác nhau thực sự **chơi khác nhau**.

---

## 1. Mục tiêu của hệ thống

Loadout system tồn tại để đạt 5 mục tiêu:

1. giữ bề mặt combat đủ gọn để đọc nhanh,
2. tạo chiều sâu thật từ số lượng slot ít,
3. giữ dice là trung tâm của quyết định thay vì để skill list nuốt mất bản sắc dice,
4. giữ Basic Attack và Basic Guard còn giá trị chiến thuật trong suốt run,
5. khiến build mạnh lên theo hướng **chơi khác đi**, không chỉ là cộng thêm số.

---

## 2. Logic Flow

Phần này mô tả đường đi logic của build: player nhận content mới thế nào, loadout được chốt ra sao, combat đọc loadout như thế nào, và build được điêu khắc dần qua run ra sao.

### 2.1 Flow từ reward / shop tới loadout

`Player thấy content mới`
→ content mới thuộc một trong các loại: skill / passive / dice / relic
→ player so sánh món mới với build hiện tại
→ player quyết định **giữ để equip**, **thay món cũ**, hoặc **bỏ qua / bán**
→ không có inventory dự trữ để cất tạm
→ nếu slot loại tương ứng đã đầy, player phải bỏ hoặc bán món cũ mới lấy được món mới

### 2.2 Flow loadout trước combat

`Chuẩn bị vào combat`
→ hệ thống đọc đúng **6 skill slot**, **1 passive slot**, **1–3 dice đang equip**, cùng với **Basic Attack / Basic Guard**
→ build hiện tại được xem là input chính cho combat
→ combat bắt đầu với đúng loadout đã được chốt ở ngoài combat

### 2.3 Flow shaping build qua run

`Reward / shop / progression xuất hiện liên tục`
→ player đánh giá món mới có củng cố engine hiện tại không
→ nếu có: commit sâu hơn vào build hiện tại
→ nếu không: player có thể giữ hướng cũ, hoặc pivot mạnh nếu thấy xuất hiện 1–2 key piece của hướng khác
→ vì không có inventory, mỗi lần pivot luôn đi kèm trade-off thật
→ sau nhiều combat, build đi từ mơ hồ → có tín hiệu → commit có chọn lọc → engine rõ ràng

### 2.4 Flow basic actions trong cấu trúc build

`Roll xấu hoặc thiếu tài nguyên`
→ player vẫn luôn có **Basic Attack** và **Basic Guard**
→ basic action đóng vai trò bridge giữa turn hiện tại và turn sau
→ hệ thống đảm bảo player luôn có một lựa chọn tối thiểu có ý nghĩa, thay vì chết lượt hoàn toàn

---

## 3. Cấu trúc loadout hiện tại

### 3.1 Thành phần loadout

Player hiện có:

* **6 skill slot**
* **1 passive slot**
* **1 đến 3 dice slot**
* **Basic Attack** và **Basic Guard** luôn tồn tại, không nằm trong 6 slot chính

Rule làm rõ rất quan trọng:

* mỗi skill luôn chỉ chiếm **1 loadout slot** khi mang vào combat,
* nhưng mỗi skill có thể có **dice cost / slot cost 1-2-3** trong Planning phase,
* skill 3-dice không chiếm 3 chỗ trong loadout; nó chỉ **ăn 3 action economy** ở turn player assign nó.

Ví dụ: `Hellfire` chỉ chiếm **1 slot loadout**, nhưng khi assign sẽ dùng hết **3 dice**, nên turn đó gần như chỉ còn 1 action lớn duy nhất.

### 3.2 Ý nghĩa của từng lớp

#### Skill slots

Là nơi player mang các hành động chủ động vào combat. Đây là lớp quyết định build muốn **làm gì**.

#### Passive slots

Là nơi player mang engine macro vào combat. Đây là lớp quyết định build **đọc dice**, **đọc economy**, **đọc trạng thái** và **đọc basic action** như thế nào.

#### Dice slots

Là lớp quyết định **action economy** và **identity của từng turn**. Số dice equip gần như tương ứng với số action groups player có thể plan trong một lượt.

#### Basic actions

Là anchor nền của combat. Chúng giúp build không bao giờ rơi vào trạng thái “hết bài hoàn toàn”, đồng thời vẫn là một phần thật của hệ thống chứ không chỉ là fallback giả.

---

## 4. Quy tắc loadout ngoài combat

### 4.1 Không có inventory dự trữ

Game không dùng inventory kiểu cất tạm để tính sau.

Khi xuất hiện content mới, player phải quyết định ngay:

* lấy và equip,
* thay món cũ,
* hoặc bỏ qua / bán.

Điều này khiến mỗi quyết định loadout đều có ma sát thật và giúp run mang cảm giác điêu khắc dần, thay vì gom đồ về kho rồi tối ưu muộn.

### 4.2 Full slot handling

Nếu slot loại tương ứng còn trống, player có thể lấy món mới vào loadout.

Nếu slot đã đầy, player chỉ có thể:

* thay món cũ bằng món mới,
* hoặc bỏ qua / bán món mới.

Game không cho phép lấy trước rồi giữ trong kho để quyết định sau.

### 4.3 Swap tự do ngoài combat

Skill, passive và dice đều có thể được thay đổi ngoài combat.
Hệ thống hướng tới cảm giác linh hoạt kiểu Balatro: player có thể thay đổi cấu trúc build giữa run nếu nhìn thấy cơ hội đủ mạnh.

### 4.4 Pivot giữa run

Game cho phép pivot mạnh hơn Slay the Spire và gần với tinh thần Balatro:

* nếu player nhìn thấy 1–2 key piece của một hướng build khác, họ có thể đổi hướng đáng kể,
* nhưng pivot không bao giờ miễn phí vì phải bỏ món cũ ngay,
* 1 key piece không đủ để một build tự chạy hoàn chỉnh,
* build mới vẫn cần thêm support piece và bridge piece để trở nên ổn định.

Mục tiêu là để pivot là một phần thú vị của run, nhưng không trở thành “thấy món tím là auto quay xe”.

---

## 5. Basic actions như một phần của build

### 5.1 Basic Attack

* **0 Focus**
* gây **4 damage gốc**
* cho **+1 Focus**

Basic Attack vẫn nhận `Added Value` khi resolve damage.
Vì nó không phải `Physical`, crit của Basic Attack dùng hệ số crit thường `+20% Base Value` để tạo Added Value.
Fail chỉ cắt nửa damage gốc của Basic Attack xuống `2`, không làm đổi Base Value và không trừ Added Value.

Basic Attack không chỉ là fallback.
Nó còn là điểm gắn với:

* economy loop,
* low-risk sequencing,
* các payoff liên quan hit cơ bản,
* những build muốn dùng 1 slot để vá nhịp thay vì commit quá sâu.

### 5.2 Basic Guard

* **0 Focus**
* tạo Guard = **Base Value** của die dùng cho action đó

Basic Guard không chỉ là nút thủ.
Nó là nền cho:

* build Guard-retention,
* build muốn đổi sống sót thành tài nguyên,
* các payoff liên quan Guard,
* ổn định turn xấu.

### 5.3 Guardrail cực quan trọng

Dù build về late run mạnh đến đâu, Basic Attack và Basic Guard vẫn phải giữ vai trò hệ thống thực sự.
Nếu mọi build cuối cùng đều bỏ hẳn basic actions khỏi tư duy chiến thuật, economy nền đang yếu đi và cấu trúc combat sẽ trở nên quá phụ thuộc vào high-roll.

---

## 6. Vai trò của dice trong cấu trúc build

Dice không chỉ là “nguồn số cho skill”.
Trong cấu trúc build, dice quyết định:

* số action groups mỗi turn,
* loại condition nào build có thể khai thác ổn định,
* crit / fail profile,
* parity profile,
* exact-value fantasy,
* mức độ snowball theo run,
* độ ổn định hoặc độ cực đoan của payoff.

Vì vậy, build identity luôn phải được hiểu là:

**skill + passive + dice + sequencing**

chứ không phải chỉ là một list kỹ năng.

---

## 7. Build identity — game muốn điều gì

Build mạnh không chỉ là build có số to.
Build mạnh đúng hướng là build khiến người chơi:

* ưu tiên lane khác,
* đọc board khác,
* chọn target khác,
* cân Focus khác,
* dùng hoặc không dùng basic actions khác,
* chấp nhận anti-synergy khác,
* và đánh giá reward / shop khác.

Nói cách khác: build tốt phải thay đổi **cách player ra quyết định**, không chỉ thay đổi damage output.

---

## 8. Build composition rule

Một build hoàn chỉnh thường gồm 3 lớp:

### 8.1 Core piece

Là những món mở engine chính của build.
Không có core piece thì build chưa có bản sắc rõ.

### 8.2 Support piece

Là những món khiến core piece hoạt động ổn định hơn, mạnh hơn, hoặc đều hơn qua nhiều tình huống.

### 8.3 Bridge / utility piece

Là những món vá nhịp, vá sống sót, vá economy, hoặc giúp build chưa hoàn thiện vẫn có thể sống qua mid-run.

Rule quan trọng là:

* **core piece không đủ để build tự thắng**,
* build mạnh phải có thêm support piece đúng hướng,
* bridge piece giúp build sống được trong lúc engine chưa hoàn chỉnh.

---

## 9. Off-build utility cho phép tới đâu

Nguyên tắc của game là:

* **build càng thuần thì trần sức mạnh càng cao**,
* nhưng player vẫn được phép giữ **1–2 skill hoặc passive trái hệ / universal** để vá nhịp, giữ sống sót, hoặc làm cầu nối tạm thời.

Ví dụ các món off-build hợp lệ:

* passive kiểu `mỗi turn +1 Focus`,
* một skill phòng thủ không cùng hệ,
* một công cụ ổn định turn xấu,
* một món utility dùng được ở nhiều build.

Các món off-build này được phép tồn tại vì chúng giúp run linh hoạt và làm pivot mượt hơn.
Nhưng chúng không được mạnh tới mức mọi build đều nhặt giống nhau, nếu không build identity sẽ bị hội tụ.

---

## 10. Intentional anti-synergy trong cấu trúc build

Không phải mọi món mạnh đều nên cộng hưởng với nhau.
Một số anti-synergy có chủ đích là tốt nếu nó:

* giữ build sắc,
* buộc player chọn hướng,
* ngăn một run ôm mọi payoff tốt nhất cùng lúc.

Ví dụ đúng tinh thần:

* `Hellfire` là exact-value engine,
* `Crit Escalation` đẩy mặt dice lên,
* hai thứ này anti-synergy là hợp lý và giúp build identity sắc nét hơn.

---

## 11. Loop xây build qua run

Ở cấp loadout, quá trình build nên cho cảm giác:

1. bắt đầu với khung đơn giản,
2. tìm thấy direction đầu tiên,
3. quyết định món nào là core, món nào chỉ là cầu nối,
4. cắt bỏ thứ không hợp,
5. thêm skill / passive / dice / relic đẩy đúng engine,
6. đến late run thì build không chỉ mạnh hơn mà còn **chơi khác đi rõ rệt**.

Build không nên tiến triển theo hướng:

* nhặt cái gì mạnh nhất trước mặt,
* dồn thành một đống “good stuff” không có identity,
* hoặc chỉ cộng thêm damage trung tính mà không đổi quyết định.

---

## 12. Build Examples — current direction

Phần này chỉ là ví dụ minh họa để chứng minh framework build đang hoạt động.
Đây không phải full content pool.

### 12.1 Fire Build — Hellfire

**Fantasy:** setup rồi detonate.
**Loop ngắn:** áp Burn hoặc tích đủ điều kiện → dùng skill consume Burn để nổ damage lớn → dùng món hỗ trợ để giữ nhịp và sống sót → lặp lại setup → detonate.
**Identity:** single-target burst, payoff sắc, phụ thuộc sequencing đúng và điều kiện đúng.
**Cấu trúc build:** cần core piece gây / giữ Burn, cần payoff consume Burn, và cần support piece giúp đúng ngưỡng dice hoặc đúng nhịp turn.

### 12.2 Ice Build — Freeze Loop

**Fantasy:** cắt nhịp combat rồi đổi control thành tài nguyên.
**Loop ngắn:** turn đầu cố Freeze sớm một mục tiêu khi combat có 3 địch để giảm áp lực xuống còn 2 địch hoạt động → trong cửa sổ control đó, dùng các skill băng khác để tăng Guard và khai thác payoff theo Guard → giữ nhịp sống sót, Focus và tempo → tiếp tục kiểm soát combat dài hơi.
**Identity:** không thắng bằng burst ngay, mà thắng bằng giảm áp lực đầu combat, sống dai hơn và chuyển Guard / control thành tài nguyên đánh lâu.
**Lưu ý:** build này mạnh nhất khi đúng điều kiện, nhưng vẫn phải còn giá trị ở combat ít địch hơn thông qua lớp Guard / Focus payoff.

### 12.3 Lightning Build — Mark Spread

**Fantasy:** đánh đúng mục tiêu để lan cả board rồi payoff AoE.
**Loop ngắn:** turn đầu áp Mark lên một mục tiêu → dùng direct-hit để tiêu Mark và lan Mark sang các mục tiêu khác → nếu cần, áp lại Mark vào mục tiêu vừa bị tiêu → turn sau dùng AoE payoff lên board đang có Mark để tạo burst diện rộng.
**Identity:** build này thay đổi rõ cách player chọn target và sắp action.
**Cấu trúc build:** cần core piece áp Mark, support piece giúp lan Mark, và payoff piece chuyển board đã được đánh dấu thành damage diện rộng.

---

## 13. Những rule thiết kế nên giữ khi thêm content mới

### 13.1 Mỗi skill / passive mới nên trả lời được

* nó thay đổi cách đọc dice như thế nào?
* nó thay đổi sequencing ra sao?
* nó có cho player một quyết định mới thật hay chỉ cộng số?
* nó có giữ basic actions còn chỗ đứng hay xóa mất chúng?
* nó là core piece, support piece hay bridge piece?

### 13.2 Build không nên bị hội tụ

Không để mọi run cuối cùng đều hội tụ về:

* một kiểu burst vô danh,
* một kiểu crit stack chung chung,
* hoặc một đường build universal tốt hơn mọi hướng còn lại.

### 13.3 Số lượng công cụ ít nhưng giàu tương tác

Đây là nguyên tắc sống còn của loadout system hiện tại:

* slot ít,
* hành động ít,
* nhưng mỗi phần tử phải giàu tương tác.

---

## 14. Out of Scope của file này

File này **không** chịu trách nhiệm chốt các phần sau:

* full shop rules về giá mua / bán / reroll,
* unlock order chi tiết của từng skill / passive / dice,
* tỷ lệ xuất hiện và anti-duplicate logic cụ thể,
* rarity math hoặc reward math,
* chỉ số balance cuối cùng của từng skill.

Những phần đó thuộc các spec khác.

---

## 15. Current Locked Direction

Những phần sau nên được coi là direction đã khóa đủ mạnh ở cấp loadout:

* **6 skill slot / 1 passive slot / 1–3 dice slot**,
* **Basic Attack** và **Basic Guard** luôn tồn tại,
* không có inventory dự trữ,
* full slot thì phải thay món cũ hoặc bỏ qua,
* skill / passive / dice đều được swap ngoài combat,
* build identity phải đến từ **skill + passive + dice + sequencing**,
* build thuần thường mạnh hơn nhưng được phép giữ **1–2 off-build utility / bridge piece**,
* pivot giữa run được phép mạnh theo tinh thần Balatro, nhưng luôn có trade-off,
* intentional anti-synergy là hợp lệ,
* basic actions phải tiếp tục là một phần hệ thống thật.

---

## Runtime Update Note (2026-04)

- Passive loadout runtime hien tai da khoa o `1 passive slot`.
- Skill loadout van la `6 skill slot`.
- Combat/runtime grammar hien tai dang dung:
  - player roll dice dau luot
  - reorder neu can
  - keo skill icon tu skill slot vao target de cast truc tiep
  - bam End Turn khi khong dung them skill
- Huong doi nay duoc note day du hon trong [COMBAT_CHANGES_2026.md](/C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Docs/Detail/COMBAT_CHANGES_2026.md).
