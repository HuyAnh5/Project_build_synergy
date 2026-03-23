# RELICS_AND_DICE_PROGRESSION_SPEC.md

> Tài liệu này mô tả **consumable / relic system**, **dice progression**, **dice customization** và vai trò của chúng trong run progression.  
> Đây là tầng nối giữa combat core, build shaping, và quá trình hoàn thiện bộ dice qua từng run.

---

## 1. Mục tiêu của hệ thống

Hệ relic / consumable và dice progression tồn tại để:

- bẻ hướng run,
- tạo tăng trưởng không chỉ nằm ở skill / passive,
- khiến dice thực sự là trục progression độc đáo của game,
- mở ra utility đột biến mà không phá combat loop nền,
- cho player cảm giác “điêu khắc” dice dần dần thành một bộ hoàn chỉnh đúng build.

---

## 2. Terminology chuẩn

### 2.1 Base Value

**Base Value** là giá trị gốc của mặt dice.  
Đây là giá trị dùng để kiểm tra các điều kiện như:

- chẵn / lẻ,
- crit / fail,
- exact value (`= 7`, `<= 3`, v.v.),
- highest / lowest trong local group.

### 2.2 Added Value

**Added Value** là phần cộng thêm vào output của mặt đó.  
Added Value không đổi bản chất của Base Value.

### 2.3 Không dùng Effective Value nữa

Từ bản này trở đi, thuật ngữ **Effective Value** không còn là source of truth.  
Mọi phần trước đây dùng `Effective Value` nên được hiểu lại thành:

- **Base Value**
- **Added Value**

---

## 3. Consumable framework — current direction

### 3.1 Shared consumable slots

Player có **3 consumable slot dùng chung**.  
Các consumable đều được đối xử như cùng một lớp tài nguyên chung, gần với tinh thần Balatro.

### 3.2 Ba nhóm consumable hiện tại

#### A. Seals
Dùng để:

- gây damage,
- áp / khai thác status,
- hoặc bẻ board state theo cách trực tiếp.

#### B. Zodiac
Dùng để:

- chỉnh dice,
- chỉnh mặt dice,
- sculpt build dài hạn qua run.

#### C. Runes
Dùng để:

- buff,
- hồi tài nguyên,
- utility trong combat,
- hoặc hỗ trợ player mà không chiếm action slot của skill.

### 3.3 Không có rarity kiểu common / uncommon / rare

Consumable trong hệ này **không có rarity kiểu truyền thống**.  
Tinh thần đúng hơn là:

- giống Tarot / Planet / special consumables trong Balatro,
- xuất hiện thường xuyên,
- dùng để bẻ run hoặc sculpt build,
- không phải đồ hiếm kiểu “rơi ít mới mạnh”.

### 3.4 Không có unlock riêng cho consumable

Các consumable này **không cần unlock**.  
Chúng tồn tại trong game ngay từ đầu; khác nhau chỉ ở chỗ player có gặp và nhặt được trong run hay không.

### 3.5 Nguồn xuất hiện

Current direction:

- shop (có reroll kiểu Balatro),
- reward từ enemy,
- event,
- passive / system khác,
- và các nguồn run-based khác.

Player được chỉnh dice **thường xuyên**, không phải hiếm hoi.

---

## 4. Logic Flow

### 4.1 Flow consumable

`Player nhận consumable`  
→ hệ thống xác định loại: **Seal / Zodiac / Rune**  
→ player quyết định giữ hay dùng  
→ nếu là combat-use consumable: dùng trong combat theo timing hợp lệ  
→ nếu là Zodiac hoặc consumable can thiệp dice:
   - dice và consumable đều có thể được chọn theo thứ tự bất kỳ
   - action **Use** chỉ hợp lệ khi current context đã có **consumable selected** và đã thỏa **target requirement** của effect đó
   - nếu target chưa đủ hoặc target không hợp lệ, **Use** phải ở trạng thái disabled
   - khi player bấm **Use**, hệ thống mới mở dice-edit overlay theo đúng context hiện tại
   - logic edit là **một hệ thống thống nhất**; khác nhau chỉ ở phạm vi target mà overlay đang cho phép
→ thay đổi được áp vào dice state theo đúng loại effect:
   - **temporary** nếu chỉ tồn tại trong turn / combat hiện tại
   - **permanent** nếu consumable đó là dạng sculpt dài hạn
→ build state / dice state được cập nhật

### 4.2 Flow dice progression

