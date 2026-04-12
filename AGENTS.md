# AGENTS_LOGIC_ONLY.md

> Đây là bản **logic + content handoff file** của project.
> Mục tiêu: gửi sang chat AI khác hoặc cho người khác đọc nhanh mà vẫn hiểu đúng gameplay logic, skill/passive hiện tại, và triết lý thiết kế.
> File này **không chứa code architecture, class name, folder structure, refactor state, implementation note**.
> Nếu cần hiểu codebase hoặc implementation state thì phải đọc `PROJECT_META.md`.

---

## 1. Mục đích của file này

File này dùng cho 5 việc:

1. Giải thích game này là game gì và cảm giác chơi mong muốn là gì.
2. Ghi lại các rule gameplay đã chốt: dice, focus, skill, status, lane, payoff.
3. Ghi lại toàn bộ skill/passive hiện tại với context thiết kế.
4. Ghi lại triết lý build, boss, relic và content direction hiện tại.
5. Làm handoff document để gửi sang chat khác mà không cần paste file master dài.

---

## 2. Tổng quan project

Đây là game **roguelike turn-based mobile** với mô tả đúng nhất hiện tại là:

**Dice-driven Tactical Combat Roguelike**

### Platform

**PC trước, mobile sau** — giống Balatro và STS. Design và playtest trên PC cho đến khi combat feel ổn, sau đó mới port lên mobile.

### Nguồn cảm hứng chính

- **Balatro**: passive kiểu joker, build engine, custom dice giống custom deck, consumable có tính bẻ hướng run, endless mode.
- **Slay the Spire**: combat theo nhịp turn rõ ràng, intent system, boss là bức tường cơ chế thay vì chỉ là bao cát máu lớn.
- **Persona / Expedition 33**: loadout kỹ năng rõ ràng, decision-based combat.
- **D&D**: crit/fail, giá trị mặt xúc xắc là identity thật của hành động, exact value matters.

### Bản sắc cốt lõi

Game xoay quanh 5 trụ chính: dice, skill slot, passive, status/payoff, lane order/action order.

**Dice là trung tâm, mọi thứ còn lại phải xoay quanh dice.**

### Trải nghiệm mong muốn mỗi turn

1. Dùng die nào cho skill nào
2. Dùng thứ tự nào
3. Setup trước hay nổ trước
4. Chấp nhận lock plan và sống với kết quả roll đó

---

## 3. Core Loop

**Roll dice → Plan → Lock → Execute → Enemy turn**

- **Roll**: roll tất cả dice hiện đang equip cho turn đó.
- **Plan**: kéo dice vào skill slot, chọn cách gán, có thể reorder cặp hành động trong planning.
- **Lock**: xác nhận kế hoạch, từ thời điểm này không đổi thứ tự nữa.
- **Execute**: các action chạy từ trái sang phải theo lane hiện tại.
- **Enemy turn**: enemy hành động, status tick, cleanup, chuẩn bị sang lượt mới.

---

## 4. Action Economy và Loadout

### 4.1 Những gì player có trong combat

- **6 skill slot**
- **1 passive slot**
- **1 đến 3 dice** (đầu run = 1 dice)

Số dice equip = số action groups mỗi turn.

### 4.2 Focus economy

Rule đã chốt:

- **Max Focus = 9**
- **Start combat = 2 Focus**
- **Đầu mỗi turn = +1 Focus**
- **Không được nợ Focus**
- Turn 1 thực tế = **3 Focus**

### 4.3 Basic actions

Luôn có sẵn, không nằm trong 6 skill slot chính:

- **Basic Attack**: 0 Focus, 4 damage cố định, +1 Focus
- **Basic Guard**: 0 Focus, Guard = Base Value của die dùng cho action đó

Vai trò: luôn cho player lựa chọn hợp lệ khi roll xấu hoặc thiếu Focus.

### 4.4 Loadout ngoài combat

- 6 skill slot: swap tự do ngoài combat, mua ở shop, bán ngoài combat.
- 1 passive slot: swap tự do ngoài combat.
- 1-3 dice slot: số dice equip = số action/turn.
- Basic Attack + Basic Guard luôn tồn tại, không thể bỏ.

---

## 5. Dice System — Rule quan trọng nhất

### 5.1 Base Value và Added Value

- **Base Value** = mặt thật roll ra
- **Added Value** = phần cộng thêm vào output cuối
- **Mọi condition phải đọc từ Base Value**
- Added Value không làm thay đổi bản chất của die

Các condition phải đọc theo Base Value: chẵn/lẻ, crit/fail, exact number, ≤ hoặc ≥ ngưỡng, die cao nhất/thấp nhất.

### 5.2 Local die context

Skill 2-3 slot có text "die cao nhất / thấp nhất / exact value trong nhóm" → chỉ xét trong nhóm dice của chính skill đó, không xét toàn bàn.

### 5.3 Crit / Fail

