# GDD_VISION.md

> Đây là **bản định hướng** của project game.
> Mục tiêu của file này là khóa lại **ý đồ cốt lõi ban đầu** của game để mọi quyết định về combat, system, content, UX và mở rộng về sau đều không đi lệch hướng.
> 
> File này **không phải** bản implementation spec, không phải bản code handoff, và không thay thế các tài liệu chi tiết như `COMBAT_RULES.md`, `SKILLS_PASSIVES.md`, `PROJECT_META.md` hoặc `MASTER_CONTEXT.md`.
> 
> File này dùng để trả lời 5 câu hỏi:
> 1. Game này là game gì?
> 2. Người chơi phải cảm thấy gì khi chơi?
> 3. Combat loop cốt lõi là gì?
> 4. Những hệ thống nào tồn tại để phục vụ loop đó?
> 5. Những hướng nào tuyệt đối không nên để game trôi sang?

---

# I. GIỚI THIỆU NGẮN GỌN

## Tên định danh hiện tại

**Dice-driven Tactical Combat Roguelike**

Đây là game roguelike turn-based với trọng tâm là **dice**, **skill slot**, **passive**, **status/payoff**, và **lane order**. Người chơi không rút bài hay phản xạ thời gian thực; thay vào đó, họ roll xúc xắc, nhìn board hiện tại, sắp xếp kế hoạch hành động, rồi khóa kế hoạch để chấp nhận hậu quả từ chính lượt roll đó.

## Trải nghiệm cốt lõi phải truyền tải

Người chơi phải luôn cảm thấy rằng:

- họ đang **ra quyết định chiến thuật**, không chỉ đang hưởng may rủi,
- xúc xắc là **ngôn ngữ trung tâm của toàn bộ combat**, không phải một lớp random phủ lên hệ thống khác,
- mỗi lượt ngắn nhưng có ý nghĩa vì phải quyết định **gán die nào cho skill nào, đi lane nào trước, setup trước hay nổ trước**,
- build tốt sẽ tạo ra **synergy và payoff rõ ràng**, chứ không chỉ cộng thêm chỉ số,
- game có chiều sâu nhưng vẫn phải **đọc được bằng mắt**, không bắt người chơi tính nhẩm quá nhiều tầng modifier ẩn.

## Điểm khác biệt thật sự của game

Game này không đi theo hướng deckbuilder kiểu rút bài như Slay the Spire, cũng không đi theo hướng score-explosion puzzle như Balatro. Bản sắc riêng của game nằm ở chỗ:

- xúc xắc có **bản sắc thật** qua Base Value, Crit/Fail, Exact Value, parity và face customization,
- hành động được thực hiện qua **skill slot theo lane order**, không phải chỉ là chọn skill rồi bấm,
- trạng thái trong game chủ yếu tồn tại để phục vụ **setup → payoff**, không phải để trang trí,
- build mạnh là build khiến người chơi **chơi khác đi**, không phải chỉ gây số to hơn.

---

# II. TỔNG QUAN TRẢI NGHIỆM

## Nguồn cảm hứng chính

- **Balatro**: passive kiểu joker, build engine, consumable bẻ hướng run, endless mode, unlock discovery.
- **Slay the Spire**: turn rhythm rõ ràng, intent system, boss như mechanic wall, build synergy.
- **Persona / Expedition 33**: loadout rõ ràng, combat nhấn mạnh lựa chọn hành động thay vì spam input phức tạp.
- **D&D**: crit/fail, nhiều loại dice, mặt xúc xắc có ý nghĩa thật, exact value matters.

## Nền tảng phát triển hiện tại

**PC trước, mobile sau.**

Combat, feel, readability, tốc độ ra quyết định và độ rõ của system sẽ được chốt và playtest trên PC trước. Sau khi combat đã đủ chắc, game mới được tối ưu hóa và port sang mobile. Điều này rất quan trọng vì game không được hy sinh chiều sâu của combat chỉ để thuận tiện cho mobile quá sớm.

## Đối tượng người chơi mục tiêu

Game hướng tới người chơi thích:

- turn-based combat có nhịp rõ,
- build synergy,
- tactical decision-making,
- cảm giác “xoay sở với tài nguyên hiện tại” rồi chuyển nó thành lượt đánh tốt,
- discovery dần dần qua unlock, content pool và build engine.

Game không ưu tiên phục vụ nhóm người chơi muốn:

- hành động thời gian thực,
- combo cơ học nhanh tay,
- tính toán số học nặng nề ở từng thao tác,
- chơi theo kiểu xem animation dài nhưng quyết định nghèo nàn.

---

# III. CORE LOOP

## Combat loop cốt lõi

Core loop runtime hiện tại của từng lượt combat là:

**Roll Dice → Reorder nếu cần → Drag skill vào target để cast ngay → End Turn → Enemy Turn**

### 1. Roll

Người chơi roll toàn bộ dice đang equip ở turn đó. Kết quả roll không chỉ quyết định damage mà còn quyết định điều kiện, payoff, parity, Crit/Fail, exact number và nhiều lớp ý nghĩa khác.

### 2. Reorder nếu cần

Người chơi nhìn thứ tự dice hiện tại và có thể reorder trước khi dùng skill. Sequencing vẫn là phần chiến thuật thật, nhưng trọng tâm hiện tại là thứ tự consume dice thay vì gán tay từng die vào từng skill.

### 3. Drag skill vào target để cast ngay

Người chơi kéo icon skill từ skill slot vào enemy hoặc vùng self-cast hợp lệ. Skill được cast ngay khi drop hợp lệ; không còn bước lock plan riêng.

### 4. End Turn

Khi không muốn dùng thêm skill, người chơi bấm End Turn để chốt lượt. Dice đã dùng được xem là đã consume cho lượt đó.

### 5. Enemy Turn

Enemy hành động, status tick, cleanup diễn ra, intent mới được hình thành cho turn tiếp theo. Đây là nhịp phản công và cũng là nơi build phòng thủ, Guard, freeze tempo hoặc resource loop chứng minh giá trị.

## Run loop tổng quát

Ở cấp độ toàn run, nhịp chơi mong muốn là:

**Combat → Reward / Shop / Progression → Chỉnh build → Combat khó hơn**

Người chơi đánh trận, nhận thưởng, chỉnh skill/passive/dice/relic, rồi tiếp tục vào trận khó hơn với một build ngày càng rõ cá tính hơn.

## Kết thúc run

Khi thắng boss cuối, player có thể:

- kết thúc run như một chiến thắng hoàn chỉnh,
- hoặc tiếp tục sang **Endless Mode** để đẩy build đi xa hơn.

Khi thua run, người chơi mất toàn bộ tiến trình của run hiện tại: dice, skill, passive, relic, consumable và các tăng trưởng chỉ dành cho run đó. Thứ được giữ lại là **unlock progress**.

Ý nghĩa định hướng: mỗi run phải có đủ giá trị độc lập để đáng chơi lại, nhưng vẫn cần meta progression đủ vừa phải để tạo discovery dài hạn.

---

# IV. CORE PILLARS

## Pillar 1 — Dice-first Tactics

Dice phải là trung tâm thật sự của combat. Mọi hệ thống mạnh nhất của game đều phải quay về xúc xắc:

- skill đọc Base Value,
- Crit/Fail có ý nghĩa,
- chẵn/lẻ, exact number, high/low có gameplay value,
- customization của face thay đổi cách build vận hành,
- số dice equip thay đổi action economy.

Nếu một tính năng mới có thể tồn tại y hệt mà không cần dice, nó đáng bị nghi ngờ.

## Pillar 2 — Setup → Payoff

Game này phải mạnh ở cảm giác “chuẩn bị rồi kích nổ”. Người chơi setup Burn, Mark, Bleed, Freeze/Chilled, Guard break, lane order hoặc focus state để rồi chuyển chúng thành payoff rõ ràng ở đúng thời điểm.

Game không nên bị biến thành nơi mọi skill chỉ là “gây damage lớn hơn” mà không cần chuẩn bị gì.

## Pillar 3 — Build Expression

Build khác nhau phải khiến người chơi chơi khác nhau. Fire, Lightning, Ice, Bleed, Physical, exact-value build, crit build, guard build, consumable build, dice customization build phải tạo ra lựa chọn khác về lane, target priority, tempo và payoff.

Nếu hai build khác nhau nhưng chỉ thay đổi con số mà không thay đổi cách ra quyết định, thì build identity đang yếu.

## Pillar 4 — Readable Complexity

Game có thể sâu, nhưng độ sâu phải đọc được. Người chơi không nên bị buộc phải tính nhẩm quá nhiều phép nhân chồng lớp trong đầu mới biết một hành động có đáng dùng hay không.