`Player nhận cơ hội chỉnh dice`  
→ player mở đúng context chỉnh dice hiện có  
→ player chọn consumable hoặc kiểu chỉnh sửa tương ứng  
→ nếu effect cần chọn mặt:
   - mở dice-edit overlay
   - player xoay dice đang được hiển thị
   - chọn đủ mặt theo target requirement của effect
   - bấm Confirm / Cancel
→ hệ thống áp dụng thay đổi vào đúng target faces đã chọn  
→ thay đổi có thể có hiệu lực:
   - **ngay trong combat hiện tại** nếu là tactical edit hợp lệ
   - **từ combat sau trở đi** nếu là permanent sculpt / enchant
→ die mới trở thành một phần của build identity

### 4.3 Flow build direction từ consumable + dice
### 4.3 Flow build direction từ consumable + dice

`Run tiếp tục`  
→ Seals / Runes cho utility hoặc power spike trực tiếp  
→ Zodiac thay đổi chính bộ dice  
→ dice progression và consumable cùng nhau đẩy build theo một hướng rõ hơn thay vì chỉ cộng số chung

---

## 5. Dice progression như một trục tăng trưởng thật sự

### 5.1 Dice không phải hệ tĩnh

Mỗi run không chỉ cho player nhặt skill/passive tốt hơn.  
Run còn cho player **biến đổi chính bộ dice của mình**.

### 5.2 Hai hướng tăng trưởng chính

Dice progression hiện có 2 trục chính:

1. **chỉnh value các mặt**,
2. **enchant từng mặt**.

### 5.3 Viên dice hoàn chỉnh là gì

Một **viên dice hoàn chỉnh** là viên mà:

- các mặt đã có Base / Added phù hợp,
- mỗi mặt có enchant đúng vai trò trong build,
- toàn bộ viên dice phục vụ rõ một fantasy build.

Ví dụ late-run fantasy:

- build `Hellfire` sculpt dần thành một viên **full 7**,
- rồi enchant các mặt để hỗ trợ Burn payoff,
- biến bản thân viên dice đó thành một engine hoàn chỉnh cho đúng build Fire.

### 5.4 Tần suất chỉnh dice

Current direction rất rõ:

- player **được phép chỉnh dice thường xuyên**, càng nhiều càng tốt,
- dice progression là nhịp tăng trưởng thật của run,
- không phải hệ phụ chỉ lâu lâu mới chạm tới.

---

## 6. Zodiac — Dice Edit consumables

### 6.1 Vai trò

Zodiac là nhóm consumable dùng để chỉnh dice và chỉnh mặt dice.  
Chúng có thể được dùng:

- **trong combat** để cứu roll xấu, bẻ turn hiện tại hoặc mutate dice đang dùng,
- **trong shop / loadout overlay** để sculpt build dài hạn và thao tác trên nhiều dice cùng lúc.

Rule rất quan trọng:

- flow dùng Zodiac không khóa cứng thứ tự chọn
- player có thể select consumable trước hoặc chọn target trước tùy context
- action **Use** chỉ bật khi target requirement của effect đã được thỏa
- cùng một nhóm Zodiac có thể tồn tại cả:
  - effect **temporary / tactical** dùng ngay trong combat
  - effect **permanent / progression** dùng để sculpt dice dài hạn

Ngoài combat bình thường, player có thể mở panel dice để **inspect / sell**, nhưng **không edit tự do** nếu không ở shop / loadout overlay hoặc combat context hợp lệ.

### 6.2 Current edit pool
### 6.2 Current edit pool

#### A. +1 Face (Permanent)
Thêm 1 mặt cho 1 dice.

#### B. -1 Face (Permanent)
Xóa 1 mặt khỏi 1 dice.

#### C. Copy / Paste Face (Permanent)
Copy một mặt A và paste sang mặt B.

**Rule khóa cứng:** Copy / Paste Face sẽ copy **toàn bộ gói của mặt đó**, gồm:

- Base Value,
- Added Value,
- enchant hiện có trên mặt đó.

Sau khi confirm, mặt đích trở thành bản sao của mặt nguồn.

#### D. Double Base + Added Value (1 turn)
Chọn **1 dice**.  
Trong **1 turn**, toàn bộ các mặt của dice được chọn được nhân đôi:

- **Base Value**
- **Added Value**

Đây là buff tạm thời cho đúng dice đó, không áp cho toàn bộ bộ dice.

### 6.3 Note về rule edit value khác

Nếu về sau bổ sung các consumable kiểu `+1 / -1 Base` lên các mặt được chọn, chúng nên được mô tả rõ bằng rule riêng trong cùng nhóm Zodiac.  
Hiện tại trọng tâm đã khóa là 4 thao tác trên.

### 6.4 Current interaction flow for Zodiac / dice edit

