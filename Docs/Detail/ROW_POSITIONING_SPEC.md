# ROW_POSITIONING_SPEC.md

> File này là spec riêng cho **front row / back row**.
> Chỉ ghi lại **logic của row**:
> - row là gì,
> - rule target chuẩn là gì,
> - rule AoE chuẩn là gì,
> - row áp dụng cho phe nào,
> - row được content đọc như thế nào,
> - row tách với lane/order ra sao.
>
> File này **không** ghi flow/control grammar, lane sequencing, hay damage modifier ngoài row.

---

## 1. Row là gì

**Front Row / Back Row** là lớp:

- vị trí
- quyền target
- combat state

Row **không phải** lớp cộng/trừ damage mặc định.

Không có rule nền kiểu:

- front row luôn tăng damage
- back row luôn giảm damage nhận
- front row luôn tank hơn bằng modifier ẩn
- back row luôn yếu hơn bằng modifier ẩn

Rule này áp dụng **đối xứng cho cả 2 phe**:

- player team
- enemy team

Sau này nếu player có ally thì ally cũng dùng đúng rule row này.

---

## 2. Tách thành 2 lớp riêng

Row system nên được đọc qua **2 lớp riêng**:

### 2.1 Access Type

- `Strike`
- `Range`

### 2.2 Delivery Pattern

- `Single Target`
- `Row AoE`
- `All-row / Cross-row`

`All-row / Cross-row` là **ngoại lệ hiếm**, không phải mặc định.

---

## 3. Rule target chuẩn

### 3.1 Strike

Nếu phe đối diện vẫn còn ít nhất 1 unit sống ở **front row**:

- không được target **back row**

Chỉ khi **front row trống hết**:

- mới được target **back row**

Rule rất quan trọng:

- việc attacker đang đứng **front** hay **back** không tự mở quyền đánh xuyên row,
- thứ quyết định quyền chạm mục tiêu là **Access Type**,
- với `Strike`, front row luôn là lớp chặn mặc định.

Nói gọn:

- **Strike luôn bị front row chặn**

### 3.2 Range

`Range` có thể target tự do:

- front row
- back row

`Range` không bị front row chặn như `Strike`.

---

## 4. Rule AoE chuẩn

### 4.1 Strike AoE

Nếu phe đối diện còn unit sống ở **front row**:

- chỉ quét được **front row**

Chỉ khi **front row trống**:

- mới quét được **back row**

Nói gọn:

- `Strike AoE` quét hàng ngoài cùng còn đang chặn.

### 4.2 Range AoE

`Range AoE` được chọn tự do **1 row** để quét:

- front
- hoặc back

### 4.3 Cross-row / All-row AoE

Đây là **ngoại lệ hiếm có chủ đích**.

Loại này có thể:

- chạm cả 2 row,
- hoặc vượt luật chặn row thông thường.

Dành cho các case đặc biệt như:

- vài skill hiếm của player,
- vài đòn boss,
- các ngoại lệ kiểu `Lightning / Mark`.

---

## 5. Điều rất quan trọng cần hiểu đúng

Câu đúng là:

**Nếu phe đối diện còn front row thì đòn kiểu Strike không thể đánh vào back row, bất kể người đánh đang đứng front hay back.**

Điều này có nghĩa:

- vị trí của attacker **không** tự mở quyền đánh xuyên row,
- đứng back row không làm `Strike` thành `Range`,
- đứng front row cũng không cho đặc quyền vượt row.

Thứ quyết định quyền target là:

1. `Access Type`
2. formation hiện tại của phe đối diện
3. delivery pattern
4. exception đặc biệt nếu có

Không phải:

- attacker đang đứng hàng nào

---

## 6. Áp dụng cho cả 2 phe

Rule row là rule dùng chung cho toàn combat, không phải rule riêng của enemy side.

### 6.1 Player side

Player có thể có:

- `Single Target`
- `Row AoE`
- `Cross-row`

Sau này ally của player cũng đọc row theo cùng hệ rule đó.

### 6.2 Enemy side

Enemy cũng dùng rule y hệt:

- có thể có `Single Target`
- có thể có `Row AoE`
- có thể có move hiếm kiểu `Cross-row`

Không có chuyện:

- row chỉ là rule để player target enemy,
- hoặc row chỉ là rule của enemy formation mà không áp lại cho player team.

---

## 7. Row có thể được đọc bởi content

Ngoài target legality nền, row còn là một **combat state** để content đọc.

Row có thể được dùng bởi:

- skill
- passive
- consumable
- enemy move
- boss move

Ví dụ các kiểu đọc hợp lệ:

- tăng sức mạnh nếu đang ở `Front Row`
- tăng sức mạnh nếu đang ở `Back Row`
- mở payoff nếu target đang ở một row cụ thể
- đổi vị trí của bản thân
- đổi vị trí đồng minh
- kéo enemy đổi hàng

Nói cách khác:

- row không chỉ dùng để chặn target,
- row còn là một trục condition / state cho content.

---

## 8. Ý nghĩa thiết kế

Row trả lời:

- **đánh được ai**

Access Type trả lời:

- **đòn này có bị front row chặn hay không**

Delivery Pattern trả lời:

- **đánh 1 mục tiêu, 1 hàng, hay cả 2 hàng**

Lane / order là lớp khác và trả lời:

- **resolve theo thứ tự nào**

Vì vậy:

- không được trộn row với lane,
- không dùng lane để suy ra target access,
- không dùng row để thay thế sequencing logic.

---

## 9. Runtime / implementation guardrail

- Không hardcode row thành damage multiplier mặc định.
- Không biến player row thành free stance toggle nếu không có skill/passive/effect cho phép.
- Không để vị trí attacker tự quyết định quyền đánh xuyên row.
- Không gộp row logic vào lane logic.
- `Cross-row / All-row` phải là exception rõ ràng, không phải default.
- `Lightning / Mark` có thể là exception vượt row, nhưng không vì thế mà mọi AoE khác đều thành full-board.
- Khi sau này player có ally, phải tiếp tục dùng cùng rule row thay vì tạo luật riêng cho player team.

---

## 10. Tóm tắt cực ngắn

1. Row là lớp vị trí và quyền target, không phải damage modifier nền.
2. Rule này áp dụng đối xứng cho cả player team và enemy team.
3. `Strike` luôn bị front row chặn.
4. `Range` target tự do front hoặc back.
5. `Strike AoE` quét row ngoài cùng còn đang chặn.
6. `Range AoE` chọn tự do 1 row để quét.
7. `Cross-row` là ngoại lệ hiếm có chủ đích.
8. Vị trí của attacker không tự mở quyền đánh xuyên row.
9. Row là lớp khác với lane/order.
