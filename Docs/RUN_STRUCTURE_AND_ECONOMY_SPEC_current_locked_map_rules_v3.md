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


---

## 11A. Addendum — Current Locked Rules for the Act Map Prototype

Phần này **chỉ bổ sung** thêm các rule đã chốt trong quá trình iterate prototype map.  
Không thay thế các phần cũ phía trên; mục tiêu là khóa lại direction hiện tại cho bản **map-only beta**.

### 11A.1 Mục tiêu của bản prototype map hiện tại

Bản này chỉ dùng để test:

- cảm giác đọc map nhanh,
- cảm giác route choice,
- cảm giác né `Elite` bằng đường khác,
- cảm giác backtrack trong act,
- cảm giác reveal `Boss Intel`,
- và cảm giác reset act để sinh layout mới.

Prototype này **không** nhằm validate combat loop đầy đủ, inventory đầy đủ, hay economy chi tiết trong node.

Tức là đây là một **map-only beta** phục vụ việc test:

- readability,
- route pressure,
- pacing của decision point,
- backtracking rule,
- boss hint flow,
- và vai trò của `Shop / Forge` trên route.

### 11A.2 Giao diện prototype hiện tại

Bản prototype map hiện tại đi theo hướng:

- web prototype bằng `HTML + CSS + JavaScript thuần`,
- có thể gói thành `1 file HTML duy nhất` để test nhanh,
- mood hình ảnh là `medieval / dark fantasy nhẹ`,
- icon phải đọc nhanh, không được bắt player đoán bằng text phụ.

UI hiện tại không dùng cặp nút cố định `Run / Fight` trên top bar nữa.

Thay vào đó:

- khi player đi vào một node thì hiện `popup / panel theo loại node`,
- nếu node đó là `Combat / Elite` thì popup mới hiện lựa chọn `Run` và `Fight`,
- nếu node đó là `Event / Rest / Shop / Forge` thì popup hiện hành động phù hợp của node đó,
- sau khi xử lý xong thì player tiếp tục chọn đường đi trên map.

Ngoài ra:

- player có `icon riêng` trên map,
- hover vào node **không được làm node xê dịch khỏi vị trí neo**,
- hit area của node phải ổn định, không làm khó click.

### 11A.3 Rule tổng quát của graph trong act

Direction hiện tại của act map là:

- `Start` ở đáy,
- `Boss` ở đỉnh,
- toàn act có khoảng `20–30 node`,
- graph đọc theo chiều từ dưới lên trên,
- có `split`,
- có `merge`,
- có `đường vòng`,
- có `dead end hợp lý`,
- nhưng không được trở thành một sơ đồ quá rối hoặc quá cơ học.

Map đúng hướng phải cho cảm giác:

- giống tinh thần `STS`,
- nhưng có backtrack trong phần đường đã mở,
- vẫn đọc nhanh,
- không biến thành mê cung exploration.

Rule hình học hiện tại:

- không nối node ở khoảng cách quá xa một cách khó đọc,
- không để line cắt nhau,
- không để edge đè lên icon node khác,
- không để node chồng / đè lên nhau,
- node chính trên route không được có degree quá lớn gây rối mắt.

Rule khóa hiện tại:

- **mỗi node tối đa chỉ được nối với 4 node khác**.

### 11A.4 Nhịp route choice hiện tại

Map không được tạo cảm giác một hành lang dài một chiều.

Direction hiện tại là:

- chuỗi `single-path` dài cần bị hạn chế,
- player phải gặp lại decision point đủ thường xuyên,
- map phải có nhịp split / merge đều hơn.

Rule đã chốt:

- đoạn `3 node liên tiếp` chỉ có một đường đi là **chấp nhận được và có thể xảy ra khá thường xuyên**,
- đoạn `4 node liên tiếp` chỉ có một đường đi là **vẫn được phép và xuất hiện ở mức vừa phải / tương đối thường xuyên**,
- đoạn `5 node liên tiếp` chỉ có một đường đi phải là **hiếm**,
- các đoạn dài hơn nữa không nên là pattern phổ biến của act.

