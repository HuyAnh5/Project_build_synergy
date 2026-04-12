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

Hướng hiện tại từ source hiện tại:

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

## 6A. Map Structure Trong Act

### 6A.1 Khung map hiện tại

Mỗi act hiện đang đi theo hướng:

- node graph đi từ `đáy lên đỉnh` như STS
- player bắt đầu ở `đáy map`
- `Boss` luôn nằm sẵn ở `đỉnh map`
- player có thể đi lên nhanh để rush boss
- hoặc quay lại đường cũ để tối ưu tài nguyên trong act đó

Điểm khác với STS clone thuần:

- không khóa di chuyển một chiều hoàn toàn
- cho phép backtrack trong phần đường đã mở
- act có cảm giác như một vùng nhỏ để crawl, nhưng vẫn đọc nhanh và không biến thành game khám phá

Ý nghĩa thiết kế của hướng này:

- giữ được cảm giác “đang đi xuyên qua một vùng / khu vực” thay vì chỉ bấm chọn route trên một sơ đồ trừu tượng,
- nhưng vẫn không làm game trượt sang exploration-heavy,
- và quan trọng nhất: map phải tiếp tục phục vụ `combat -> reward -> build shaping -> combat khó hơn`, chứ không được tranh vai với combat.

Map không nên bị hiểu là:

- một cây route khóa cứng hoàn toàn như STS,
- cũng không phải chuỗi menu / shop / reward liên tiếp như Balatro,
- và càng không phải bản đồ để người chơi điều tra bằng tay, soi từng điểm nhỏ, hay nhớ lore mơ hồ.

Map đúng của game này là:

- đọc nhanh,
- cho player agency về đường đi,
- cho phép chọn giữa `clear thêm để mạnh hơn` và `đi nhanh để chấp nhận rủi ro`,
- nhưng luôn buộc player đánh đổi tài nguyên, thời gian, và độ an toàn của build.

### 6A.2 Các loại node đang dùng

| Node | Icon | Combat | Vai trò |
|---|---|---:|---|
| Combat | 💀 | ✅ | Encounter thường, nguồn reward cơ bản và có thể cho Boss Intel |
| Elite | ☠️ | ✅ | Encounter khó hơn, reward cao hơn, có thể cho Boss Intel |
| Event | 📜 | ❌ | Event ngắn kiểu STS, có thể cho reward / choice / secret / Boss Intel |
| Shop | 🛒 | ❌ | Mua `skill / relic / consumable / dice`, và có thể mua `Boss Intel` đúng `1 lần` |
| Rest | 🛏️ | ❌ | Hồi phục |
| Hub / Forge | 🔨 | ❌ | Điểm xuất phát ở đáy act, nơi forge dice |
| Boss | 👹 | ✅ | Final boss của act, luôn nằm ở đỉnh map |

Rule hiển thị:

- về sau các node nên được biểu diễn bằng icon rõ loại ngay trên map,
- không nên dùng node tròn giống nhau rồi bắt player đoán bằng text phụ,
- mục tiêu là nhìn vào map phải hiểu ngay đâu là:
  - combat thường,
  - elite,
  - event,
  - shop,
  - rest,
  - hub/forge,
  - boss.

Điều này bám sát triết lý `Readable Complexity`:

- độ sâu nằm ở quyết định đường đi và quyết định build,
- không nằm ở việc giải mã UI.

### 6A.3 Hub / Forge và Shop là 2 thứ khác nhau

`Hub / Forge`:

- nằm ở đáy map
- là nơi player xuất phát khi vào act
- có thể quay lại nếu đường đã mở
- là lò rèn dice
- hiện dùng để forge `whole-die tag / màu` cho dice
- mỗi lần forge tốn `gem` theo đúng loại màu / tag muốn gắn, ví dụ `Patina`

`Hub / Forge` không phải là shop thu nhỏ.
Nó là:

- base camp của act,
- điểm quay về để xử lý phần dice-level progression,
- và là nơi thể hiện rõ rằng `whole-die color / tag` là một trục phát triển riêng với skill/relic.

`Shop`:

- là node riêng trên map, không nằm chung với `Hub / Forge`
- thường xuất hiện khá sớm trong act, kiểu sau `1-2 node`
- bán:
  - `skill`
  - `relic / consumable`
  - `dice`
- ngoài ra có thể bán `Boss Intel`, nhưng mỗi shop chỉ mua intel được `1 lần`

`Shop` phục vụ nhu cầu commerce / pivot build:

- vá chỗ hở của build,
- mua công cụ mới,
- đẩy nhanh một hướng build nếu shop ra đúng món,
- hoặc mua `Boss Intel` khi player muốn tiết kiệm thời gian hunt clue.

Việc tách `Hub / Forge` và `Shop` là quan trọng vì:

- forge là progression của dice,
- shop là nơi đổi tài nguyên lấy công cụ,
- hai việc này khác nhau về cảm giác quyết định và không nên bị gộp thành cùng một node.

### 6A.4 Rule di chuyển và backtrack

- player chỉ backtrack trong `act hiện tại`
- chỉ di chuyển tự do trên phần đường đã mở
- node đã clear / đã đi qua sẽ chuyển thành `hình tròn rỗng`
- đi qua node rỗng chỉ là di chuyển, không có combat hay event mới
- map phải cho phép:
  - đánh đường vòng để né `elite`
  - quay lại tối ưu tài nguyên
  - hoặc đi nhanh lên boss nếu player muốn

Rule này tạo ra 2 kiểu tiếp cận hợp lệ:

1. `clear-heavy`
   - đánh nhiều node hơn,
   - nhận thêm reward / build pieces / clue,
   - nhưng tốn thời gian và có thể mất thêm HP / resource.

