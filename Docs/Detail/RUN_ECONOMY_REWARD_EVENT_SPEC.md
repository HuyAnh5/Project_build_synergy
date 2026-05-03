# RUN_ECONOMY_REWARD_EVENT_SPEC.md

> Tài liệu này mô tả **vòng run tổng thể**, **reward / shop / progression loop**, **resource flow ngoài combat** và ý nghĩa kinh tế của mỗi run.  
> File này không đi sâu vào core combat rules; phần đó nằm ở `COMBAT_CORE_SPEC.md`.

> **Split note:** File này là bản tách từ `RUN_STRUCTURE_AND_ECONOMY_SPEC_UPDATED_EVENT_REWARD_V2.md`.  
> File này giữ phần **run economy / reward / event / shop / progression**.  
> Phần **map graph / node / navigation / backtrack** đã được chuyển sang `MAP_STRUCTURE_AND_NAVIGATION_SPEC.md`.

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

## 5A. Reward sau combat — Reward Gacha

Phần này khóa hướng hiện tại cho reward sau khi thắng `Combat`, `Elite` và `Boss`.

Mục tiêu của reward sau combat:

- cho player cảm giác mỗi trận đánh đều có giá trị,
- tạo nhịp mở thưởng ngắn sau combat,
- cho player thêm công cụ để điêu khắc build,
- không biến reward thành một bể random quá khó đọc,
- không ép player chọn giữa quá nhiều vật phẩm nhỏ vô nghĩa.

Flow sau khi thắng combat:

```text
Combat kết thúc
→ player nhận Base Gold chắc chắn
→ mở reward gacha
→ hiện số card theo loại encounter
→ player chọn số card được phép
→ reward được áp vào run/loadout/inventory theo type
→ quay lại map / shop / forge / node kế tiếp
```

Điểm rất quan trọng:

- `Base Gold` luôn nhận sau combat.
- `Extra Gold` nếu xuất hiện trong reward card là **bonus Gold**, không thay thế Base Gold.
- Reward card là phần lựa chọn thêm, không phải toàn bộ phần thưởng.
- Reward sau combat phục vụ build shaping, không chỉ là loot nhiều cho vui.

### 5A.1 Bảng mode reward hiện tại

| Encounter | Số card hiện | Số card được chọn | Tỉ lệ rarity |
|---|---:|---:|---|
| `Combat thường` | `3` | `1` | `65% Xám / 30% Xanh / 5% Vàng / 0% Đỏ` |
| `Elite` | `4` | `1` | `35% Xám / 45% Xanh / 18% Vàng / 2% Đỏ` |
| `Boss` | `5` | `2` | `0% Xám / 60% Xanh / 35% Vàng / 5% Đỏ` |

Diễn giải:

```text
Combat thường:
3 card, chọn 1
Reward chủ yếu là Xám/Xanh
Có cơ hội nhỏ ra Vàng
Không có Đỏ

Elite:
4 card, chọn 1
Xanh là màu có tỉ lệ cao nhất
Vàng/Đỏ cao hơn Combat thường
Không có guarantee

Boss:
5 card, chọn 2
Không có Xám
Xanh là nền chính
Vàng nhiều
Đỏ vẫn hiếm
Có guarantee ít nhất 2 card Vàng/Đỏ
```

### 5A.2 Rarity / màu reward

Rarity trả lời câu hỏi:

```text
Reward này mạnh / hiếm / đặc biệt tới đâu?
```

Rarity không trả lời reward dùng để làm gì.  
Reward dùng để làm gì được quyết định bởi `Purpose`.

| Rarity | Màu | Nội dung |
|---|---|---|
| `Common` | Xám | Extra Gold, skill cơ bản |
| `Uncommon` | Xanh | Tất cả consumable, skill tốt hơn, passive bắt đầu xuất hiện |
| `Rare` | Vàng | Skill/passive mạnh, Whole-die Color material |
| `Special` | Đỏ | Skill/passive cực mạnh, run-defining reward |

Rule:

- Skill có thể xuất hiện từ `Common → Special`.
- Passive chỉ xuất hiện từ `Uncommon → Special`.
- Tất cả consumable đều là `Uncommon`.
- Whole-die Color material là `Rare`.
- Combat thường không có `Special`.
- Boss không có `Common`.

### 5A.3 Purpose / danh mục reward

Purpose trả lời câu hỏi:

```text
Reward này phục vụ việc gì trong run?
```

Card reward nên hiển thị theo format:

```text
Purpose
Reward Name
```

Ví dụ:

```text
Edit Dice
Adjust Face +
```

```text
Combat Aid
Last Rite
```

```text
Skill
Fire Slash
```

Bảng Purpose hiện tại:

| Purpose | Gồm những gì | Vai trò |
|---|---|---|
| `Economy` | Extra Gold | Lấy thêm tiền ngoài Base Gold |
| `Skill` | Skill từ Common đến Special | Thêm/thay hành động combat |
| `Passive` | Passive từ Uncommon đến Special | Đổi engine / hướng build |
| `Edit Dice` | Zodiac, Whole-die Color material | Chỉnh dice, chỉnh mặt, enchant, đổi màu/tag dice |
| `Combat Aid` | Seal | Hỗ trợ combat trực tiếp: damage, status, cứu nguy |
| `Utility Support` | Rune | Hỗ trợ đa dụng: Focus, reroll, cleanse, utility |

### 5A.4 Consumable trong reward

Tất cả consumable là `Uncommon`, nhưng không nằm chung một bể lớn.

Consumable được chia theo Purpose:

| Nhóm consumable | Purpose | Vai trò |
|---|---|---|
| `Zodiac` | Edit Dice | Chỉnh dice, chỉnh mặt, enchant, sculpt dice |
| `Seal` | Combat Aid | Công cụ combat trực tiếp, status, damage, cứu nguy |
| `Rune` | Utility Support | Utility đa dụng, Focus, reroll, cleanse, gold utility |

Lý do không để consumable thành một pool khổng lồ:

- player sẽ rất khó tìm đúng món mình cần,
- reward screen khó đọc,
- các tool khác vai trò bị trộn lẫn,
- player không biết mình đang được offer “chỉnh dice”, “cứu combat” hay “utility”.