- **Crit** = roll trúng giá trị mặt cao nhất của die
- **Fail** = roll trúng giá trị mặt thấp nhất của die
- Crit / Fail không đổi Base Value, chỉ tạo bonus/penalty lên output

Hệ số đã chốt:

- **Crit thường = +20% Base**
- **Crit Physical = +50% Base**
- **Fail = -50% Base**

### 5.4 Trường hợp dice đã custom mạnh

- Nhiều mặt cùng max → tất cả là Crit
- Nhiều mặt cùng min → tất cả là Fail
- Nếu max == min → tất cả là Crit, không có Fail (Crit thắng Fail)

### 5.5 Làm tròn và minimum output

- Toàn game dùng **floor**
- Damage sau tính toán nếu còn dương mà < 1 → **minimum 1**
- Guard chặn hết → có thể không mất HP

### 5.6 Dice customization

- Từng mặt dice có thể chỉnh Base Value bằng consumable
- Đơn vị tăng trưởng cơ bản: **+1 base cho 1 mặt**
- Giá trị max 1 mặt: **99**
- Kể cả d2 có thể thành `1 / 99` nếu đầu tư đủ sâu
- Custom dice thay đổi toàn bộ logic crit/fail, parity, exact, threshold

### 5.7 Reorder trong Planning

Player có thể kéo **cặp dice + skill** sang vị trí khác trong Planning.
Reorder ảnh hưởng đồng thời tới dice assignment và thứ tự execute.
Khi đã vào Execute phase thì reorder bị khóa.

Ý nghĩa thiết kế: dùng lane đầu để setup trạng thái/phá Guard, lane cuối làm finisher bằng die mạnh.

---

## 6. Turn Flow — Rule đã chốt

1. **Start phase**: +1 Focus, passive đầu lượt, enemy chốt intent
2. **Roll phase**: roll các dice đang có
3. **Planning phase**: gán dice vào skill, reorder được
4. **Execution phase**: chạy trái sang phải theo lane hiện tại, khóa reorder
5. **Enemy / End phase**: enemy hành động, status tick và cleanup, Guard của player thường biến mất cuối lượt trừ khi có rule giữ Guard

---

## 7. Core Combat Rules

### 7.1 Damage và Guard

- Damage trừ Guard trước, phần dư mới trừ vào HP.

### 7.2 Stagger

- Khi Guard từ > 0 về 0 → mục tiêu vào **Stagger**
- Overflow của hit phá Guard vẫn vào HP bình thường, không được ×1.2
- Chỉ **hit kế tiếp duy nhất** ăn **×1.2 tổng damage**
- Hết turn không có hit bồi → Stagger biến mất
- Lightning shock không consume Stagger
- Bleed tick không consume Stagger
- Burn consume trong direct-hit → tính vào tổng damage trước khi nhân 1.2

### 7.3 Skill tags nền tảng

- `Attack`, `Guard`, `Status`, `Sunder`, `Buff`, `Debuff`
- **Sunder không có hidden bonus** — tương tác đặc biệt phải viết rõ

---

## 8. 5 hệ chính và các effect cốt lõi

### 8.1 Physical

- Burst thẳng, anti-Guard, damage đơn giản
- **Crit Physical = +50% Base**

### 8.2 Fire / Burn

Burn **không phải DoT chính**. Burn là **tài nguyên để consume**.

- Burn có stack
- Consume mặc định = **+2 damage mỗi stack Burn bị xóa**
- Chỉ skill đặc biệt mới override baseline này

### 8.3 Ice / Freeze / Chilled

- **Freeze**: skip 1 turn
- Hết Freeze → thành **Chilled** (tồn tại 2 turn)
- Đang Freeze hoặc Chilled → miễn Freeze mới
- Ice damage hit vào target Freeze/Chilled → player **+1 Focus +3 Guard**

### 8.4 Lightning / Mark

Mark là weak point để direct-hit khai thác. Mark **không stack**.

- **Non-Lightning hit vào Mark**: +3 direct damage lên chính mục tiêu đó
- **Lightning hit vào Mark**: hit chính damage bình thường → proc **3 damage all enemies**
- Shock phụ của Lightning: không cộng Added Value, không tiêu Mark, không proc Mark, không chain tiếp
- AoE Lightning hit nhiều target có Mark → mỗi target tạo 1 shock proc, chạy tuần tự, cách nhau 0.2s

### 8.5 Bleed

- Gây damage **đầu lượt**, bỏ qua Guard
- Giảm dần: **-1 stack mỗi cuối lượt**

---

## 9. Ailment

Ailment hiện được xem là **enemy-side system**.

- Enemy là bên chủ yếu dùng ailment lên player
- Player-side ailment không phải trục gameplay ưu tiên
- **Enemy → player = 100% chance** hiện tại

---

## 10. Lane Mapping / Reorder / Pair Identity Rule

**`1/2/3` là identity ban đầu. `A/B/C` là lane hiện tại. Logic phải đọc theo `A/B/C`, không đọc theo `1/2/3`.**

