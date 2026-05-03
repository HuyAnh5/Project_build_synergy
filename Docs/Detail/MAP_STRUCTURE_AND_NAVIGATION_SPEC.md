# MAP_STRUCTURE_AND_NAVIGATION_SPEC.md

> Tài liệu này mô tả **map graph**, **node structure**, **navigation/backtrack**, **Boss Preparation trên map**, **Boss Intel ở map layer** và các rule di chuyển trong act.  
> File này được tách từ `RUN_STRUCTURE_AND_ECONOMY_SPEC_UPDATED_EVENT_REWARD_V2.md` để `RUN_ECONOMY_REWARD_EVENT_SPEC.md` chỉ còn tập trung vào reward / event / economy / progression.  
> File này không đi sâu vào reward gacha, event package hay shop economy; phần đó nằm ở `RUN_ECONOMY_REWARD_EVENT_SPEC.md`.

---

## 1. Map Structure Trong Act

### 1.1 Khung map hiện tại

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

### 1.2 Các loại node đang dùng

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

### 1.3 Hub / Forge và Shop là 2 thứ khác nhau

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

### 1.4 Rule di chuyển và backtrack

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


### 1.4A Boss Preparation / Act time pressure

`Boss Preparation` là clock mềm của act, dùng để cân bằng giữa:

- đi nhanh tới boss với build mỏng hơn,
- và khám phá thêm để có nhiều reward / shop / consumable / dice edit hơn.

Boss Preparation không nhằm phạt việc backtrack.  
Nó chỉ đo việc player đã khai thác thêm bao nhiêu giá trị mới trong act.

Rule hiện tại:

- mỗi node mới được vào / resolve lần đầu: `+1 Boss Preparation`
- `Combat`: `+1`
- `Elite`: `+1`
- `Event`: `+1`
- `Rest`: `+1` khi dùng lần đầu
- `Shop`: `+1` khi ghé lần đầu
- `Forge`: `+1` khi ghé / dùng lần đầu
- đi qua node đã clear / node rỗng: `+0`
- quay lại trong phần đường đã mở: `+0`
- vào `Boss node`: không tăng Preparation

Điểm rất quan trọng:

- `Elite` không tăng `+2 Preparation`.
- Elite đã có giá riêng qua độ khó combat, mất HP nhiều hơn và risk cao hơn.
- Nếu Elite còn tăng Preparation mạnh hơn, player dễ bị phạt kép và sẽ né Elite quá nhiều.

Mốc Boss Preparation hiện tại:

| Preparation | Trạng thái boss | Ý nghĩa |
|---:|---|---|
| `0–12` | Boss `Unprepared` | Player đến sớm; boss bị bất lợi theo trạng thái riêng của từng boss |
| `13–19` | Boss bình thường | Vùng kỳ vọng: player thường đã có đủ đồ cơ bản để đánh boss |
| `20–25` | Modifier nhẹ | Player đang greed thêm reward / shop / lựa chọn, boss bắt đầu phản ứng |
| `26+` | Modifier rõ | Player over-explore; build có thêm công cụ nhưng boss cũng được chuẩn bị rõ hơn |

Ý nghĩa thiết kế:

- khoảng `~8 node` là route rush hợp lệ,
- khoảng `~15 node` là mức đủ đồ / đủ build shaping cho act,
- `20+ node` là vùng greed / explore-heavy,
- `26+` là over-greed và phải có rủi ro rõ.

Boss Preparation nên được hiểu là **soft cost của việc lấy thêm value**, không phải phí di chuyển.

#### 1.4A.1 Boss `Unprepared` state và Prepared Modifier

Ở mốc `0–12 Boss Preparation`, boss không chỉ đơn giản là “không có modifier”.  
Đây là trạng thái **Boss Unprepared**: player đến đủ sớm để bắt gặp boss khi nó chưa hoàn tất trạng thái chiến đấu.

Rule rất quan trọng:

- `Unprepared` không phải một debuff chung áp y hệt cho mọi boss.
- `Light Modifier` và `Strong Modifier` cũng không nên là tăng stat chung kiểu `+HP / +damage` cho mọi boss.
- Mỗi boss nên có bộ trạng thái Preparation riêng, bám theo fantasy và mechanic chính của boss đó.
- Modifier nên đánh vào **nhịp hoặc cơ chế đặc trưng** của boss, không biến boss thành stat wall.
- Boss vẫn phải giữ identity và vẫn nguy hiểm nếu player vào boss với build quá mỏng.

Mốc tổng quát vẫn giữ:

| Preparation | Trạng thái tổng quát | Ý nghĩa |
|---:|---|---|
| `0–12` | `Unprepared` | Player đến sớm; boss bị bất lợi theo trạng thái riêng của boss |
| `13–19` | `Normal` | Boss ở bản baseline |
| `20–25` | `Light Modifier` | Boss đã chuẩn bị một phần, modifier nhẹ theo mechanic riêng |
| `26+` | `Strong Modifier` | Boss đã chuẩn bị rõ, modifier mạnh hơn theo mechanic riêng |

##### Ví dụ 1 — Dragon

