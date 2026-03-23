# RUN_STRUCTURE_AND_ECONOMY_SPEC.md

> Tài liệu này mô tả **vòng run tổng thể**, **reward / shop / progression loop**, **resource flow ngoài combat** và ý nghĩa kinh tế của mỗi run.  
> File này không đi sâu vào core combat rules; phần đó nằm ở `COMBAT_CORE_SPEC.md`.

---

## 1. Mục tiêu của hệ thống

Run structure phải tạo cảm giác:

- mỗi trận đấu có giá trị rõ trong hành trình build,
- thưởng sau trận giúp player “điêu khắc build”,
- thua run có cái giá thật,
- nhưng vẫn có meta progression đủ để tạo discovery lâu dài,
- player luôn đứng trước bài toán giữ cái gì, bỏ cái gì, commit hướng nào.

---

## 2. Run loop tổng quát

Nhịp chơi ở cấp độ toàn run mong muốn là:

**Combat → Reward / Shop / Progression → Chỉnh build → Combat khó hơn**

Giải thích:

1. **Combat**: dùng build hiện tại để vượt qua một bài kiểm tra cơ chế.
2. **Reward / Shop / Progression**: nhận công cụ mới hoặc cách đổi hình build hiện tại.
3. **Chỉnh build**: quyết định giữ gì, bỏ gì, hướng nào đáng đào sâu.
4. **Combat khó hơn**: game phản hồi lại bằng encounter / boss / pressure cao hơn.

---



## 3. Logic Flow

Phần này mô tả **đường đi logic của một run**: player bắt đầu ở đâu, thắng / thua dẫn tới đâu, reward / shop / unlock được chèn vào nhịp run như thế nào, và build được điêu khắc dần ra sao.

### 3.1 Flow tổng quát của một run

`Start Run`  
→ Hệ thống tạo trạng thái run mới  
→ Giao bộ công cụ khởi đầu theo current progression rule  
→ Player bắt đầu với build còn mơ hồ và ít tài nguyên

`Combat`  
→ Player vào trận với loadout hiện tại  
→ Thắng thì chuyển sang reward / progression; thua thì kết thúc run theo current fail-state rule

`Reward / Shop / Progression`  
→ Player nhận lựa chọn mới: skill / passive / relic / dice / utility / unlock progress liên quan  
→ Đánh giá món nào giúp build hiện tại rõ hơn  
→ Giữ, thay, bỏ qua hoặc commit sâu hơn vào một engine

`Next Combat`  
→ Build đã chỉnh xong quay lại combat khó hơn  
→ Loop lặp lại: Combat → Reward / Shop / Progression → Chỉnh build → Combat khó hơn

`End Run`  
→ Nếu thua: mất tiến trình của run hiện tại nhưng giữ unlock progress  
→ Nếu thắng boss cuối: có thể kết thúc run hoặc đi tiếp Endless

### 3.2 Flow reward decision

`Reward xuất hiện`  
→ Hệ thống đưa một hoặc nhiều lựa chọn  
→ Player hỏi: món này có đẩy đúng hướng build mình đang đi không?  
→ Nếu có: take và commit thêm  
→ Nếu không: skip hoặc chọn utility ngắn hạn / bridge item

### 3.3 Flow unlock progression

`Combat / run milestone / discovery đạt điều kiện`  
→ Hệ thống tăng unlock progress  
→ Content mới dần mở ra cho các run sau  
→ Player thất bại vẫn giữ lại phần unlock này  
→ Độ dày pool content tăng dần theo thời gian thay vì dồn từ đầu


## 4. Các loại tài nguyên trong combat và ngoài combat

### 4.1 Tài nguyên trong combat

Combat hiện xoay quanh:

- dice outcome,
- Focus,
- Guard,
- status trên target,
- lane order,
- relic / consumable,
- số lượng dice đang equip.

Điểm rất quan trọng: nhiều thứ trong số này phải được đối xử như **tài nguyên chiến thuật**, không chỉ là chỉ số phụ.

Ví dụ:

- Burn là tài nguyên để consume,
- Bleed có thể trở thành Guard hoặc consumable,
- Mark là weak point để direct-hit khai thác,
- Chilled là cửa sổ payoff,
- exact value là tài nguyên build-level.

### 4.2 Kinh tế ngoài combat

Run progression phải cho người chơi cảm giác tăng trưởng theo ba trục:

- **mở rộng công cụ**: skill, passive, relic,
- **biến đổi công cụ**: customize dice,
- **làm rõ build**: bỏ thứ không hợp, giữ thứ phục vụ engine đang hình thành.

---

## 5. Reward structure — vai trò thiết kế