Về lâu dài, UI, tooltip, preview và runtime text phải ưu tiên clarity. Hệ thống được phép sâu, nhưng trải nghiệm không được mù mờ.

---

# V. ANTI-PILLARS — NHỮNG HƯỚNG KHÔNG ĐƯỢC ĐI LỆCH

## 1. Không biến thành deckbuilder kiểu rút bài

Game này không xoay quanh draw/discard/hand management. Skill loadout là cố định theo build, còn xúc xắc mới là lớp biến động chính của từng turn.

## 2. Không biến dice thành RNG trang trí

Nếu quyết định cuối cùng không còn thật sự phụ thuộc vào xúc xắc mà chỉ phụ thuộc vào số modifier chồng lên nhau, game sẽ mất bản sắc. Dice phải luôn là nguồn ý nghĩa, không chỉ là animation mở màn.

## 3. Không ép người chơi làm toán quá nhiều

Các multiplier và exception không được phát triển theo hướng khiến mỗi action đòi hỏi quá nhiều tính nhẩm. Độ khó nên đến từ đọc tình huống và sequencing, không đến từ cộng trừ nhân chia dài dòng.

## 4. Không để mọi build hội tụ thành “damage build chung chung”

Status, lane, guard, consume, exact value, consumable, passive và dice customization phải giữ được cá tính riêng. Không để mọi hệ cuối cùng chỉ là cách khác nhau để stack cùng một loại burst vô danh.

## 5. Không tạo hard-counter làm tắt hẳn build

Boss hoặc enemy có thể tạo friction, nhưng không được chặn tuyệt đối một build đến mức người chơi không còn chơi được game của mình.

---

# VI. CÁC HỆ THỐNG LỚN PHẢI PHỤC VỤ CORE PILLARS

## 1. Dice System

Đây là hệ thống trung tâm nhất. Dice không chỉ là damage source mà là ngôn ngữ chung của toàn bộ combat.

### Những rule định hướng cần luôn giữ

- **Base Value** là giá trị thật của die.
- **Added Value** chỉ là phần cộng vào output cuối, không làm đổi bản chất của die.
- Điều kiện như crit/fail, exact number, parity, threshold, highest/lowest phải đọc từ **Base Value**.
- Skill 2-3 slot nếu đọc “cao nhất/thấp nhất” thì chỉ đọc trong **local group** của skill đó.
- Exact value matters là một hướng identity quan trọng của game, không phải gimmick một chiều.

### Ý nghĩa thiết kế

Dice phải tạo ra cả ba loại quyết định:

- quyết định **ngay trong turn**,
- quyết định **xây build theo run**,
- quyết định **meta-progression** qua unlock và customization.

### Dice customization là differentiator cốt lõi

Việc chỉnh từng mặt dice không phải lớp phụ trang trí. Đây là một trục tăng trưởng thật sự của game. Nó phải ảnh hưởng mạnh tới:

- Crit/Fail,
- parity,
- exact-value skill,
- threshold skill,
- unlock condition,
- build engine về dài hạn.

## 2. Action Economy và Loadout

Combat cần đơn giản ở bề mặt nhưng giàu quyết định ở bên trong.

### Cấu trúc loadout hiện tại

- 4 skill slot
- 1 passive slot
- 1 đến 3 dice
- Basic Attack và Basic Guard luôn tồn tại

### Ý nghĩa định hướng

- số lượng công cụ phải đủ ít để người chơi đọc turn nhanh,
- nhưng mỗi công cụ phải đủ giàu tương tác để build có chiều sâu,
- basic actions tồn tại như anchor của economy, đảm bảo player luôn có một cách chơi hợp lệ khi roll xấu hoặc thiếu Focus,
- số dice equip phải có ý nghĩa thật với action economy, không chỉ là cosmetic progression.

## 3. Focus Economy

Focus là tài nguyên nhịp độ. Nó không chỉ giới hạn số skill mạnh mà còn quyết định player có chọn setup, payoff hay basic action trong turn hiện tại.

Focus phải luôn tạo cảm giác:

- thiếu vừa đủ để buộc lựa chọn,
- có thể xoay xở bằng build hoặc sequencing tốt,
- không trở thành nút thắt khiến combat tê liệt.

Các skill, passive, basic attack và một số payoff phải tương tác với Focus sao cho người chơi cảm thấy có thể **xoay lượt**, chứ không chỉ chờ đủ mana rồi bấm kỹ năng mạnh nhất.