Mục tiêu là:

- map không quá rối vì ép rẽ nhánh dày đặc,
- nhưng cũng không bị biến thành một chuỗi leo thẳng quá một chiều.

### 11A.5 Rule di chuyển và pathing hiện tại

Player không cần click từng node cũ một để quay lại.

Direction hiện tại là:

- player có thể click vào một node hợp lệ đã mở,
- hệ thống sẽ tự tìm `đường ngắn nhất` để đưa icon player đi tới đó,
- việc auto path này chỉ được dùng qua các node đã trở thành `safe transit node`.

Cần hiểu đúng `safe transit node` là:

- node đã được xử lý xong và không còn encounter nữa,
- quay lại chỉ là di chuyển qua, không phát sinh event/combat mới.

Ngược lại:

- các node `Combat / Elite` đã từng `Run` mà chưa `Fight` xong thì vẫn còn enemy,
- các node đó không được coi là safe transit để backtrack tự do,
- về logic pathing, chúng vẫn là chỗ còn nguy hiểm / còn encounter.

### 11A.6 Rule xử lý node theo loại

#### Combat / Elite

Khi đi vào `Combat / Elite`:

- popup hiện `Run` và `Fight`.

Nếu `Fight`:

- node được xem là đã clear,
- icon của node biến mất,
- node chuyển thành `hình tròn rỗng`,
- node trở thành `safe transit node`,
- quay lại không trigger combat nữa.

Nếu `Run`:

- player vẫn di chuyển qua node đó,
- icon combat / elite **không biến mất**,
- enemy vẫn được hiểu là còn ở đó,
- node đó chưa được coi là clear,
- về sau node đó không được xem là điểm backtrack an toàn.

#### Event / Rest

Direction prototype hiện tại:

- `Event` và `Rest` chỉ trigger `1 lần`,
- sau khi đi qua / dùng xong thì node đó không còn nội dung mới,
- về mặt hiển thị hiện tại chúng đi theo hướng:
  - sau khi xử lý xong thì không giữ vai trò landmark như `Shop / Forge`,
  - và được xem là phần đường đã dùng xong để player đi qua an toàn.

#### Shop / Forge

`Shop` và `Forge` là ngoại lệ quan trọng:

- **không biến mất** sau khi player đã ghé,
- vẫn giữ icon trên map,
- tiếp tục tồn tại như landmark / địa điểm,
- nhưng về logic pathing thì vẫn là node an toàn để player đi lại.

Lý do là:

- về cảm giác world map, `Shop / Forge` là một nơi chốn,
- không phải encounter “xử lý xong rồi biến mất” như `Combat` hay `Event`.

### 11A.7 Rule placement của Shop và Forge trong act

Direction đã chốt:

- **mỗi act luôn phải có đúng `1 Shop` và `1 Forge`**.

Hai node này đều là:

- `leaf node`,
- chỉ có `1 cạnh đi vào`,
- không có node nào nối tiếp từ chúng,
- tới đó là `dead end`,
- player muốn đi tiếp phải quay lại node cha.

#### Shop

`Shop` phải:

- nằm ở đầu act,
- không được xuất hiện ngay sát `Start`,
- thường chỉ xuất hiện sau khi player đi qua `1 node`,
- nhiều thì là sau `2 node`,
- không nên trễ hơn nhiều so với đó.

Ngoài ra:

- nhánh vào `Shop` nên xuất phát từ route sớm của act,
- hiện direction ưu tiên nó mọc từ `Combat` sớm,
- `Shop` không phải node nằm thẳng trên main progression line.

#### Forge

`Forge` phải:

- nằm ở nửa sau của act,
- thường ở khu vực gần cuối act,
- nhưng vẫn là `leaf node`,
- tới `Forge` là ngõ cụt,
- player phải quay lại node cha để đi tiếp lên boss.

