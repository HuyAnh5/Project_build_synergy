# Combat UI / Preview Feedback Spec

> File handoff cho Codex/dev.  
> Mục tiêu: mô tả logic UI combat cho HP bar, Guard, Stagger, intent, status, resource preview, dice preview, skill preview và roll feedback.  
> Đây là **UI/behavior spec**, không phải code implementation.


> **CURRENT SOURCE UPDATE:** Bản này đã nhập các rule hiện tại từ các draft cũ. Không dùng file `archived combat change draft` làm source nữa. Resource hiện tại là **AP**. Combat flow hiện tại là **Player Phase**: dice tự roll đầu phase → player reorder → click/drag skill vào target để cast ngay → dice chuyển used state → End Phase → Enemy Phase.

---
---

## 1. Mục tiêu UI

UI combat phải giúp player đọc được kết quả của hành động **trước khi commit**.

Nguyên tắc chính:

```text
Preview phải là kết quả thật nếu action được cast ngay lúc đó.
Execution phải khớp với preview.
```

Game có nhiều layer như dice, AP, Guard, Stagger, status, relic, buff/debuff và modifier. Player không nên phải tự tính tất cả trong đầu. UI phải cho họ thấy:

- skill sẽ dùng bao nhiêu AP;
- skill sẽ consume dice nào;
- skill có cast được không;
- target sẽ còn bao nhiêu HP;
- target sẽ còn bao nhiêu Guard;
- target sẽ nhận / mất status nào;
- player sẽ nhận Guard / heal / status gì nếu action target vào self.

---

## 2. Actor UI dùng chung

Mọi actor có HP đều dùng chung hệ UI này:

- Player
- Enemy
- Boss
- Ally nếu sau này có

Không tạo một hệ HP/Guard riêng cho enemy và một hệ khác cho player.

```text
Any actor with HP uses the same HP / Guard / Stagger / Status preview grammar.
```

Target là ai thì preview hiện trên actor đó:

- kéo skill vào enemy → preview trên enemy;
- kéo self-skill vào player → preview trên player;
- enemy intent nếu preview được damage lên player thì cũng dùng cùng grammar về sau.

---

## 3. HP bar direction

HP bar là thanh máu chính của actor, giống hướng Slay the Spire:

- HP là máu thật;
- HP bar hiển thị HP hiện tại / HP tối đa;
- màu HP bình thường là đỏ;
- outline bình thường là đen.

Guard không thay thế HP. Guard là một lớp bảo vệ riêng.

```text
Guard is Block-like, not extra HP.
HP bar remains the actor's real health bar.
Guard is rendered as a separate shield/value layer attached to the HP bar.
```

---

## 4. Guard direction

Guard hoạt động và hiển thị giống Block trong Slay the Spire:

- Guard là lớp chặn damage trước HP.
- Guard hiển thị bằng icon/shield + số.
- Số Guard nằm bên trái hoặc gắn sát cạnh trái của HP bar.
- Khi actor có Guard, Guard phải đọc được ngay cạnh HP bar.

Visual direction:

- actor có Guard → HP/guard state dùng accent xanh;
- outline HP bar chuyển trắng;
- số Guard hiển thị rõ ở bên trái HP bar.

Guard không phải là phần máu xanh thay thế HP.  
Guard là shield/value layer đi cùng HP bar.

---

## 5. Stagger direction

Stagger là hệ riêng của game này, không phải hệ của Slay the Spire.

Khi actor đang Stagger thật:

- HP bar dùng visual riêng để báo Stagger;
- HP/outline chuyển accent vàng/trắng theo style của game;
- player phải nhìn ra actor đang ở cửa sổ Stagger.

Stagger là state thật, không phải chỉ là màu preview.

Lưu ý quan trọng:

```text
Nếu một action sẽ phá Guard và gây Stagger sau khi cast, UI có thể preview Stagger sẽ xảy ra sau action, nhưng không được dùng Stagger đó để tăng damage cho chính hit đang preview.
```

