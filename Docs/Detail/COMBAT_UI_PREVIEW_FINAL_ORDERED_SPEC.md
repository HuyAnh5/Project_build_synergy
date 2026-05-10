# Combat UI Preview — Final Implementation Handoff

> Mục tiêu: triển khai UI preview combat theo thứ tự ưu tiên, tránh làm lan man.  
> Scope file này chỉ gồm: HP/Guard/Stagger UI, resource preview, dice preview, skill tooltip, target overlay, target preview, status preview, roll feedback và test case.

---

## 0. Nguyên tắc gốc

UI không tự tính combat math.

Combat system / preview resolver phải tính kết quả.  
UI chỉ render kết quả đó.

```text
Preview = kết quả thật nếu action được cast ngay lúc đó.
Execution phải khớp preview.
```

Nếu có random effect thì UI phải báo rõ đó là random.  
Nếu không random, preview và execution phải ra cùng số.

---

# PHẦN LÀM TRƯỚC

## 1. Tạo Preview State chung

Làm đầu tiên.

Preview State là dữ liệu UI nhận từ combat system để render preview.

Preview State cần đủ thông tin:

```text
Action hợp lệ hay không
Lý do invalid nếu có

AP/Focus cost thật
Dice sẽ bị consume

Skill output hiện tại
Target HP final nếu có target
Target Guard final nếu có target
Target status final nếu có target

Self/player HP final nếu skill target self
Self/player Guard final nếu skill target self
Self/player status final nếu skill target self
```

Không cần ghi `Stagger final` như một chỉ số riêng.

Stagger chỉ là state/visual flag của actor, không phải thanh riêng và không phải resource.

```text
Stagger is an actor state that changes HP bar visual style.
HP bar still only displays real HP and Guard.
```

---

## 2. Actor HP / Guard / Stagger UI

Mọi actor có HP đều dùng chung UI grammar:

```text
Player
Enemy
Boss
Ally nếu sau này có
```

### HP

HP là máu thật, giống Slay the Spire.

```text
HP bình thường: đỏ
Viền bình thường: đen
Text: HP hiện tại / HP tối đa
```

### Guard

Guard giống Block của Slay the Spire.

```text
Guard không phải HP xanh.
Guard là shield/value layer riêng cạnh HP bar.
Số Guard hiển thị bên trái hoặc sát cạnh trái HP bar.
```

Khi actor có Guard:

```text
Guard number hiện rõ
Accent xanh
Viền HP bar trắng
```

### Stagger

Stagger là state riêng của game.

```text
Không phải thanh riêng.
Không phải resource.
Không phải HP phụ.
```

Khi actor đang Stagger thật:

```text
HP bar dùng accent vàng
Viền trắng
```

Nếu một hit sẽ phá Guard và gây Stagger sau khi cast, có thể preview icon/state Stagger sẽ xuất hiện sau action, nhưng không dùng Stagger mới đó để tăng damage cho chính hit đang preview.

---

## 3. AP / Focus preview

AP/Focus UI gồm:

```text
Số tổng hiện tại
Các segment nhỏ
Mỗi segment = 1 AP/Focus
```

Khi hover hoặc drag skill:

```text
Chỉ các segment sẽ bị consume chuyển màu vàng.
Chỉ các segment màu vàng nhấp nháy nhẹ.
Các segment không bị consume giữ bình thường.
Không hiển thị kiểu AP còn lại.
```

Ví dụ:

```text
Player có 9 AP.
Skill tốn 3 AP.
→ 3 segment chuyển vàng và nhấp nháy.
→ 6 segment còn lại bình thường.
```

Nếu không đủ AP/Focus:

```text
Highlight các segment hiện có sẽ bị dùng.
Background/frame resource chuyển đỏ.
Skill không cast được.
```

Tooltip skill vẫn ghi cost thật sau mọi modifier.

---

## 4. Dice preview

Khi hover hoặc drag skill:

```text
Dice mà skill sẽ consume nhấp nháy nhẹ.
Dice được chọn theo thứ tự hiện tại từ trái sang phải.
Reorder dice phải cập nhật preview ngay.
```

Nếu skill cần nhiều dice hơn số dice available:

```text
Tất cả dice hiện đỏ nhẹ.
Skill không cast được.
```

Crit / Fail visual:

```text
Crit: outline vàng
Fail: outline đỏ nhẹ
Normal: visual hiện tại
```

Consume preview không được che mất thông tin Crit / Fail.

---