Điều cấm kỵ:

- Không đọc execute order theo 1/2/3 cố định
- Không để UI reorder chỉ đổi hình mà logic không đổi lane thật
- Không đập lại reorder chỉ vì muốn refactor nếu chưa có bug rõ

---

## 10.1 Row / Target taxonomy note

`Row` và `target tag` là lớp gameplay thật, không phải chỉ để UI tô sáng.

Target taxonomy đang dùng:

- `Self`
- `Single Enemy`
- `Single Ally`
- `Row Enemies`
- `Row Allies`
- `All Enemies`
- `All Allies`

Rule đọc:

- `Row Enemies` / `Row Allies` = đúng 1 row
- `All Enemies` / `All Allies` = toàn phe đó
- `Strike / Range` là lớp riêng, quyết định target có bị front row chặn hay không

Lưu ý gameplay rất quan trọng:

- row của player hiện cũng là combat state thật
- skill/passive có thể check player đang ở `Front Row` hay `Back Row`
- enemy move / boss move cũng có thể đọc row này
- player cũng phải thấy khi enemy đang chuẩn bị đòn nhắm `Front Row` hoặc `Back Row`

---

## 11. Relic / Consumable System

- Player có **3 relic slot**
- Relic là **one-shot consumable** — dùng xong mất khỏi slot
- Pool relic lớn là chủ đích — tìm đúng relic cần là chuyện khó (giống Balatro tarot)
- Drop thường xuyên từ enemy hoặc passive/skill

Pool hiện tại đã chốt ở mức framework:

- **22 consumable**
- chia thành **11 Zodiac / 5 Seals / 6 Runes**
- tất cả dùng chung **3 shared slot**

**Type A — dùng trong combat**: quick action, không chiếm dice slot, có thể cần chọn target.

**Type B — chỉnh dice ngoài combat**: sculpt dice, copy/paste face, gắn enchant, chỉnh Base/Added theo đúng rule của Zodiac.

### 11.1 Zodiac — 11 cái

- `Adjust Face (+)`: chọn `1-3 mặt` trên cùng `1 dice`, tất cả `+1 Base`, permanent
- `Adjust Face (-)`: chọn `1-3 mặt` trên cùng `1 dice`, tất cả `-1 Base`, permanent
- `Copy / Paste Face`: copy `Base + Added + enchant` từ `1 mặt nguồn` sang `1 mặt đích`, permanent
- `Double Value`: chọn `1 dice`, trong turn đó **mặt thực tế được roll ra** nhận nhân đôi `Base + Added`
- `Value +N`: hiện tại `N = 3`, thêm `+3 Added Value` lên `1-2 mặt`, permanent
- `Guard Boost`: gắn enchant lên `1-2 mặt`, permanent
- `Gold Proc`: gắn enchant lên `1-2 mặt`, permanent
- `Fire`: gắn enchant lên `1-2 mặt`, permanent
- `Ice`: gắn enchant lên `1-2 mặt`, permanent
- `Bleed`: gắn enchant lên `1-2 mặt`, permanent
- `Lightning`: gắn enchant lên `1-2 mặt`, permanent

### 11.2 Seals — 5 cái

- `Ignite Spread` (Fire): dàn Burn từ `1 mục tiêu` sang các mục tiêu khác
- `Cryostasis` (Ice): đòn đánh tiếp theo vào player gây `0 damage`, kẻ gây damage bị `Freeze 1 lượt`
- `Exploit Mark` (Mark): mỗi `Mark` trên enemy tạo `1 consumable`, tối đa `4`
- `Exsanguinate` (Bleed): tiêu Bleed trên `1 mục tiêu` để hồi HP bằng lượng Bleed đã tiêu
- `Final Verdict` (Physical): gây `20 damage` thuần lên `1 mục tiêu`

### 11.3 Runes — 6 cái

- `Restore Focus`: `+3 Focus`
- `Heal`: `+8 HP`
- `Cheat Death`: dùng chủ động để hồi `4 HP`; nếu giữ tới lúc chết thì tự vỡ và cứu player ở `1 HP`
- `Double Gold`: nhân đôi `Gold hiện có`, lượng tăng thêm tối đa `+30`
- `Create Last Used`: tạo lại loại consumable vừa dùng gần nhất
- `Cleanse`: xóa tất cả hiệu ứng bất lợi hiện có trên player

### 11.4 Enchant pool hiện tại

Enchant pool lấy từ Zodiac và hiện có 7 loại:

- `Value +N`
- `Guard Boost`
- `Gold Proc`
- `Fire`
- `Bleed`
- `Lightning`
- `Ice`

4 trục hiện tại của face enchant:

- `Value` = số: `Value +N`
- `Resource` = tài nguyên: `Guard Boost`, `Gold Proc`
- `Seed` = áp status: `Fire`, `Bleed`
- `Rewrite` = đổi identity của mặt: `Lightning`, `Ice`