Hướng đúng:

```text
Roll rarity
→ roll Purpose hợp lệ
→ roll item trong Purpose đó
```

Không làm:

```text
Roll đại một consumable trong toàn bộ consumable pool
```

### 5A.5 Purpose duplication rule

Trong một lần reward roll:

```text
Economy tối đa 1 card.
Các Purpose khác tối đa 2 card.
```

Áp dụng cho:

```text
Combat thường
Elite
Boss
```

Ví dụ hợp lệ:

```text
Skill / Edit Dice / Economy
```

```text
Skill / Skill / Utility Support
```

```text
Edit Dice / Edit Dice / Combat Aid / Passive
```

Ví dụ không hợp lệ:

```text
Economy / Economy / Skill
```

```text
Skill / Skill / Skill
```

```text
Edit Dice / Edit Dice / Edit Dice
```

Lý do:

- Economy chỉ là bonus Gold, nếu xuất hiện nhiều sẽ làm reward screen thành lựa chọn tiền giả.
- Các Purpose khác có thể xuất hiện 2 lần vì player có thể muốn nhiều lựa chọn cùng hướng build.
- Không Purpose nào được flood toàn bộ reward screen.

### 5A.6 Boss reward guarantee

Boss roll theo tỉ lệ gốc:

```text
0% Xám / 60% Xanh / 35% Vàng / 5% Đỏ
```

Sau khi roll đủ 5 card, hệ thống kiểm tra:

```text
Boss reward phải có ít nhất 2 card high-rarity.
High-rarity = Rare hoặc Special.
High-rarity = Vàng hoặc Đỏ.
```

Các kết quả hợp lệ:

```text
Xanh / Xanh / Vàng / Xanh / Vàng
```

```text
Xanh / Xanh / Vàng / Đỏ / Xanh
```

```text
Xanh / Đỏ / Xanh / Đỏ / Xanh
```

Các kết quả không hợp lệ:

```text
Xanh / Xanh / Xanh / Xanh / Xanh
```

```text
Xanh / Vàng / Xanh / Xanh / Xanh
```

Nếu kết quả không hợp lệ, hệ thống upgrade card không phải high-rarity lên `Rare` cho đến khi đủ 2 card Vàng/Đỏ.

Ví dụ:

```text
Roll gốc:
Xanh / Xanh / Xanh / Xanh / Xanh

Sau guarantee:
Xanh / Vàng / Xanh / Xanh / Vàng
```

Đỏ vẫn chủ yếu đến từ tỉ lệ 5%, không nên bị guarantee ép ra thường xuyên.

Lưu ý:

- Guarantee có thể làm thống kê thực tế lệch khỏi tỉ lệ gốc.
- Đây là chủ đích vì boss là mốc lớn.
- Player không nên thắng boss xong nhìn thấy 5 card toàn Xanh.

### 5A.7 Elite không có guarantee

Elite không guarantee Vàng/Đỏ.

Elite đã có:

- 4 card thay vì 3,
- tỉ lệ Xanh cao nhất,
- tỉ lệ Vàng/Đỏ tốt hơn Combat thường.

Vì vậy Elite chỉ dùng tỉ lệ tốt hơn, không có guarantee.

```text
Elite:
35% Xám / 45% Xanh / 18% Vàng / 2% Đỏ
```

Triết lý:

```text
Combat thường = reward cơ bản
Elite = reward tốt hơn nhưng vẫn có variance
Boss = reward lớn, không được tệ
```

### 5A.8 Những thứ không nằm trong reward gacha sau combat

Không đưa các thứ sau vào reward gacha sau combat hiện tại:

```text
Boss Intel
Basic Ore
Shard
Forge Dust
Forge Material purpose riêng
Dice Reward
Special Dice
Mythic Material
Secret Reward bucket
```

Lý do:

- Boss Intel thuộc hệ event/clue/shop intel, không phải card gacha thường.
- Basic Ore / Shard / Forge Dust bị bỏ vì việc chỉnh dice là vai trò của Zodiac.
- Whole-die Color material vẫn tồn tại, nhưng thuộc `Edit Dice`.
- Không mở thêm quá nhiều bucket khi reward loop chưa cần.

### 5A.9 Reward sau combat và loadout

Khi player nhận reward thật:

- `Skill`: nếu còn slot thì có thể equip; nếu full slot thì phải thay/bỏ/bán theo loadout rule.
- `Passive`: vì chỉ có 1 passive slot, mỗi passive reward là quyết định lớn.
- `Edit Dice`: Zodiac có thể vào consumable slot hoặc dùng theo context; Whole-die Color cần Forge để sử dụng.
- `Combat Aid`: Seal đi vào consumable slot.
- `Utility Support`: Rune đi vào consumable slot.
- `Economy`: cộng Gold ngay.

Điểm cần giữ:

```text
Reward phải khiến player điêu khắc build.
Không có inventory dự trữ lớn để gom mọi thứ rồi tối ưu sau.
```

### 5A.10 Combat phát sinh từ Event vẫn dùng Reward Gacha

Nếu một event dẫn tới combat, phần thưởng sau khi thắng combat đó vẫn dùng hệ `Reward Gacha` bình thường.

Rule:

```text
Event có combat
→ player đánh combat
→ nếu thắng, nhận Base Gold theo độ khó combat
→ mở reward gacha theo độ khó combat
→ sau đó mới resolve reward/clue/event package còn lại nếu event định nghĩa có
```

Độ khó của combat trong event quyết định bảng gacha:

| Event combat tag | Dùng bảng reward |
|---|---|
| `Normal Event Combat` | bảng `Combat thường`: 3 card, chọn 1 |
| `Elite Event Combat` | bảng `Elite`: 4 card, chọn 1 |
| `Boss / Mini-boss Event Combat` | chỉ dùng bảng `Boss` nếu event được định nghĩa là boss-class encounter |

Ví dụ:

```text
Event: Warm Camp
Choice: Search deeper
→ bị ambush bởi quái thường
→ thắng combat
→ nhận reward như Combat thường: 3 card, chọn 1
```

Ví dụ khác:

