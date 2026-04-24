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

Pool current direction đã chốt:

- **23 consumable**
- chia thành **11 Zodiac / 5 Seals / 7 Runes**
- tất cả dùng chung **3 shared slot**

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

Hướng hiện tại:

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

Hướng hiện tại rất rõ:

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

### 6.2 Pool Zodiac hiện tại (11)

#### A. Adjust Face (+) (Permanent)

- Chọn `1-3 mặt` trên cùng `1 dice`
- Tất cả các mặt được chọn nhận `+1 Base`
- Một lần dùng chỉ chỉnh cùng một chiều tăng

#### B. Adjust Face (-) (Permanent)

- Chọn `1-3 mặt` trên cùng `1 dice`
- Tất cả các mặt được chọn nhận `-1 Base`
- Một lần dùng chỉ chỉnh cùng một chiều giảm

#### C. Copy / Paste Face (Permanent)

- Chọn `1 mặt nguồn` và `1 mặt đích`
- Mặt đích trở thành bản sao hoàn toàn của mặt nguồn

**Rule khóa cứng:** Copy / Paste Face copy toàn bộ gói của mặt nguồn, gồm:

- Base Value
- Added Value
- enchant hiện có trên mặt đó

#### D. Double Value (1 turn)

- Chọn `1 dice`
- Trong `1 turn`, dice đó được xem là đang ở trạng thái `Double Value`
- Runtime hiện tại đang làm theo hướng:
  - toàn bộ `face value` của runtime die được nhân đôi ngay lập tức
  - clamp từng mặt về `99`
  - hết turn thì trả về `base` trước khi double
- Đây là buff theo `1 dice trong 1 turn`, không phải permanent sculpt

Rule runtime hiện đang chốt:

- player không cần đợi roll mới thấy dice đã x2
- nếu mặt gốc `10`, khi double sẽ thấy `20` ngay
- nếu trong lúc đang double mà dùng `+1 / -1` lên mặt đó:
  - `10 -> 20 -> 21 -> hết turn = 11`
  - `10 -> 20 -> 19 -> hết turn = 9`
- `Copy / Paste Face` trong lúc dice đang double hiện giữ behavior runtime hiện tại, chưa có rule riêng tách bạch `display value` và `stored base` cho thao tác paste tuyệt đối

#### E. Value +N (Permanent)

- Hiện tại `N = 3`
- Chọn `1-2 mặt`
- Thêm `+3 Added Value` lên các mặt được chọn

#### F. Guard Boost (Permanent)

- Chọn `1-2 mặt`
- Gắn enchant `Guard Boost`

#### G. Gold Proc (Permanent)

- Chọn `1-2 mặt`
- Gắn enchant `Gold Proc`

#### H. Fire (Permanent)

- Chọn `1-2 mặt`
- Gắn enchant `Fire`

#### I. Ice (Permanent)

- Chọn `1-2 mặt`
- Gắn enchant `Ice`

#### J. Bleed (Permanent)

- Chọn `1-2 mặt`
- Gắn enchant `Bleed`

#### K. Lightning (Permanent)

- Chọn `1-2 mặt`
- Gắn enchant `Lightning`

### 6.3 Zodiac targeting / overwrite rules

- `Adjust Face (+)` và `Adjust Face (-)` chỉ chọn trên cùng `1 dice`
- Enchant Zodiac gắn lên `1-2 mặt`
- Enchant mới ghi đè enchant cũ trên chính mặt đó
- `Value +N` là Added Value enchant / modifier, không tạo effect riêng

### 6.4 Flow tương tác hiện tại cho Zodiac / dice edit

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

### 7.3 Pool enchant hiện tại (7)

Face enchant hiện tại được khóa theo 4 trục:

- `Value` = số
- `Resource` = tài nguyên
- `Seed` = áp status
- `Rewrite` = đổi identity của mặt

#### 1. Value layer (`Value = số`)

**Value +N**
- Bản chất là `+Added Value`
- Hiện tại `N = 3`
- Vai trò: tăng output thuần

#### 2. Resource layer (`Resource = tài nguyên`)

