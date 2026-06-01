# Codex Source of Truth — Reward System + 12 Face Enchant

> Mục tiêu: thay reward gacha hỗn hợp hiện tại và thay hệ face enchant nguyên tố cũ bằng một hệ thống rõ vai trò hơn.
>
> File này là source of truth mới cho phần:
>
> - map reward / combat reward / shop / forge / event;
> - consumable pool và Consumable Pack;
> - 12 face enchant mới;
> - `Broken`, `Echo`, `Stone`, overwrite enchant;
> - migration từ spec/code cũ.

---

## 0. Không được phá các hệ hiện có

Giữ nguyên các hệ sau nếu chúng đang tồn tại trong project:

- combat turn flow;
- dice roll đầu Player Phase;
- reorder dice;
- AP;
- skill target / skill preview;
- enemy intent;
- status và element của skill như `Fire`, `Burn`, `Ice`, `Freeze`, `Chilled`, `Lightning`, `Mark`, `Bleed`;
- whole-die tag/color như `Patina`;
- `Boss Preparation` trên map;
- `Boss Intel` nếu đang có;
- `Ore` dùng cho Forge;
- Shop là node trên map;
- Forge là Hub/Forge riêng, không phải shop nhỏ.

Không được xóa hệ element/status của skill. Chỉ thay **face enchant trên từng mặt dice**.

---

# PHẦN A — REWARD / MAP / CONSUMABLE

## A1. Hướng thiết kế cuối cùng

Không copy hoàn toàn một game nào.

Dùng mô hình lai:

```text
Map cây rẽ nhánh kiểu Slay the Spire
+ nhìn trước loại reward trên node kiểu Hades
+ Consumable Pack 3 chọn 1 kiểu Balatro
+ mỗi node/dịch vụ có vai trò riêng kiểu Monster Train
```

Không dùng shop sau mỗi combat. Không dùng reroll shop vô hạn. Không dùng một gacha pool trộn chung `Skill / Relic / Consumable / Gold`.

---

## A2. Bỏ reward gacha hỗn hợp sau combat

Deprecate flow cũ:

```text
Thắng combat
→ hiện 3/4/5 card reward
→ card có thể là Skill / Relic / Extra Gold / Fate / Seal / Rune
→ chọn 1 hoặc 2
```

Lý do bỏ:

- Skill, Relic, Consumable và Gold tranh cùng một slot reward.
- Nếu tăng consumable thì skill/relic bị loãng.
- Nếu giảm consumable thì custom dice bị đói.
- Game có nhiều trục build, nên mỗi trục cần reward lane riêng.

---

## A3. Combat node có `Spoils Lane` nhìn thấy trước

Mỗi Combat node trên map có một `Spoils Lane` hiển thị trước bằng icon.

Các lane cơ bản:

| Spoils Lane | Ý nghĩa | Reward sau khi thắng |
| --- | --- | --- |
| `Skill Cache` | Player cần đổi/mở rộng loadout skill | Base Gold + mở 3 Skill random, chọn 1 hoặc Skip |
| `Consumable Pack` | Player cần sculpt dice / dùng utility | Base Gold + mở 3 consumable random, chọn 1 hoặc Skip |
| `Gold Cache` | Player cần economy | Base Gold + Extra Gold cố định |

Node hiển thị trước loại reward, nhưng không hiển thị món cụ thể bên trong.

Ví dụ map:

```text
Start
├─ Combat: Skill Cache
└─ Combat: Consumable Pack
```

Player được chọn loại reward mình đang thiếu, nhưng không được chọn chính xác món cần tìm.

---

## A4. Combat thường

Combat thường resolve như sau:

```text
Thắng Combat thường
→ nhận Base Gold
→ nhận reward theo Spoils Lane của node
→ quay lại map
```

### Skill Cache

```text
Mở 3 Skill random
→ chọn 1 hoặc Skip
```

Skill không cần xuất hiện sau mọi combat. Chỉ xuất hiện khi node là `Skill Cache`.

### Consumable Pack

```text
Mở 3 consumable random từ toàn bộ consumable pool
→ chọn 1 hoặc Skip
```

Rule:

- mọi consumable có cùng rarity;
- mọi consumable có cùng weight `1`;
- không có weight ẩn cho Rune/Fate/enchant;
- không guarantee đúng enchant player cần;
- trong một Pack không nên có duplicate cùng tên;
- tăng số món được nhìn thấy bằng Pack, không tăng số món được sở hữu.

### Gold Cache

```text
Nhận Base Gold + Extra Gold
```

Không cần mở gacha.