```text
Event: Guarded Noble Wreckage
Choice: Break the seal
→ spawn Elite Ambush
→ thắng combat
→ nhận reward như Elite: 4 card, chọn 1
→ sau đó nhận thêm cargo của event nếu event data ghi rõ
```

Điểm quan trọng:

- Event combat không dùng một reward table riêng nếu không có lý do đặc biệt.
- Độ khó combat quyết định gacha.
- Event reward package có thể cộng thêm loot/clue riêng, nhưng phải được ghi rõ trong data của event đó.
- Không tự động cộng cả gacha lớn và event loot lớn nếu event không định nghĩa rõ, để tránh event combat vượt value quá mạnh.

## 5B. Event reward / Event loop — Base Type + Modifier

Event là node không combat kiểu Slay the Spire, nhưng trong game này event không chỉ là text chọn 1 dòng.

Event phải phục vụ 4 việc:

1. tạo nhịp nghỉ giữa các combat,
2. cho reward hoặc trade-off khác combat,
3. kể chuyện / tạo hình ảnh của vùng đang đi qua,
4. gắn Boss Intel / clue vào thế giới game một cách tự nhiên.

Event không nên chỉ là:

```text
+1 Boss Intel
```

Event đúng nên là:

```text
Một cảnh cụ thể
+ một function gameplay cụ thể
+ một hoặc nhiều modifier
+ reward/cost tương ứng
+ có thể chứa clue về boss
```

Cấu trúc event hiện tại:

```text
Event = Event Base Type + Event Modifier
```

Trong đó:

- `Event Base Type` quyết định event **làm gì** ở gameplay chính.
- `Event Modifier` quyết định event có **twist / clue / risk / flavor** gì.

Ví dụ:

```text
Clawed Carriage
= Loot Cache + Boss-related Trace
```

Nghĩa là:

- Base Type là `Loot Cache`: player có thể loot đồ.
- Modifier là `Boss-related Trace`: hiện trường có dấu vết liên quan boss.

---

### 5B.1 Event Base Type

Event Base Type là khung chức năng chính của event.

Danh sách Base Type hiện tại:

```text
Loot Cache
Choice Event
Trade Event
Risk Event
Rest-like Event
Combat Ambush Event
Shop-like Event
```

## 5B.1.1 Loot Cache

`Loot Cache` là event nơi player tìm thấy đồ có thể lấy.

Ví dụ fantasy:

- xe ngựa bị tấn công,
- trại bỏ hoang,
- thùng hàng bị rơi,
- xác thương nhân,
- kho nhỏ bị bỏ lại,
- rương ven đường.

Flow:

```text
Player vào event
→ đọc cảnh
→ chọn lấy đồ / kiểm tra kỹ / bỏ đi
→ nhận reward package
→ nếu có modifier thì resolve thêm clue/risk/cost
```

Loot Cache không nhất thiết phải bắt chọn 1 trong 3.  
Nhiều Loot Cache có thể cho player **lấy hết**, nhưng package phải được giới hạn.

Reward package gợi ý:

```text
Gold chắc chắn
+ 1 reward nhỏ hoặc consumable
+ 1 cargo / item roll
+ optional clue nếu modifier có Intel/Boss Trace
```

Ví dụ:

```text
Event: Attacked Carriage
Base Type: Loot Cache

Nhận:
+18 Gold
Utility Support
Dice Reroll

Cargo:
Skill
Fire Slash
```

Loot Cache nên có giá trị vừa phải vì nó không yêu cầu combat.

## 5B.1.2 Choice Event

`Choice Event` là event có nhiều lựa chọn khác nhau, mỗi lựa chọn dẫn tới reward/cost khác nhau.

Ví dụ fantasy:

- gặp người bị thương,
- shrine có nhiều cách cầu nguyện,
- xác quái vật có thể mổ lấy đồ hoặc đốt đi,
- NPC cần giúp đỡ,
- cánh cửa khóa có nhiều cách mở.

Flow:

```text
Player đọc tình huống
→ chọn 1 trong nhiều hướng xử lý
→ mỗi hướng có reward/cost khác nhau
```

Ví dụ:

```text
Event: Wounded Scout

Choice A: Treat the scout
- mất 8 Gold
- nhận +1 Boss Intel
- nhận 1 Rune

Choice B: Take the scout's map
- nhận +1 Boss Intel
- mất một lượng nhỏ reputation/event flag nếu sau này có

Choice C: Leave
- không nhận gì
```

Choice Event tốt khi lựa chọn phản ánh tính cách/hướng build của player, không chỉ là chọn reward cao nhất.

## 5B.1.3 Trade Event

`Trade Event` là event nơi player trả một tài nguyên để nhận tài nguyên khác.

Tài nguyên có thể trả:

```text
Gold
HP
Consumable
Skill
Passive
Dice state
Boss Preparation / time
```

Tài nguyên có thể nhận:

```text
Skill
Passive
Consumable
Edit Dice
Whole-die Color material
Gold
Boss Intel
```

Ví dụ:

```text
Event: Old Dice Carver

Pay 25 Gold
→ nhận 1 Zodiac

Pay 8 HP
→ nhận Whole-die Color material

Leave
→ không có gì
```

Trade Event dùng để tạo quyết định:

```text
Mình có nên hy sinh tài nguyên hiện tại để lấy power dài hạn không?
```

## 5B.1.4 Risk Event

`Risk Event` là event cho player lựa chọn giữa an toàn và mạo hiểm.

Flow thường:

```text
Lấy ít nhưng an toàn
hoặc
mạo hiểm để lấy nhiều hơn
```

Rủi ro có thể là:

```text
mất HP
combat ambush
dính debuff combat sau
mất Gold
tăng Boss Preparation
mất consumable
```

Ví dụ:

```text
Event: Black Chest

Open carefully
→ nhận 12 Gold

Force it open
→ nhận Rare reward
→ mất 8 HP

Leave
→ không có gì
```

Risk Event tốt khi player có thể đánh giá tình trạng run:

- HP còn nhiều không?
- build có đủ mạnh nếu bị ambush không?
- có cần reward lớn để pivot không?

## 5B.1.5 Rest-like Event

`Rest-like Event` là event cho hồi phục hoặc ổn định run, nhưng không thay thế Rest node chính.