Reward không chỉ là “được thêm đồ”.  
Reward phải luôn ép player đối diện với ít nhất một câu hỏi:

- thứ này có đẩy đúng hướng build mình đang đi không?
- có đáng bỏ một món hiện tại để lấy không?
- có nên commit vào một engine sâu hơn không?
- có nên lấy utility ngắn hạn thay vì payoff dài hạn không?

Điều này đặc biệt quan trọng với game thiên về build identity và intentional anti-synergy.

---

## 6. Shop trong run

Current direction từ source hiện tại:

- skill slot có thể swap ngoài combat,
- passive slot có thể swap ngoài combat,
- skill có thể mua ở shop,
- hướng tổng thể của run là “điêu khắc build”, không phải chỉ tích item.

### 6.1 Vai trò của shop

Shop phải phục vụ các nhu cầu khác nhau:

- vá build còn thiếu trụ cột,
- cho cơ hội commit sâu hơn vào một engine,
- cho utility đổi hướng run,
- cho người chơi cân giữa power hiện tại và power dài hạn.

### 6.2 Guardrail

Shop không nên deterministic quá mức.  
Cảm giác “khó tìm đúng món mình cần” là một phần hợp lệ của roguelike run, miễn là player vẫn có đủ agency để xoay sở.

---

## 7. Run progression và build shaping

Mỗi run nên có cảm giác tiến qua các giai đoạn:

1. **Khởi đầu đơn giản**: ít dice, ít slot meaningful, build còn mơ hồ.
2. **Nhận tín hiệu hướng đi**: một vài skill / passive / relic hé lộ engine phù hợp.
3. **Commit có chọn lọc**: player bỏ bớt thứ không hợp để làm đậm đúng hướng.
4. **Engine rõ ràng**: đến late run, build có cá tính chiến thuật rõ rệt.
5. **Kiểm tra cuối**: boss / endless ép build chứng minh giá trị thật.

---

## 8. Thua run, thắng run và thứ được giữ lại

### 8.1 Khi thua run

Theo current vision:

- player mất toàn bộ tiến trình của run hiện tại,
- gồm dice, skill, passive, relic, consumable và các tăng trưởng chỉ dành cho run đó,
- thứ được giữ lại là **unlock progress**.

Ý nghĩa thiết kế:

- mỗi run phải có trọng lượng,
- thất bại phải có giá,
- nhưng player vẫn có cảm giác discovery dài hạn.

### 8.2 Khi thắng boss cuối

Player có thể:

- kết thúc run như một chiến thắng hoàn chỉnh,
- hoặc tiếp tục sang **Endless Mode**.

Điều này giúp phục vụ hai kiểu người chơi:

- người muốn chốt run gọn,
- người muốn kéo build tới mức tối đa.

---

## 9. Unlock system như một phần của economy

Unlock không chỉ là meta progression bên ngoài gameplay.  
Unlock là một phần của economy theo nghĩa rộng vì nó quyết định:

- tốc độ player được tiếp cận mechanic mới,
- độ dày pool content,
- độ khó đọc của game ở giai đoạn đầu,
- hướng build có thể hình thành trong tương lai.

Unlock phải làm 4 việc:

1. giãn tốc độ lộ mechanic,
2. tránh overwhelm,
3. buộc player hiểu ngôn ngữ gameplay trước khi trao payoff phức tạp,
4. gắn progression vào gameplay thật thay vì grind vô nghĩa.

---

## 10. Endless mode ở góc nhìn run economy

Endless không chỉ là “đánh tiếp cho vui”.  
Ở góc nhìn run structure, Endless phải phục vụ:

- người thắng sớm nhưng build chưa hoàn chỉnh,
- người muốn ép build tới mức tối đa,
- người muốn biểu diễn sức mạnh hệ thống,
- người muốn gặp hidden boss như bài kiểm tra tối hậu.

Endless không được làm lu mờ run chính; nó là sân chơi hậu kỳ cho build pursuit.

---

## 11. Những gì chưa final trong file này

Các vùng sau chưa có đủ data khóa cứng:

- full node / map structure của run,
- reward rate cụ thể,
- shop inventory math,
- reroll cost,
- tiền tệ chi tiết,
- encounter pacing theo từng floor/map.

Nhưng các phần sau là current direction mạnh:

- run loop = `Combat → Reward / Shop / Progression → Chỉnh build → Combat khó hơn`,
- build shaping là mục tiêu chính của progression,
- thua run mất tiến trình run nhưng giữ **unlock progress**,
- chiến thắng boss cuối có thể dẫn sang **Endless Mode**,
- reward / shop / unlock / relic pool phải cùng phục vụ cảm giác “điêu khắc build”.