Chỉ khi actor đã đang Stagger trước action thì damage preview mới tính bonus Stagger cho action đó.

---

## 6. Enemy intent

Enemy intent đi theo hướng Slay the Spire:

- intent hiển thị bằng icon;
- nếu intent gây damage thì hiện số damage;
- nếu intent là Guard / status / setup thì dùng icon tương ứng;
- không dùng text dài nếu icon + số đã đủ rõ.

Intent phải giúp player đọc trước enemy sắp làm gì:

- Attack
- Guard
- Apply Status
- Setup Big Attack
- Heal
- Special / Boss move nếu có

---

## 7. Status UI

Status hiển thị bằng icon.

### Status có số stack

Những status có số stack thì hiển thị icon + số:

- Burn
- Bleed

Ví dụ:

```text
Burn icon + 7
Bleed icon + 4
```

### Status không có số stack

Những status dạng có/không thì chỉ hiện icon:

- Freeze
- Chilled
- Mark
- Stagger nếu đang hiển thị như status icon phụ

Ví dụ:

```text
Freeze icon
Mark icon
```

Không thêm số nếu status đó không cần số để đọc.

---

## 8. Preview State rule

Toàn bộ UI preview phải dựa trên một kết quả preview đã được combat system tính sẵn.

UI không tự tính damage, Guard, status hoặc resource cost bằng logic riêng.

```text
UI renders Preview State.
UI does not become the source of truth.
```

Preview State cần đại diện cho trạng thái cuối cùng nếu action được cast ngay lúc đó:

- HP cuối;
- Guard cuối;
- Stagger cuối;
- status cuối;
- AP còn lại;
- dice sẽ bị consume;
- action có hợp lệ không;
- lý do invalid nếu không hợp lệ.

Câu chốt cho Codex/dev:

```text
Combat preview resolver calculates the answer.
UI only renders the answer.
Execution must use the same calculation path or same result model as preview.
```

---

## 9. HP / Guard preview rule

Khi hover hoặc drag skill/action lên một actor, HP bar của actor đó hiển thị **post-action state**.

Không cần hardcode riêng từng case trong UI.  
UI chỉ nhận kết quả đã tính và render:

- HP cuối;
- Guard cuối;
- Stagger state cuối;
- phần nào là preview;
- phần nào là state thật hiện tại.

Rule tổng quát:

```text
HP bar preview always shows the actor's post-action state if the action is cast immediately.
```

Guard preview phải giống Block preview:

- damage trừ Guard trước;
- chỉ damage vượt Guard mới trừ HP;
- nếu Guard chặn hết thì HP không đổi;
- nếu còn Guard sau preview thì Guard số mới được hiển thị.

Nếu HP sẽ bị mất:

- HP còn lại vẫn là HP thật sau action;
- phần HP sẽ mất được hiển thị bằng màu cam;
- phần cam nhấp nháy nhẹ để báo đây là preview.

Nếu Guard sẽ thay đổi:

- Guard final được hiển thị ở shield/value layer;
- phần thay đổi do preview nhấp nháy nhẹ.

Nếu action sẽ heal actor:

- HP final sau heal được preview;
- phần HP sẽ hồi nhấp nháy nhẹ theo style heal preview nếu có;
- không vượt quá max HP.

Nếu action sẽ gain Guard:

- Guard final sau gain được preview;
- nếu actor hiện chưa có Guard, shield/value layer preview xuất hiện;
- preview nhấp nháy nhẹ.

---

## 10. Status preview rule

Status cũng phải được preview giống HP/Guard.

Khi một action sẽ apply, consume, remove hoặc change status trên actor, status UI của actor đó phải hiển thị trạng thái cuối cùng sau action.

Tất cả status preview đều nhấp nháy nhẹ.

### Status có số

Ví dụ Burn / Bleed:

- nếu action apply thêm Burn, preview số Burn final;
- nếu action consume Burn, preview số Burn còn lại;
- nếu số final là 0, preview icon sẽ biến mất hoặc fade out theo style preview.