Nó có thể cho:

```text
hồi HP nhỏ
cleanse debuff
nhận Guard/Focus cho combat sau
xóa một downside
đổi một consumable xấu lấy một consumable khác
```

Ví dụ:

```text
Event: Quiet Shrine

Pray
→ hồi 8 HP

Meditate
→ combat tiếp theo bắt đầu với +1 Focus

Search shrine
→ nhận clue nhưng không hồi HP
```

Rest-like Event nên thấp hơn Rest thật để Rest node vẫn có giá trị.

## 5B.1.6 Combat Ambush Event

`Combat Ambush Event` là event không phải node combat chính, nhưng có thể biến thành combat.

Nó thường đi cùng modifier `Hidden Danger`.

Flow:

```text
Player thấy event có loot/clue
→ nếu chọn hành động mạo hiểm
→ spawn combat ambush
→ thắng thì nhận reward tốt hơn
```

Ví dụ:

```text
Event: Warm Campfire

Take supplies quickly
→ nhận 10 Gold

Search the tents
→ 35% bị ambush
→ nếu không bị ambush: nhận 1 consumable
→ nếu bị ambush: vào combat, thắng nhận reward tốt hơn
```

Ambush phải đọc được bằng hint nhẹ, không nên là phạt ngẫu nhiên hoàn toàn.

## 5B.1.7 Shop-like Event

`Shop-like Event` là event giống giao dịch mini nhưng không phải Shop node đầy đủ.

Ví dụ:

- thương nhân lạc đường,
- kẻ đổi đồ bí ẩn,
- xác thương nhân còn vài món,
- quầy hàng bỏ hoang.

Nó có thể cho:

```text
mua 1 món
bán 1 món
đổi consumable
mua clue
mua heal nhỏ
```

Khác với Shop:

- inventory ít hơn,
- không refresh như shop,
- có thể có giá lạ hơn,
- có thể gắn với modifier như Corrupted/Blessed/Rare.

Ví dụ:

```text
Event: Dead Merchant Cart
Base Type: Shop-like Event
Modifier: Loot Cache / Intel Clue

Options:
- Buy one sealed box: 20 Gold
- Search corpse: +1 Boss Intel
- Loot loose coins: +8 Gold
```

---

### 5B.2 Event Modifier

Event Modifier là lớp phụ gắn lên Base Type.

Modifier không tự quyết định event chính làm gì.  
Nó thêm twist, clue, rủi ro hoặc bias reward.

Danh sách Modifier hiện tại:

```text
Intel Clue
Hidden Danger
Corrupted Loot
Blessed Loot
Boss-related Trace
Elemental Trace
Rare Cache
```

## 5B.2.1 Intel Clue

`Intel Clue` nghĩa là event có thông tin hữu ích.

Thông tin này có thể là:

```text
Boss clue
map clue
enemy clue
event chain clue
shop/rest clue
```

Trong context hiện tại, Intel Clue thường được dùng để tăng tiến độ Boss Intel hoặc thêm clue vào clue set.

Ví dụ:

```text
Event: Torn Journal
Modifier: Intel Clue

Player đọc nhật ký
→ nhận 1 clue về boss
→ có thể +1 Boss Intel progress
```

Intel Clue không nhất thiết phải là boss-specific.  
Nếu clue liên quan trực tiếp đến boss thì nên dùng `Boss-related Trace`.

## 5B.2.2 Hidden Danger

`Hidden Danger` nghĩa là event nhìn có vẻ an toàn nhưng có rủi ro ẩn.

Tác dụng có thể là:

```text
spawn ambush
mất HP
dính debuff combat sau
mất Gold
tăng Boss Preparation
```

Ví dụ:

```text
Event: Abandoned Camp
Base Type: Loot Cache
Modifier: Hidden Danger

Take visible supplies
→ nhận 10 Gold, an toàn

Search deeper
→ nhận 1 consumable
→ 35% bị ambush
```

Hidden Danger tốt khi player được đọc dấu hiệu trước:

```text
lửa còn ấm
dấu chân mới
máu chưa khô
tiếng động trong lều
```

Không nên là bẫy vô hình hoàn toàn.

## 5B.2.3 Corrupted Loot

`Corrupted Loot` nghĩa là reward mạnh hơn bình thường nhưng bị nhiễm downside.

Downside có thể là:

```text
mất HP
combat sau enemy mạnh hơn
nhận debuff tạm
tăng Boss Preparation
mất Focus đầu combat sau
một mặt dice bị bất lợi tạm thời
```

Ví dụ:

```text
Event: Black Relic Chest
Base Type: Loot Cache
Modifier: Corrupted Loot

Open
→ nhận Rare Skill
→ combat tiếp theo player bắt đầu với -1 Focus

Purge then open
→ mất 10 Gold
→ nhận Uncommon reward sạch

Leave
→ không nhận gì
```

Corrupted Loot nên hấp dẫn nhưng không miễn phí.

## 5B.2.4 Blessed Loot

`Blessed Loot` nghĩa là reward an toàn, sạch, có thể kèm lợi ích nhỏ.

Tác dụng có thể là:

```text
hồi HP nhỏ
combat sau +1 Focus
nhận consumable sạch
xóa debuff nhẹ
```

Ví dụ:

```text
Event: Saint's Remains
Base Type: Loot Cache
Modifier: Blessed Loot

Pay respect
→ hồi 6 HP
→ nhận 1 Rune

Loot disrespectfully
→ nhận thêm Gold
→ mất blessing
```

Blessed Loot giúp tạo event tích cực, không phải event nào cũng là bẫy/trade-off.

## 5B.2.5 Boss-related Trace

`Boss-related Trace` nghĩa là hiện trường có dấu vết liên quan trực tiếp đến boss identity hoặc boss mechanic.

Đây là modifier quan trọng nhất cho Boss Intel.

Ví dụ trace:

```text
vết móng vuốt cháy đen
vảy nóng bất thường
dấu giáp khổng lồ kéo lê trên đất
vết kiếm cắt đôi đá
xác bị đóng băng dù trời không lạnh
đồng xu bị nung chảy
```

Tác dụng:

```text
+1 boss clue
hoặc
+1 Boss Intel progress
hoặc
reveal một phần clue set
```