---

## A5. Elite

Elite là node rủi ro cao và phải có reward lane riêng.

Flow:

```text
Thắng Elite
→ nhận Base Gold
→ nhận Ore chắc chắn
→ nhận 1 Relic ngẫu nhiên hoặc Relic reward theo hệ hiện có
→ mở 1 Consumable Pack: 3 chọn 1
→ quay lại map
```

Elite không dùng reward gacha hỗn hợp.

Nếu playtest thấy Elite quá lời, giảm phần `Consumable Pack` trước. Không nên làm `Ore` hoặc `Relic` thành random vì chúng là identity chính của Elite.

---

## A6. Boss

Boss reward là reward lớn riêng.

Flow:

```text
Thắng Boss
→ nhận Boss Reward / Major Reward riêng
→ ví dụ chọn 1 trong 3 Boss Relic hoặc major upgrade
→ sang act tiếp theo
```

Không trộn Boss Reward với:

- Skill thường;
- consumable nhỏ;
- Gold card;
- Fate/Seal/Rune card nhỏ.

Boss reward phải tạo cảm giác chọn hướng phát triển lớn.

---

## A7. Shop

Shop vẫn là node trên map. Không xuất hiện sau mỗi combat.

Shop inventory baseline:

| Hàng bán | Số lượng |
| --- | ---: |
| Skill trực tiếp | 3 |
| Consumable trực tiếp | 3 |
| Consumable Pack | 1 pack |
| Relic | 1 |
| Dice | 1 |

`Consumable Pack` trong shop:

```text
Mua Pack
→ mở 3 consumable random
→ chọn 1
→ 2 món còn lại biến mất
```

Shop không deterministic quá mức:

- không bán đúng enchant player chọn từ list đầy đủ;
- không có quầy Rune riêng;
- không cho reroll vô hạn;
- không shop sau mỗi combat.

Shop refresh:

- giữ rule refresh theo `Boss Preparation` hiện có;
- shop không refresh chỉ vì player mở lại cùng shop;
- player có thể greed đi thêm node để chờ refresh, nhưng Boss Preparation tăng.

---

## A8. Forge

Forge chỉ phục vụ whole-die progression bằng `Ore`.

Flow:

```text
Forge
→ dùng Ore
→ chỉnh whole-die color / whole-die tag / Patina-like progression
```

Forge không bán consumable thường.
Forge không cho chọn chính xác enchant như `Echo`, `Double`, `Stone`.
Forge không phải shop thu nhỏ.

Lý do: nếu Forge cho chọn enchant cụ thể, player có thể tự fix dice theo công thức và variance biến mất.

---

## A9. Event

Event là nguồn phá nhịp có kiểm soát.

Event có thể:

```text
Cho 1 Consumable Pack
Cho 1 Skill Cache
Cho đổi 1 consumable đang giữ lấy 1 consumable random khác
Cho mua 1 Pack giá rẻ
Cho mở 2 Pack nhưng chỉ chọn 1 món
Cho reward mạnh kèm cost như mất HP hoặc tăng Boss Preparation
```

Event không cần dùng combat gacha hỗn hợp.

Nếu event có combat:

- combat reward dùng theo lane đã ghi trong event;
- event reward riêng phải ghi rõ;
- không tự động cộng thêm reward lớn nếu không có cost rõ.

---

## A10. Consumable pool

Tất cả consumable cùng rarity và cùng weight.

Rule tổng quát:

```text
Every consumable in the global consumable pool has weight = 1.
```

Không chia:

- Common / Rare / Epic;
- Rune hiếm hơn Fate;
- Echo Rune hiếm hơn Power Rune;
- utility consumable hiếm hơn enchant consumable.

Cân bằng bằng:

- số `Consumable Pack` xuất hiện trên map;
- số lựa chọn trong mỗi Pack;
- giá bán ở Shop;
- inventory limit;
- Boss Preparation cost khi greed thêm node.

Không cân bằng bằng hidden rarity.

---

## A11. Consumable dùng để gắn face enchant

Consumable gắn enchant có thể thuộc group hiện có như `Fate`, hoặc rename theo project. Nhưng behavior cuối là:

```text
Một consumable apply đúng 1 enchant lên đúng 1 face dice được chọn.
```

Rule khi apply:

- chọn 1 die;
- chọn 1 logical face;
- apply enchant mới lên face đó;
- enchant mới ghi đè enchant cũ;
- nếu face đang `Broken`, face được repair;
- nếu face là `Stone`, Base Value hiện lại nếu enchant mới không phải Stone;
- mỗi face chỉ có tối đa 1 stored enchant.