#### Hình học của nhánh Shop / Forge

Direction hiện tại là:

- nhánh vào `Shop / Forge` phải đi theo cảm giác `chéo lên`,
- không đi ngang phẳng,
- không được đè lên node khác,
- không được cắt line khác,
- `Shop / Forge` không bắt buộc phải nằm ở mép ngoài map,
- chúng có thể nằm ở khu vực giữa map nếu vị trí đó an toàn,
- nhưng vẫn luôn phải là `leaf node` và không có outgoing connection.

Ngoài ra, `Shop / Forge` phải giữ một khoảng cách nhìn hợp lý với node cha:

- không quá gần,
- không quá xa,
- không khóa cứng thành một góc duy nhất,
- cũng không khóa cứng thành một khoảng cách tuyệt đối duy nhất,
- nhưng phải nằm trong một `distance band` đủ đẹp để không tạo cảm giác lúc dính sát, lúc bị văng quá xa.

Nếu generator không đặt được `Shop / Forge` trong khoảng hình học hợp lý đó thì map nên bị loại và sinh lại, thay vì cố nhét vào một vị trí chỉ “vừa đủ hợp lệ”.

### 11A.8 Rule boss và Boss Intel trong prototype hiện tại

Direction hiện tại tiếp tục giữ đúng tinh thần cũ:

- `Boss` luôn nằm sẵn ở đỉnh map,
- player không cần đủ intel để được vào boss,
- `Boss Intel` chỉ dùng để reveal identity.

Rule hiện tại của prototype:

- UI hiển thị `Boss Hint: 0/3`,
- lúc đầu boss hiện dưới dạng `Unknown Boss / ?`,
- khi đủ `3/3` thì reveal boss thật.

Nguồn intel / hint hiện tại:

- `Shop` luôn là một nguồn hint,
- ngoài ra còn có `3` nguồn hint khác rải trên map,
- nhưng không phải nguồn nào cũng cho miễn phí.

Rule lấy hint đã chốt:

- nếu hint nằm trong `Combat / Elite` thì **bắt buộc phải `Fight`** node đó mới nhận được hint,
- nếu chỉ `Run` qua combat đó thì **không** nhận hint,
- `Run` không được dùng để ăn hint miễn phí.

`Shop` là nguồn hint đặc biệt:

- vào `Shop` không auto cộng hint,
- player phải chủ động bấm `Buy Hint`,
- sau khi mua thì mới `+1 hint`,
- hiện tại chưa trừ tiền thật vì đây vẫn là prototype,
- nhưng flow phải mô phỏng đúng tinh thần “mua intel”.

UI của shop hiện tại cần có:

- `Buy Hint`
- `Đi tiếp`

Ngay khi vào shop là hiện 2 lựa chọn này.

Sau khi đã mua:

- nút `Buy Hint` phải chuyển sang trạng thái `disabled / tối đi`,
- để player biết shop đó đã bán intel rồi và không thể mua lại.

Ngoài ra, vị trí xuất hiện hint trên map phải bị khống chế:

- không spawn quá gần boss cuối act,
- vùng xa nhất mà hint có thể xuất hiện chỉ nên tới khoảng `3/4 act`,
- không dồn hint lên sát boss row.

Không cho hint từ:

- `Forge`
- `Rest`

### 11A.9 Rule backtrack và trạng thái node sau khi đã xử lý

Direction hiện tại là:

- player được phép quay lại trong `act hiện tại`,
- chỉ đi lại tự do trên phần đường đã mở,
- những node đã trở thành safe transit thì quay lại không có nguy cơ gì mới.

Cần phân biệt rõ:

1. `Fight xong`
   - node clear,
   - thành vòng tròn rỗng,
   - pathfinding đi xuyên qua được,
   - quay lại không trigger nữa.