Boss-related Trace có thể gắn lên bất kỳ Base Type nào:

```text
Loot Cache + Boss-related Trace
Choice Event + Boss-related Trace
Risk Event + Boss-related Trace
Combat Ambush Event + Boss-related Trace
```

Ví dụ:

```text
Event: Clawed Carriage
Base Type: Loot Cache
Modifier: Boss-related Trace

Player loot xe ngựa
Nhưng đồng thời thấy vết cào cháy sém
→ nhận clue "Burned Claw Marks"
```

## 5B.2.6 Elemental Trace

`Elemental Trace` nghĩa là event có dấu vết nguyên tố.

Elemental Trace có thể:

```text
hint boss/enemy dùng element gì
bias reward pool theo element
mở option đặc biệt nếu player có skill/status tương ứng
```

Ví dụ:

```text
Event: Frozen Shrine
Base Type: Choice Event
Modifier: Elemental Trace

Dấu hiệu:
Mặt đất quanh shrine đóng băng dù vùng này không lạnh.

Tác dụng:
- tăng chance reward Ice Skill / Ice Rune / Cryostasis
- có thể là clue cho boss Ice
```

Elemental Trace không nhất thiết là Boss Intel.  
Nó chỉ trở thành Boss Intel nếu element đó là một phần clue set của boss hiện tại.

## 5B.2.7 Rare Cache

`Rare Cache` nghĩa là event có kho đồ hiếm hơn bình thường.

Tác dụng:

```text
tăng chance Rare
có thể xuất hiện Whole-die Color material
Gold cao hơn
cargo tốt hơn
```

Ví dụ:

```text
Event: Noble Wreckage
Base Type: Loot Cache
Modifier: Rare Cache

Reward:
+25 Gold
1 cargo roll với chance Rare cao hơn
5-10% Whole-die Color material
```

Rare Cache không nên xuất hiện quá thường xuyên.  
Nếu quá nhiều Rare Cache, reward curve sẽ bị đẩy nhanh.

---

### 5B.3 Cách ghép Base Type + Modifier

Một event có thể có:

```text
1 Base Type
+ 0 đến 2 Modifier chính
```

Không nên gắn quá nhiều modifier cùng lúc trong MVP vì event sẽ khó đọc.

Ví dụ tốt:

```text
Clawed Carriage
= Loot Cache + Boss-related Trace
```

```text
Black Chest
= Loot Cache + Corrupted Loot
```

```text
Frozen Shrine
= Choice Event + Elemental Trace
```

```text
Warm Camp
= Loot Cache + Hidden Danger
```

```text
Dead Merchant Cart
= Shop-like Event + Intel Clue
```

Ví dụ nên tránh:

```text
Loot Cache + Hidden Danger + Corrupted Loot + Rare Cache + Boss-related Trace
```

Quá nhiều lớp khiến player không biết event chính đang làm gì.

### 5B.4 Event reward package

Event không bắt buộc dùng reward gacha 3/4/5 card như combat.

Event nên dùng `Reward Package` theo tình huống.

Các dạng reward package:

```text
Fixed reward
Random package
Choice package
Trade package
Risk package
Clue package
```

## Fixed reward

Reward cố định, dùng cho event đơn giản.

```text
Nhận +15 Gold
Nhận +1 Boss clue
```

## Random package

Reward roll từ một table nhỏ.

```text
Nhận 1 Rune ngẫu nhiên
Nhận 1 Skill Common/Uncommon
```

## Choice package

Player chọn reward theo lựa chọn event.

```text
Help the knight
→ nhận Passive

Rob the knight
→ nhận Gold + Combat Aid
```

## Trade package

Player trả cost để nhận reward.

```text
Pay 20 Gold
→ nhận 1 Zodiac
```

## Risk package

Reward tốt hơn nhưng có rủi ro.

```text
Search deeper
→ 50% nhận Rare reward
→ 50% bị ambush
```

## Clue package

Reward chính là thông tin.

```text
Nhận clue "Burned Claw Marks"
Tiến độ Boss Intel +1
```

Clue package có thể đi kèm loot package nếu event hợp lý.

Ví dụ xe ngựa bị tấn công:

```text
Loot package:
+18 Gold
1 Utility Support

Clue package:
Burned Claw Marks
```

### 5B.4A Event combat reward và event package

Nếu event không có combat, reward đến từ `Event Reward Package`.

Nếu event có combat, reward tách thành 2 lớp:

```text
1. Combat Reward
2. Event Package
```

## Combat Reward

Combat Reward dùng hệ gacha sau combat theo độ khó:

```text
Normal Event Combat → Combat gacha
Elite Event Combat → Elite gacha
Boss-class Event Combat → Boss gacha
```

## Event Package

Event Package là phần thưởng/cost riêng của event, ví dụ:

```text
loot trong xe ngựa
clue tìm được
hàng hóa khóa trong rương
vật phẩm NPC trao
downside từ Corrupted Loot
```

Event Package không bắt buộc lúc nào cũng có.  
Nó phải được ghi rõ trong data của từng event.

Ví dụ event có combat nhưng không có package thêm:

```text
Search the camp
→ bị ambush
→ thắng combat
→ nhận Combat gacha bình thường
→ không có loot thêm
```

Ví dụ event có combat và có package thêm:

```text
Search the noble carriage
→ Elite Ambush
→ thắng combat
→ nhận Elite gacha
→ mở được cargo: +20 Gold và 1 clue
```

Guardrail:

- Nếu event combat đã cho gacha, package thêm không nên quá lớn trừ khi combat rất khó hoặc event có cost trước đó.
- Nếu event muốn cho reward rất lớn, nên dùng `Elite Event Combat`, `Boss-class Event Combat`, `Corrupted Loot`, hoặc cost rõ ràng.
- Nếu event chỉ là ambush bất ngờ, reward gacha sau combat thường đã đủ.

### 5B.5 Event và Boss Intel / clue set

Boss Intel không nên chỉ là một con số trừu tượng.

Hướng hiện tại:

```text
Mỗi boss có 3 clue.
Mỗi clue được gắn vào event/encounter/shop intel cụ thể.
Một số clue có thể dùng chung giữa nhiều boss.
Bộ 3 clue đi cùng nhau mới reveal boss identity.
```