## 5. Skill tooltip / skill hover

Tooltip skill phải hiển thị:

```text
AP/Focus cost thật
Dice cost thật
Dice nào sẽ bị consume
Skill đủ tài nguyên hay không
Damage/output hiện tại của skill
```

Damage trong tooltip không được là số tĩnh nếu số thật đã thay đổi.

Khi hover skill, damage/output phải được tính theo:

```text
Dice skill sẽ consume
Base Value
Added Value
Crit / Fail
Condition của chính skill
Modifier global không cần target
```

Ví dụ:

```text
Skill text gốc: Deal 3 damage.
Dice/crit/added làm output thành 5.
Tooltip hover phải hiện damage hiện tại là 5.
```

---

## 6. Skill condition highlight

Skill có thể sáng hơn bình thường nếu đạt condition.

Rule:

```text
Condition phải check trên đúng dice mà skill sẽ consume nếu cast ngay.
Không check toàn bộ dice chung chung.
```

Ví dụ:

```text
Dice order: 1 - 4 - 3
Skill condition: consumed die must be even
```

Nếu skill sẽ consume `1`:

```text
Condition không đạt.
Skill không sáng.
```

Nếu reorder để `4` ở đầu:

```text
Condition đạt.
Skill sáng hơn.
```

Nếu dùng skill khác trước để consume `1`, skill tiếp theo sẽ đọc `4` và có thể sáng.

Nếu skill thiếu AP/Focus hoặc thiếu dice:

```text
Skill vẫn tối / unusable.
Condition đúng không override thiếu tài nguyên.
```

---

## 7. Targetability Overlay

Khi hover hoặc giữ/drag skill:

```text
Tất cả target hợp lệ của skill phải hiện overlay marker rõ ràng.
```

Overlay này dùng để cho player biết:

```text
Skill này có thể dùng lên actor nào.
```

Target hợp lệ:

```text
Hiện overlay / marker / glow / image phủ lên actor.
```

Target không hợp lệ:

```text
Không hiện valid overlay.
Có thể hiện mờ/blocked nếu cần.
```

Overlay xuất hiện khi:

```text
Hover skill
Drag/giữ skill
```

Overlay biến mất khi:

```text
Rời hover
Hủy drag
Cast xong
```

Targetability overlay khác với HP preview:

```text
Overlay = con nào target được.
HP/Guard/status preview = target đó sẽ thành ra sao nếu cast.
```

---

## 8. Drag skill vào actor: final target preview

Khi drag skill lên một actor cụ thể, UI phải preview post-action state của actor đó.

Target preview phải tính mọi modifier hợp lệ tại thời điểm đó:

```text
Guard
Stagger nếu actor đã đang Stagger
Status trên target
Buff/debuff
Passive
Modifier của player/enemy/encounter
Modifier làm đổi damage hoặc resource cost
```

Kết quả preview trên actor gồm:

```text
HP final
Guard final
Status final
HP bar visual state đúng
```

Nếu HP sẽ mất:

```text
Phần HP mất màu cam.
Chỉ phần cam nhấp nháy nhẹ.
HP còn lại là HP thật sau action.
```

Nếu Guard sẽ đổi:

```text
Shield/value layer hiển thị Guard final.
Phần Guard preview nhấp nháy nhẹ.
```

Nếu actor đã Stagger sẵn:

```text
Damage preview đã tính Stagger.
Preview là số final nếu cast.
```

Nếu action phá Guard và sẽ tạo Stagger:

```text
Có thể preview Stagger sẽ xuất hiện sau action.
Không dùng Stagger mới đó để tăng damage cho chính hit đang preview.
```

---

## 9. Status preview

Status preview cũng là kết quả thật sau action.

Tất cả status preview nhấp nháy nhẹ.

### Status có số

```text
Burn
Bleed
```

Hiển thị:

```text
Icon + số final
```

Ví dụ:

```text
Enemy có 3 Burn.
Skill apply 5 Burn.
Preview hiển thị Burn = 8 và nhấp nháy.
```

Nếu skill consume Burn:

```text
Preview số Burn còn lại.
Nếu về 0, preview icon sẽ mất/fade.
```

### Status không có số

```text
Freeze
Chilled
Mark
Stagger nếu hiện như icon phụ
```

Hiển thị:

```text
Icon nhấp nháy nếu sẽ được apply/remove/change.
```

Nếu status không apply được vì immunity hoặc target invalid:

```text
Không preview như thể thành công.
Có thể báo invalid bằng icon đỏ nhẹ hoặc tooltip ngắn.
```

---

# PHẦN LÀM SAU

## 10. Roll animation

Làm sau khi preview chính đã ổn.

Trong Roll Phase đầu lượt:

```text
Dice 1 dừng trước.
Dice 2 dừng sau.
Dice 3 dừng cuối.
```

Mục tiêu là tạo cảm giác slot machine.

Nếu reroll bằng consumable hoặc effect khác:

```text
Tất cả dice dừng cùng lúc.
Không dùng staggered stop.
```

---

## 11. Visual polish nhẹ

Sau khi logic đúng mới polish:

```text
Blink nhẹ, không quá gắt.
Không làm cả bar nhấp nháy nếu chỉ một phần preview.
Không làm UI quá noisy.
```

Chỉ phần preview nhấp nháy:

```text
AP segment sẽ consume
Dice sẽ consume
HP phần sẽ mất
Guard/status phần thay đổi
```

---

# KHÔNG LÀM TRONG PASS NÀY

Không làm các phần sau trong pass UI preview này:

```text
Reward
Shop
Map
Passive random
Run economy
Full enemy roster
Full boss UI
Mobile layout
Art polish lớn
Sound/VFX lớn
```

---

# TEST CASE CẦN THỰC HIỆN

## A. AP / Focus preview

### Test A1 — Đủ AP

Setup:

```text
Player có 9 AP.
Skill cost 3 AP.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
3 AP segment chuyển vàng và nhấp nháy.
6 AP segment còn lại bình thường.
Không hiển thị AP như còn lại 6.
Tooltip ghi cost 3 AP.
```

### Test A2 — Thiếu AP

Setup:

```text
Player có 2 AP.
Skill cost 3 AP.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
2 AP segment hiện có được highlight vàng/nhấp nháy.
Resource background/frame chuyển đỏ.
Skill unusable.
```

---

## B. Dice preview

### Test B1 — Skill 2 dice

Setup:

```text
Có 3 dice available: A, B, C.
Skill cost 2 dice.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
Dice A và B nhấp nháy nhẹ.
Dice C bình thường.
```

### Test B2 — Reorder đổi dice preview

Setup:

```text
Dice order ban đầu: A, B, C.
Skill cost 2 dice.
```

Hành động:

```text
Reorder thành C, A, B.
Hover skill.
```

Kỳ vọng:

```text
Dice C và A nhấp nháy.
Dice B bình thường.
```

### Test B3 — Không đủ dice

Setup:

```text
Chỉ còn 1 dice available.
Skill cost 2 dice.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
Tất cả dice hiện đỏ nhẹ.
Skill unusable.
```

---

## C. Skill condition highlight

### Test C1 — Condition không đạt

Setup:

```text
Dice order: 1 - 4 - 3.
Skill cost 1 die.
Skill condition: consumed die must be even.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
Skill không sáng condition.
Vì skill sẽ consume die đầu là 1.
```

### Test C2 — Reorder làm condition đạt

Setup:

```text
Dice order: 1 - 4 - 3.
Skill condition: consumed die must be even.
```

Hành động:

```text
Reorder thành 4 - 1 - 3.
Hover skill.
```

Kỳ vọng:

```text
Skill sáng condition.
Vì skill sẽ consume 4.
```

### Test C3 — Condition đúng nhưng thiếu AP

Setup:

```text
Dice đầu là 4.
Skill condition cần even.
Player không đủ AP để cast.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
Skill vẫn tối/unusable.
Không được sáng như castable.
Resource báo đỏ.
```

---

## D. Targetability overlay

### Test D1 — Hover attack skill

Setup:

```text
Skill target enemy.
Có 3 enemy sống.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
3 enemy hợp lệ hiện targetability overlay.
Player không hiện overlay nếu skill không target self.
```

### Test D2 — Hover self skill

Setup:

```text
Skill target self.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
Player/self zone hiện overlay hợp lệ.
Enemy không hiện valid overlay.
```

### Test D3 — Hủy hover/drag

Setup:

```text
Overlay đang hiện.
```

Hành động:

```text
Rời chuột khỏi skill hoặc hủy drag.
```

Kỳ vọng:

```text
Tất cả targetability overlay biến mất.
```

---

## E. Skill tooltip damage/output

### Test E1 — Damage cập nhật theo dice

Setup:

```text
Skill gốc: Deal 3 damage.
Dice/Added Value khiến output hiện tại = 5.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
Tooltip hiện damage hiện tại = 5.
Không chỉ hiện số tĩnh 3.
```