## 4. Turn Structure và Lane Order

Thứ tự lane là một phần của gameplay cốt lõi, không phải chỉ là biểu diễn UI.

### Điều phải giữ

- Planning cho phép reorder.
- Execute đọc theo lane hiện tại, không đọc theo identity ban đầu của skill/dice.
- `1/2/3` là identity gốc; `A/B/C` là lane thực thi hiện tại.
- Logic phải đọc theo lane hiện tại sau khi reorder.

### Ý nghĩa thiết kế

Lane phải là nơi người chơi tạo sequencing:

- skill đầu phá Guard,
- skill giữa setup status,
- skill cuối làm finisher,
- hoặc ngược lại tùy build.

Nếu lane order mất ý nghĩa chiến thuật, game sẽ mất một nửa chiều sâu hiện có.

## 5. Element / Status / Payoff Engine

Status trong game này không phải chỉ để gây thêm số. Mỗi hệ phải có identity và kiểu quyết định riêng.

### Physical

Physical là trục burst thẳng, anti-Guard, và damage rõ ràng. Vai trò của hệ này là tạo cảm giác direct, clean, decisive.

### Fire / Burn

Burn không phải DoT chính; Burn là tài nguyên để consume. Fire build phải cho cảm giác “tích nguyên liệu rồi kích nổ”.

### Ice / Freeze / Chilled

Ice là tempo/control. Freeze tạo khoảng nghỉ; Chilled tạo cửa sổ payoff. Ice không mạnh vì số to, mà mạnh vì bẻ nhịp combat.

### Lightning / Mark

Mark là weak point để direct-hit khai thác. Lightning phải cho cảm giác đánh đúng mục tiêu rồi lan ảnh hưởng ra cả board. Đây là hệ của board pressure và propagation.

### Bleed

Bleed là áp lực kéo dài và có thể chuyển hóa thành giá trị khác. Bleed không chỉ là mất máu theo thời gian; nó còn là một dạng tài nguyên build có thể đổi sang Guard, consumable hoặc payoff đặc thù.

### Stagger

Guard break phải tạo ra cơ hội chiến thuật rõ ràng. Stagger tồn tại để biến việc phá Guard thành một khoảnh khắc có giá trị sequencing, chứ không chỉ là bỏ một lớp giáp trừu tượng.

## 6. Ailment System

Ailment hiện là trục thiên về enemy-side system. Đây không phải trung tâm fantasy của player build ở giai đoạn hiện tại.

Ý nghĩa định hướng:

- ailment là công cụ để enemy tạo pressure và disruption,
- player build không nên bị lệch sang một hệ ailment rối rắm nếu chưa có chủ đích rõ,
- nếu sau này mở rộng ailment cho player, phải đảm bảo nó phục vụ pillar chứ không tạo một game phụ tách rời khỏi dice loop.

## 7. Relic / Consumable System

Relic trong game là **one-shot consumable** mang vai trò bẻ nhịp run hoặc bẻ nhịp turn, không phải item bị động vĩnh viễn mặc định.

### Type A — Combat relic

Dùng trong combat như quick action, không chiếm dice slot. Vai trò của chúng là tạo các khoảnh khắc can thiệp ngắn, gọn, đúng lúc.

### Type B — Dice manipulation relic

Dùng ngoài combat để chỉnh face dice. Đây là trục cực quan trọng vì nó nối thẳng sang dice identity, unlock condition và build engine.

### Pool dilution là chủ đích

Relic pool lớn là dụng ý thiết kế, không phải lỗi. Cảm giác “khó tìm đúng món mình cần” là một phần của sức nặng roguelike run, giống tinh thần Balatro tarot hơn là shop deterministic.

## 8. Unlock System

Unlock không chỉ là meta progression để kéo dài nội dung. Unlock còn là cách game **dạy người chơi đúng thứ tự**.

### Vai trò định hướng

- giãn tốc độ lộ mechanic,
- tránh overwhelm quá sớm,
- buộc player chứng minh đã hiểu một ngôn ngữ gameplay trước khi nhận payoff mạnh hơn,
- gắn meta progression vào gameplay thật thay vì grind vô nghĩa.

### Ý nghĩa đặc biệt với project này

Dice customization và unlock condition có thể feed nhau. Đây là điểm rất đáng giữ vì nó làm progression của game khác với mô hình “mở thêm item vào pool” thông thường.