Nói cách khác:

```text
Boss Intel = tiến độ đọc bộ clue của boss.
```

Không phải:

```text
Boss Intel = nhặt 3 token vô danh.
```

Rule:

- Boss hiện tại của act có một `clue set` gồm 3 clue.
- Event có thể chứa một clue trong clue set đó.
- Combat/Elite có thể cho clue nếu encounter có dấu vết phù hợp.
- Shop có thể bán Boss Intel 1 lần như thông tin mua được.
- Khi player có đủ 3/3 clue, boss identity được reveal.
- Player vẫn có thể đánh boss với 0/3 clue; clue chỉ giúp chuẩn bị.

### 5B.6 Clue dùng chung giữa nhiều boss

Một clue không cần độc quyền cho một boss.

Ví dụ:

```text
Attacked Carriage
```

Có thể là clue cho:

```text
Dragon
Giant Beast
Werewolf Lord
Bandit Warlord
```

Vì nhiều thứ có thể tấn công xe ngựa.

Do đó mỗi boss cần 3 clue để tránh lộ quá sớm.

Thiết kế tốt:

```text
1 clue chung
+ 1 clue bán riêng
+ 1 clue signature
= reveal khá chắc boss identity
```

Ví dụ Dragon:

```text
Attacked Carriage
+ Half-eaten Remains
+ Burned Claw Marks
= Dragon rất có khả năng
```

Nếu chỉ có `Attacked Carriage`, player chưa biết chắc.

### 5B.6A Cấu trúc clue overlap giữa nhiều boss

Clue có thể chồng chéo giữa nhiều boss.

Mẫu logic:

```text
Boss A = clue 1 + clue 2 + clue 3
Boss B = clue 1 + clue 4 + clue 5
Boss C = clue 2 + clue 6 + clue 7
```

Ý nghĩa:

```text
clue 1 dùng chung giữa Boss A và Boss B
clue 2 dùng chung giữa Boss A và Boss C
nhưng chỉ đủ bộ 3 clue mới reveal đúng boss
```

Vì vậy một clue đơn lẻ không nên tự reveal boss.

Ví dụ fantasy:

```text
Dragon
= Attacked Carriage
+ Half-eaten Remains
+ Burned Claw Marks
```

```text
Giant Beast
= Attacked Carriage
+ Crushed Footprints
+ Broken Trees
```

```text
Devourer
= Half-eaten Remains
+ Acid Saliva
+ Burrow Holes
```

Trong ví dụ trên:

- `Attacked Carriage` dùng chung giữa Dragon và Giant Beast.
- `Half-eaten Remains` dùng chung giữa Dragon và Devourer.
- `Burned Claw Marks` mới là clue signature đẩy mạnh về Dragon.
- `Crushed Footprints` đẩy mạnh về Giant Beast.
- `Acid Saliva` hoặc `Burrow Holes` đẩy mạnh về Devourer.

Điểm thiết kế:

```text
1 clue = nghi ngờ rộng
2 clue = thu hẹp nhóm boss có thể là đúng
3 clue = reveal boss identity
```

Cách này giúp event hint xoay vòng được giữa nhiều boss mà không cần mỗi boss có toàn bộ event độc quyền.

### 5B.7 Event hint ví dụ — Dragon

Dragon clue set ví dụ:

| Clue | Event function | Ý nghĩa |
|---|---|---|
| `Attacked Carriage` | Loot Cache | Xe ngựa bị phá, hàng hóa còn lại, player loot được đồ |
| `Half-eaten Remains` | Corpse Scene / Risk Event | Người và ngựa bị ăn dở, cho thấy boss là predator lớn |
| `Burned Claw Marks` | Investigation Trace / Boss-related Trace | Vết móng vuốt cháy sém, signature rất mạnh của Dragon |

#### Event 1: Attacked Carriage

```text
Base Type:
Loot Cache

Modifier:
Boss-related Trace

Scene:
Một xe ngựa nằm nghiêng bên đường.
Thùng hàng bị phá tung.
Người đánh xe và con ngựa đã chết.
Một phần đồ đạc vẫn còn dùng được.

Gameplay:
Player có thể loot đồ.

Reward ví dụ:
+18 Gold
1 Utility Support hoặc 1 Combat Aid
1 cargo roll nhỏ

Clue:
Attacked Carriage
```

Event này chưa reveal Dragon vì boss khác cũng có thể tấn công xe ngựa.

#### Event 2: Half-eaten Remains

```text
Base Type:
Risk Event hoặc Investigation Event

Modifier:
Hidden Danger / Boss-related Trace

Scene:
Xác người và ngựa không chỉ bị giết.
Một phần đã bị ăn mất.
Xương lớn bị nghiền nát.

Gameplay:
Player có thể kiểm tra xác để lấy clue,
nhưng có thể bị mất HP / gặp scavenger ambush / dính stress/debuff nếu sau này có.

Reward ví dụ:
+1 Boss clue
35% chance bị ambush nhỏ
hoặc nhận 1 Combat Aid nếu search kỹ

Clue:
Half-eaten Remains
```

Event này nói boss có hành vi săn mồi/ăn thịt.

#### Event 3: Burned Claw Marks

```text
Base Type:
Choice Event hoặc Investigation Trace

Modifier:
Boss-related Trace + Elemental Trace

Scene:
Trên thành xe có các vết cào rất sâu.
Mé rìa vết cào bị cháy đen.
Gỗ không cháy lan như lửa thường, mà như bị nung từ chính vết móng.

Gameplay:
Player điều tra dấu vết.
Có thể nhận clue trực tiếp.
Nếu có Ice/Water/anti-Fire tool sau này, có thể mở lựa chọn phụ.

Reward ví dụ:
+1 Boss clue
Có thể bias reward sang Fire-related item hoặc anti-Fire utility

Clue:
Burned Claw Marks
```

Khi 3 clue này đi cùng nhau:

```text
Attacked Carriage
+ Half-eaten Remains
+ Burned Claw Marks
→ Reveal: Dragon
```

### 5B.8 Event hint ví dụ — Knight

Knight clue set ví dụ:

| Clue | Event function | Ý nghĩa |
|---|---|---|
| `Duel Remnants` | Loot Cache / Investigation | Tàn tích của một trận đấu danh dự |
| `Polished Armor Shards` | Rare Cache / Trace | Mảnh giáp được bảo dưỡng kỹ, hint boss giáp nặng |
| `Oath Marks` | Choice Event / Intel Clue | Dấu ấn lời thề, luật danh dự, hint mechanic Guard/retaliation |

#### Event 1: Duel Remnants

```text
Base Type:
Loot Cache

Modifier:
Intel Clue

Scene:
Một vòng tròn đấu tay đôi được vạch trên đất.
Có vết máu, kiếm gãy và vài mảnh giáp rơi lại.
Không giống một vụ phục kích; giống một nghi thức hơn.

Reward:
+10-20 Gold
có thể nhận 1 Skill Physical / Guard utility

Clue:
Duel Remnants
```

#### Event 2: Polished Armor Shards

```text
Base Type:
Rare Cache hoặc Loot Cache

Modifier:
Boss-related Trace

Scene:
Bạn tìm thấy những mảnh giáp vỡ.
Dù nằm trong bùn, chúng vẫn được đánh bóng kỹ.
Có dấu khắc cùng một huy hiệu trên từng mảnh.

Reward:
1 cargo roll nghiêng về Guard / Sunder / anti-Guard
hoặc 1 Rare chance nhỏ

Clue:
Polished Armor Shards
```

#### Event 3: Oath Marks

```text
Base Type:
Choice Event

Modifier:
Intel Clue / Boss-related Trace

Scene:
Trên đá có khắc lời thề chiến đấu.
Những kẻ phá luật bị gạch tên.
Những kẻ chiến thắng được ghi danh.

Choices:
Read the oath
→ nhận clue

Offer respect
→ nhận blessing nhỏ hoặc Guard cho combat sau

Deface the oath
→ nhận Gold/loot nhưng tăng rủi ro ambush hoặc debuff

Clue:
Oath Marks
```

Khi 3 clue này đi cùng nhau:

```text
Duel Remnants
+ Polished Armor Shards
+ Oath Marks
→ Reveal: Knight
```

### 5B.9 Event generation trong act

Event generation nên làm theo hướng:

```text
Act chọn boss ẩn ở đầu act
→ boss có clue set 3 clue
→ map spawn một số event có khả năng chứa clue
→ các clue được gắn vào event hợp lý
→ player có thể gặp 0-3 clue tùy route
```

Không nên spawn clue vô nghĩa không liên quan cảnh.

Ví dụ sai:

```text
Event: Frozen Shrine
Clue: Burned Claw Marks
```

Trừ khi có lý do rất rõ.

Ví dụ đúng:

```text
Event: Clawed Carriage
Clue: Burned Claw Marks
```

```text
Event: Duel Remnants
Clue: Oath Marks
```

### 5B.10 Event UI / readability

Event UI phải làm rõ:

```text
Event title
Scene text ngắn
Choice list
Reward preview nếu biết
Risk preview nếu có thể đọc
Clue gained nếu đã nhận
```

Không nên giấu mọi thứ dưới text mơ hồ.

Ví dụ UI sau khi nhận clue:

```text
Boss Clue Found:
Burned Claw Marks

Boss Intel:
2 / 3
```

Nếu clue chỉ là hint chưa định danh, có thể hiện:

```text
You found a boss trace.
Boss Intel +1
```

Nhưng trong log/history nên lưu tên clue cụ thể để player nhớ.

### 5B.11 Event và Boss Preparation

Event là node mới nên khi resolve lần đầu:

```text
+1 Boss Preparation
```

Theo rule map hiện tại, event giống Combat/Elite/Shop/Rest ở chỗ resolve node mới làm boss có thêm thời gian chuẩn bị.

Ý nghĩa:

- đi thêm event giúp player có loot/clue,
- nhưng cũng làm tăng Boss Preparation,
- player phải cân giữa hiểu boss / mạnh hơn / boss chuẩn bị hơn.

Event không được là value miễn phí tuyệt đối.

### 5B.12 Event guardrails

Event không nên:

- luôn cho reward lớn không cost,
- luôn là bẫy ngẫu nhiên,
- bắt player đọc lore dài mới hiểu reward,
- tách Boss Intel thành token vô danh,
- làm map thành investigation game quá nặng,
- ép player phải có 3/3 clue mới được đánh boss.

Event nên:

- ngắn,
- rõ function,
- có hình ảnh mạnh,
- có reward/cost phù hợp,
- thỉnh thoảng có clue gắn với boss,
- phục vụ run loop `Combat → Reward / Shop / Progression → Chỉnh build → Combat khó hơn`.

### 5B.13 Template để thêm từng event cụ thể sau này

Sau này khi thêm reward cho từng event một, mỗi event nên được viết theo template này để không mất thông tin:

```text
Event ID:
Event Name:
Biome / Act:
Base Type:
Modifier:
Combat:
    None / Normal Event Combat / Elite Event Combat / Boss-class Event Combat
Boss Clue:
    None / clue name
Scene:
Choices:
Rewards:
Costs / Risks:
Boss Preparation:
Notes:
```

Giải thích từng field:

| Field | Ý nghĩa |
|---|---|
| `Event ID` | ID nội bộ để data dễ gọi |
| `Event Name` | tên hiển thị cho player |
| `Biome / Act` | event xuất hiện ở vùng nào, nếu có giới hạn |
| `Base Type` | Loot Cache / Choice / Trade / Risk / Rest-like / Combat Ambush / Shop-like |
| `Modifier` | Intel Clue / Hidden Danger / Corrupted Loot / Blessed Loot / Boss-related Trace / Elemental Trace / Rare Cache |
| `Combat` | event có sinh combat không; nếu có thì dùng gacha theo độ khó |
| `Boss Clue` | clue cụ thể nếu event chứa clue |
| `Scene` | mô tả hình ảnh ngắn, đủ mạnh để player nhớ |
| `Choices` | các lựa chọn của player |
| `Rewards` | reward chính xác của từng choice |
| `Costs / Risks` | HP, Gold, ambush, debuff, downside |
| `Boss Preparation` | thường là +1 khi resolve event lần đầu |
| `Notes` | guardrail riêng, ví dụ không spawn nếu boss không phù hợp |

