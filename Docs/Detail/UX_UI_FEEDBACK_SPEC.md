# UX_UI_FEEDBACK_SPEC.md

> Tài liệu này mô tả **định hướng UX/UI/feedback** cho game: combat readability, tooltip/preview, state readability, intent clarity và những gì player phải hiểu được bằng mắt trong mỗi turn.

---

## 1. Mục tiêu của hệ thống

UX của game phải phục vụ pillar:

**Readable Complexity**

Game được phép sâu ở backend, nhưng frontend phải giúp player:

- nhìn vào board là hiểu điều quan trọng,
- đọc được sequencing hiện tại,
- biết hành động nào đáng dùng,
- không phải tự tính nhẩm quá nhiều lớp modifier,
- hiểu thất bại của mình đến từ quyết định nào.

---

## 2. Những thứ player phải luôn đọc được bằng mắt

Ở cấp lượt combat, UI phải giúp player đọc được tối thiểu:

- lane order hiện tại,
- die nào đang gắn với skill nào,
- action nào sẽ resolve trước,
- player còn bao nhiêu Focus,
- action đã lock hay chưa,
- mục tiêu nào đang có Guard,
- mục tiêu nào đang Stagger,
- mục tiêu nào đang Burn / Mark / Freeze / Chilled / Bleed,
- enemy intent ở turn tới hoặc turn hiện tại,
- status nào là direct payoff opportunity.

Nếu một trong các thông tin trên quá mờ, chiều sâu combat sẽ biến thành opacity thay vì strategy.

---

## 3. Tooltip / preview direction

### 3.1 Ngoài combat / trong shop / khi chưa roll

Tooltip nên hiện ở dạng tĩnh, ví dụ:

- `Deal X damage`
- `Gain X Guard`

Mục tiêu là để player hiểu purpose của skill mà không cần ngữ cảnh roll hiện tại.

### 3.1A Dice presentation face outside combat

Rule nay chi la presentation, khong phai roll state that.

- Shop:
  - Moi vien dice duoc ban trong shop random 1 mat de show cho player khi shop do duoc tao ra.
  - Mat nay phai duoc giu nguyen trong suot vong doi cua cung mot shop do.
  - Thoat shop roi vao lai cung shop van giu nguyen mat dang show.
  - Sang shop khac, hoac inventory shop duoc reroll / spawn dice moi, tung vien dice moi duoc random lai 1 mat presentation moi.
- Bag / inventory:
  - Moi lan player mo tui do, tat ca dice dang co trong tui se random lai 1 mat de hien thi.
  - Day chi la random presentation cho lan mo tui do do, khong phai reroll that va khong duoc sua data mat.
- Guardrail:
  - Reorder, refresh layout, va UI rebuild khong duoc tu random lai presentation face neu context presentation chua doi.
  - Presentation face phai bi rang buoc theo context entry: tao shop moi, mo bag moi.

### 3.2 Trong combat / khi đã roll

Tooltip và preview nên hiện **số thật đã resolve** hoặc gần-resolve nhất có thể.

Nguyên tắc cực quan trọng:

- tooltip,
- preview,
- execute result

về lâu dài phải dùng **cùng một nguồn số**.

Không được để:

- tooltip nói một kiểu,
- preview nói một kiểu,
- execute cho ra con số khác.

### 3.3 Ý nghĩa UX

Người chơi nên biết action này có đáng dùng hay không **bằng cách nhìn**, chứ không phải tự cộng trừ nhân chia trong đầu quá lâu.

---

## 4. Dice UI và slot readability

### 4.1 Dice phải đọc được identity thật

Dice UI không chỉ hiển thị số. Nó phải giúp player hiểu:

- đâu là Base Value hiện tại,
- đâu là Crit / Fail,
- die này gắn với lane nào,
- die này thuộc local group nào nếu skill dùng nhiều slot,
- việc reorder đang tác động ra sao.

### 4.2 Slot và lane phải đọc được sequencing

Player phải thấy rõ:

- lane `A/B/C` hiện tại,
- thứ tự resolve từ trái sang phải,
- relation giữa pair gốc `1/2/3` và lane mới sau reorder,
- transition từ Planning sang Execute đã khóa hay chưa.