## 9. Boss System

Boss phải là **mechanic wall**, không phải stat wall.

### Điều đúng hướng

- Boss tạo friction với build.
- Boss buộc player thích nghi hoặc sequencing tốt hơn.
- Boss làm lộ điểm yếu của build nhưng không tắt hoàn toàn fantasy của build đó.

### Điều sai hướng

- miễn nhiễm tuyệt đối một hệ,
- hard-block toàn bộ payoff của player,
- phản damage vô điều kiện,
- biến trận boss thành bài kiểm tra chỉ số thay vì bài kiểm tra hiểu cơ chế.

## 10. Endless Mode và Hidden Boss

Endless không chỉ là “đánh tiếp cho vui”. Nó phải phục vụ 3 kiểu người chơi:

- người thắng sớm nhưng build chưa hoàn chỉnh,
- người muốn kết thúc run đúng lúc,
- người muốn ép build lên mức hoàn hảo trước hoặc sau boss cuối.

Hidden Boss chỉ nên xuất hiện trong Endless và đóng vai trò là bài kiểm tra cuối cùng của toàn bộ hệ thống build, sequencing và hiểu mechanic.

---

# VII. DANH SÁCH TÍNH NĂNG TỔNG QUAN

Các hệ thống lớn của game tồn tại để phục vụ trực tiếp cho core pillars gồm:

1. **Dice combat system**
2. **Skill slot + lane order system**
3. **Passive build engine**
4. **Element/status/payoff engine**
5. **Guard / Stagger / sequencing layer**
6. **Relic / consumable system**
7. **Dice customization system**
8. **Unlock system**
9. **Boss mechanic system**
10. **Run progression / reward / shop loop**
11. **Endless mode + hidden boss**
12. **Tooltip / preview / clarity layer**

Nếu một feature mới không phục vụ rõ ít nhất một trong các hệ thống trên hoặc không củng cố core pillars, nó không nên được ưu tiên thêm vào.

---

# VIII. CƠ CHẾ TÀI NGUYÊN / KINH TẾ

## 1. Tài nguyên trong combat

Combat hiện xoay quanh các loại tài nguyên sau:

- dice outcome,
- Focus,
- Guard,
- status trên target,
- lane order,
- relic/consumable,
- số lượng dice đang equip.

Điểm quan trọng là nhiều thứ trong số này phải được đối xử như **tài nguyên chiến thuật**, không chỉ như chỉ số phụ. Ví dụ:

- Burn là tài nguyên để consume,
- Bleed có thể trở thành Guard hoặc consumable,
- Mark là điểm yếu để direct-hit khai thác,
- Chilled là cửa sổ payoff,
- Guard vừa là phòng thủ vừa là cơ sở cho một số payoff,
- exact value là tài nguyên build-level chứ không chỉ là điều kiện câu chữ.

## 2. Kinh tế ngoài combat

Run progression phải cho người chơi cảm giác tăng trưởng theo ba trục:

- **mở rộng công cụ**: skill, passive, relic,
- **biến đổi công cụ**: customize dice,
- **làm rõ build**: bỏ thứ không hợp, giữ thứ phục vụ engine đang hình thành.

Shop, reward, unlock và relic pool cần cùng phục vụ cảm giác rằng mỗi run là một quá trình “điêu khắc build”, không phải chỉ là cộng item vào túi.

---

# IX. NGUYÊN TẮC RA QUYẾT ĐỊNH KHI THÊM TÍNH NĂNG MỚI

Khi có ý tưởng mới, phải tự kiểm tra các câu hỏi sau:

1. Ý tưởng này có làm **dice** quan trọng hơn hay làm dice mờ đi?
2. Nó có tạo thêm **setup → payoff** hay chỉ là một damage button mới?
3. Nó có làm build **chơi khác đi thật** hay chỉ tăng số?
4. Nó có làm combat **rõ hơn hoặc sâu hơn**, hay chỉ rối hơn?
5. Nó có bắt người chơi tính toán quá nhiều không?
6. Nó có giữ được giá trị của **lane order / sequencing** không?
7. Nó có làm một hệ trở thành hard-counter vô lý hoặc auto-pick không?
8. Nó có phục vụ ít nhất một core pillar không?

Nếu không trả lời được ít nhất một nửa số câu trên theo hướng tích cực, ý tưởng đó không nên được ưu tiên.

---