Rule runtime mong muốn hiện tại:

- enchant gắn `1-2 mặt`, permanent
- enchant trigger **ngay khi face roll ra**
- enchant **độc lập hoàn toàn với skill**
- face không có skill vẫn trigger enchant
- face có skill thì enchant trigger riêng, không chờ skill
- enchant mới ghi đè enchant cũ trên mặt đó
- `Fire / Bleed` target random trong `all alive enemies`
- `Lightning` không áp status; nó rewrite cách mặt dice được đọc cho condition
- `Ice` là enchant kiểu stone-like: luôn cho `+5 Added Value`, nhưng không còn là mặt số bình thường

---

## 12. Boss Design Philosophy

- Boss là **bức tường cơ chế**, không phải stat wall
- Boss **không hard counter hoàn toàn** bất kỳ build nào
- Boss tạo **friction** — Fire build vẫn thắng được nhưng nhọc hơn
- Player luôn có ít nhất 3 hướng: adapt, chấp nhận khó hơn, brute force nếu build đủ mạnh

Ví dụ đúng tinh thần:
- Không nên: `miễn Burn hoàn toàn`
- Nên: `đầu lượt tự clear 2 Burn stack`
- Không nên: `reflect mọi damage`
- Nên: `mỗi khi nhận Burn thì +2 Guard`

---

## 13. Endless Mode và Hidden Boss

### Endless Mode

Sau khi thắng boss cuối, player có thể tiếp tục.

3 loại player được serve:
- **Player A**: thắng boss sớm, build chưa xong → Endless để tiếp tục
- **Player B**: thắng boss đúng lúc → kết thúc tự nhiên
- **Player C**: muốn perfect build trước khi vào boss → grind Endless trước

### Hidden Boss

- **Chỉ xuất hiện trong Endless mode**
- Placement: đủ sâu để không thấy ngay, không quá sâu đến mức full build cũng không tới
- Mechanic: **rotate ability của các boss khác mỗi 2-3 turn**
- Không có tất cả ability cùng lúc — lấy từ pool toàn bộ boss trong game
- Đây là thử thách tối thượng của game

---

## 14. Unlock System

Giống Balatro — skill và passive không phải lúc nào cũng có sẵn trong pool ngay từ đầu. Player phải đạt điều kiện cụ thể để unlock, sau đó mới có cơ hội thấy nó xuất hiện trong run.

Ý nghĩa thiết kế:

- Kiểm soát tốc độ lộ mechanic — player không bị overwhelm bởi toàn bộ 5 hệ ngay run đầu
- Unlock condition gắn với gameplay thật, không phải grind số
- Skill phức tạp / cần setup sâu chỉ xuất hiện khi player đã chứng minh hiểu cơ chế liên quan
- Tăng replay value — mỗi lần unlock là discovery moment

Ví dụ định hướng:

- **Hellfire** — unlock khi có 30 Burn trên 1 kẻ địch (chứng minh hiểu Fire engine trước khi thấy payoff lớn nhất)
- Balatro tương đương: Brainstorm yêu cầu bỏ 1 Royal Flush để unlock

Unlock condition chưa được thiết kế đầy đủ cho toàn bộ skill pool — đây là phần còn mở.

## 14.1 Map Structure Note

Map hiện tại đi theo hướng:

- mỗi act là một `node graph` đi từ `đáy lên đỉnh` như STS
- player bắt đầu ở `Start / Hub / Forge` ở đáy map
- `Boss` luôn nằm sẵn ở đỉnh map
- player có thể đi nhanh lên boss hoặc backtrack trong phần đường đã mở để tối ưu tài nguyên trong act hiện tại

Các loại node hiện tại:

| Node | Icon | Vai trò ngắn |
|---|---|---|
| Combat | 💀 | encounter thường |
| Elite | ☠️ | encounter khó hơn, reward cao hơn |
| Event | 📜 | event ngắn, quyết định nhanh |
| Shop | 🛒 | mua `skill / relic / consumable / dice`, và mua `Boss Intel` đúng `1 lần` |
| Rest | 🛏️ | hồi phục |
| Hub / Forge | 🔨 | điểm xuất phát ở đáy act, forge `whole-die tag / màu` cho dice bằng `gem` |
| Boss | 👹 | final boss của act |

Rule map rất quan trọng:

- `Hub / Forge` và `Shop` là 2 node khác nhau
- node đã clear / đã mở đường sẽ thành `hình tròn rỗng`, đi qua chỉ là di chuyển
- backtrack chỉ trong `act hiện tại`
- boss luôn nhìn thấy trên map, nhưng `Boss Intel` quyết định player có biết boss đó là ai hay không
- `3/3 intel` chỉ để reveal boss identity, không khóa quyền vào đánh boss
- kể cả `0/3 intel` vẫn có thể đi đánh boss nếu đi tới được
- intel hiện đến từ `Combat / Elite / Event`, và `Shop` có thể bán `1 lần`
- lần 1, 2, 3 gặp boss đó vẫn theo rule đầy đủ; từ lần 4 trở đi chỉ cần `1/3 intel`
- player có thể chạy khỏi combat nếu roll đạt điều kiện chạy; điều kiện cụ thể chưa chốt
- nếu player chạy thì node chưa tính thắng và quay lại enemy full HP
- nếu enemy tự chạy thì vẫn tính là player thắng, có reward, và lần gặp lại encounter mạnh hơn / reward cao hơn