**Guard Boost**
- roll ra mặt này nhận `+3 Guard`
- Vai trò: cho tài nguyên trực tiếp

**Gold Proc**
- roll ra mặt này nhận `+5 Gold`
- Vai trò: cho tài nguyên trực tiếp

#### 3. Status-seed layer (`Seed = áp status`)

**Fire**
- roll ra mặt này áp `2 Burn` lên `1 enemy random` còn sống
- Vai trò: gieo status lên enemy

**Bleed**
- roll ra mặt này áp `2 Bleed` lên `1 enemy random` còn sống
- Vai trò: gieo status lên enemy

#### 4. Identity-rewrite layer (`Rewrite = đổi identity của mặt`)

**Lightning**
- mặt này được tính là **cả Crit và Fail** cho các điều kiện skill / passive
- mặt này **không nhận bonus Crit**
- mặt này **không chịu penalty Fail**
- Vai trò: đổi cách mặt dice này được đọc / hoạt động

**Ice**
- mặt này luôn cho `+5 Added Value`
- mặt này **không còn là mặt số bình thường nữa**
- Vai trò: đổi cách mặt dice này được đọc / hoạt động

### 7.4 Trigger timing tổng quát

Enchant Face hiện tại kích hoạt theo timing:

- `face roll ra`
- `enchant trigger ngay`
- `skill resolve sau nếu có`

Điểm khóa cứng:

- enchant hoàn toàn độc lập với skill
- face không có skill vẫn trigger enchant
- face có skill cũng trigger enchant riêng
- enchant không chờ `Executing Phase` mới chạy sau skill nữa

---

## 8. Enchant resolve rules trong combat

Toàn bộ enchant pool hiện tại đều là `on-roll auto proc`.

### 8.1 Rule chung

- không cần skill để kích hoạt
- không cần player chọn target tay
- nếu enchant là proc áp status thì target được chọn theo rule random hiện tại
- nếu target không hợp lệ thì enchant phải reroll hoặc skip theo đúng rule từng loại

### 8.2 Target / effect rule cho enchant auto

`Value +N`  
- chỉ tăng số

`Guard Boost`  
- `+3 Guard` cho player

`Gold Proc`  
- `+5 Gold` cho player

`Fire`  
- áp `2 Burn` lên `1 enemy random` trong `all alive enemies`

`Bleed`  
- áp `2 Bleed` lên `1 enemy random` trong `all alive enemies`

`Lightning`  
- không áp status
- không cho tài nguyên trực tiếp
- rewrite cách mặt đó được đọc cho condition của skill / passive:
  - luôn tính là `Crit`
  - luôn tính là `Fail`
  - nhưng không nhận bonus Crit
  - và không chịu penalty Fail

`Ice`  
- luôn cho `+5 Added Value`
- mặt đó không còn được đọc như một mặt số bình thường nữa

---

## 9. Seals — direct combat consumables

### 9.1 Rule nền

Quy tắc nền hiện tại:

- consumable / skill gây **damage** thì **không tạo effect**,
- consumable / skill áp **effect** thì damage rất thấp hoặc không có.

Mục tiêu là giữ role clarity.

### 9.2 Seals pool (5)

#### Burn

**Ignite Spread**  
- Dàn Burn từ 1 mục tiêu sang các mục tiêu khác.

#### Freeze

**Cryostasis**  
- Bật trạng thái kéo dài đến hết Enemy Turn.  
- Đòn tấn công đầu tiên vào player gây **0 damage**.  
- Kẻ tấn công đó bị **Freeze 1 lượt**.

#### Mark

**Exploit Mark**  
- Mỗi `Mark` đang có trên enemy tạo `1 consumable`
- Tối đa `4`

#### Bleed

**Exsanguinate**  
- Tiêu thụ Bleed trên 1 mục tiêu để hồi máu theo lượng Bleed đã tiêu.

### 9.3 Direct damage Seal

**Last Rite**  
- Chọn `1 mục tiêu`
- Gây `20 damage` thuần
- Không tạo effect đi kèm

---

## 10. Runes — buff / utility consumables

Runes là nhóm consumable dùng trong combat để:

- hồi tài nguyên,
- buff,
- cứu nguy,
- hoặc tạo utility ngắn hạn.

### 10.1 Pool rune hiện tại

**Restore Focus**  
- Hồi **3 Focus**.

**Heal**  
- Hồi **8 HP**.

**Cheat Death**  
- Là lá bùa hộ mệnh giữ trong slot
- Có thể dùng chủ động để hồi `4 HP`
- Nếu không dùng trước và player chết:
  - consumable tự vỡ
  - player sống lại với `floor(MaxHP / 2)`

**Dice Reroll**  
- Chọn dice
- reroll những dice đã chọn

**Double Gold**  
- Nhân đôi `Gold hiện có` tại thời điểm dùng
- Lượng Gold tăng thêm tối đa `+30`

**Create Last Used Consumable**  
- Tạo lại loại consumable vừa dùng gần nhất
- Tinh thần đúng là gần với lá `Death` trong Balatro: sao chép lại đúng loại vừa commit dùng

**Cleanse**  
- Xóa tất cả hiệu ứng bất lợi hiện có trên player
- Mỗi consumable `Cleanse` chỉ được dùng `1 lần mỗi combat`

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

## 13. Hướng khóa hiện tại

Các điểm sau hiện nên được coi là direction đã khóa đủ mạnh:

- player có **3 shared consumable slot**,
- consumable chia thành **Seals / Zodiac / Runes**,
- consumable **không có rarity kiểu truyền thống**,
- consumable **không cần unlock riêng**,
- shop có thể reroll kiểu Balatro,
- dice progression có 2 trục chính: **edit value** và **enchant face**,
- `Copy / Paste Face` copy **Base + Added + enchant**,
- `Double Value` là buff **1 dice trong 1 turn**; mặt nào roll ra thì mặt đó nhân đôi `Base + Added`,
- mỗi mặt chỉ có **1 enchant** và enchant mới **ghi đè** enchant cũ,
- `Value +N` là **Added Value**, không phải Base,
- enchant trigger theo rule **on-roll**, độc lập với skill,
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
- cách trình bày UI đầy đủ của dice-edit overlay và consumable selection,
- toàn bộ animation timing cuối cùng,
- `Enchant Slot` / `Extra Dice Socket`.

Nhưng với trạng thái hiện tại, file này đã đủ mạnh để làm source of truth cho:

- phân loại consumable,
- terminology Base / Added,
- dice progression direction,
- enchant rules,
- và flow resolve giữa skill với enchant.
---

## 14A. Bổ Sung Về Whole-Die Color

Phần bổ sung này cập nhật hướng khóa hiện tại của tài liệu.

Nếu phần này mâu thuẫn với các dòng cũ trong file vẫn còn mô tả dice progression chỉ có 2 trục, thì phần bổ sung này được ưu tiên.

### 14A.1 Trục progression thứ ba

Dice progression hiện tại có 3 trục:

1. `edit value`
2. `face enchant`
3. `whole-die color / whole-die tag`

`whole-die color` là lớp áp lên toàn bộ viên dice.
Nó tách riêng khỏi `face enchant`.

### 14A.2 Vai trò của whole-die color

Whole-die color là:

- một lớp profile / tăng trưởng cho cả viên dice
- không phải lớp proc riêng cho từng mặt
- không phải passive áp lên cả build
- không phải lớp đổi action economy

Cách đọc ngắn:

- `face enchant` = hành vi / payoff ở cấp từng mặt
- `whole-die color` = identity / tăng trưởng ở cấp cả viên dice

### 14A.3 Luật toàn cục

- Mỗi viên dice chỉ có `1 whole-die color`.
- Color mới ghi đè color cũ.
- Khi bị ghi đè:
  - hiệu ứng đang chạy của color cũ dừng lại
  - các thay đổi vĩnh viễn mà color cũ đã tạo ra vẫn được giữ lại

Face enchant vẫn giữ luật cũ:

- mỗi mặt chỉ có `1 enchant`
- enchant mới ghi đè enchant cũ

### 14A.4 Luật UI / trình bày

- màu thân viên dice = `whole-die color`
- icon / font / symbol trên từng mặt = `face enchant`