Ví dụ:

```text
Enemy đang có 3 Burn.
Skill sẽ apply 5 Burn.
Preview hiển thị Burn = 8, nhấp nháy nhẹ.
```

### Status không có số

Ví dụ Freeze / Chilled / Mark:

- nếu action sẽ apply status, icon status preview xuất hiện và nhấp nháy;
- nếu action sẽ remove status, icon status preview fade/nhấp nháy theo hướng mất đi;
- nếu action không áp được vì immunity hoặc target invalid, không preview như thể thành công.

Ví dụ:

```text
Kéo Deep Freeze vào enemy hợp lệ:
Freeze icon preview xuất hiện và nhấp nháy.

Kéo Deep Freeze vào enemy đang Freeze/Chilled:
Freeze không được preview thành công.
UI nên báo invalid hoặc không đủ điều kiện.
```

---

## 11. AP UI

AP hiển thị bằng:

- con số tổng hiện tại;
- các đoạn/segment nhỏ;
- mỗi segment tượng trưng 1 AP.

Khi hover hoặc drag skill:

- AP cost thật của skill được preview;
- các segment sẽ bị tiêu chuyển vàng;
- các segment đó nhấp nháy nhẹ;
- tooltip ghi cost thật sau mọi modifier.

Nếu không đủ AP:

- vẫn preview phần cost hiện có;
- phần resource preview nhấp nháy;
- background/frame AP chuyển đỏ;
- skill không cast được.

Không được hover hiện cost một kiểu nhưng cast thật trừ kiểu khác.

## 12. Dice cost preview

Khi hover hoặc drag skill:

- UI phải preview dice nào sẽ bị consume;
- dice sẽ bị consume nhấp nháy nhẹ;
- số dice preview dựa trên slot cost thật của skill;
- dice được chọn theo thứ tự hiện tại từ trái sang phải, sau reorder.

Nếu skill cần nhiều dice hơn số dice còn available:

- tất cả dice hiện màu đỏ nhẹ;
- skill không cast được;
- resource preview vẫn được hiển thị để player hiểu vì sao không đủ.

Dice preview chỉ là preview. Dice chỉ thật sự bị consume khi action được cast.

---

## 13. Skill condition highlight

Skill có thể sáng hơn bình thường nếu đạt condition.

Rule quan trọng:

```text
Condition highlight must check the exact dice that the skill would consume now.
```

Không check toàn bộ dice chung chung.

Ví dụ:

```text
Dice order: 1 - 4 - 3
Skill condition: consumed die must be even
```

Nếu skill sẽ consume die đầu tiên là `1`:

- condition không đạt;
- skill không sáng.

Nếu player reorder để `4` ở đầu:

- condition đạt;
- skill sáng hơn.

Nếu player dùng skill khác trước để consume `1`, lúc này skill tiếp theo sẽ đọc `4`:

- condition có thể sáng lên sau khi state dice thay đổi.

Nếu skill thiếu AP hoặc thiếu dice:

- skill vẫn bị tối/unusable;
- condition đúng không được override trạng thái thiếu tài nguyên.

---

## 14. Skill preview vs target preview

Cần tách rõ hai tầng preview:

### 14.1 Hover skill

Khi chỉ hover skill, UI preview:

- AP cost thật;
- dice sẽ bị consume;
- skill có đủ tài nguyên không;
- condition có đạt không;
- output cơ bản sau khi tính skill + dice + global modifier không cần target.

Skill tự tính phần liên quan đến:

- dice skill sẽ dùng;
- Base Value;
- Added Value;
- Crit / Fail;
- condition của chính skill;
- damage/output gốc của skill.

### 14.2 Drag skill lên actor

Khi drag skill lên một actor cụ thể, UI preview thêm target context:

- HP final của target;
- Guard final của target;
- Stagger final của target;
- status final của target;
- target-specific modifier;
- buff/debuff;
- relic;
- mọi rule ngoại cảnh hợp lệ tại thời điểm đó.