Flow interaction hiện tại được chốt theo hướng **state-based** và dùng **một logic edit thống nhất** giữa các context.

1. Player có thể hover dice để đọc rõ target đang được nhắm tới.
2. Player có thể click dice hoặc consumable theo thứ tự bất kỳ để đưa chúng vào trạng thái selected.
3. Action **Use** chỉ bật khi:
   - đã có consumable phù hợp đang selected
   - current context đang expose đủ target hợp lệ cho effect đó
4. Khi player bấm **Use**, hệ thống mở dice-edit overlay theo đúng context hiện tại.
5. Trong overlay:
   - player xoay dice đang được hiển thị
   - chọn đủ mặt theo target requirement của effect
   - **Confirm** chỉ sáng khi target requirement đã hoàn tất
6. **Cancel** hủy thao tác; **Confirm** mới áp effect.

Rule context rất quan trọng:

- **Combat context**: overlay hiển thị **1 dice tại một thời điểm**, nên mọi thao tác chỉ xảy ra trong phạm vi dice đó
- **Shop / loadout overlay context**: overlay hiển thị **cả 3 dice**, nên các effect có thể target mặt ở nhiều dice khác nhau
- logic effect là **giống nhau**; khác nhau chỉ ở tập target mà UI/context đang mở cho phép chọn

Rule UX:
- click lại object đang selected → bỏ chọn
- UI không được là source of truth; nó chỉ phản ánh selection state hiện tại
- nếu target requirement chưa đủ, **Use** hoặc **Confirm** phải disabled / màu xám

## 7. Enchant Face
## 7. Enchant Face

### 7.1 Vai trò

Enchant Face là lớp khiến từng mặt dice có “tính cách” riêng, gần với tinh thần enhancement của Balatro.

### 7.2 Rule khóa cứng

- Mỗi mặt chỉ có **1 enchant**.
- Gắn enchant mới vào cùng mặt sẽ **ghi đè** enchant cũ.
- Enchant không stack chồng theo kiểu `+4` rồi thêm `+4` thành `+8` trên cùng một enchant slot.

### 7.3 Current enchant pool

#### A. Value +N
- Bản chất là **+Added Value**
- Không đổi Base Value
- Giá trị này được tính trước và hiển thị sẵn trong tooltip / preview của skill.

#### B. Bomb
- Proc damage lên **front target(s)** theo rule formation hiện hành.
- Dùng để tạo pressure trực diện.

#### C. Snipe
- Proc damage lên **farthest / back target(s)** theo rule formation hiện hành.
- Dùng để chạm mục tiêu sau lưng mà không cần skill riêng.

#### D. Guard Boost
- Cho Guard cho player như một proc tự động.

#### E. Burn Proc
- Áp Burn theo rule proc hiện hành.

#### F. Bleed Proc
- Áp Bleed theo rule proc hiện hành.

#### G. Ice Proc
- Proc Ice theo rule hiện hành.  
- Nếu mục tiêu đã **Freeze** hoặc **Chilled**, player nhận ngay:
  - **+1 Focus**
  - **+3 Guard**

#### H. Mark Proc
- Áp Mark theo rule hiện hành.

#### I. Gold Proc
- Cho Gold khi mặt đó resolve.  
- Đây là enchant thiên về kinh tế, không phải DPS.

### 7.4 Trigger timing tổng quát

Enchant Face kích hoạt ở **Executing Phase**.

#### Rule rất quan trọng:

- **Skill resolve trước**
- **Face enchant resolve sau**

Điều này được giữ để:

- skill tạo trạng thái trước,
- enchant mới đọc hoặc tận dụng trạng thái đó sau.

Ví dụ:

- player dùng skill Freeze trước,
- sau đó `Ice Proc` mới check mục tiêu đang Freeze / Chilled,
- và player nhận ngay `+1 Focus` + `+3 Guard`.

Tương tự:

- Burn / Mark từ skill được áp trước,
- rồi Burn Proc / Mark Proc mới resolve sau,
- không phải ngược lại.

---

## 8. Enchant resolve rules trong combat

### 8.1 Khi mặt enchant đang gắn với action có skill

Nếu player đã equip skill vào action đó:

1. player chọn target cho skill như bình thường,
2. skill resolve trước,
3. enchant của mặt dice gắn với action đó resolve sau.

Ví dụ:

- action 1 dùng mặt có `Bomb`
- action 2 dùng mặt có `Burn Proc`
- action 3 dùng mặt có `Gold Proc`

Khi vào Executing:

- skill 1 → `Bomb`
- skill 2 → `Burn Proc`
- skill 3 → `Gold Proc`

### 8.2 Khi mặt enchant không gắn với skill nào