---

## 15. Lenticular Design / Build Philosophy

### Nguyên tắc cốt lõi

Không làm 20 skill = 20 build rời nhau. Làm theo kiểu skill/passive tương tác với nhau → build nảy sinh từ mạng lưới tương tác.

### Phân lớp rarity

- **Common**: tự đủ dùng, ít cần setup, mạnh ổn định ngay.
- **Uncommon**: mạnh hơn khi canh điều kiện, đúng tình huống thì bùng rõ.
- **Rare**: một mình vẫn hữu ích; khi đúng engine thì cực mạnh.

### Trục tương tác chính

**Trục dice**: Crit/Fail, Chẵn/Lẻ, Cao/Thấp, Exact value, Highest/Lowest trong nhóm

**Trục trạng thái trên target**: Burn stacks, Mark, Freeze/Chilled, Bleed stacks, Stagger

**Trục tài nguyên player**: Focus hiện tại, Guard hiện tại, lane đã dùng/còn trống

---

## 15.1 Condition Standard — khung để author 80%-85% skill

Mục tiêu của `Condition` **không phải** là diễn tả mọi skill trong game.
Mục tiêu đúng là:

- cover được khoảng `80%-85%` skill bằng authoring chuẩn,
- chỉ những skill thật sự đặc biệt mới cần custom code,
- tránh việc mỗi skill lại phải code lại toàn bộ effect từ đầu.

### Condition là gì trong game này

`Condition` là **hệ quy chiếu để đọc / chọn / phân vai cho dice, target, resource, board state**.

`Condition` **không phải payoff cố định**.

Điều này có nghĩa:

- `chẵn` không đồng nghĩa với `bonus Burn`,
- `lẻ` không đồng nghĩa với `bonus damage`,
- `highest` không đồng nghĩa cố định với `Guard`,
- `lowest` không đồng nghĩa cố định với `Burn`.

`Condition` chỉ trả lời:

- đang đọc trục nào,
- đang chọn die nào,
- đang nhìn target/state nào,
- khi nào skill được mở payoff phụ.

Còn payoff thực sự phải nằm ở `Effect / Outcome`.

### Công thức authoring chuẩn cho đa số skill

Một skill thông thường nên được ghép từ 4 lớp:

1. `Core`
   - element
   - tag
   - slot cost
   - focus cost
   - target rule
   - base output như `flat damage`, `flat guard`, `flat status`, hoặc `X`

2. `Optional Condition`
   - có thể không có
   - nếu có thì thường chỉ nên có `1 condition chính`

3. `Effect / Outcome`
   - deal damage
   - apply Burn / Freeze / Mark / Bleed
   - gain Guard / Focus
   - consume status
   - grant Added Value
   - split outcome theo die được chọn

4. `Custom Hook`
   - chỉ dùng khi skill thực sự vượt khỏi khung chuẩn

### 7 trục condition chuẩn cho skill

Skill condition nên bị khóa gọn theo 7 trục sau:

#### 1. Parity

- `Even`
- `Odd`
- Tự hiểu:
- `1 slot` = die này chẵn/lẻ
- `nhiều slot` = cả cụm đều chẵn/lẻ

#### 2. Crit / Fail

- `Crit`
- `Fail`
- Tự hiểu:
- `1 slot` = die này crit/fail
- `nhiều slot` = cả cụm đều crit/fail

#### 3. Exact value

- `Die Equals X`
- `Group Contains Pattern`
- `Random Exact Number Owned`
- `Random Exact Number Random`

#### 4. Local group / slot relation

- `Self-position`
- `Neighbor relation`
- `Split-role`
- `Self-position` chỉ dùng `Left / Right`
- `Neighbor relation` chỉ dùng `Left / Right`
- `Split-role` chỉ dùng `Highest / Lowest`

#### 5. Effect / Target State

- `Target Has Burn`
- `Target Has Mark`
- `Target Has Freeze`
- `Target Has Chilled`
- `Target Has Bleed`
- `Target Has Stagger`
- `Status History (TODO)` chưa có runtime logic, chỉ note để mở rộng sau

#### 6. Resource axis

- `Current Focus`
- `Player Guard`
- `Target Guard`

#### 7. Board / encounter axis

- `Alive Enemies Count`
- `Enemies With Status Count`

### Condition không nên ôm những gì

Không nên cố nhét vào condition system các case như:

- skill có state machine riêng qua nhiều lần dùng,
- skill alternate mode qua từng lần dùng,
- propagation / retrigger / copy logic quá đặc thù,
- rule rewrite làm đổi hẳn cách resolve của game,
- timing đặc biệt không khớp flow chuẩn,
- effect nhiều bước cần lưu state riêng cho chính skill.

Các case đó nên đi vào:

- `Effect / Outcome layer`,
- hoặc `Custom Hook`.

### Quy tắc quyết định có cần code riêng hay không

Nếu một skill có thể mô tả bằng:

- `1-2 reference`,
- `1 condition chính`,
- `1-2 outcome chuẩn`,

thì **không được code riêng**.

Chỉ cho phép custom code khi skill cần ít nhất một trong các thứ sau:

- state riêng của chính skill qua nhiều lần dùng,
- timing đặc biệt không khớp flow chuẩn,
- rule rewrite lên die/action/board,
- split-role quá đặc thù không map nổi vào branch chuẩn,
- propagation / retrigger / copy logic đặc biệt.

### Rule authoring rất quan trọng

- `Condition` mặc định phải đọc từ `Base Value`.
- `Added Value` là lớp thưởng của output, không phải input mặc định của condition.
- `Single-output skill` được phép dùng `Total Added Value` của action.
- `Split-role skill` phải resolve theo `per-die Base/Added/Crit/Fail` của đúng die được chọn cho từng branch.
- Một skill thường chỉ nên có `1 condition chính`.
- Rare rất đặc biệt mới nên có `2 condition đáng kể`.

### Bộ preset nên có trên Inspector

Inspector không nên bắt đầu bằng raw clause kỹ thuật.
Nó nên bắt đầu bằng `preset theo hệ`, sau đó mới cho advanced override nếu thật sự cần.

Ví dụ preset đủ dùng:

#### Fire

- Any Base Odd
- Any Base Even
- Exact All Bases = N
- Highest Base = N
- Lowest Base = N
- Occupied Slots = N
- Remaining Slots = N
- Target Has Burn

#### Ice

- Any Base Odd
- Any Base Even
- Exact All Bases = N
- Highest Base = N
- Lowest Base = N
- Occupied Slots = N
- Target Is Frozen
- Target Is Chilled

#### Lightning

- Any Base Odd
- Any Base Even
- Any Die Crit
- Any Die Fail
- Highest Base = N
- Lowest Base = N
- Target Has Mark
- Marked Enemies >= N

#### Physical

- Any Base Odd
- Any Base Even
- Any Die Crit
- Any Die Fail
- Highest Base = N
- Lowest Base = N
- Current Focus >= N
- Occupied Slots = N
- Remaining Slots = N

#### Bleed

- Any Base Odd
- Any Base Even
- Any Die Crit
- Any Die Fail
- Highest Base = N
- Lowest Base = N
- Target Has Bleed
- Total Bleed On Board >= N

### Câu chốt

Muốn một skill nằm trong `80%-85% skill chuẩn`, nó phải đọc được bằng:

`Core + 1 condition chính + 1-2 effect chuẩn`

Nếu vượt khỏi khung đó, mới cho phép thêm custom code.

---

## 16. Skills — Đã Chốt

### Ember Weapon [Status / Buff] — 1 slot, 2 Focus, Uncommon

Trong **3 turn tiếp theo**, Basic Attack gây thêm **+1 damage**. Nếu đòn Basic Attack đó **Crit** thì áp **Burn = tổng damage gây ra**.

> Basic Attack base 4 dmg → với Ember Weapon = 5 dmg
> Nếu hit đó Crit và gây 5 total damage → áp 5 Burn
> Với Elemental Catalyst passive → thành 6 Burn

---

### Hellfire [Attack] — 3 slot, 2 Focus, Rare

Consume toàn bộ Burn, gây **3 damage mỗi stack Burn**. Sau đó mỗi dice trong nhóm đổ ra đúng **7** → áp lại **7 Burn mới**.

> Điều kiện exact 7 là intentional — chỉ hoạt động sau khi mod dice
> **Anti-synergy cứng với Crit Escalation** (intentional)
> Loop win condition: 3 dice mod 100% mặt 7 → 21 Burn → Hellfire mỗi turn → 63 dmg/turn

---

## 17. Skills — Chưa Chốt

### 16.1 Hệ Physical

| Skill | Tag | Slot / Focus | Rarity | Effect |
|---|---|---|---|---|
| **Precision Strike** | Attack | 1 / 2 | Common | Gây X dmg. Nếu Base Value chẵn → luôn Crit (×1.5). |
| **Brutal Smash** | Attack | 1 / 1 | Common | Gây 12 dmg cố định. Nếu target có Mark trước khi trúng → hồi 1 Focus. |
| **Heavy Cleave** | Attack | 2 / 3 | Uncommon | Gây dmg = X + Base Value cao nhất trong 2 dice đang dùng. |
| **Execution** | Attack | 3 / 4 | Rare | Gây tổng X dmg. Nếu target chết → overkill dmg cộng vào Added Value cho Attack/Sunder đầu tiên lượt kế. |
| **Sunder** | Sunder | 2 / 2 | Uncommon | Gây X dmg. Bỏ qua Guard và xóa toàn bộ Guard của target. |
| **Fated Sunder** | Sunder | 1 / 2 | Uncommon | Gây 2 dmg. Nếu Base Value = "số định mệnh" của combat đó → xóa sạch Guard trước khi gây dmg. Không hưởng ×1.2 từ Stagger. |