Player phải có thể nhìn lướt là đọc được:

- viên dice đang mang màu gì
- mặt đó đang có enchant gì

### 14A.5 Pool whole-die color hiện tại

Giá trị hiện đang khóa:

- `None`
- `Patina`

### 14A.6 Patina

`Patina` là whole-die color đầu tiên.

Fantasy:

- tăng trưởng / làm mượt profile của cả viên dice

Hiệu ứng:

- sau mỗi combat
- nếu viên dice này đã được dùng ít nhất 1 lần trong combat đó
- và viên dice này có đúng `1 mặt thấp nhất duy nhất`
- thì mặt thấp nhất đó nhận `+1 Base vĩnh viễn`

Luật bổ sung:

- nếu có nhiều mặt đồng hạng thấp nhất -> không tăng
- nếu tất cả các mặt bằng nhau -> không tăng

### 14A.7 Ý nghĩa thiết kế của Patina

- Hợp với `d12 / d20` đặc biệt tốt vì nó dần dần xóa bớt các mặt rác.
- Cố ý anti-synergy với các build exact-value / perfect-number.
- Nó không buff damage trực tiếp; nó đổi quỹ đạo tăng trưởng dài hạn của viên dice.
- Nó có trade-off thật vì việc nâng từ đáy lên có thể làm tăng số lượng mặt đồng hạng thấp nhất, từ đó ảnh hưởng profile `Fail` dưới luật Base Value hiện tại.

Crit / Fail vẫn tiếp tục đọc từ `Base Value`.

### 14A.8 Những gì Patina không được làm

- không tạo proc damage / status trực tiếp
- không refund resource cho cả build
- không giảm slot cost / không cho thêm action / không cho thêm dice socket
- không rewrite trực tiếp lane-order hay action economy

`Enchant Slot / Extra Dice Socket` vẫn còn mở và chưa được khóa.

### 14A.9 Cập nhật locked direction

Từ phần bổ sung này trở đi, hướng hiện tại phải được hiểu là:

- dice progression có 3 trục: `edit value`, `face enchant`, `whole-die color`
- mỗi viên dice có tối đa `1 whole-die color / tag`
- whole-die color là lớp profile / tăng trưởng, không phải lớp proc
- `Patina` là whole-die color đầu tiên đã được khóa
- `Patina`: sau combat, nếu viên dice đã được dùng ít nhất 1 lần và có đúng `1 mặt thấp nhất duy nhất`, mặt đó nhận `+1 Base vĩnh viễn`
- nếu có nhiều mặt đồng hạng thấp nhất hoặc tất cả các mặt bằng nhau thì `Patina` không cho gain
- luật trình bày:
  - thân viên dice = `whole-die color`
  - dấu / icon / symbol trên mặt = `face enchant`

## 15. Trạng Thái Dice Edit Sandbox - SampleScene

Phần này ghi lại trạng thái thực tế đang dùng để test `dice edit` trong `SampleScene`.
Đây là sandbox runtime tách riêng khỏi `GameScene`, mục tiêu là verify interaction chọn mặt dice trước khi nối vào consumable flow.

### 15.1 Mục tiêu đã làm

- Có `runtime sandbox` riêng cho `SampleScene`
- Có panel runtime để test `Use / Clear / Flip`
- Player có thể:
  - giữ chuột và xoay dice tự do
  - click vào một mặt cụ thể trên mesh 3D
  - mặt được click sẽ được map về `logical face`
  - highlight dung mat da chon

### 15.2 Script sandbox hiện tại

Toàn bộ logic sandbox đang nằm trong:

- `Assets/Scripts/DiceEditSandbox/DiceEditRuntimeBootstrap.cs`
- `Assets/Scripts/DiceEditSandbox/DiceEditSandboxController.cs`
- `Assets/Scripts/DiceEditSandbox/DiceEditInteractable.cs`
- `Assets/Scripts/DiceEditSandbox/DiceFaceSelectionMap.cs`
- `Assets/Scripts/DiceEditSandbox/DiceFaceHighlightRenderer.cs`

Ý nghĩa:

- `DiceEditRuntimeBootstrap`: tu boot sandbox khi mo `SampleScene`
- `DiceEditSandboxController`: panel runtime va state `selected / committed`
- `DiceEditInteractable`: free inspect rotation + click select
- `DiceFaceSelectionMap`: map `triangleIndex -> face group -> logical face`
- `DiceFaceHighlightRenderer`: render highlight mesh cho dung mat duoc chon

### 15.3 Setup scene đang dùng

Với một dice trong `SampleScene`, setup đang dùng là:

- `Pivot_D8`
  - `DiceSpinnerGeneric`
  - `DiceEditInteractable`
  - `DiceFaceSelectionMap`
  - `DiceFaceHighlightRenderer`

- `Dice_d8` (object con cua pivot)
  - `MeshFilter`
  - `MeshRenderer`
  - `MeshCollider`

Rule quan trọng:

- `Pivot_*` la object xoay chinh
- object con mesh la noi nhan raycast triangle
- highlight phai bam vao transform cua mesh con, khong bam vao pivot

### 15.4 Rule chọn mặt đang dùng

Hướng đang dùng hiện tại là:

- player xoay dice tu do bang chuot
- khi click, he thong raycast vao `MeshCollider`
- doc `RaycastHit.triangleIndex`
- tu `triangleIndex` map sang `face group`
- tu `face group` map sang `logical face index`
- highlight dung mat tam giac / polygon da chon

Đây là hướng `click polygon thật` chứ không phải:

- chon theo `front-facing face`
- không phải chọn theo `face index browse`
- không phải chọn qua overlay 2D

### 15.5 Điều kiện kỹ thuật bắt buộc

Muốn chọn mặt theo triangle thì mesh asset phải:

- `Read/Write Enabled = ON`

Nếu không, code không đọc được:

- `mesh.vertices`
- `mesh.triangles`

và sẽ không build được `face groups`.

Với `d8` đang test, phải bật trên asset mesh nguồn, không phải object trong scene.

### 15.6 Điều đã xác nhận

- Raycast vao mesh con da hit dung collider
- Runtime da co the phan biet object xoay (`Pivot_D8`) va object mesh (`Dice_d8`)
- Sandbox da khong con phu thuoc vao `GameScene`
- Highlight da duoc tach thanh mesh rieng cho mat da chon

### 15.7 Điều chưa khóa

Những điểm sau chưa final:

- free inspect rotation hiện tại chưa phải feel cuối cùng
- orientation presentation của từng face chưa được author riêng
- mapping face của `d8` đã chạy theo mesh click, nhưng chưa verify đủ cho các dice khác
- Zodiac / dice-edit consumable flow thật vẫn chưa khóa
- `Use` trong sandbox vẫn chưa là flow consumable final

### 15.7A Trạng thái runtime đã có sau đợt update này

Mặc dù sandbox consumable chưa khóa, lớp runtime của progression đã được nối vào combat theo hướng sau:

- `DiceFace` đã có field `enchant`
- `DiceSpinnerGeneric` đã có `wholeDieTag`
- `DiceSpinnerGeneric` có thể hiển thị debug text của mặt hiện tại qua `enchantText`
- `Value +N` đã đi vào Added Value runtime
- face enchant đã có logic resolve combat riêng
- whole-die `Patina` đã có logic tăng trưởng riêng ở cuối combat

Face enchant runtime hiện đang dùng:

- `Guard Boost` -> `+3 Guard`
- `Gold Proc` -> `+5 Gold`
- `Fire` -> áp `2 Burn` lên `1 enemy random`
- `Bleed` -> áp `2 Bleed` lên `1 enemy random`
- `Lightning` -> được đọc là cả `Crit` và `Fail` cho condition, nhưng không ăn crit bonus và không chịu fail penalty
- `Ice` -> luôn cho `+5 Added Value` và không còn là mặt số bình thường

Luật runtime hiện đang dùng:

- enchant trigger khi face roll ra
- enchant độc lập với skill
- face không có skill vẫn trigger enchant
- face có skill cũng trigger enchant ngay sau roll, không chờ skill resolve xong mới chạy