Đây là kết quả cuối cùng nếu thả skill ngay.

```text
Hover skill = preview tài nguyên + skill/dice output.
Drag onto target = preview final post-action state on that target.
```

---

## 15. Preview phải tính mọi modifier hợp lệ

Preview không chỉ tính Stagger.

Tất cả yếu tố ngoài skill nhưng ảnh hưởng đến kết quả cuối đều phải được tính ở preview:

- Guard;
- Stagger;
- relic;
- buff/debuff;
- status trên target;
- modifier của player;
- modifier của enemy;
- modifier của encounter;
- modifier thay đổi resource cost;
- các rule ngoại cảnh khác.

Ví dụ giả định:

```text
Skill tự tính được 10 damage.
Player có relic giả định +20% damage output.
Enemy đang Stagger.
```

Target preview phải hiện damage cuối sau khi tính tất cả modifier hợp lệ.

Ví dụ resource:

```text
Skill cost gốc = 2 AP.
Modifier khiến mọi skill +1 AP cost.
Hover preview phải trừ 3 AP.
```

Preview là đáp án chính xác, không phải gợi ý sơ bộ.

---

## 16. Preview visual language

Mọi phần preview đều nhấp nháy nhẹ để phân biệt với state thật.

Các phần có thể nhấp nháy:

- HP mất / hồi;
- Guard nhận / mất;
- status mới / status thay đổi;
- AP segment sẽ dùng;
- dice sẽ consume;
- skill condition highlight;
- invalid background nếu thiếu tài nguyên.

Nhấp nháy nên nhẹ, không gây khó đọc.

Không làm preview quá noisy.  
Nếu nhiều thứ cùng preview, ưu tiên readability:

1. target HP/Guard final;
2. resource cost;
3. dice consume;
4. status change;
5. condition highlight.

---

## 17. Dice visual state

### Active

Dice active là dice còn available trong Player Phase:

- nằm ở vị trí Y bình thường,
- background active,
- có thể được skill consume,
- có thể được preview khi hover/drag skill.

### Used

Dice đã dùng không biến mất.

Khi bị consume:

- die hạ nhẹ trục Y,
- background đổi màu sang trạng thái used,
- die vẫn hiển thị số / crit / fail,
- die không còn available cho cast tiếp theo.

Khi được refresh bởi turn mới hoặc consumable/effect:

- die nâng Y trở lại,
- background trở về active,
- die có thể available lại theo rule.

### Crit

Crit cần có outline / accent riêng, ví dụ vàng.

### Fail

Fail cần có outline / accent riêng, ví dụ đỏ nhẹ.

Consume/used preview không được che mất thông tin Crit / Fail.

## 18. Dice roll animation

Trong Player Phase start đầu lượt:

- dice không dừng cùng lúc;
- Dice 1 dừng trước;
- Dice 2 dừng sau;
- Dice 3 dừng cuối;
- tạo cảm giác giống slot machine.

Hiệu ứng dừng lần lượt chỉ áp dụng cho Player Phase start đầu lượt.

Nếu dice được reroll bằng consumable hoặc effect khác:

- tất cả dice dừng cùng lúc như bình thường;
- không dùng slot-machine staggered stop.

---

## 19. Invalid preview

Nếu action không hợp lệ, UI vẫn nên cho player hiểu vì sao.

Các invalid phổ biến:

- thiếu AP;
- thiếu dice;
- target không hợp lệ;
- status không apply được vì immunity;
- skill condition bắt buộc không đạt nếu skill yêu cầu condition để cast.

Visual:

- thiếu AP → resource background đỏ;
- thiếu dice → tất cả dice đỏ nhẹ;
- target invalid → target highlight đỏ hoặc không nhận drop;
- status invalid → không preview status thành công, có thể icon đỏ nhẹ hoặc tooltip ngắn.