# X. ĐỊNH HƯỚNG CHO SKILL, PASSIVE VÀ BUILD

## 1. Lenticular design là bắt buộc

Game không nên đi theo hướng 1 skill = 1 build biệt lập. Thay vào đó, skill và passive phải được thiết kế để:

- tự hoạt động được,
- nhưng mạnh hơn khi gặp đúng điều kiện,
- và ghép với nhau thành build engine lớn hơn.

## 2. Rare không được chỉ là số to hơn

Rare nên mở ra dạng chơi, dạng payoff hoặc dạng engine mới. Nếu rare chỉ là bản common với damage lớn hơn, rarity đang bị dùng sai.

## 3. Intentional anti-synergy là hợp lệ

Không phải mọi thứ đều cần cộng hưởng với mọi thứ. Một số anti-synergy có chủ đích là tốt nếu nó:

- giữ cho build identity sắc hơn,
- buộc player chọn hướng,
- ngăn một engine ôm hết mọi payoff mạnh nhất.

Ví dụ tinh thần đúng hướng là những trường hợp exact-value engine không hợp với crit-scaling engine. Điều đó giúp build có cá tính thay vì trở thành “lấy hết đồ mạnh”.

## 4. Basic action vẫn phải còn chỗ đứng

Dù build về late run mạnh đến đâu, Basic Attack và Basic Guard vẫn nên giữ vai trò hệ thống thực sự. Nếu mọi build cuối cùng đều bỏ hẳn basic action khỏi tư duy chiến thuật, economy nền đang yếu đi.

---

# XI. ĐỊNH HƯỚNG CHO UX / TOOLTIP / PREVIEW

Game này có thể có chiều sâu số học ở backend, nhưng frontend phải ngày càng rõ ràng hơn.

### Điều phải giữ

- Tooltip và preview về lâu dài phải ưu tiên số đã resolve hoặc số gần-resolved.
- Người chơi nên biết hành động này “đáng dùng hay không” mà không cần tự tính mọi lớp modifier trong đầu.
- Complexity được chấp nhận ở design; opacity không được chấp nhận ở UX.

Nếu một hệ thống chỉ hoạt động được khi người chơi nhớ quá nhiều luật ngầm hoặc phải suy luận từ text không rõ, thì hệ thống đó đang đi lệch pillar Readable Complexity.

---

# XII. NHỮNG GÌ BẢN ĐỊNH HƯỚNG NÀY KHÔNG LÀM

File này không dùng để chốt:

- con số damage cụ thể của toàn bộ skill,
- data table đầy đủ,
- implementation details,
- class name, folder structure, source of truth code,
- bug state hoặc refactor state,
- edge-case code-level chi tiết,
- full content list cuối cùng của mọi skill/passive.

Các phần đó phải nằm ở tài liệu chi tiết hơn.

---

# XIII. KẾT LUẬN NGẮN

Nếu sau này có bất kỳ tính năng, content, UI hay rework nào được đề xuất, trước tiên phải hỏi:

**Nó có làm game này trở thành một game dice-first tactical roguelike rõ ràng hơn không?**

Nếu có, tiếp tục xem xét.
Nếu không, dù ý tưởng có thú vị đến đâu, nó cũng không nên được ưu tiên.

Tóm lại, hướng đi đúng của game là:

- ít thao tác nhưng nhiều quyết định,
- ít slot nhưng nhiều tương tác,
- xúc xắc là trung tâm thật sự,
- status tồn tại để phục vụ payoff,
- lane order là sức mạnh,
- build phải đổi cách chơi, không chỉ đổi con số,
- độ sâu phải luôn đi kèm khả năng đọc và hiểu.

---

## Runtime Direction Note (2026-04)

- Vision goc cua file nay van hop le.
- De tranh hieu nham voi implementation hien tai:
  - build runtime hien tai chi con `1 passive slot`
  - combat runtime hien tai dang dung grammar moi theo huong PC-first:
    - `roll dice`
    - `reorder neu can`
    - `drag skill tu skill slot vao target de cast ngay`
    - `bam End Turn de sang enemy turn`
  - roll dice chua bi doi trong batch thu nghiem nay
  - dice da dung hien tai van dang duoc bieu dien bang state spent / dim 50%, chua consume bien mat that
- Huong note / thay doi 2026 duoc ghi rieng o [COMBAT_CHANGES_2026.md](/C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Docs/Detail/COMBAT_CHANGES_2026.md).