Whole-die runtime hiện đang dùng:

- `None`
- `Patina`

`Patina` hiện tại:

- chỉ xét khi thắng combat
- dice phải đã được dùng ít nhất 1 lần trong combat
- dice phải có `1 mặt thấp nhất duy nhất`
- nếu hợp lệ -> mặt đó nhận `+1 Base vĩnh viễn`
- nếu có nhiều mặt đồng hạng thấp nhất hoặc tất cả các mặt bằng nhau -> không tăng

Debug test hook tạm thời đã có ở `DiceSlotRig`:

- checkbox `enableDebugRollHotkeys`
  - `Space` = roll tất cả
  - `A / S / D` = reroll slot `1 / 2 / 3`
- checkbox `allowDebugRerollThisTurn`
  - bật -> cho reroll trong cùng turn
  - tắt -> giữ khóa roll sau lần đầu

Hook này đọc theo `slot hiện tại`, không theo identity của từng die.

### 15.8 Hướng tiếp theo sau sandbox này

Sau khi sandbox chọn mặt ổn định, hướng tiếp theo nên là:

1. verify trong Unity combat runtime của `face enchant` và `Patina`
2. khóa interaction grammar cho `inspect -> select face -> Use / Confirm`
3. nối `selected logical face` vào Zodiac consumable đầu tiên
4. phân biệt rõ:
   - tactical combat dice edit
   - permanent shop / loadout dice edit
5. chỉ sau đó mới refine presentation / animation feel

### 15.9 Khoảng trống implementation hiện tại trước batch consumable tiếp theo

Tính đến cuối đợt chat này, những phần sau của `consumable` vẫn chưa được code:

- chưa có `consumable data model` thật để author `Zodiac / Seals / Runes`
- chưa có runtime inventory cho `3 shared consumable slot`
- chưa có `use flow` đầy đủ cho consumable trong combat
- chưa có `use flow` đầy đủ cho Zodiac dice-edit ngoài combat / trong sandbox
- chưa có link giữa `selected logical face` và một consumable Zodiac cụ thể

Nói rõ hơn, “chưa có consumable thật” ở đây có nghĩa là chưa có đầy đủ 3 lớp sau:

1. `lớp dữ liệu`
   - chưa có asset / data model thật để define từng consumable,
   - chưa có nơi author rarity / type / target / use rule / effect payload theo một khung ổn định.

2. `lớp inventory / slot`
   - chưa có runtime đại diện cho `3 shared consumable slot`,
   - chưa có logic add / remove / stack / overwrite / fill / consume trong inventory thật.

3. `lớp tương tác / sử dụng`
   - chưa có flow để player bấm vào consumable, chọn target / chọn face / xác nhận sử dụng,
   - chưa có route riêng cho:
     - consumable dùng trong combat,
     - Zodiac dùng để edit dice,
     - consumable utility như heal / focus / cleanse / double gold / cheat death.

Nghĩa là:

- spec consumable đã đủ rõ
- runtime enchant / dice progression đã có một phần nền
- nhưng hệ consumable thật vẫn chưa bắt đầu ở lớp `data -> slot -> use`

Những gì đã có hiện tại chỉ là nền để nối consumable vào sau này:

- face enchant runtime mới,
- whole-die `Patina`,
- dice-edit sandbox có logic inspect / select face,
- debug test hook để reroll dice nhanh trong scene.

Những thứ đó chưa đủ để gọi là đã có hệ consumable runtime.

Batch tiếp theo nên đi theo thứ tự này:

1. tạo `consumable data`
2. tạo `3 shared consumable slot runtime`
3. nối `use flow`
4. rồi mới nối consumable Zodiac đầu tiên vào dice-edit sandbox / combat flow

Thứ tự này quan trọng vì:

- nếu làm `slot/use flow` trước khi có data model, hệ sẽ dễ vỡ,
- nếu nối Zodiac vào sandbox trước khi có shared-slot runtime, sẽ rất dễ phải viết tạm logic riêng rồi đập đi,
- nếu có data + slot trước, thì các consumable sau này sẽ đi vào cùng một khung ổn định hơn.