Không mặc định apply lên 2 mặt.

Có thể tạo consumable đặc biệt sau này apply 2 mặt, nhưng đó là item phá luật riêng, không phải baseline.

---

## A12. Map generation quota mềm

Khi sinh map, dùng quota mềm cho Spoils Lane để route bình thường có đủ cơ hội build nhưng không hoàn hảo deterministic.

Baseline mỗi act:

| Reward lane | Số lần có thể tiếp cận trên route bình thường |
| --- | ---: |
| `Skill Cache` | khoảng 3 |
| `Consumable Pack` | khoảng 3 |
| `Gold Cache` | khoảng 1–2 |
| `Shop` | khoảng 1 |
| `Elite` | khoảng 1 nếu player chọn liều |
| `Forge` | theo Hub/Forge hiện có |

Không hard-cap số enchant trên dice. Dùng số cơ hội thấy consumable để cân bằng.

Mục tiêu trước final boss:

```text
Run bình thường:
- dice tốt nhất có identity rõ;
- khoảng 70–85% mặt đúng ý;
- chưa chắc hoàn hảo.

Run may mắn:
- có thể hoàn thiện 100% một dice.

Run bình thường:
- không đủ để hoàn thiện cả 3 dice.
```

---

# PHẦN B — 12 FACE ENCHANT

## B1. Danh sách 12 enchant cuối cùng

Mỗi face chỉ có tối đa một enchant.

| Enchant | Timing | Hiệu ứng | Sau khi kích |
| --- | --- | --- | --- |
| `Power` | `On Use` | Skill/action nhận `+2 Added Value`. | Giữ nguyên |
| `Guard` | `On Use` | Nhận Guard bằng resolved Value hiện tại của mặt này. | Giữ nguyên |
| `Charge` | `On Use` | Nhận `+1 AP`. | Giữ nguyên |
| `Gold` | `On Use` | Đánh dấu nhận thêm Gold sau khi thắng combat. | Giữ nguyên |
| `Gum` | `Passive` | Mặt đối diện dễ roll ra hơn. | Giữ nguyên |
| `Relay` | `On Use` | Dice ngay bên phải nhận `+2 Value` trong Player Phase hiện tại. | Giữ nguyên |
| `Double` | `On Use` | Value hiện tại của mặt này `×2` trong action hiện tại. | `Broken` |
| `Repeat` | `Post-Skill` | Skill payload thực thi thêm `1` lần, không trả thêm AP/dice. | `Broken` |
| `Reload` | `Post-Skill` | Sau skill, reroll dice này và cho phép dùng lại theo rule Broken. | `Broken` |
| `Heavy` | `Pay Cost` | Face này đóng góp `2 dice` khi trả dice-slot cost. | `Broken` |
| `Echo` | Effective copy | Copy enchant hợp lệ của dice bên trái, không copy Value. | `Broken` sau mọi committed use |
| `Stone` | Static identity override + `On Use` | Face này mất numeric identity. Khi dùng, skill/action nhận `+5 Added Value`. | Giữ nguyên |

Crit multiplier hiện tại là:

```text
Crit = ×1.3
```

Giữ nguyên multiplier này. Không khôi phục multiplier cũ.

---

## B2. Xóa face enchant nguyên tố cũ

Face enchant cũ không còn là source of truth:

- `Value +N`
- `Guard Boost`
- `Gold Proc`
- `Fire`
- `Bleed`
- `Lightning`
- `Ice`

Mapping migration đề xuất:

| Legacy face enchant | New value |
| --- | --- |
| `Value +N` | `Power` |
| `Guard Boost` | `Guard` |
| `Gold Proc` | `Gold` |
| `Fire` | `None` |
| `Bleed` | `None` |
| `Lightning` | `None` |
| `Ice` | `None` hoặc migrate thủ công sang `Stone` nếu asset đó rõ ràng đang dùng làm non-numeric face |

Không xóa `Fire / Ice / Lightning / Bleed` khỏi skill/status system.

---

## B3. Timing model

Không resolve mọi enchant khi roll.

Timing mới:

| Timing | Enchant |
| --- | --- |
| `Passive` | `Gum` |
| `Pay Cost` | `Heavy`, `Echo` nếu copy `Heavy` |
| `Static identity override` | `Stone`, `Echo` nếu copy `Stone` |
| `On Use` | `Power`, `Guard`, `Charge`, `Gold`, `Relay`, `Double`, `Stone`, `Echo` khi copy các enchant này |
| `Post-Skill` | `Repeat`, `Reload`, `Echo` khi copy các enchant này |