Ví dụ template điền cho Dragon:

```text
Event ID:
dragon_clawed_carriage

Event Name:
Clawed Carriage

Biome / Act:
Any road / wilderness-like act

Base Type:
Loot Cache

Modifier:
Boss-related Trace

Combat:
None

Boss Clue:
Attacked Carriage hoặc Burned Claw Marks tùy version event

Scene:
Một xe ngựa bị xé nát bên đường.
Người đánh xe và ngựa đã chết.
Một phần hàng hóa vẫn còn nguyên.
Trên gỗ có vết móng vuốt cháy sém.

Choices:
1. Loot the cargo
2. Inspect the claw marks
3. Leave

Rewards:
Loot the cargo:
- +15-25 Gold
- 1 Utility Support hoặc Combat Aid

Inspect the claw marks:
- nhận clue Burned Claw Marks
- có thể nhận ít Gold hơn hoặc không nhận cargo

Leave:
- không nhận gì

Costs / Risks:
Không có hoặc có Hidden Danger nếu muốn spawn scavenger

Boss Preparation:
+1 khi resolve event lần đầu

Notes:
Attacked Carriage có thể dùng chung với Giant Beast.
Burned Claw Marks là clue nghiêng mạnh về Dragon.
```

Ví dụ template cho event combat:

```text
Event ID:
abandoned_camp_ambush

Event Name:
Warm Camp

Base Type:
Combat Ambush Event

Modifier:
Hidden Danger

Combat:
Normal Event Combat

Boss Clue:
None hoặc một clue nếu camp có dấu vết boss

Scene:
Một trại bỏ hoang, lửa vẫn còn ấm.
Có tiếng động rất nhẹ trong lều.

Choices:
1. Take visible supplies
2. Search the tents
3. Leave

Rewards:
Take visible supplies:
- +8-12 Gold
- không combat

Search the tents:
- spawn Normal Event Combat
- thắng combat nhận Combat gacha: 3 card, chọn 1
- nếu event data muốn, có thể thêm 1 package nhỏ

Costs / Risks:
Search the tents có thể bắt đầu combat ngay.

Boss Preparation:
+1 khi resolve event lần đầu

Notes:
Vì combat đã cho gacha, không tự động cho thêm reward lớn nếu không ghi rõ.
```

Nguyên tắc cập nhật về sau:

- Nếu event có reward riêng, ghi reward đó trong chính event.
- Nếu event có combat, ghi rõ combat dùng bảng gacha nào.
- Nếu event có clue, ghi rõ clue name.
- Không để reward/clue nằm trong trí nhớ chat mà không ghi vào file.

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

### 6.3 Shop inventory hiện tại

Mỗi lần shop generate inventory, shop bán:

- `7 skill`
- `5 consumable`
- `1 dice`

Các con số này là current direction cho shop inventory cơ bản, chưa phải balance cuối.

### 6.4 Shop refresh

Shop không refresh inventory mỗi lần player mở lại shop.

Shop refresh sau `6 lần di chuyển trên map`, dùng cùng kiểu clock với hệ `Boss Preparation`.

Ý nghĩa:

- player không thể spam mở shop để reroll miễn phí,
- shop vẫn có nhịp thay đổi theo hành trình trong act,
- việc đi thêm node để chờ shop refresh phải là một trade-off vì cũng đẩy clock của boss / act pressure.

### 6.5 Dice bán trong shop

Dice bán trong shop là dice random theo số mặt của chính nó.

Rule:

- mỗi mặt dice được random giá trị trong khoảng `1` đến `số mặt của dice đó`,
- giá trị các mặt được phép trùng nhau,
- không mặt nào được vượt quá số mặt tự nhiên của dice.

Ví dụ `d4`:

```text
3 / 3 / 2 / 4
```

Đây là dice hợp lệ vì mọi mặt đều nằm trong khoảng `1-4`.

Ví dụ không hợp lệ cho `d4`:

```text
3 / 5 / 2 / 4
```

Vì `5` vượt quá số mặt của `d4`.

Tương tự:

- `d6`: mỗi mặt random `1-6`
- `d8`: mỗi mặt random `1-8`
- `d12`: mỗi mặt random `1-12`

Ý nghĩa thiết kế: dice shop tạo biến thể khác nhau nhưng vẫn nằm trong identity tự nhiên của loại dice đó. Dice mua từ shop chưa phải dice đã được forge / custom vượt giới hạn.
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
- full content pool cụ thể cho từng reward Purpose,
- shop inventory math,
- reroll cost,
- tiền tệ chi tiết,
- encounter pacing theo từng floor/map.

Nhưng các phần sau là current direction mạnh:

- run loop = `Combat → Reward / Shop / Progression → Chỉnh build → Combat khó hơn`,

- reward sau combat đã có rate hiện tại cho `Combat / Elite / Boss`,
- reward sau combat dùng `Purpose` để đọc nhanh vai trò của reward,
- `Economy` tối đa 1 card mỗi reward roll; Purpose khác tối đa 2,
- Boss reward có guarantee ít nhất 2 card `Rare/Special`,
- event được cấu trúc bằng `Event Base Type + Event Modifier`,
- event có thể vừa cho loot/reward vừa chứa boss clue,
- Boss Intel nên được biểu diễn bằng `3 clue cụ thể` theo từng boss thay vì token vô danh,
- combat phát sinh từ event vẫn dùng reward gacha theo độ khó combat,
- mỗi boss nên có 3 clue; clue có thể chồng chéo giữa nhiều boss theo cấu trúc `Boss A = clue 1 + clue 2 + clue 3`,
- từng event cụ thể về sau phải ghi rõ `Base Type`, `Modifier`, `Combat tag`, `Boss Clue`, `Rewards`, `Costs/Risks`,
- build shaping là mục tiêu chính của progression,
- thua run mất tiến trình run nhưng giữ **unlock progress**,
- chiến thắng boss cuối có thể dẫn sang **Endless Mode**,
- reward / shop / unlock / relic pool phải cùng phục vụ cảm giác “điêu khắc build”.
