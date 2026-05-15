# UX_UI_FEEDBACK_SPEC.md

> Tài liệu này mô tả **định hướng UX/UI/feedback** cho game: combat readability, tooltip/preview, state readability, intent clarity và những gì player phải hiểu được bằng mắt trong mỗi turn.


> **CURRENT SOURCE UPDATE:** Bản này đã nhập các rule hiện tại từ các draft cũ. Không dùng file `archived combat change draft` làm source nữa. Resource hiện tại là **AP**. Combat flow hiện tại là **Player Phase**: dice tự roll đầu phase → player reorder → click/drag skill vào target để cast ngay → dice chuyển used state → End Phase → Enemy Phase.

---
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

- thứ tự dice hiện tại,
- dice nào sẽ bị skill tiếp theo consume,
- dice nào đang active và dice nào đang used,
- player còn bao nhiêu AP,
- target nào hợp lệ cho skill đang hover/drag,
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

#### Consumable liên quan dice
Với consumable nhóm **Fate** hoặc bất kỳ consumable nào cần target dice/face:

- phải có consumable đang selected,
- phải có dice/face target hợp lệ đang selected hoặc context target hợp lệ,
- nút **Use** chỉ bật khi target requirement đã đủ,
- nút **Confirm** chỉ bật khi trong overlay đã chọn đủ face/target yêu cầu.

Nếu thiếu consumable selected, thiếu dice selected, hoặc target chưa đủ, **Use / Confirm** phải disabled và UI phải nói rõ lý do.

#### Dice-edit overlay
- combat dùng **single-die overlay** nếu effect chỉ thao tác trên 1 die trong combat
- shop / loadout dùng **multi-die overlay** nếu context cho phép thao tác trên nhiều dice
- logic edit face là giống nhau; khác nhau chỉ ở tập target mà context cho phép chọn

Guardrail:
- UI phải luôn cho player hiểu object nào đang được target
- UI không được gợi cảm giác “đã Confirm” khi player mới chỉ hover hoặc mới chỉ select
- UI phải luôn cho player hiểu vì sao `Use` hoặc `Confirm` đang bị khóa

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

### 7.1 Player Phase

UI phải chuyển người chơi sang trạng thái “đọc tình huống” ngay từ đầu Player Phase:

- dice tự roll ở đầu phase,
- thấy kết quả roll,
- thấy Crit / Fail nổi bật,
- thấy AP hiện tại,
- thấy dice nào active / used,
- thấy skill nào cast được.

Trong Player Phase, UI phải ưu tiên:

- reorder dice rõ ràng,
- preview dice nào sẽ bị consume theo thứ tự hiện tại,
- click/drag skill vào target để cast ngay,
- target hợp lệ rõ ràng,
- invalid reason rõ khi thiếu AP / thiếu dice / target sai.

### 7.2 Dice used state

Dice đã dùng không biến mất khỏi UI.

Khi một die bị consume:

- die đó hạ nhẹ trục Y,
- background đổi màu sang trạng thái used,
- die vẫn hiển thị số/crit/fail để player nhớ lượt đã diễn ra thế nào,
- die không còn available cho skill tiếp theo.

Khi dice được refresh bởi turn mới hoặc consumable/effect:

- die nâng Y về vị trí active,
- background trở về trạng thái active,
- die có thể được dùng lại nếu rule cho phép.

### 7.3 End Phase

End Phase dùng để cleanup lượt player:

- tắt preview,
- khóa input player,
- resolve end-of-player effects,
- chuyển sang Enemy Phase.

### 7.4 Enemy Phase

Enemy Phase phải hiển thị rõ:

- enemy đang làm gì,
- damage/status/guard xảy ra với player,
- status tick / cleanup,
- khi nào quay lại Player Phase.

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
- UI phải làm lane order, AP, Guard, Stagger, status và intent đọc được rõ,
- clarity quan trọng hơn flashy,
- complexity được chấp nhận ở design; opacity không được chấp nhận ở UX.

---