2. `Run qua combat`
   - enemy vẫn còn,
   - icon combat / elite còn nguyên,
   - không tính là clear,
   - node đó không được xem là safe node cho free backtrack.

3. `Event / Rest đã xử lý`
   - không trigger lại,
   - chỉ còn là phần đường đã mở.

4. `Shop / Forge đã ghé`
   - vẫn giữ icon,
   - không phải encounter một lần rồi biến mất,
   - nhưng vẫn là node an toàn để quay lại.

### 11A.10 Rule readability / interaction trên map

Prototype map hiện tại phải tiếp tục bám các guardrail sau:

- icon node phải dễ đọc,
- line không được cắt nhau,
- line không được đè lên icon node khác,
- hover không được làm node lệch khỏi vị trí,
- player phải click vào node một cách ổn định, không bị hitbox khó chịu.

Về mặt UX:

- map phải cho cảm giác như một `act route board` kiểu STS,
- nhưng có thêm lớp backtrack cục bộ,
- không bị rối tới mức player phải “giải câu đố UI”.

### 11A.11 Ghi chú chốt hiện tại về generator

Các rule dưới đây đã được coi là direction đã chốt cho generator của bản map prototype:

- act map là `node graph` từ đáy lên đỉnh,
- có `Start` ở dưới và `Boss` ở trên,
- có khoảng `20–30 node`,
- có split / merge / dead end / đường vòng hợp lý,
- không crossing,
- không overlap,
- node degree tối đa là `4`,
- `Shop` luôn có đúng `1`,
- `Forge` luôn có đúng `1`,
- `Shop` là nhánh phụ đầu act, thường sau `1–2 node`,
- `Forge` là nhánh phụ gần cuối act,
- cả hai đều là `dead-end leaf node`,
- `Shop / Forge` không biến mất sau khi ghé,
- `Combat / Elite` nếu `Fight` thì biến thành vòng tròn rỗng,
- `Combat / Elite` nếu `Run` thì enemy vẫn còn,
- auto-path chỉ nên đi qua các node đã an toàn,
- `3 node` một đường có thể xảy ra nhiều,
- `4 node` một đường vẫn có nhưng không được thống trị toàn act,
- `5 node` một đường là hiếm,
- hint không được spawn quá gần boss,
- hint trong combat chỉ lấy khi `Fight`,
- shop phải bấm `Buy Hint` mới cộng intel.

Phần addendum này nên được hiểu là:

- khóa current direction của `map-only beta`,
- để về sau khi iterate tiếp combat / reward / economy vẫn có một bộ rule map ổn định làm nền.

---

## 11B. Addendum — các rule map prototype chốt thêm ở vòng iterate hiện tại

Phần này **chỉ bổ sung thêm** các rule đã chốt sau addendum trước, không thay thế phần cũ.

### 11B.1 Rule hiển thị Boss Hint trên map

Direction hiện tại là:

- **không hiện icon hint ở đâu trên map trong trải nghiệm chuẩn**,
- player chỉ thấy progress tổng quát qua UI:
  - `Boss Hint: 0/3`
  - `1/3`
  - `2/3`
  - `3/3`

Lý do là để:

- tránh biến map thành bài toán săn marker,
- giữ `Boss Intel` là thứ đến từ gameplay bình thường,
- và không kéo route choice sang mode “đi gom clue”.

Tuy nhiên, cho mục đích test nội bộ prototype hiện tại có thể tồn tại một **debug toggle**:

- `Show Hint Nodes: Off / On`

Rule của toggle này:

- mặc định là `Off`,
- khi `Off`: không có marker hint nào hiện trên map,
- khi `On`: chỉ hiện marker để test placement / flow,
- đây là công cụ test tạm thời, không phải direction player-facing cuối cùng.

### 11B.2 Rule degree tối đa của node

Rule đã chốt:

- **mỗi node tối đa chỉ được nối với `4` node khác**

Điều này áp dụng cho:

- node thường,
- `Shop`,
- `Forge`,
- và toàn bộ graph của act prototype.

Mục tiêu:

- tránh các node bị dính quá nhiều line,
- giữ graph dễ đọc,
- giảm cảm giác rối và giảm tình trạng nhiều edge chụm vào cùng một điểm.

Nếu map nào sinh ra node có degree vượt `4` thì map đó nên bị loại và generate lại.

### 11B.3 Rule spacing của Rest

`Rest` không được xuất hiện quá sát nhau như các trường hợp hai campfire đứng gần liên tiếp trên cùng một route.

Rule đã chốt:

- giữa hai `Rest` phải có **ít nhất `2` node đệm**,
- hiểu theo cảm giác route là:
  - `3 node` trở lên mới có thể gặp lại `Rest`,
  - không được để hai `Rest` dính quá sát nhau chỉ qua `0–1` node chuyển tiếp.

Mục tiêu của rule này:

- giữ nhịp route tự nhiên hơn,
- tránh việc map nhìn như lặp lại cùng một utility node quá dày,
- và giữ cho `Rest` vẫn là một nhịp thở có giá trị chứ không thành spam landmark.

Nếu map nào sinh ra `Rest` quá gần nhau thì map đó nên bị loại và generate lại.

### 11B.4 Cách hiểu thống nhất của các rule mới này

Các rule bổ sung ở addendum 11B phải được hiểu là đang phục vụ ba mục tiêu:

1. **readability**
   - map dễ đọc,
   - không quá rối,
   - không biến thành màn hình đầy marker.

2. **route quality**
   - player có lựa chọn đường đi rõ ràng,
   - utility node không bị spam quá sát,
   - nhịp map giữ được tiết tấu tốt.

3. **debuggability**
   - vẫn cho phép đội thiết kế / test nội bộ bật một số công cụ tạm thời như `Show Hint Nodes`,
   - nhưng không làm lệch direction player-facing của game.

Phần addendum này tiếp tục là phần khóa current direction cho `map-only beta` ở thời điểm hiện tại.


---

## 11C. Addendum — Current Source of Truth for the HTML Map Prototype

> **Phần 11C này là current source of truth cho map prototype.**  
> Nếu có chỗ nào ở các section map cũ phía trên mâu thuẫn với 11C, thì **11C được ưu tiên**.  
> Mục tiêu của phần này là đồng bộ lại spec theo bản HTML prototype đã chốt gần nhất để có thể đưa thẳng cho Codex.

### 11C.1 Mục tiêu của bản map prototype hiện tại

Bản prototype map hiện tại dùng để test:

- readability của map,
- route choice,
- split / merge / đường vòng,
- backtrack qua các node an toàn,
- flow `Boss Hint`,
- và flow popup / action theo từng loại node.

Prototype này **không** nhằm chốt combat thật, economy thật, inventory thật hoặc chi phí thật của shop.  
Các nút như `Fight`, `Run`, `Buy Hint`, `Đi tiếp` hiện tại chủ yếu dùng để giữ nguyên flow prototype map.

---

### 11C.2 Quy mô map và khung graph hiện tại

Map hiện tại đi theo hướng:

- `Start` ở đáy map,
- `Boss` ở đỉnh map,
- phần map chính có **20–30 intermediate nodes**,
- `Start` và `Boss` **không** được tính vào mốc `20–30` đó.

Sau khi sinh xong phần map chính, hệ thống mới thêm:

- **1 Shop** ở nhánh phụ đầu act,
- **1 Forge** ở nhánh phụ gần cuối act.

Map hiện tại vẫn là:

- graph đọc từ dưới lên trên,
- có split,
- có merge,
- có đường vòng,
- có backtrack trong phần đường đã mở,
- nhưng không được crossing, không được overlap khó đọc, và không được biến thành flowchart quá cứng.

---

### 11C.3 Các loại node hiện tại