Không được preview một kết quả như thể action sẽ thành công nếu thực tế cast sẽ bị reject.

---

## 20. Player-specific preview

Player dùng chung HP/Guard/Stagger UI như enemy.

Khi skill/action target vào player hoặc self:

- player HP bar preview HP final;
- player Guard preview Guard final;
- status trên player preview status final;
- resource/dice cost preview vẫn hoạt động như bình thường.

Ví dụ:

```text
Player hiện có 0 Guard.
Skill self: gain 5 Guard.
Preview: player shield/value layer hiện 5 Guard và nhấp nháy.
```

Ví dụ:

```text
Player hiện có 10/20 HP.
Skill self: heal 4.
Preview: player HP final = 14/20.
```

Ví dụ:

```text
Skill self: lose 3 HP, gain 8 Guard.
Preview: player HP final = 7 HP, Guard final = 8.
```

---

## 21. Guardrails cho implementation

### 21.1 UI không tự tính combat math

UI không được tự viết logic riêng kiểu:

- tự tính damage;
- tự trừ Guard;
- tự cộng relic;
- tự quyết định status final.

UI chỉ render kết quả preview đã được combat system tính.

### 21.2 Preview và execution phải cùng nguồn số

Không được để:

```text
Tooltip nói 10.
Preview nói 12.
Cast thật ra 15.
```

Trừ khi action có random effect được ghi rõ và UI cũng báo rõ đây là random.

### 21.3 Không hardcode từng status/case trong HP bar

HP bar không nên có nhiều nhánh logic gameplay riêng.

Hướng đúng:

```text
Combat system tạo Preview State.
HP bar render Preview State.
```

### 21.4 Guard không phải HP xanh

Guard là Block-like shield/value layer.

Không biến toàn bộ HP bar thành một thanh máu xanh mới.  
HP vẫn là máu thật. Guard là lớp số/biểu tượng gắn cạnh HP.

### 21.5 Stagger là custom state

Stagger là state riêng của game.  
Không mô tả Stagger như Block của STS.  
Stagger có visual riêng, ưu tiên accent vàng/trắng.

---


## 22. Implementation Priority / Test Cases

### Làm trước

1. Preview State chung.
2. Actor HP / Guard / Stagger UI.
3. AP preview.
4. Dice preview và used state.
5. Skill tooltip / skill hover.
6. Targetability overlay.
7. Drag/click skill vào actor: final target preview.
8. Status preview.

### Làm sau

- roll animation polish,
- blink / VFX nhẹ,
- sound feedback,
- art polish lớn.

### Không làm trong pass UI preview này

- reward,
- shop,
- map,
- mobile layout,
- full boss UI,
- art polish lớn.

### Test case bắt buộc

- đủ AP: segment AP bị consume chuyển vàng.
- thiếu AP: AP frame đỏ, skill unusable.
- skill 2 dice: preview đúng 2 dice available đầu tiên theo thứ tự hiện tại.
- reorder dice: preview dice consume đổi ngay.
- dice used: sau cast die hạ Y + đổi background + không còn available.
- dice refresh: die nâng Y + background active lại.
- condition highlight: check đúng dice sẽ bị consume, không check toàn bộ dice.
- target preview: HP/Guard/status final khớp execution.
- invalid target: không preview như thể cast thành công.

## 23. Definition of Done

UI preview pass được xem là đạt khi:

- mọi actor có HP dùng chung HP/Guard/Stagger grammar;
- enemy và player đều có thể preview HP/Guard/status thay đổi;
- Guard hiển thị như Block-like shield number cạnh HP;
- attack intent hiển thị icon + số như STS-style intent;
- Stagger có visual riêng;
- hover skill preview đúng AP cost thật;
- hover skill preview đúng dice sẽ consume;
- skill condition highlight đọc đúng dice sẽ consume;
- drag skill lên actor preview post-action state thật;
- status preview hiện icon/số final và nhấp nháy;
- mọi phần preview nhấp nháy nhẹ;
- execution khớp preview.