### 4.3 Guardrail cực quan trọng

Không được để UI trông như đã reorder nhưng logic vẫn resolve theo vị trí cũ.  
Nếu chuyện đó xảy ra, toàn bộ UX trust sẽ gãy.

### 4.4 Dice selection / consumable interaction grammar

Ở các màn có dice và consumable, interaction hiện tại đi theo grammar sau:

#### Hover
- hover vào die → die nhô nhẹ lên theo trục Y
- mục tiêu là để player hiểu mình đang trỏ vào die nào
- hover **không** tự mở menu action

#### Selected
- click vào die hoặc consumable → object đó vào trạng thái selected
- object selected phải nổi bật hơn hover
- click lại đúng object đó → bỏ selected, trở về trạng thái bình thường

#### Use / Confirm gating
- action **Use** chỉ bật khi:
  - đã có consumable phù hợp đang selected
  - current context đang expose đủ target hợp lệ cho effect đó
- action **Confirm** chỉ bật khi target requirement của effect đã hoàn tất
- nếu target chưa đủ hoặc target không hợp lệ, **Use / Confirm** phải hiển thị disabled / màu xám

#### Dice-edit overlay
- combat dùng **single-die overlay**
- shop / loadout dùng **multi-die overlay**
- logic edit face là giống nhau; khác nhau chỉ ở tập target mà context cho phép chọn
- trong combat, vì overlay chỉ hiển thị 1 dice, mọi thao tác chỉ xảy ra trong phạm vi dice đó
- trong shop / loadout, vì overlay hiển thị cả 3 dice, effect có thể target mặt ở nhiều dice khác nhau

Guardrail:
- UI phải luôn cho player hiểu object nào đang được target
- UI không được gợi cảm giác “đã Confirm” khi player mới chỉ hover hoặc mới chỉ select
- UI phải luôn cho player hiểu vì sao `Use` hoặc `Confirm` đang bị khóa

---

## 5. Intent readability của enemy
## 5. Intent readability của enemy

Intent system là một phần UX quan trọng, không chỉ là mechanic.

Player phải đọc được:

- enemy sắp đánh,
- sắp thủ,
- sắp áp status,
- sắp setup hoặc chuẩn bị move lớn,
- có cơ hội bị punish hay không.

Mục tiêu là để combat tạo cảm giác đọc trận, không phải đoán mò.

---

## 6. State readability cho 5 effect chính

### 6.1 Burn

Player phải nhìn ra:

- mục tiêu nào đang có Burn,
- Burn đang là tài nguyên để consume,
- có đáng dồn thêm stack trước khi detonate hay không,
- Burn là tài nguyên có nhịp ngắn hạn; nếu để quá lâu thì phần Burn cũ sẽ rụng dần.

Rule hiển thị Burn đã chốt:

- UI chỉ hiển thị **tổng Burn stack hiện tại** trên mục tiêu.
- UI **không cần** hiển thị turn count / tuổi thọ của từng batch Burn cho player.
- Backend vẫn phải track Burn theo từng batch apply riêng để expire đúng rule.
- Mục tiêu UX là để player cảm nhận Fire là build phải **add Burn sớm, nổ sớm**, không phải tích vô hạn rồi để đó mãi.

### 6.2 Mark

Player phải nhìn ra:

- mục tiêu nào đang có Mark,
- direct-hit vào đâu sẽ tạo payoff tốt nhất,
- Mark không stack,
- shock phụ của Lightning không làm mất Mark.

### 6.3 Freeze / Chilled

Player phải nhìn ra:

- mục tiêu nào đang Freeze,
- mục tiêu nào đã sang Chilled,
- target đó còn là CC hay đã là cửa sổ payoff,
- target đang miễn Freeze mới hay không.

### 6.4 Bleed

Player phải nhìn ra:

- mục tiêu nào đang có Bleed,
- Bleed là áp lực đầu lượt,
- Bleed có thể là tài nguyên cho build chứ không chỉ là DoT.

### 6.5 Stagger