### 16.2 Hệ Fire

| Skill | Tag | Slot / Focus | Rarity | Effect |
|---|---|---|---|---|
| **Ignite** | Status / Debuff | 1 / 2 | Common | Áp X Burn. Nếu Base Value lẻ → áp thêm 2 Burn. |
| **Cauterize** | Status / Buff / Debuff | 2 / 2 | Uncommon | Áp Burn = dice thấp nhất. Nhận Guard = dice cao nhất trong 2 dice đang dùng. |
| **Fire Slash** | Attack | 1 / 2 | Common | Gây X dmg. Consume toàn bộ Burn → +2 dmg/stack. |

### 16.3 Hệ Ice

| Skill | Tag | Slot / Focus | Rarity | Effect |
|---|---|---|---|---|
| **Deep Freeze** | Status / Debuff | 1 / 3 | Common | Áp Freeze. Không tác dụng nếu target đã Freeze/Chilled. |
| **Shatter** | Attack | 1 / 2 | Uncommon | Gây X dmg. Nếu target đang Chilled → +50% Guard hiện có của player (tối đa +20). |
| **Frost Shield** | Guard / Buff | 2 / 2 | Common | Nhận X Guard. |
| **Winter's Bite** | Attack | 1 / 1 | Common | Gây 6 dmg cố định. Kéo dài Chilled thêm 1 turn. |
| **Permafrost Chain** | Status / Debuff | 2 / 6 | Rare | Áp Freeze lên 1 địch. Khi Freeze hết → nảy sang 1 địch khác (ưu tiên chưa bị khống chế). |
| **Cold Snap** | Attack | 2 / 2 | Uncommon | X = dice thấp nhất trong cụm. Gây X dmg và Freeze ngẫu nhiên 1 địch. |

### 16.4 Hệ Lightning

| Skill | Tag | Slot / Focus | Rarity | Effect |
|---|---|---|---|---|
| **Static Conduit** | Status / Debuff | 2 / 2 | Uncommon | Gây 4 dmg. Nếu target có Mark → áp Mark lên toàn bộ địch còn lại. |
| **Flash Step** | Status / Debuff | 1 / 1 | Common | Áp Mark lên target. Ghi đè Base Value của dice tiếp theo bằng giá trị dice hiện tại. |
| **Spark Barrage** | Attack | 1 / 2 | Uncommon | Gây X dmg. Nếu Base Value chẵn → hit nảy sang 1 target khác. |
| **Overload** | Attack | 1 / 2 | Uncommon | Gây X dmg + **4 dmg cho mỗi Mark trên toàn sân**. Ví dụ: die 20, 3 Mark → 32 dmg. |
| **Thunderclap** | Attack | 3 / 4 | Rare | X = dice cao nhất. Gây X dmg lên tất cả. +4 dmg cho mỗi địch đang có Mark. |

### 16.5 Hệ Bleed

| Skill | Tag | Slot / Focus | Rarity | Effect |
|---|---|---|---|---|
| **Lacerate** | Attack / Debuff | 1 / 3 | Common | Áp X Bleed. Nếu Crit → áp thêm X Bleed nữa. |
| **Blood Ward** | Guard / Buff | 1 / 2 | Uncommon | Nhận Guard = tổng Bleed stack trên tất cả địch. |
| **Siphon** | Status | 2 / 3 | Uncommon | Consume toàn bộ Bleed trên 1 target. Cứ 5 Bleed → tạo 1 Consumable ngẫu nhiên (tối đa 3). |
| **Hemorrhage** | Attack / Debuff | 2 / 2 | Rare | Áp Bleed lên target = đúng số HP player đã mất ở lượt trước. |

---

## 18. Passives — Chưa Chốt

> Rarity mapping đã có, text chưa phải final.

