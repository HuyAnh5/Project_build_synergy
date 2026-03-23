# ENEMIES_BOSSES_ENCOUNTERS_SPEC.md

> Tài liệu này mô tả **enemy intent**, **encounter pressure**, **boss philosophy** và vai trò của hidden boss / endless escalation**.  
> Mục tiêu của file là khóa hướng tạo áp lực lên player mà không làm game đi lệch khỏi build fantasy.

---

## 1. Mục tiêu của hệ thống

Enemy và boss tồn tại để:

- tạo áp lực chiến thuật thật,
- buộc player đọc board và sequencing tốt,
- làm lộ điểm yếu của build,
- nhưng không tắt hẳn fantasy của build đó.

Boss đặc biệt phải là:

**mechanic wall, không phải stat wall**

---



## 2. Logic Flow

Phần này mô tả **đường đi logic của pressure phía enemy side**: encounter được đọc ra sao, enemy chọn move thế nào, boss tạo friction bằng cách nào, và hidden boss / endless nằm ở đâu trong chuỗi escalation.

### 2.1 Flow của một enemy turn

`Enemy Turn Start`  
→ Resolve start-of-turn state của enemy  
→ Đọc intent / pattern / condition hiện tại  
→ Chọn move theo weighted + condition + anti-spam + pattern  
→ Execute move  
→ Resolve damage / status / heal / guard / summon theo loại move  
→ Resolve end-of-turn state  
→ Chuyển lượt lại cho player nếu combat chưa kết thúc

### 2.2 Flow áp lực encounter

`Encounter bắt đầu`  
→ Player đọc enemy intent và pressure pattern  
→ Hệ thống đặt ra bài toán chiến thuật: phòng thủ, sequencing, control, payoff hay burst  
→ Player phản ứng qua combat loop của mình  
→ Encounter kiểm tra xem build có đủ trụ cột hay không mà không được hard-block hẳn fantasy của build

### 2.3 Flow boss pressure

`Boss encounter bắt đầu`  
→ Boss lộ pattern / phase / unique build-interaction mechanic  
→ Player phải đọc cơ chế và sequencing tốt hơn combat thường  
→ Boss tạo friction, ép thích nghi hoặc pivot nhẹ  
→ Nếu player hiểu hệ thống: vẫn luôn có counterplay; nếu không: build bị lộ điểm yếu

### 2.4 Flow endless / hidden boss

`Thắng boss cuối`  
→ Player có thể dừng run hoặc vào Endless  
→ Endless tiếp tục tăng pressure để build được test xa hơn  
→ Hidden boss chỉ xuất hiện trong Endless đủ sâu  
→ Hidden boss đóng vai trò bài kiểm tra cuối của build, sequencing và hiểu mechanic


## 3. Enemy system — current direction

### 3.1 Intent telegraph

Enemy phải có **intent rõ ràng** theo tinh thần Slay the Spire:

- player đọc được hướng hành động chính,
- combat có nhịp dự đoán / phản ứng,
- sequencing phòng thủ / control / payoff có cơ sở.

### 3.2 Enemy không dùng cùng economy với player

Current direction từ project context:

- enemy **không dùng focus / dice** như player,
- enemy được xây theo move set, pattern, weight và condition.

### 3.3 Move selection logic

Current finalized direction trong project context:

- move selection = **weighted + condition + anti-spam + pattern**

Ý nghĩa:

- enemy không quá ngẫu nhiên,
- nhưng cũng không cứng tới mức giải như bài toán cố định từ turn 1,
- mỗi enemy có cá tính và nhịp hành vi riêng.

### 3.4 Role archetypes hiện tại

Current direction có 4 role chính:

- **Bruiser**
- **Controller**
- **Assassin**
- **Support**

Các role này dùng để định hình pressure pattern chứ không phải chỉ để đặt tên cho đẹp.

---

## 4. Encounter pressure và move type

Các move type đã được định hình trong project context gồm:

- Basic
- Heavy
- Guard
- Guard Punish (giới hạn, không one-shot)
- Status
- Heal (`< 50% HP`)
- Setup → Big
- rare Summon

### 4.1 Guardrails cho áp lực damage

Current direction:

- burst cap khoảng **70–80% max HP mỗi enemy turn** ở mức tổng áp lực, tránh cảm giác chết oan không đọc được,
- tránh pattern kiểu triple Heavy hoặc triple Guard Punish,
- Guard Punish chỉ nên tạo áp lực để player nghĩ lại việc spam Guard, không nên xóa bỏ hẳn fantasy phòng thủ.

### 4.2 Mục tiêu thiết kế của encounter

Encounter tốt phải:

- khiến player đọc intent,
- kiểm tra sequencing,
- kiểm tra build có đủ trụ cột chưa,
- làm status/payoff/lane order có ý nghĩa,
- không biến combat thành cuộc đua damage thuần.

---

## 5. Boss philosophy — current locked direction

Boss hiện được định nghĩa theo triết lý sau:

- Boss là **bức tường cơ chế**, không phải stat wall đơn thuần
- Boss **không hard counter hoàn toàn** bất kỳ build nào
- Boss tạo **friction**, không tạo “cấm chơi hệ này”

### 5.1 Điều boss phải làm được

- ép player thích nghi hoặc sequencing tốt hơn,
- làm lộ điểm yếu của build,
- buộc player dùng đúng phần sâu của hệ thống,
- tạo một bài kiểm tra hiểu cơ chế chứ không chỉ kiểm tra số.

### 5.2 Điều boss không được làm

- miễn nhiễm tuyệt đối một hệ,
- tắt hẳn payoff của player,
- phản damage vô điều kiện, thiếu counterplay,
- biến trận đấu thành “nếu build không đúng meta thì thua từ màn hình chọn”.

### 5.3 Boss design ở cấp structure

Current finalized direction trong project context:

- Boss có khoảng **4 move**, 
- pattern rõ,
- phase-based,
- có **unique build-interaction mechanic**, 
- không hard-counter build,
- luôn có counterplay.

Ví dụ triết lý đúng hướng:

- boss đánh vào cách build vận hành,
- nhưng không xóa sạch fantasy của build đó,
- buộc player sequencing hoặc pivot nhẹ, thay vì buộc respec toàn bộ.

---

## 6. Hidden boss và endless escalation

### 6.1 Endless mode

Sau khi thắng boss cuối, player có thể tiếp tục đi Endless.  
Mục tiêu của Endless:

- hoàn thiện build,
- test sức mạnh build,
- đạt trạng thái “full build hoàn hảo”,
- phục vụ kiểu chơi muốn optimize run xa hơn chiến thắng đầu tiên.

### 6.2 Hidden boss

Current design direction:

- chỉ xuất hiện trong **Endless mode**,
- nằm đủ sâu để không lộ quá sớm,
- nhưng không quá sâu đến mức full build xong vẫn không gặp nổi,
- hidden boss là bài kiểm tra cuối cùng của hệ thống build, sequencing và hiểu mechanic.

Trong source hiện tại còn có direction:

- hidden boss có thể **xoay vòng ability của các boss khác** theo chu kỳ,
- nhưng không dùng tất cả ability cùng lúc,
- nó lấy từ pool ability của boss toàn game.

---

## 7. Nguyên tắc tạo encounter mới

Khi tạo enemy hoặc boss mới, phải tự kiểm tra:

1. nó tạo pressure kiểu gì?
2. pressure đó có đọc được qua intent không?
3. nó đang kiểm tra trụ cột nào của player: damage, sequencing, Guard, status, tempo, resource hay lane order?
4. nó có tạo friction hay đang hard-block một build?
5. nó có mở ra counterplay hay chỉ phạt player thô bạo?

Nếu không trả lời rõ các câu trên, encounter đó chưa đủ chín.

---

## 8. Những gì chưa final trong file này

Các vùng sau chưa có data khóa cứng đầy đủ:

- full enemy roster,
- full boss roster,
- stat table cụ thể,
- encounter distribution theo floor/map,
- summon logic chi tiết,
- hidden boss final moveset.

Nhưng các phần sau là direction rất mạnh / đã chốt ở mức triết lý:

- enemy dùng intent system,
- move selection = weighted + condition + anti-spam + pattern,
- có các role Bruiser / Controller / Assassin / Support,
- boss là mechanic wall, không hard-counter build,
- hidden boss chỉ nên nằm trong Endless,
- encounter pressure phải đọc được và phải có counterplay.