### Test E2 — Fail ảnh hưởng output

Setup:

```text
Skill gốc: Deal 4 damage.
Dice sẽ dùng là Fail.
Fail làm base output còn 2.
```

Hành động:

```text
Hover skill.
```

Kỳ vọng:

```text
Tooltip hiện output đã bị Fail ảnh hưởng.
```

---

## F. HP / Guard target preview

### Test F1 — Guard chặn hết damage

Setup:

```text
Enemy: 20 HP, 8 Guard.
Skill final damage vào target = 5.
```

Hành động:

```text
Drag skill lên enemy.
```

Kỳ vọng:

```text
HP không mất.
Guard preview còn 3.
HP không hiện phần cam mất máu.
Guard preview nhấp nháy nhẹ.
```

### Test F2 — Không có Guard

Setup:

```text
Enemy: 20 HP, 0 Guard.
Skill final damage = 6.
```

Hành động:

```text
Drag skill lên enemy.
```

Kỳ vọng:

```text
Enemy preview còn 14 HP.
Phần 6 HP sẽ mất màu cam và nhấp nháy nhẹ.
```

### Test F3 — Guard bị phá

Setup:

```text
Enemy: 20 HP, 4 Guard.
Skill final damage = 7.
```

Hành động:

```text
Drag skill lên enemy.
```

Kỳ vọng:

```text
Guard preview về 0.
HP preview còn 17.
Chỉ 3 HP vượt Guard hiển thị màu cam.
Nếu action tạo Stagger sau hit, không dùng Stagger mới để tăng damage của chính hit này.
```

### Test F4 — Enemy đã Stagger sẵn

Setup:

```text
Enemy đang Stagger thật.
Skill damage sau khi tính Stagger = 9.
Enemy có 20 HP.
```

Hành động:

```text
Drag skill lên enemy.
```

Kỳ vọng:

```text
Enemy preview còn 11 HP.
Phần 9 HP mất màu cam.
Số preview là final nếu cast.
```

---

## G. Player self preview

### Test G1 — Skill gain Guard

Setup:

```text
Player có 0 Guard.
Self skill gain 5 Guard.
```

Hành động:

```text
Hover/drag self skill vào self zone.
```

Kỳ vọng:

```text
Player Guard preview = 5.
Shield/value layer xuất hiện và nhấp nháy nhẹ.
```

### Test G2 — Skill heal

Setup:

```text
Player 10/20 HP.
Self skill heal 4.
```

Hành động:

```text
Drag skill vào self zone.
```

Kỳ vọng:

```text
Player HP preview = 14/20.
Heal preview nhấp nháy nhẹ nếu có heal visual.
```

---

## H. Status preview

### Test H1 — Apply Burn

Setup:

```text
Enemy có 3 Burn.
Skill apply 5 Burn.
```

Hành động:

```text
Drag skill lên enemy.
```

Kỳ vọng:

```text
Burn preview = 8.
Burn icon + số nhấp nháy nhẹ.
```

### Test H2 — Apply Freeze hợp lệ

Setup:

```text
Enemy chưa Freeze/Chilled.
Skill apply Freeze.
```

Hành động:

```text
Drag skill lên enemy.
```

Kỳ vọng:

```text
Freeze icon preview xuất hiện và nhấp nháy nhẹ.
```

### Test H3 — Apply Freeze không hợp lệ

Setup:

```text
Enemy đang Freeze hoặc Chilled.
Skill apply Freeze.
```

Hành động:

```text
Drag skill lên enemy.
```

Kỳ vọng:

```text
Không preview Freeze như thể thành công.
Target/action báo invalid hoặc icon đỏ nhẹ.
```

---

## I. Execution khớp preview

### Test I1 — Damage final khớp preview

Setup:

```text
Kéo skill lên enemy.
Preview hiện enemy còn 12 HP.
```

Hành động:

```text
Thả skill để cast.
```

Kỳ vọng:

```text
Sau execution enemy thật sự còn 12 HP.
```

### Test I2 — Guard final khớp preview

Setup:

```text
Preview hiện enemy còn 3 Guard sau hit.
```

Hành động:

```text
Thả skill để cast.
```

Kỳ vọng:

```text
Sau execution enemy thật sự còn 3 Guard.
```

### Test I3 — Status final khớp preview

Setup:

```text
Preview hiện enemy sẽ có 8 Burn.
```

Hành động:

```text
Thả skill để cast.
```

Kỳ vọng:

```text
Sau execution enemy thật sự có 8 Burn.
```

---

## J. Roll animation

### Test J1 — Roll đầu lượt

Setup:

```text
Bắt đầu player turn mới.
```

Hành động:

```text
Roll dice trong Roll Phase.
```

Kỳ vọng:

```text
Dice 1 dừng trước.
Dice 2 dừng sau.
Dice 3 dừng cuối.
```

### Test J2 — Reroll bằng consumable

Setup:

```text
Dùng consumable/effect reroll dice.
```

Hành động:

```text
Reroll.
```

Kỳ vọng:

```text
Các dice được reroll dừng cùng lúc.
Không dùng staggered slot-machine stop.
```

---

## Definition of Done

Pass này hoàn thành khi:

```text
UI dùng Preview State từ combat system.
AP/Focus highlight đúng segment sẽ consume.
Dice preview đúng dice sẽ consume.
Skill tooltip hiện đúng cost và output hiện tại.
Skill condition highlight đọc đúng dice sẽ consume.
Targetability overlay hiện đúng target hợp lệ.
Drag skill lên actor preview đúng HP/Guard/status final.
Player và enemy dùng chung HP/Guard/Stagger grammar.
Stagger chỉ là visual/state flag, không phải thanh/resource.
Mọi phần preview nhấp nháy nhẹ ở phần thay đổi.
Execution khớp preview trong test case.
```


Dựa trên file đặc tả COMBAT_UI_PREVIEW_FINAL_ORDERED_SPEC.md, đây là checklist ngắn gọn các hạng mục UI Preview cần hoàn thiện theo đúng thứ tự ưu tiên:

1. Nền tảng Preview State (Bắt buộc làm đầu tiên)

 UI chỉ render kết quả từ Preview State (không tự tính toán).
 Dữ liệu đủ: Cost thật, Dice tiêu tốn, Target final state (HP, Guard, Status).
2. Actor HP / Guard / Stagger UI

 HP: thanh đỏ, viền đen.
 Guard: layer riêng, viền trắng, accent xanh.
 Stagger: đổi accent HP sang vàng, viền trắng (không phải thanh riêng).
3. AP / Focus Preview

 Hover/Drag: Các segment AP bị tiêu tốn chuyển vàng và nhấp nháy.
 Nếu thiếu AP: Background/frame đỏ, báo lỗi không cast được.
4. Dice Preview

 Hover/Drag: Dice bị tiêu tốn nhấp nháy.
 Reorder: Cập nhật preview ngay lập tức.
 Thiếu dice: Báo đỏ. Có viền riêng cho Crit (vàng) / Fail (đỏ nhẹ).
5. Skill Tooltip / Hover

 Hiển thị chính xác cost và output damage hiện tại (đã cộng dồn mọi modifiers, buff, debuff).
6. Skill Condition Highlight

 Skill sáng lên nếu điều kiện đạt (chỉ tính trên dice sẽ bị consume). (Phần này có thể cần kiểm tra lại xem đã hoàn thiện 100% chưa).
7. Targetability Overlay

 Hover/Drag: Hiện marker/glow/overlay lên TẤT CẢ mục tiêu hợp lệ.
 Rời hover/hủy drag: Biến mất.
8. Final Target Preview (Drag / Hover mục tiêu)

 Tính toán toàn bộ modifier hiện tại khi preview lên mục tiêu.
 HP mất: Phần cam nhấp nháy, số HP final.
 Guard thay đổi: Nhấp nháy phần thay đổi, số Guard final.
 Stagger: Nếu phá Guard, có thể preview Stagger (nhưng không cộng dồn damage).
9. Status Preview (Hover / Drag vào mục tiêu)

 Status có số (Burn, Bleed): Add -> số nhấp nháy; Consume -> Icon + số nhấp nháy (kể cả về 0).
 Status không số (Freeze, Chilled, Mark): Nhấp nháy Icon nếu có sự thay đổi.
10. Polish & Animation (Làm sau cùng)

 Roll animation: Dice dừng tuần tự (Slot machine feel).
 Visual polish: Nhấp nháy nhẹ nhàng, tinh tế, không làm ồn (noisy) UI.
Nhìn chung chúng ta đã bao phủ gần như toàn bộ các tính năng cốt lõi trong danh sách này qua các bản vá vừa rồi. Bạn cần tôi đi sâu vào xử lý nốt mục nào tiếp theo không?