| Passive | Rarity | Text hiện tại |
|---|---|---|
| **Crit Escalation** | Rare | Mỗi khi roll Crit → toàn bộ mặt dice nhận +1 base cho đến hết combat. |
| **Dice Forging** | Rare | Lần đầu tiên mỗi trận dùng Basic Attack → mặt dice vừa dùng nhận +1 base vĩnh viễn cho toàn bộ Run. |
| **Clear Mind** | Rare | Bắt đầu mỗi lượt, hồi thêm 1 Focus (tổng hồi 2 mỗi lượt). |
| **Iron Stance** | Rare | Guard không biến mất vào cuối lượt của Player. |
| **Even Resonance** | Uncommon | Mỗi dice có Base Value chẵn nhận thêm +3 Added Value cho đòn đó. |
| **Elemental Catalyst** | Uncommon | Khi địch nhận Burn hoặc Bleed, cộng thêm 1 stack khuyến mãi. |
| **Spiked Armor** | Uncommon | Khi địch đánh vào Guard của bạn, chúng nhận lại Physical damage = lượng Guard bị phá vỡ. |
| **Mitigation (Desperate Guard)** | Common | Dùng Basic Guard bằng dice có Base Value ≤ 3 → tạo 1 Consumable. |
| **Fail Forward** | Common | Mỗi khi roll ra Fail (mặt thấp nhất) → nhận ngay 3 Guard. |
| **Alchemist** | Common | Bắt đầu mỗi combat, nhận 1 Consumable ngẫu nhiên. |

---

## 19. Anti-Synergy Notes

| Cặp | Loại | Lý do |
|---|---|---|
| **Hellfire + Crit Escalation** | Hard anti-synergy | Crit Escalation đẩy mặt dice lên → exact 7 của Hellfire không còn đúng. **Intentional.** |
| **Hellfire + Clear Mind** | Synergy mạnh | Clear Mind +1 Focus/turn → Hellfire loop 2 Focus/turn tự duy trì hoàn toàn. Sacrifice toàn bộ passive flexibility còn lại. |
| **Dice Forging + Crit Escalation** | Dùng được chung | Permanent theo run vs in-combat. Target khác nhau, không cancel nhau. |
| **Iron Stance + Guard build** | Synergy mạnh | Guard không mất cuối lượt → Frost Shield, Blood Ward, Cauterize tích lũy qua nhiều turn. |
| **Even Resonance + Precision Strike** | Synergy | Precision Strike crit khi chẵn → Even Resonance cộng thêm Added Value cho chính lượt đó. |
| **Elemental Catalyst + Ember Weapon** | Synergy | Basic Attack apply Burn → Elemental Catalyst +1 stack → Hellfire nhận thêm stack. |

---

## 20. Combo Engines đã Identify

### Fire Loop Engine

```text
Ember Weapon (slot 1)
→ Basic Attack × 2: áp 10 Burn, +2 Focus
→ Turn sau: Hellfire → consume Burn, 30 dmg
→ Với Elemental Catalyst: 12 Burn → 36 dmg
→ Dice mod 100% mặt 7: Hellfire loop → 63 dmg/turn vô tận
```

### Lightning Board Control

```text
Flash Step (Mark 1 target) → Static Conduit (Mark all) → Overload (X + 4×marks)
Hoặc: Static Conduit → Thunderclap (X all + 4/mark)
```

### Bleed Resource Engine

```text
Lacerate (Crit → double Bleed) → Blood Ward (Guard = total Bleed) → Siphon (Bleed → Consumable)
```

### Crit Snowball

```text
Crit Escalation → Precision Strike (chẵn = Crit) → face tăng → chẵn nhiều hơn → Crit nhiều hơn
→ Lacerate Crit → double Bleed → Blood Ward Guard
```

### Dice Customization Engine

```text
Dice Forging → Basic Attack mỗi combat → mặt specific tăng base vĩnh viễn
→ Even Resonance (+3 Added khi chẵn) → Spark Barrage (chẵn nảy) → Overload
```

---

## 21. Những gì chưa nên coi là final

- Full content list chi tiết của mọi skill và passive (chỉ Hellfire + Ember Weapon đã chốt)
- Toàn bộ con số balance cuối
- Run structure chi tiết (map, node types, shop placement)
- Regular enemy design chi tiết
- Consumable pool chi tiết
- Tooltip / runtime preview formatter final
- Hidden boss placement cụ thể trong Endless

---

## 22. Kết luận thực dụng

1. Đây là **dice-driven tactical roguelike**, dice là trung tâm.
2. Core loop runtime hiện tại: **Roll Dice → Reorder nếu cần → Drag skill vào target để cast ngay → End Turn → Enemy turn**.
3. Player có **6 skill slot, 1 passive slot, 1-3 dice, Focus economy rõ ràng**.
4. **Base Value** quyết định condition; **Added Value** chỉ tăng output.
5. Crit/Fail, exact value, parity, threshold đều là **trục build thật**.
6. 5 hệ: **Physical, Fire, Ice, Lightning, Bleed**. 4 effect cốt lõi: **Burn, Freeze/Chilled, Mark, Bleed**.
7. **Lane mapping phải đọc theo lane hiện tại (A/B/C), không đọc theo identity ban đầu (1/2/3)**.
8. Boss phải là **mechanic wall**, không hard-counter hoàn toàn build.
9. Relic và custom dice là trục progression quan trọng.
10. Đây là game theo hướng **lenticular design** — không phải 1 skill = 1 build.

Nếu cần implementation context hoặc codebase state → đọc `PROJECT_META.md`.