Các loại node đang có trong prototype:

- `Start`
- `Combat`
- `Elite`
- `Event`
- `Shop`
- `Rest`
- `Forge`
- `Boss`

Ý nghĩa hiện tại:

- `Combat / Elite / Boss`: hostile node
- `Event / Rest`: one-shot node
- `Shop / Forge`: landmark node, không biến mất sau khi ghé
- `Boss`: luôn ở đỉnh map và luôn đánh được, kể cả khi chưa đủ hint

---

### 11C.4 Rule route / linearity hiện tại

Map không được tạo cảm giác hành lang một chiều quá dài.

Rule đã chốt:

- `single-path streak` tối đa là **5 node**
- `4 node liên tiếp` vẫn được phép
- `5 node liên tiếp` là ngưỡng trần hiện tại của prototype

Ý nghĩa:

- map vẫn được phép có những đoạn thẳng ngắn,
- nhưng không được thống trị toàn act bằng các corridor dài một đường duy nhất.

Ngoài ra:

- **mỗi node tối đa nối với 4 node khác**
- đây là `total degree`, không phải chỉ forward branch

---

### 11C.5 Rule Rest hiện tại

`Rest` dùng để hồi HP trong prototype.

Rule số lượng `Rest` đã chốt:

- nếu map có **ít hơn 25 intermediate nodes** → **2 Rest**
- nếu map có **từ 25 intermediate nodes trở lên** → **3 Rest**

Rule spacing của `Rest` đã chốt:

- giữa hai `Rest` phải có **ít nhất 2 node nằm giữa** trên `shortest path`
- `minRestRowGap = 2`

Hiểu ngắn gọn:

- không được để hai `Rest` quá sát nhau,
- map lớn phải thật sự có khả năng sinh ra đủ `3 Rest`.

---

### 11C.6 Rule Event hiện tại

`Event` hiện tại là one-shot node.

Sau khi đi vào và xử lý xong:

- event không trigger lại,
- node event trở thành node an toàn để đi xuyên qua.

Rule số lượng `Event` đã chốt:

- tổng `Event` target là **xấp xỉ 23%** số node phù hợp trong phần map chính
- đây là **tỷ lệ mềm**
- hiện tại generator cho phép dao động khoảng **-1 đến +2 node**
- và có `floor tối thiểu` là **4 Event**

Rule lớp đầu nối với `Start`:

- các node nối trực tiếp với `Start` có thể là `Combat` hoặc `Event`
- nhưng **tối đa chỉ có 2 Event** ở lớp đầu

Rule adjacency của `Event`:

- cho phép tối đa **2 Event nối trực tiếp với nhau**
- **không cho cụm 3 Event liền nhau**
- một `Event node` không được có **hơn 1 hàng xóm trực tiếp cũng là Event**

Nói ngắn gọn:

- `Event -> Event` là được,
- `Event -> Event -> Event` là không được.

---

### 11C.7 Rule Boss Hint hiện tại

`Boss` luôn hiện sẵn ở đỉnh map.  
`Boss Hint` **không mở khóa quyền đánh boss**.  
Nó chỉ dùng để reveal identity của boss.

UI hiện tại hiển thị:

- `Boss Hint: 0/3`
- `1/3`
- `2/3`
- `3/3`

Khi đủ `3/3`:

- boss không còn là `Unknown Boss / ?`
- UI reveal boss thật

Rule nguồn hint hiện tại đã chốt:

1. **Shop luôn là 1 nguồn hint riêng**
   - vào shop **không tự động cộng hint**
   - player phải bấm **`Buy Hint`**
   - sau khi mua thì mới `+1 Boss Hint`
   - mỗi shop chỉ mua hint được **1 lần**
   - sau khi đã mua, nút `Buy Hint` phải chuyển sang trạng thái `disabled / tối đi`

2. Ngoài Shop, map còn có **3 Event hint sources**
   - các hint source này nằm trên một số `Event`
   - đi qua / hoàn thành event hint thì game tự cộng hint