Roll ra mặt không tự kích:

- `Power`
- `Guard`
- `Charge`
- `Gold`
- `Relay`
- `Double`
- `Repeat`
- `Reload`
- `Heavy`
- `Echo`
- bonus Added Value của `Stone`

Roll chỉ cần hiển thị state hiện tại, ví dụ Stone che số và Gum ảnh hưởng weight.

---

## B4. Broken state

`Broken` là face state riêng, không phải enchant.

Khi một face Broken:

- toàn bộ logical face bị vô hiệu hóa;
- face không thể dùng để trả cost;
- face không kích enchant;
- nếu roll trúng face Broken, die đó unusable;
- Base Value và stored enchant vẫn được giữ để repair sau này;
- used state reset theo phase bình thường;
- Broken state không tự reset;
- Broken tồn tại cho tới khi được repair hoặc overwrite enchant.

Không model Broken bằng cách set enchant thành `None`.

---

## B5. Overwrite enchant + repair rule

Mỗi logical face chỉ có tối đa 1 stored enchant.

Khi apply enchant mới lên một face:

```text
Enchant mới ghi đè enchant cũ.
Face được repair nếu đang Broken.
Base Value được giữ nguyên.
UI refresh ngay.
```

Ví dụ:

| Trước | Apply | Sau |
| --- | --- | --- |
| `[Stone]`, hidden Base Value `4` | `Power` | `[Power 4]` |
| `[Power 4]` | `Relay` | `[Relay 4]` |
| `[Double 5]` | `Stone` | `[Stone]`, hidden Base Value `5` |
| `[Broken Double 5]` | `Power` | `[Power 5]`, repaired |
| `[Stone]`, hidden Base Value `3` | `None` | `[None 3]`, repaired |

Apply `None` cũng là clear enchant có chủ đích:

- xóa enchant;
- repair face;
- hiện lại Base Value;
- không tạo Broken.

`Echo` không dùng overwrite rule vì Echo chỉ tạo effective enchant tạm thời trong action.

---

## B6. Power

```text
Power: On Use
Effect: +2 Added Value cho skill/action hiện tại
After: giữ nguyên
```

Rule:

- nhiều Power stack cộng dồn;
- `Power + Echo` = tổng `+4 Added Value` nếu Echo copy Power;
- Repeat payload không kích lại Power lần nữa.

---

## B7. Guard

```text
Guard: On Use
Effect: gain Guard bằng resolved Value hiện tại của face
After: giữ nguyên
```

Rule:

- đọc Value sau modifier đã áp trước đó trong chuỗi resolve trái sang phải;
- nếu Echo copy Guard, dùng Value của Echo.

Ví dụ:

```text
[Guard 5] [Echo 2]
→ gain 5 Guard + 2 Guard
→ tổng 7 Guard
→ Echo Broken
```

---

## B8. Charge

```text
Charge: On Use
Effect: +1 AP
After: giữ nguyên
```

Rule:

- chỉ kích sau khi action đã valid và committed;
- không thể retroactively giúp cast hiện tại nếu thiếu AP;
- AP nhận được dùng cho cast tiếp theo trong cùng Player Phase.

---

## B9. Gold

```text
Gold: On Use
Effect: mark face để nhận thêm Gold sau khi thắng combat
After: giữ nguyên
```

Rule:

- không cấp Gold ngay;
- chỉ cấp khi victory commit;
- mỗi physical logical face chỉ mark một lần mỗi combat;
- tránh farm vô hạn bằng Reload;
- Echo copy Gold mark token của Echo face, không mark lại source face;
- default reward tạm: `+5 Gold` mỗi marked face, config được trong Inspector.

---

## B10. Gum

```text
Gum: Passive
Effect: mặt đối diện có roll weight cao hơn
After: giữ nguyên
```

Rule:

- Gum không cần face được consume;
- Gum ảnh hưởng roll weight khi dice được roll/reroll;
- opposite face phải dựa trên logical face index/metadata, không suy ra từ numeric value;
- default: `+1` roll weight trên baseline `1`;
- nhiều Gum có thể stack;
- Gum Broken không có tác dụng;
- Echo không copy Gum vì Gum là Passive.

Nếu `Echo` đứng bên phải `Gum`:

```text
[Gum] [Echo]
→ Echo không có effect hợp lệ để copy
→ nếu Echo được dùng, Echo vẫn Broken
```

---

## B11. Relay

```text
Relay: On Use
Effect: dice ngay bên phải nhận +2 Value trong Player Phase hiện tại
After: giữ nguyên
```