Player phải nhìn ra:

- Guard vừa bị phá,
- mục tiêu đang ở cửa sổ Stagger,
- chỉ hit direct kế tiếp mới ăn payoff,
- shock phụ / tick không ăn hộ Stagger.

---

## 7. Trạng thái và feedback theo phase

### 7.1 Roll phase

UI phải chuyển người chơi sang trạng thái “đọc tình huống”:

- thấy kết quả roll,
- thấy Crit / Fail nổi bật,
- thấy resource hiện tại đủ để làm gì.

### 7.2 Planning phase

UI phải ưu tiên:

- drag/drop rõ ràng,
- reorder rõ ràng,
- highlight lane rõ ràng,
- target preview nếu có,
- phân biệt action hợp lệ / không hợp lệ.

### 7.3 Lock / Execute

Khi lock plan:

- player phải nhận ra **reorder** và **skill đã đưa vào turn hiện tại** không còn đổi được,
- execute phải theo thứ tự nhìn thấy được,
- UI phải phân biệt rõ giữa:
  - **line hành động đã khóa**
  - và **dice state vẫn còn có thể mutate** nếu consumable hợp lệ cho phép edit dice trong Execute.

Nếu combat hỗ trợ consumable can thiệp dice, UI phải phân biệt rõ:
- đang ở trạng thái **planning skill**
- hay đang ở trạng thái **dice-edit / consumable targeting**

Player không được nhầm giữa:
- reorder lane,
- đổi skill,
- và mutate dice state.

### 7.4 Enemy / End phase
### 7.4 Enemy / End phase

Player phải hiểu:

- intent nào vừa được thực hiện,
- damage đến từ đâu,
- status nào vừa tick,
- Guard nào mất vì end phase,
- sang lượt mới sẽ ở tình trạng gì.

---

## 8. Nguyên tắc cho VFX / SFX / animation

File này không khóa asset cụ thể, nhưng khóa nguyên tắc:

- VFX không được che mất thông tin chiến thuật,
- feedback phải phục vụ hiểu mechanic trước khi phục vụ “đẹp”,
- action resolve nên rõ về thứ tự,
- proc phụ như shock tuần tự cần dễ đọc, không loạn,
- trạng thái control như Freeze / Chilled cần khác nhau đủ để player không nhầm.

---

## 9. Những vùng nên được giữ đơn giản

Các vùng sau càng đơn giản càng tốt:

- số lượng icon cùng lúc trên một target,
- số lớp text runtime chồng nhau,
- số phép nhân player phải tự nhớ,
- animation không tạo thêm thông tin giả.

Game này không cần “hoành tráng” bằng cách làm người chơi mù board.

---

## 10. Những gì chưa final trong file này

Các vùng sau chưa có final implementation detail:

- layout UI cuối cùng cho PC và mobile,
- icon set cuối cùng,
- VFX/SFX library,
- runtime formatter chi tiết,
- target highlight exact behavior,
- full animation timing table,
- style cuối cùng của button `Use / Sell / Confirm / Cancel`,
- presentation cuối cùng giữa single-die combat overlay và multi-die shop/loadout overlay.

Nhưng các phần sau là direction mạnh / đã chốt:

- ngoài combat dùng text tĩnh kiểu `Deal X`, `Gain X`,
- trong combat ưu tiên số đã resolve,
- UI phải làm lane order, Focus, Guard, Stagger, status và intent đọc được rõ,
- clarity quan trọng hơn flashy,
- complexity được chấp nhận ở design; opacity không được chấp nhận ở UX.

---

## Runtime Note (2026-04)

- UX/runtime combat hien tai dang dung grammar:
  - `roll dice`
  - `reorder neu can`
  - `drag skill icon tu skill slot vao enemy de cast truc tiep`
  - `bam End Turn`
- Co `SelfCastDropZone` rieng cho skill target = self.
- Roll dice van giu nguyen.
- Dice da dung tam thoi van dim 50%, chua bien mat that.
- Neu can huong design tiep theo, xem [COMBAT_CHANGES_2026.md](/C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Docs/Detail/COMBAT_CHANGES_2026.md).