Dragon là boss tạo áp lực bằng **Breath timing**.

| Preparation | Trạng thái Dragon | Rule |
|---:|---|---|
| `0–12` | `Sleeping / Unprepared` | Dragon đang ngủ hoặc chưa chú ý. Player có tempo setup đầu trận. |
| `13–19` | `Normal` | Breath đầu tiên ở `turn 10`. Sau đó cooldown giữ nguyên: cứ `10 turn` dùng Breath 1 lần. |
| `20–25` | `Light Modifier` | Breath đầu tiên đến sớm hơn, ở `turn 7`. Sau Breath đầu tiên, cooldown vẫn là `10 turn`. |
| `26+` | `Strong Modifier` | Breath đầu tiên đến rất sớm, ở `turn 5`. Sau Breath đầu tiên, cooldown vẫn là `10 turn`. |

Rule thức dậy cho Dragon ở `0–12`:

- Dragon tỉnh ngay nếu nhận **direct damage**.
- Các hành động setup không gây damage trực tiếp như `apply Mark`, `apply Burn`, `apply Chilled`, buff bản thân, gain Guard... mặc định **không đánh thức Dragon**, trừ khi boss-specific rule ghi rõ.
- Nếu player không gây direct damage trong `2 player turns`, Dragon vẫn tự tỉnh dậy.
- Khi Dragon tỉnh, Dragon có thể mất lượt đầu để `wake / roar / enter combat stance`, rồi sau đó mới bắt đầu intent cycle bình thường.

Ví dụ timeline:

- `Normal`: Breath ở `turn 10 -> 20 -> 30...`
- `20–25`: Breath ở `turn 7 -> 17 -> 27...`
- `26+`: Breath ở `turn 5 -> 15 -> 25...`

Ý nghĩa: Preparation **không làm Dragon spam Breath nhanh hơn cả trận**.  
Nó chỉ làm **Breath đầu tiên đến sớm hơn**, thể hiện Dragon đã chuẩn bị / tỉnh táo hơn khi player đến muộn.

##### Ví dụ 2 — Knight

Knight là boss tạo áp lực bằng **Guard / Armor / retaliation**.

| Preparation | Trạng thái Knight | Rule |
|---:|---|---|
| `0–12` | `Unarmored / Unprepared` | Knight chưa mặc đủ giáp. Có thể bắt đầu với ít HP hiện tại hơn, không có Guard mở đầu, hoặc thiếu Armor phase đầu. Damage vẫn giữ nguyên. |
| `13–19` | `Normal` | Knight ở bản baseline, không có modifier chuẩn bị đặc biệt. |
| `20–25` | `Light Modifier` | Cứ mỗi `3 turn`, Knight tự động nhận `+4 Guard`. |
| `26+` | `Strong Modifier` | Giữ rule `mỗi 3 turn +4 Guard`; thêm phản đòn: nếu Knight đang có Guard, mỗi lần player gây damage vào Knight, player bị phản lại `4 HP`. |

Rule phản đòn ở `26+`:

- Chỉ phản khi Knight **đang có Guard**.
- Nếu Knight đã mất hết Guard, hit vào HP không phản.
- Mục tiêu là buộc player quan tâm sequencing: phá Guard / Sunder / setup đúng lúc trước khi burst.

Ý nghĩa: Preparation của Knight là **giáp và thế thủ được chuẩn bị tốt hơn**, không phải chỉ tăng damage/HP chung.

##### Boss khác

Các boss khác sẽ được định nghĩa sau theo cùng nguyên tắc:

- `Unprepared` phải là trạng thái riêng của boss đó.
- `Light Modifier` và `Strong Modifier` cũng phải theo mechanic riêng của boss đó.
- Chưa chốt Mage trong file này; Mage không nên bị khóa vội nếu mechanic ritual / seal chưa rõ.
- Không dùng modifier chung kiểu `mọi boss +10% damage` làm hướng chính.

Mục tiêu của hệ Preparation:

- `rush boss` có lợi thế riêng ngoài việc giữ HP,
- player ít đồ hơn nhưng phá được một phần sự chuẩn bị của boss,
- `explore-heavy` có nhiều reward / lựa chọn hơn nhưng boss cũng vào fight với trạng thái chuẩn bị tốt hơn,
- mỗi boss có fantasy riêng khi bị rush hoặc khi được chuẩn bị đầy đủ,
- nhưng không biến boss thành hard-counter hoặc stat wall.

### 1.4B Shop refresh theo Boss Preparation

Shop dùng Boss Preparation như clock nhập hàng.

Rule hiện tại:

- shop refresh rotating stock mỗi `6 Boss Preparation`
- các mốc refresh là khoảng: `6 / 12 / 18 / 24 / 30...`
- refresh chỉ áp dụng cho nhóm hàng có thể xoay vòng
- shop không refresh khi player chỉ backtrack qua node đã clear nếu Preparation không tăng

Nhóm hàng có thể refresh:

- `skill`
- `consumable`
- `relic` thường
- material / utility nhỏ nếu có

Nhóm hàng không tự có lại sau refresh:

- `dice`
- `voucher`
- `Boss Intel` đã mua
- unique item của shop
- mọi món được định nghĩa là `one-per-shop`

Nói cách khác:

- shop có cảm giác “đi nhập hàng” theo thời gian của act,
- nhưng không biến thành máy reroll vô hạn cho các món độc nhất,
- và player không thể mua lại dice / voucher chỉ vì chờ shop refresh.

Shop refresh giúp route khám phá có thêm agency:

- nếu player quay lại shop sau vài node mới, họ có thể thấy lựa chọn khác,
- nhưng việc đi thêm để refresh đồng thời làm tăng Boss Preparation,
- nên shop refresh là cơ hội, không phải value miễn phí.

### 1.4C Forge không refresh theo Preparation

Forge không dùng logic nhập hàng như shop.

Rule hiện tại:

- Forge không refresh inventory theo Boss Preparation.
- Forge là nơi player đem quặng / gem / material tới để enchant hoặc chỉnh dice.
- Giá trị của Forge đến từ tài nguyên player mang tới, không đến từ việc Forge tự đổi hàng.

Ý nghĩa thiết kế:

- Shop = commerce / pivot / item access.
- Forge = dice progression / enchant / build sculpting.
- Hai node này không nên dùng cùng một logic refresh, vì cảm giác quyết định khác nhau.

### 1.4D Boss node khóa act

Khi player bước vào `Boss node`, game cần cảnh báo rõ:

`Entering Boss will end this Act.`

Rule hiện tại:

- player vào boss với HP hiện tại,
- không full heal miễn phí trước boss,
- nếu thắng boss: act kết thúc và chuyển sang act tiếp theo / act reward,
- player không được quay lại các phòng cũ sau khi đã thắng boss,
- nếu thua boss: run kết thúc theo fail-state hiện tại,
- nếu chạy khỏi boss và chưa thắng boss: chưa được sang act mới.

Điểm cần khóa:

- đánh xong boss không cho quay lại map cũ để farm nốt,
- nếu cho quay lại sau boss, trade-off trước boss sẽ bị phá,
- boss phải là điểm kết thúc act, không phải một node lớn có thể clear trước rồi quay lại gom tài nguyên sau.


### 1.5 Boss Intel

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

### 1.5A Boss Intel dưới dạng boss-specific clue set

Từ direction event mới, `Boss Intel` nên được hiểu cụ thể hơn là hệ clue theo boss.

Rule:

```text
Mỗi boss có 3 clue.
Mỗi clue là một dấu vết / event hint cụ thể.
Một số clue có thể dùng chung giữa nhiều boss.
Khi đủ 3/3 clue, boss identity được reveal.
```

Điểm quan trọng:

- player không nhặt `Intel token` vô danh nếu có thể tránh;
- player nên nhớ clue bằng hình ảnh và event cụ thể;
- cùng một event có thể vừa có reward vừa có clue;
- clue không cần độc quyền 100%, vì tổ hợp 3 clue mới là thứ reveal boss.

Ví dụ:

```text
Dragon:
Attacked Carriage
+ Half-eaten Remains
+ Burned Claw Marks
= reveal Dragon
```

```text
Knight:
Duel Remnants
+ Polished Armor Shards
+ Oath Marks
= reveal Knight
```

Event chỉ là nơi chứa clue, không phải clue luôn là event độc lập.

Ví dụ:

```text
Clawed Carriage
= Loot Cache + Boss-related Trace
= vừa loot đồ, vừa nhận clue
```

### 1.6 Meta progression của Boss Intel

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

### 1.7 Escape / retreat rule trên map

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
- nếu thắng boss thì act kết thúc, không quay lại map cũ để farm nốt node chưa đi

### 1.8 Những gì chưa chốt ở map layer

Các điểm sau vẫn đang mở và không nên coi là final:

- điều kiện dice để chạy khỏi combat,
- tỷ lệ / tần suất spawn chính xác của từng loại node,
- công thức reward scaling khi enemy tự chạy và encounter mạnh lên,
- số lượng shop / rest / event tối ưu cho mỗi act,
- act layout variation giữa các biome / vùng khác nhau,
- modifier cụ thể của boss ở từng mốc Boss Preparation,
- nội dung rotating stock cụ thể của shop.

Nhưng những thứ đã đủ mạnh để coi là current direction gồm:

- act là `node graph` từ đáy lên đỉnh,
- có backtrack trong phần đường đã mở,
- backtrack qua node đã clear không tăng Boss Preparation,
- khai thác node mới tăng Boss Preparation,
- Boss Preparation dùng các mốc `0–12 / 13–19 / 20–25 / 26+`,
- `Hub / Forge` ở đáy act,
- `Shop` là node riêng và có rotating stock refresh mỗi `6 Boss Preparation`,
- `Forge` không refresh theo Preparation,
- boss luôn ở đỉnh map,
- thắng boss sẽ khóa act và sang act tiếp theo / act reward,
- `Boss Intel` chỉ reveal identity, không mở khóa quyền đánh,
- intel được ghi nhận tự động theo gameplay thường.

---