Nếu player không equip skill cho mặt đó, enchant vẫn có thể resolve như một action riêng tùy loại.

#### A. Tự động resolve

Các enchant sau được coi là **auto action**:

- `Bomb`
- `Snipe`
- `Guard Boost`
- `Gold Proc`

Khi bấm sang Executing:

- hệ thống tự lần lượt resolve từng proc,
- mỗi proc cách nhau khoảng **0.3s**,
- về sau có thể tăng lên **0.4–0.5s** nếu animation dài hơn.

Mỗi proc là một **hit / resolve riêng**, không gộp thành một hit lớn duy nhất.

#### B. Cần target như skill

Các enchant sau nếu không gắn skill thì được coi như **một action cần target**:

- `Burn Proc`
- `Bleed Proc`
- `Mark Proc`
- `Ice Proc`

Nói cách khác: những enchant không auto sẽ được đối xử gần như skill và vẫn cần player chọn target.

#### C. Tự động skip

`Value +N` không tự resolve độc lập nếu không có skill.  
Nó chỉ đóng vai trò tăng output của skill, nên nếu không có skill đi kèm thì được **skip tự động**.

---

## 9. Seals — direct combat consumables

### 9.1 Rule nền

Quy tắc nền hiện tại:

- consumable / skill gây **damage** thì **không tạo effect**,
- consumable / skill áp **effect** thì damage rất thấp hoặc không có.

Mục tiêu là giữ role clarity.

### 9.2 Relic áp effect (8)

#### Burn

**Ember Double**  
- Nhân đôi Burn stacks trên 1 mục tiêu.

**Ignite Spread**  
- Dàn Burn từ 1 mục tiêu sang các mục tiêu khác.

#### Freeze

**Hard Freeze**  
- 100% đóng băng 1 enemy trong 1 lượt.

**Cryostasis**  
- Bật trạng thái kéo dài đến hết Enemy Turn.  
- Đòn tấn công đầu tiên vào player gây **0 damage**.  
- Kẻ tấn công đó bị **Freeze 1 lượt**.

#### Mark

**Mark All**  
- Áp Mark lên mọi enemy.

**Exploit Mark**  
- Tiêu thụ toàn bộ Mark trên tất cả enemy.  
- Player nhận **+1 Focus cho mỗi Mark** đã tiêu.

#### Bleed

**Exsanguinate**  
- Tiêu thụ Bleed trên 1 mục tiêu để hồi máu theo lượng Bleed đã tiêu.

**Bloodletting**  
- Tiêu hết Bleed trên tất cả enemy ngay lập tức.  
- Gây damage bằng **tổng Bleed** đã tiêu.

### 9.3 Relic gây damage trực tiếp

**Target Strike**  
- Chọn 1 mục tiêu.  
- Gây **15 damage**.  
- Không tạo effect đi kèm.

---

## 10. Runes — buff / utility consumables

Runes là nhóm consumable dùng trong combat để:

- hồi tài nguyên,
- buff,
- cứu nguy,
- hoặc tạo utility ngắn hạn.

### 10.1 Current rune pool

**Restore Focus**  
- Hồi **3 Focus**.

**Heal**  
- Hồi **8 HP**.

**Cheat Death**  
- Có thể dùng chủ động để hồi **4 HP**.  
- Nếu giữ tới lúc chết: consumable vỡ và hồi `floor(MaxHP / 2)`.

**Overcap Focus**  
- Trong **1 combat**, player có thể cast skill mà không bị chặn bởi việc thiếu Focus theo rule hiện hành.

**Double Gold**  
- Nhân đôi Gold nhận được, tối đa **+30**.

**Create Last Used Consumable**  
- Tạo lại consumable vừa dùng gần nhất theo rule hiện hành.

**Cleanse**  
- Giải tất cả hiệu ứng trên player.

### 10.2 Direct status utilities (current direction hợp lệ)

Ngoài pool trên, current direction cũng chấp nhận các rune / seal đơn giản theo kiểu:

- chọn 1 mục tiêu áp **5 Burn**,
- chọn 1 mục tiêu **Freeze**,
- chọn 1 mục tiêu áp **Mark**,
- chọn 1 mục tiêu áp **Bleed**.

Các biến thể này chỉ nên được giữ nếu chúng không làm trùng vai quá mạnh với Seals named pool ở trên.

---

## 11. Quan hệ giữa consumable và build direction

### 11.1 Consumable không phải đồ phụ

Ở đúng run, consumable có thể là thứ:

- mở engine mới,
- cứu economy,
- chỉnh die để unlock exact-value payoff,
- cho utility thay đổi sequencing,
- hoặc tạo bước ngoặt khiến build commit sâu hơn vào một hướng.