2. `rush-heavy`
   - đi đường ngắn hơn tới boss,
   - chấp nhận build mỏng hơn hoặc ít thông tin hơn,
   - đổi lại vào boss sớm hơn.

Mục tiêu là:

- `clear all` không được luôn luôn là đáp án tối ưu,
- `rush boss` cũng không được mặc định là quyết định ngu,
- mà phải tùy build, tùy tình trạng run, và tùy player đọc được act đó ra sao.

### 6A.5 Boss Intel

`Boss Intel` là hệ thông tin để reveal boss identity, không phải để mở khóa quyền đánh boss.

Rule hiện tại:

- `Boss` luôn hiện sẵn ở đỉnh map
- player không cần `3/3 intel` để vào đánh boss
- kể cả `0/3 intel`, nếu đi tới boss thì vẫn có thể vào đánh
- `3/3 intel` chỉ để biết boss đó là ai
- tiến độ intel phải được ghi nhận tự động và hiển thị rõ cho player
- UI chỉ hiện player đang có bao nhiêu intel, không “chỉ boss ở node nào” vì boss vốn đã luôn ở đỉnh map

Đây là điểm rất quan trọng:

- boss luôn nằm sẵn trên map,
- player luôn biết “đỉnh act là nơi boss ở”,
- cái bị ẩn không phải là vị trí boss,
- mà là **identity của boss đó**.

Nói cách khác:

- player biết nơi mình sẽ tới,
- nhưng chưa chắc biết mình sắp phải đánh con gì,
- và `Boss Intel` là cách biến chuyện đó thành một lớp chuẩn bị chiến thuật, chứ không phải puzzle điều tra.

Nguồn intel hiện tại:

- `Combat`
- `Elite`
- `Event`
- `Shop` có thể bán `Boss Intel` đúng `1 lần`

Không cho intel từ:

- `Hub / Forge`
- `Rest`

Ý nghĩa thiết kế:

- intel đến từ những thứ player vốn đã làm trong run loop bình thường,
- chứ không ép player chuyển sang mode “đi tìm clue bằng tay”.

Tức là:

- đánh combat,
- thắng elite,
- vào event,
- hoặc trả tiền ở shop,

đều là các hành động tự nhiên trong run.
Game tự ghi nhận và tự cập nhật tiến độ `Boss Intel`.

### 6A.6 Meta progression của Boss Intel

Rule hiện tại:

- lần 1, 2, 3 gặp boss đó vẫn theo rule đầy đủ
- từ lần 4 chạm trán boss đó trở đi, player chỉ cần `1/3 intel` là reveal boss identity

Mục tiêu của rule này:

- lần đầu vẫn giữ cảm giác hunt / discovery
- về sau giảm thời gian lặp lại khi player đã quen boss đó

Chi tiết rule hiện tại nên hiểu đúng là:

- lần 1, 2, 3 chạm trán boss loại đó:
  - vẫn theo rule đầy đủ, tức player cần tự kiếm intel như bình thường nếu muốn reveal identity,
- từ lần 4 trở đi:
  - chỉ cần `1/3 intel` là game tự reveal boss identity.

Đây là meta progression theo hướng:

- reward cho trí nhớ và kinh nghiệm lâu dài của player,
- giảm friction lặp lại trên các boss đã quá quen,
- nhưng không phá hoàn toàn cảm giác chuẩn bị ở những lần đầu tiên.

### 6A.7 Escape / retreat rule trên map

Player hiện có lựa chọn chạy khỏi combat, nhưng:

- phải roll dice đạt điều kiện chạy
- điều kiện chạy cụ thể chưa chốt

Escape ở đây là một phần của tactical economy, không phải nút “thoát miễn phí”.

Player có quyền chạy, nhưng:

- phải trả giá bằng việc chưa thắng node,
- và phải đạt điều kiện dice mới chạy được.

Nếu `player chạy`:

- không tính là thắng node
- quay lại node đó thì vẫn phải đánh lại
- enemy reset full HP, không giữ máu cũ

Nếu `enemy tự chạy`:

- vẫn tính là player thắng
- player vẫn nhận reward
- nếu node đó còn quay lại được thì encounter sau sẽ mạnh hơn
- reward của encounter sau cũng cao hơn

Trường hợp này phải được hiểu đúng:

- không phải narrative kiểu “quái rút về căn cứ”,
- mà là một trạng thái combat đặc biệt: enemy tự chạy khỏi player.

Vì vậy hệ thống xem đây là:

- một chiến thắng hợp lệ cho player ở lần đó,
- nhưng vẫn để lại khả năng encounter quay lại ở mức khó hơn nếu map còn cho phép gặp lại.

Nếu gặp `Boss`:

- player vẫn có thể chạy
- nhưng chưa thắng `final boss` của act thì chưa được sang act mới

### 6A.8 Những gì chưa chốt ở map layer

Các điểm sau vẫn đang mở và không nên coi là final:

- điều kiện dice để chạy khỏi combat,
- trade-off cụ thể giữa `clear-heavy` và `rush-heavy`,
- tỷ lệ / tần suất spawn chính xác của từng loại node,
- công thức reward scaling khi enemy tự chạy và encounter mạnh lên,
- số lượng shop / rest / event tối ưu cho mỗi act,
- act layout variation giữa các biome / vùng khác nhau.

Nhưng những thứ đã đủ mạnh để coi là current direction gồm:

- act là `node graph` từ đáy lên đỉnh,
- có backtrack trong phần đường đã mở,
- `Hub / Forge` ở đáy act,
- `Shop` là node riêng,
- boss luôn ở đỉnh map,
- `Boss Intel` chỉ reveal identity, không mở khóa quyền đánh,
- intel được ghi nhận tự động theo gameplay thường.

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