Rule placement của `Event hint` đã chốt:

- hint event **chỉ nằm trên `Event`**
- hint event **không nằm ở lớp đầu nối với `Start`**
- hint event chỉ nên xuất hiện tới khoảng **3/4 act**
  - trong prototype hiện tại là giới hạn tới khoảng `row 6` trên `8 intermediate rows`
- giữa hai hint event phải có **ít nhất 1 node nằm giữa** trên `shortest path`

Rule ngoại lệ cần hiểu đúng:

- `Shop` là một **nguồn hint đặc biệt**
- nên dù rule hint event là “hint chỉ nằm trên Event”, vẫn phải hiểu là:
  - **hint rải trên map chính** nằm trên Event
  - **Shop vẫn có 1 hint riêng để mua**

Không cho hint từ:

- `Rest`
- `Forge`

---

### 11C.8 Rule hostile / safe / cleared hiện tại

#### Combat / Elite / Boss

Khi vào `Combat / Elite / Boss`:

- popup hiện lựa chọn phù hợp
- với `Combat / Elite` là `Fight` và `Run`
- với `Boss` là `Fight Boss`

Nếu `Fight` thắng:

- node được xem là `cleared`
- hostile không còn ở đó nữa
- node trở thành node an toàn để đi xuyên qua
- về mặt hiển thị, hostile node clear xong đi theo hướng “node đã clear / safe transit”

Nếu `Run`:

- hostile vẫn còn ở node đó
- node **không** trở thành safe transit node
- backtrack tự do không được đi xuyên qua node hostile chưa clear

#### Event / Rest

- trigger **1 lần**
- sau khi xử lý xong thì trở thành node an toàn để đi lại
- `Rest` trong prototype hiện tại có ý nghĩa hồi HP

#### Shop / Forge

- **không biến mất sau khi ghé**
- vẫn giữ icon trên map
- tiếp tục tồn tại như landmark / địa điểm
- nhưng về logic pathing vẫn là node an toàn để quay lại

---

### 11C.9 Rule di chuyển / auto path / backtrack hiện tại

Player không cần click từng node cũ một để quay lại.

Direction hiện tại là:

- player có thể click vào node hợp lệ đã mở
- hệ thống tự tìm `shortest safe path`
- auto-path chỉ đi qua các node đã trở thành `safe transit node`

Safe transit node hiện tại là:

- node đã clear xong,
- event/rest đã dùng xong,
- shop/forge đã ghé qua.

Không được coi là safe transit:

- `Combat / Elite / Boss` đã từng đi qua nhưng chỉ `Run`, chưa `Fight` clear

---

### 11C.10 Rule Shop và Forge hiện tại

#### Shop

Rule hiện tại của `Shop`:

- mỗi act có đúng **1 Shop**
- Shop là **nhánh phụ đầu act**
- Shop là **dead-end leaf node**
- tới Shop là ngõ cụt, muốn đi tiếp phải quay lại node cha
- Shop có flow popup riêng

Flow popup hiện tại của Shop:

- `Buy Hint`
- `Đi tiếp`

Lưu ý:

- prototype hiện tại chưa trừ tiền thật
- nhưng flow phải mô phỏng đúng cảm giác “mua intel”

#### Forge

Rule hiện tại của `Forge`:

- mỗi act có đúng **1 Forge**
- Forge là **nhánh phụ gần cuối act**
- Forge là **dead-end leaf node**
- tới Forge là ngõ cụt, muốn đi tiếp phải quay lại node cha
- Forge là landmark an toàn sau lần ghé đầu tiên

#### Shop / Forge không được treo lửng giữa 2 row

Rule placement đã chốt thêm:

- `Shop` và `Forge` **phải dùng cùng hệ row với các node chính**
- không được nằm ở `row + 0.75` hay ở trạng thái “lửng giữa hai tầng”
- nếu node cha ở `row N` thì `Shop / Forge` phải ở **`row N + 1`**
- tức là:
  - vẫn là nhánh phụ,
  - vẫn là dead-end,
  - nhưng về mặt trình bày chúng nằm đúng trên **row kế tiếp**

Ví dụ:

- node cha ở `row 2`
- thì `Shop / Forge` phải nằm ở `row 3`

---

### 11C.11 Rule hình học / layout của Shop và Forge

Direction hình học hiện tại là:

- nhánh vào `Shop / Forge` phải có cảm giác `chéo lên`
- không được đè lên node khác
- không được cắt line khác
- `Shop / Forge` vẫn là `leaf node`
- không có node nào nối tiếp từ `Shop / Forge`

Ngoài ra:

- `Shop / Forge` không bắt buộc phải nằm sát mép ngoài map
- nhưng phải nhìn như một nhánh phụ tự nhiên
- không được dính sát node cha
- cũng không được văng quá xa một cách khó đọc

Nếu generator không đặt được trong khoảng hình học hợp lý:

- map nên bị loại và sinh lại
- không nên cố nhét vào một vị trí chỉ vừa đủ hợp lệ

---

### 11C.12 Rule UI / interaction hiện tại

Prototype hiện tại đi theo hướng:

- 1 file `HTML + CSS + JS` có thể chạy độc lập để test
- hover vào node **không được làm node lệch khỏi vị trí neo**
- hit area phải ổn định
- có `player token` trên map
- vào node thì hiện `popup / panel theo loại node`
- hostile node hiện hành động `Fight / Run`
- shop hiện `Buy Hint / Đi tiếp`
- forge hiện `Đi tiếp`
- event / rest hiện popup một lần rồi sau đó node trở thành safe

Ngoài ra, prototype hiện có **debug toggle**:

- `Show Hint Nodes: Off / On`
- mặc định là `Off`
- bật lên chỉ để test placement nội bộ
- không phải direction player-facing cuối cùng

---

### 11C.13 Rule generator / config hiện tại

Các thông số / direction đang là current locked rules ở prototype:

- `columns = 7`
- `intermediateRows = 8`
- `pathCount = 6`
- `mapWidth = 860`
- `mapHeight = 1320`
- `maxAttempts = 140`
- `maxNodeDegree = 4`
- `bossHintsRequired = 3`
- `extraHintSources = 3`
- `latestHintRow = 6`

Các con số này hiện tại là baseline của prototype, không phải cam kết final forever, nhưng đang là source of truth cho bản HTML hiện tại.

---

### 11C.14 Cách hiểu ngắn gọn để đưa cho Codex

Nếu cần nói ngắn gọn cho implementation, hãy hiểu map prototype hiện tại như sau:

- phần map chính có `20–30 intermediate nodes`
- `Start` ở đáy, `Boss` ở đỉnh
- có `1 Shop` nhánh phụ đầu act và `1 Forge` nhánh phụ gần cuối act
- `Shop / Forge` là `dead-end leaf node`
- nhưng phải nằm ở **row kế tiếp của node cha**, không treo lửng giữa hai tầng
- `Rest` là `2 hoặc 3` tùy độ lớn map
- `Event` khoảng `23%`
- tối đa `2 Event` ở lớp đầu nối Start
- tối đa `2 Event` liền nhau
- `Shop` có `Buy Hint`
- `Shop` luôn là một nguồn hint riêng
- ngoài Shop còn có `3 Event hint sources`
- event hint không nằm ở lớp đầu và không spawn quá gần boss
- hostile node chỉ thành safe transit khi đã `Fight` clear
- `Run` không được biến hostile node thành safe transit
- `Event / Rest` là one-shot rồi thành safe transit
- `Shop / Forge` vẫn giữ icon sau khi ghé

Phần 11C này nên được xem là spec hiện hành cho bản `map-only beta`.