### 11.2 Dice progression nuôi các fantasy build nào

Dice progression hiện nuôi ít nhất các fantasy sau:

1. **Exact value build**
2. **Parity build (chẵn / lẻ)**
3. **Crit / Fail build**
4. **Highest / Lowest local-group build**
5. **Late-run sculpted dice engine**
6. **Enchant-driven utility / economy faces**

Nếu tính năng mới làm yếu vai trò của dice progression với các fantasy này, nó nên bị xem xét lại.

### 11.3 Pool lớn là chủ đích

Consumable pool rộng là một phần của thiết kế:

- tạo tension trong việc săn đúng món,
- khiến shop reroll meaningful,
- giúp discovery có giá trị,
- tránh run quá deterministic quá sớm.

---

## 12. Guardrails và note quan trọng

### 12.1 Không để dice customization thành chỉ số to hơn

Nếu custom dice chỉ làm số to hơn nhưng không đổi:

- điều kiện skill,
- exact-value access,
- parity profile,
- crit / fail profile,
- sequencing,
- utility face identity,

thì hệ progression đang nông đi.

### 12.2 Không để consumable thay hết skill / passive

Consumable phải là công cụ bẻ nhịp hoặc sculpt run,  
không được nuốt luôn vai trò của skill / passive hoặc phá combat loop nền.

### 12.3 Damage và effect phải giữ role clarity

Rule nền cần tiếp tục giữ:

- damage tool không tiện tay tạo luôn status mạnh,
- effect tool không nên vừa control vừa gây damage lớn.

### 12.4 Enchant Slot

**Enchant Slot / Extra Dice Socket** hiện **chưa chốt**.  
Nó nên được giữ ở trạng thái:

- note tham khảo,
- optional idea,
- chưa phải source of truth để implement.

Lý do: nó đụng trực tiếp tới giới hạn action economy và rất dễ phá balance nếu khóa quá sớm.

---

## 13. Current locked direction

Các điểm sau hiện nên được coi là direction đã khóa đủ mạnh:

- player có **3 shared consumable slot**,
- consumable chia thành **Seals / Zodiac / Runes**,
- consumable **không có rarity kiểu truyền thống**,
- consumable **không cần unlock riêng**,
- shop có thể reroll kiểu Balatro,
- dice progression có 2 trục chính: **edit value** và **enchant face**,
- `Copy / Paste Face` copy **Base + Added + enchant**,
- `Double Base + Added Value` áp lên **1 dice được chọn trong 1 turn**,
- mỗi mặt chỉ có **1 enchant** và enchant mới **ghi đè** enchant cũ,
- `Value +N` là **Added Value**, không phải Base,
- khi có skill: **skill resolve trước, enchant resolve sau**,
- khi không có skill: enchant được chia thành **auto resolve / cần target / auto skip**,
- `Ice Proc` có payoff phụ: nếu mục tiêu đã Freeze / Chilled → `+1 Focus` và `+3 Guard`,
- Dice progression phải tiếp tục là differentiator cốt lõi của game,
- flow dùng Zodiac / dice-edit hiện tại là **state-based selection**,
- combat và shop dùng **cùng một logic edit face**, chỉ khác context target,
- **combat overlay = single-die view**,
- **shop / loadout overlay = multi-die view**,
- trong combat, player có thể edit dice ở cả **Planning** lẫn **Execute** nếu consumable cho phép,
- ngoài combat bình thường, panel dice dùng để **inspect / sell**, không phải nơi edit tự do,
- shop / loadout overlay là nơi:
  - thay dice
  - thêm dice
  - dùng consumable lên nhiều dice
  - và làm các thao tác build-level như cross-dice copy / paste hoặc enchant nhiều mặt ở nhiều dice khác nhau

## 14. Những gì chưa final trong file này
## 14. Những gì chưa final trong file này

Các vùng sau chưa nên coi là fully final:

- full pool consumable ở mức tên gọi cuối cùng cho toàn bộ game,
- wording cuối cùng cho một số seal / rune đơn giản,
- trigger detail cụ thể của Bomb / Snipe theo formation edge case,
- cách trình bày UI đầy đủ của dice-edit overlay và consumable selection,
- toàn bộ animation timing cuối cùng,
- `Enchant Slot` / `Extra Dice Socket`.

Nhưng với trạng thái hiện tại, file này đã đủ mạnh để làm source of truth cho:

- phân loại consumable,
- terminology Base / Added,
- dice progression direction,
- enchant rules,
- và flow resolve giữa skill với enchant.