Rule:

- resolve theo row hiện tại sau reorder;
- nếu không có dice bên phải, không có effect;
- modifier đi theo target die, không đi theo slot;
- modifier tồn tại tới hết Player Phase;
- nhiều Relay stack;
- Relay target Stone không có effect vì Stone không có numeric Value.

Ví dụ:

```text
[Relay] [Dice 4]
→ Dice 4 thành 6 trong phase hiện tại
```

```text
[Relay] [Echo] [Dice 4]
→ Relay buff dice thứ 2 nếu rule target là ngay bên phải
→ Echo copy Relay và buff dice thứ 3
```

Implementation cần rõ ràng theo row và selected dice để preview/execution không lệch.

---

## B12. Double

```text
Double: On Use
Effect: Value hiện tại của chính face ×2 trong action hiện tại
After: Broken
```

Rule:

- không sửa Base Value;
- chỉ áp trong action hiện tại;
- face Broken sau committed use;
- Echo copy Double thì nhân Value của Echo, không nhân Value source.

Ví dụ:

```text
[Double 4] [Echo 3]
→ Double source đóng góp Value 8 nếu được dùng
→ Echo đóng góp Value 6
→ Echo Broken
```

---

## B13. Repeat

```text
Repeat: Post-Skill
Effect: skill payload thực thi thêm 1 lần
After: Broken
```

Rule:

- không trả AP lần nữa;
- không consume dice lần nữa;
- không chạy lại face enchant trigger trong payload lặp;
- on-hit/status hook của skill vẫn chạy cho payload thêm;
- nhiều Repeat có thể tăng repeat count nhưng phải có chain guard;
- Repeat resolve sau payload gốc.

---

## B14. Reload

```text
Reload: Post-Skill
Effect: sau skill, reroll die này và làm die usable lại theo rule Broken
After: Broken
```

Rule:

- face Reload Broken sau khi effect tham gia;
- reroll chính die đó sau payload;
- reroll dùng normal roll pipeline, bao gồm Gum weight;
- nếu roll ra face Broken khác, die vẫn unusable;
- không reroll loop vô hạn;
- nhiều Reload resolve trái sang phải.

---

## B15. Heavy

```text
Heavy: Pay Cost
Effect: face này đóng góp 2 dice khi trả dice-slot cost
After: Broken khi payment commit
```

Rule:

- normal die contribution = `1`;
- Heavy contribution = `2`;
- Echo copy Heavy cũng contribution = `2` trong payment planning;
- scan available dice trái sang phải sau reorder;
- chọn dice cho tới khi contribution đạt hoặc vượt dice cost;
- overpayment được phép;
- skill cost `2` có thể trả bằng 1 Heavy;
- skill cost `3` có thể trả bằng `[Heavy] + [normal]`;
- skill cost `1` vẫn consume Heavy nếu Heavy là selected payment die đầu tiên, phần dư bị lãng phí;
- Heavy không đổi AP cost.

---

## B16. Echo

```text
Echo: Effective copy
Effect: copy enchant hợp lệ của dice ngay bên trái, không copy Value
After: Broken sau mọi committed use
```

Echo copy:

- enchant kind hợp lệ;
- resolve effect đó trên chính Echo face;
- dùng Value hiện tại của Echo nếu effect cần Value.

Echo không copy:

- Value của source;
- Base Value của source;
- Added Value source đã tạo;
- temporary modifier của source;
- used state;
- Broken state;
- whole-die tag/color như Patina.

Echo copy hợp lệ:

- `Power`
- `Guard`
- `Charge`
- `Gold`
- `Relay`
- `Double`
- `Repeat`
- `Reload`
- `Heavy`
- `Stone`

Echo copy thất bại khi:

- không có dice bên trái;
- source không có enchant (`None`);
- source face đang Broken;
- source enchant là `Echo`;
- source enchant là `Gum`.

Dù copy thành công hay thất bại:

```text
Nếu Echo được dùng trong committed action → Echo Broken.
```

Preview khi copy thất bại:

```text
Không có enchant hợp lệ để copy.
```

Ví dụ:

```text
[Power 4] [Echo 2]
→ +2 Added Value từ Power
→ +2 Added Value từ Echo copy Power
→ tổng +4
→ Echo Broken
```

```text
[Guard 5] [Echo 2]
→ gain 7 Guard
→ Echo Broken
```

```text
[Gum] [Echo]
→ Echo không copy Gum
→ nếu Echo được dùng, Echo Broken
```

```text
[None] [Echo]
→ Echo không có effect
→ nếu Echo được dùng, Echo Broken
```

```text
[Echo] [Echo]
→ không recursion
→ Echo được dùng vẫn Broken
```

---

## B17. Stone

```text
Stone: Static identity override + On Use
Effect: face không còn numeric identity. Khi dùng, skill/action nhận +5 Added Value
After: giữ nguyên
```

Rule:

- Stone che số trên face bằng icon/cover;
- Base Value gốc vẫn lưu bên dưới;
- Stone không bị đọc là `0`, `5` hoặc hidden Base Value;
- Stone không thể Crit;
- Stone không thể Fail;
- Stone không phải Odd hoặc Even;
- Stone không thỏa exact-value condition;
- Stone không thỏa threshold condition;
- Stone không được chọn làm Highest hoặc Lowest;
- Stone không nhận Value modifier từ Relay;
- Stone không nhận Double vì không có numeric Value;
- Stone vẫn contribution `1` dice để trả dice-slot cost trừ khi effective enchant khác thay đổi contribution;
- Gum vẫn có thể tăng roll weight của Stone logical face vì Gum nhắm face index, không nhắm numeric Value;
- Echo copy Stone thì Echo tạm mất numeric identity trong action và cấp `+5 Added Value`;
- `[Stone] [Echo]` = tổng `+10 Added Value`, Echo Broken;
- Stone không Broken sau khi dùng.

Khi Stone bị overwrite:

```text
[Stone], hidden Base Value 4
+ apply Power
→ [Power 4]
```

---

# PHẦN C — PREVIEW / EXECUTION PIPELINE

## C1. Dùng chung planning object

Preview và execution phải dùng cùng một planning/resolution model.

Không để preview tính một kiểu và execution chọn dice lại từ đầu.

Action plan cần chứa:

- die identity;
- row/order tại thời điểm action;
- logical face identity;
- Base Value;
- current resolved Value;
- stored enchant;
- effective enchant sau Echo;
- Echo copy failure reason;
- numeric hay Stone-masked;
- payment contribution;
- selected for payment hay không;
- AP cost;
- Added Value bonus;
- Guard gain;
- AP gain;
- Gold mark;
- Relay modifiers;
- repeat count;
- queued reloads;
- faces sẽ Broken;
- invalid reason nếu có.

---

## C2. Resolve order đề xuất

Pipeline:

```text
1. Đọc row dice sau reorder.
2. Loại dice unusable / face Broken.
3. Tạo effective enchant view, bao gồm Echo copy và Stone mask.
4. Build payment plan trái sang phải, tính Heavy contribution.
5. Validate AP, dice cost, target, skill condition.
6. Preview từ plan này.
7. Khi commit:
   - trừ AP;
   - lock selected dice;
   - resolve Pay Cost break như Heavy;
   - resolve On Use enchant trái sang phải;
   - execute skill payload gốc;
   - execute Repeat payload thêm;
   - resolve Reload trái sang phải;
   - apply Broken state;
   - update used/available visual;
   - refresh preview.
```

Repeat không trigger lại enchant trong payload lặp.
Reload không tạo chain vô hạn.

---

## C3. UI/preview requirement

Preview phải hiển thị:

- dice nào sẽ được consume;
- Heavy contribution;
- face nào sẽ Broken;
- Power Added Value;
- Stone Added Value;
- Relay target và Value modifier;
- Double adjusted Value;
- Guard gain;
- Charge AP gain;
- Gold post-combat marker;
- Repeat extra execution;
- Reload post-skill reroll indicator;
- Echo copied enchant hoặc failure reason;
- Stone face là non-numeric;
- Broken face unusable.

UI face:

- body/material color = whole-die tag như Patina;
- icon/symbol trên face = face enchant;
- Stone che numeric Value;
- Broken overlay rõ ràng;
- overwrite enchant refresh UI ngay.

---

# PHẦN D — IMPLEMENTATION / MIGRATION

## D1. Các file likely cần kiểm tra

Codex phải search repo thật trước khi sửa. Các file trong spec hiện tại gợi ý có liên quan:

- `Assets/Scripts/Dice/DiceFaceEnchantKind.cs`
- `Assets/Scripts/Dice/DiceFaceEnchantUtility.cs`
- `Assets/Scripts/Dice/DiceCombatEnchantRuntimeUtility.cs`
- `Assets/Scripts/Dice/DiceSpinnerGeneric.cs`
- `Assets/Scripts/Dice/DiceSlotRig.cs`
- `Assets/Scripts/Combat/Turn/TurnAPger.cs`
- `Assets/Scripts/Combat/Turn/TurnAPgerCombatUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnAPgerPlanningUtility.cs`
- `Assets/Scripts/Combat/Execution/AttackPreviewCalculator.cs`
- `Assets/Scripts/Consumables/ConsumableRuntimeUtility.cs`
- `Assets/Scripts/UI/Combat/ConsumableBarUIAPger.cs`
- `Assets/Scripts/DiceEditSandbox/GameplayDiceEditController.cs`
- `Assets/Scripts/Run/RunInventoryAPger.cs`
- map generation / reward generation scripts hiện có
- shop inventory generator hiện có

Không dồn toàn bộ logic mới vào `TurnAPger`. Tách utility/resolver nếu cần.

---

## D2. Enum / serialization safety

Unity enum có thể serialize bằng integer. Không reorder enum cũ tùy tiện.

Yêu cầu:

- gán explicit integer cho enum nếu cần;
- giữ legacy entries tạm thời để migrate;
- thêm migration report;
- log asset được migrate;
- không âm thầm remap sai serialized asset;
- sau khi Unity verification mới cleanup legacy.

Selectable face-enchant pool cuối:

- `None`
- `Power`
- `Guard`
- `Charge`
- `Gold`
- `Gum`
- `Relay`
- `Double`
- `Repeat`
- `Reload`
- `Heavy`
- `Echo`
- `Stone`

`Broken` không phải selectable enchant.

---

## D3. Reward data model cần thêm/sửa

Thêm hoặc sửa enum/data:

```text
SpoilsLane:
- SkillCache
- ConsumablePack
- GoldCache
```

Combat node cần lưu `SpoilsLane` và hiển thị icon trên map.

Consumable Pack resolver:

```text
Input: global consumable pool
Rule: every consumable weight = 1
Roll: 3 unique candidates
Choice: player chọn 1 hoặc Skip
```

Shop inventory resolver:

```text
3 Skill trực tiếp
3 Consumable trực tiếp
1 Consumable Pack
1 Relic
1 Dice
```

Shop refresh theo Boss Preparation hiện có.

---

## D4. Docs cần cập nhật

Patch tối thiểu các file spec:

- `RUN_ECONOMY_REWARD_EVENT_SPEC.md`
  - bỏ combat reward gacha hỗn hợp;
  - thêm Spoils Lane;
  - thêm Consumable Pack;
  - sửa Shop inventory;
  - sửa Event reward package;
  - giữ mọi consumable cùng weight.

- `MAP_STRUCTURE_AND_NAVIGATION_SPEC.md`
  - combat node hiển thị Spoils Icon;
  - Shop vẫn là node;
  - Forge không bán consumable;
  - Boss Preparation vẫn là cost của greed.

- `RELICS_AND_DICE_PROGRESSION_SPEC.md`
  - thay face enchant cũ bằng 12 enchant mới;
  - update Fate/apply face enchant rule;
  - overwrite + repair;
  - Broken state;
  - Stone/Echo/Gum.

- `COMBAT_CORE_SPEC.md`
  - timing model enchant;
  - Heavy payment;
  - Repeat/Reload post-skill;
  - Crit `×1.3`;
  - Stone numeric opt-out.

- `COMBAT_UI_PREVIEW_FEEDBACK_SPEC.md`
  - preview cho Heavy/Stone/Echo/Relay/Reload/Repeat/Broken;
  - map reward icon UI nếu file này chứa UI reward.

- `PROJECT_META.md`
  - thêm newest source-of-truth note;
  - đánh dấu on-roll elemental face enchant và reward gacha hỗn hợp là outdated.

- `SPEC_CLEANUP_CHANGELOG.md`
  - ghi migration reward + enchant.

---

# PHẦN E — TEST CASES

## E1. Enchant tests

Tối thiểu test:

1. `Power` roll không proc; use mới `+2 Added Value`.
2. `[Power] [Echo]` cho tổng `+4 Added Value`; Echo Broken.
3. `Guard 5` use cho `5 Guard`.
4. `[Guard 5] [Echo 2]` cho `7 Guard`; Echo không copy Value.
5. `Charge` không validate cast thiếu AP hiện tại, nhưng cấp AP cho cast sau.
6. `Gold` mark một lần mỗi face mỗi combat; victory mới cộng Gold.
7. `Gum` tăng roll weight face đối diện; Echo không copy Gum.
8. `Relay` buff dice bên phải `+2 Value`.
9. Relay target Stone không có effect.
10. `Double 4` thành `8` trong action và face Broken.
11. `[Double 4] [Echo 3]` cho `8` và `6`; Echo Broken.
12. `Repeat` chạy payload thêm 1 lần, không trả cost lần nữa.
13. `Reload` Broken source face, reroll die, refresh usability nếu roll face usable.
14. `Heavy` trả cost 2 một mình và Broken.
15. `[Heavy] [normal]` trả cost 3.
16. Echo không source vẫn no effect nhưng Broken.
17. Echo source `None` no effect nhưng Broken.
18. Echo source `Broken` no effect nhưng Broken.
19. Echo source `Echo` no recursion nhưng Broken.
20. Stone use cấp `+5 Added Value`.
21. Stone không Crit/Fail/Odd/Even/exact/threshold/Highest/Lowest.
22. `[Stone] [Echo]` cấp `+10 Added Value`; Echo Broken.
23. Apply `Power` lên Stone hidden `4` thành `[Power 4]`.
24. Apply `Power` lên `[Broken Double 5]` thành `[Power 5]`, repaired.
25. Apply `None` lên Stone hidden `3` thành `[None 3]`, repaired.
26. Numeric face bình thường vẫn Crit `×1.3`.

---

## E2. Reward tests

Tối thiểu test:

1. Combat node `SkillCache` hiển thị icon trước trên map.
2. Thắng `SkillCache` mở 3 Skill, chọn 1 hoặc Skip.
3. Combat node `ConsumablePack` mở 3 consumable unique, chọn 1 hoặc Skip.
4. Mọi consumable trong pool có weight bằng nhau.
5. `GoldCache` chỉ cho Base Gold + Extra Gold, không mở gacha.
6. Elite cho Base Gold + Ore + Relic + Consumable Pack.
7. Boss không dùng combat gacha hỗn hợp.
8. Shop có 3 Skill, 3 Consumable, 1 Consumable Pack, 1 Relic, 1 Dice.
9. Shop không refresh khi mở lại cùng node.
10. Shop refresh theo Boss Preparation rule hiện có.
11. Forge không bán consumable/enchant.
12. Event có thể cho Pack/trade/risk reward nhưng không dùng mixed gacha mặc định.
13. Không còn reward screen trộn `Skill / Relic / Gold / Consumable` trong cùng một card pool.
14. Pack tăng số món được thấy, không tăng số món được lấy.
15. Không có cách deterministic để chọn chính xác `Echo`, `Double`, `Stone` từ Forge/Shop baseline.

---

# PHẦN F — BÁO CÁO CUỐI CHO CODEX

Sau khi implement, Codex phải báo cáo:

- files đã sửa;
- enum/migration đã làm;
- asset migration result;
- tests đã thêm/chạy;
- Unity-only manual checks còn cần;
- assumptions không xác minh được từ repo;
- phần nào chỉ update spec, phần nào đã update runtime.

Không được nói đã verify Unity scene nếu chưa chạy scene thật.

---

# TÓM TẮT CỰC NGẮN

```text
Reward:
- Bỏ gacha hỗn hợp.
- Combat node hiện trước Spoils Lane: Skill / Consumable / Gold.
- Skill Cache: 3 skill chọn 1.
- Consumable Pack: 3 consumable random chọn 1.
- Mọi consumable weight = 1.
- Elite: Gold + Ore + Relic + Consumable Pack.
- Shop: 3 skill + 3 consumable + 1 pack + 1 relic + 1 dice.
- Forge: chỉ dùng Ore cho whole-die progression.
- Không shop sau mỗi combat, không reroll vô hạn, không chọn đúng enchant.

12 enchant:
- Power +2 Added Value, giữ nguyên.
- Guard gain Guard = Value, giữ nguyên.
- Charge +1 AP, giữ nguyên.
- Gold mark post-combat gold, giữ nguyên.
- Gum passive opposite roll weight, giữ nguyên.
- Relay +2 Value cho dice bên phải, giữ nguyên.
- Double ×2 Value, Broken.
- Repeat skill thêm 1 lần, Broken.
- Reload reroll + usable lại, Broken.
- Heavy count as 2 dice for cost, Broken.
- Echo copy enchant hợp lệ bên trái, luôn Broken khi dùng.
- Stone +5 Added Value, mất numeric identity, giữ nguyên.

Core rules:
- Crit = ×1.3.
- Broken là face state riêng.
- Apply enchant mới ghi đè enchant cũ và repair face.
- Stone giữ Base Value ẩn, overwrite thì hiện lại số.
- Echo không copy Gum/None/Broken/Echo nhưng vẫn Broken nếu dùng.
```
