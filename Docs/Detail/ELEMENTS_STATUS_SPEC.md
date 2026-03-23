# ELEMENTS_STATUS_SPEC.md

> Tài liệu này là **source of truth cho 5 hệ chính, 4 status/effect cốt lõi và ailment system**.  
> Nó mô tả identity, quy tắc kích hoạt, payoff, interaction, edge case và định hướng UX của từng hệ.

---

## 1. Mục tiêu của hệ thống

Element / status engine tồn tại để tạo ra:

- nhịp **setup → payoff**,
- khác biệt thật sự giữa các build,
- sequencing có ý nghĩa,
- board state dễ đọc,
- chiều sâu chiến thuật mà không cần quá nhiều damage button đơn lẻ.

Trong game này, status **không chỉ để thêm số**.  
Mỗi hệ phải kéo player sang kiểu quyết định khác nhau.

---

## 2. Phạm vi file này

File này bao gồm:

- Physical,
- Fire / Burn,
- Ice / Freeze / Chilled,
- Lightning / Mark,
- Bleed,
- Ailment system,
- Stagger ở cấp interaction với status,
- rule tiêu thụ / lan / proc phụ,
- priority và edge case giữa direct-hit và effect phụ.

---



## 3. Logic Flow

Phần này mô tả **đường đi logic chung của element / status engine**: state được đọc ở đâu, consume / payoff / apply xảy ra theo thứ tự nào, và board state được cập nhật ra sao cho action kế tiếp.

### 3.1 Flow chung của một action có liên quan tới status

`Read current target state`  
→ Xác định target đang có Burn / Freeze / Chilled / Mark / Bleed / Ailment gì  
→ Kiểm tra miễn nhiễm, exclusivity hoặc special interaction rule  
→ Resolve hit / damage trước theo combat core  
→ Resolve consume / payoff nếu skill có consume window  
→ Resolve apply status mới nếu còn hợp lệ  
→ Update target state để làm truth state cho action tiếp theo

### 3.2 Flow theo nhóm hệ

**Physical**  
→ Read die / crit state  
→ Resolve direct-hit / anti-Guard / decisive finish  
→ Không dựa vào resource status riêng để payoff

**Fire / Burn**  
→ Apply Burn hoặc đọc Burn stack đang có  
→ Nếu skill là spender: consume Burn theo rule của skill  
→ Chuyển Burn stack thành bonus damage / payoff  
→ Update target state sau consume

**Ice / Freeze / Chilled**  
→ Kiểm tra target đang Freeze / Chilled hay không  
→ Nếu hợp lệ: apply Freeze hoặc chuyển sang / khai thác Chilled window  
→ Resolve payoff Guard / Focus / tempo theo rule cụ thể  
→ Update target state sau action

**Lightning / Mark**  
→ Kiểm tra target có Mark hay không  
→ Resolve direct-hit vào mục tiêu chính  
→ Nếu direct-hit hợp lệ vào target có Mark: kích hoạt lan / propagation theo current rule  
→ Remove / preserve Mark theo timing rule đã chốt

**Bleed**  
→ Apply Bleed stack lên target hoặc đọc lượng Bleed hiện có  
→ Tick ở đầu lượt target theo rule riêng của Bleed  
→ Một số build có thể convert Bleed thành tài nguyên khác như Guard / consumable

### 3.3 Flow ailment chung

`Attempt apply ailment`  
→ Xác định source là player hay enemy  
→ Tính chance theo rule ailment hiện hành  
→ Check immunity / exclusivity nếu có  
→ Nếu thành công: gắn ailment state lên target  
→ Nếu thất bại: không đổi board state  
→ State mới được resolve ở đúng timing window của ailment đó


## 4. Bảng tóm tắt identity của 5 hệ

| Hệ | Vai trò cốt lõi | Tài nguyên / điểm tựa | Kiểu payoff |
|---|---|---|---|
| Physical | Burst thẳng, anti-Guard, clean finish | Crit mạnh, hit trực diện | Damage rõ ràng, decisiveness |
| Fire | Setup tài nguyên rồi nổ | Burn stack | Consume Burn để burst |
| Ice | Tempo / control | Freeze, Chilled, Guard/Focus window | Bẻ nhịp combat, mở cửa payoff |
| Lightning | Board control / propagation | Mark | Direct-hit đúng mục tiêu rồi lan ra board |
| Bleed | Áp lực dài hạn / chuyển hóa tài nguyên | Bleed stack | DoT thật + có thể đổi ra giá trị khác |

---

## 5. Physical

### 5.1 Mục tiêu

Physical tồn tại để cho player một trục:

- burst rõ ràng,
- anti-Guard,
- hit thẳng, ít vòng vo,
- cảm giác clean, decisive.

### 5.2 Rule đã chốt

- Crit của Physical dùng **`+50% Base`** thay vì `+20% Base` như hệ thường.

### 5.3 Ý nghĩa thiết kế

Physical không cần quá nhiều trang trí status để có identity.  
Hệ này mạnh ở:

- timing,
- lane order,
- read Guard,
- chọn đúng thời điểm dồn hit chính.

### 5.4 Edge cases / lưu ý

- Nếu skill Physical có tag `Sunder`, phải đọc thêm rule riêng của `Sunder`; không có hidden multiplier mặc định.
- Physical vẫn chịu rule Base/Added như mọi hệ khác; không được vì là hệ “đánh thẳng” mà cho condition đọc từ resolved value.

---

## 6. Fire / Burn

### 6.1 Mục tiêu

Fire là hệ của:

- setup tài nguyên,
- giữ lại trên mục tiêu,
- rồi consume đúng lúc để nổ burst.

Burn **không phải DoT chính**.  
Burn là **resource để consume**.

### 6.2 Rule đã chốt cho Burn

- Burn có stack.
- Consume baseline = **`+2 damage mỗi stack Burn bị xóa`**.
- Chỉ skill đặc biệt mới được override con số baseline này.

### 6.3 Identity gameplay

Burn tồn tại để tạo ra quyết định kiểu:

- nên bồi thêm stack hay kích nổ ngay,
- nên chia Burn lên nhiều mục tiêu hay dồn một mục tiêu,
- nên dùng lane đầu để tích Burn hay lane cuối để detonate,
- nên ưu tiên exact-value / custom dice build để mở loop sâu hơn hay không.

### 6.4 Quan hệ với direct-hit

Burn consume được tính như **một phần của direct-hit** nếu nó nằm trong skill direct-hit.  
Vì vậy:

- nếu hit đó là hit direct tiếp theo sau Stagger,
- phần Burn consume trong hit đó được cộng vào tổng damage trước khi nhân `1.2`.

### 6.5 Điều Burn không nên trở thành

Burn không nên bị thiết kế thành “poison clone”.  
Nếu Fire chỉ còn là hệ gây DoT thụ động, nó sẽ mất bản sắc setup → payoff.

### 6.6 Edge cases

- Burn tiêu qua skill direct-hit được tính vào direct-hit đó.
- Burn consume không tự biến thành một effect riêng tách rời nếu text skill không nói vậy.
- Các passive tăng stack Burn phải cộng vào logic apply, không được âm thầm đổi baseline consume nếu text không ghi.

---

## 7. Ice / Freeze / Chilled

### 7.1 Mục tiêu

Ice là hệ của:

- tempo,
- control,
- tạo khoảng nghỉ,
- rồi mở cửa sổ payoff tài nguyên.

### 7.2 Rule đã chốt

- **Freeze**: skip 1 turn
- Hết Freeze → thành **Chilled**
- **Chilled tồn tại 2 turn**
- Đang Freeze hoặc Chilled thì **miễn Freeze mới**
- Ice damage hit vào target đang Freeze / Chilled → player nhận **`+1 Focus +3 Guard`**

### 7.3 Identity gameplay

Ice không mạnh vì damage thô.  
Ice mạnh vì:

- bẻ nhịp combat,
- tạo tempo swing,
- cho player cơ hội đổi control thành tài nguyên,
- ép người chơi chọn đúng cửa sổ khai thác Chilled.

### 7.4 Freeze và Chilled không phải cùng một thứ

- **Freeze** là trạng thái CC chính.
- **Chilled** là trạng thái hậu Freeze, tồn tại như cửa sổ payoff / tempo window.

Điểm rất quan trọng:  
Game không muốn Ice chỉ là “stun kéo dài”.  
Transition `Freeze → Chilled` chính là nơi hệ này có chiều sâu.

### 7.5 Edge cases

- Không áp Freeze mới nếu mục tiêu đã Freeze hoặc Chilled.
- Hit Ice vào mục tiêu Freeze/Chilled mới cho reward `+1 Focus +3 Guard`.
- Reward này là tài nguyên cho player, không phải damage bonus trực tiếp.

---

## 8. Lightning / Mark

### 8.1 Mục tiêu

Lightning là hệ của:

- board pressure,
- propagation,
- direct-hit đúng mục tiêu để lan ảnh hưởng ra cả board.

Mark là **weak point** để direct-hit khai thác.  
Mark **không stack**.

### 8.2 Rule đã chốt cho Mark

- Mark không stack.
- Shock phụ của Lightning **không làm mất Mark**.

### 8.3 Non-Lightning hit vào Mark

Nếu direct-hit **không phải Lightning** đánh vào mục tiêu đang có Mark:

- gây thêm **`+4 direct damage`** lên chính mục tiêu đó.

Ý nghĩa:

- Mark là một điểm yếu mà mọi build direct-hit có thể khai thác,
- không khóa Mark chỉ cho Lightning.

### 8.4 Lightning hit vào Mark

Nếu direct-hit **là Lightning** đánh vào mục tiêu có Mark:

- hit chính gây damage bình thường,
- sau đó proc **`4 damage all enemies`**.

Ý nghĩa:

- Lightning không chỉ lấy thêm damage đơn mục tiêu,
- Lightning biến Mark thành board-wide payoff.

### 8.5 Rule của shock phụ Lightning

Shock phụ phải giữ các rule sau:

- **không cộng Added Value**,
- **không tiêu Mark**,
- **không proc Mark**,
- **không chain tiếp**.

Shock phụ phải rõ ràng, gọn, không cascade vô hạn.

### 8.6 Nếu AoE Lightning direct-hit nhiều mục tiêu có Mark

Current locked rules:

- mỗi mục tiêu có Mark tạo **1 shock proc**,
- shock chạy **tuần tự**,
- mỗi proc cách nhau **`0.2s`**.

### 8.7 Identity gameplay

Lightning là hệ của:

- chọn đúng mục tiêu để cả board trả giá,
- ưu tiên sequencing và board setup,
- đổi direct-hit thành lan áp lực.

### 8.8 Edge cases

- Shock phụ không consume Mark.
- Shock phụ không tự proc Mark hoặc chain tiếp.
- Nếu AoE direct-hit nhiều target có Mark, tính từng proc shock tuần tự.
- Nếu hit không phải direct-hit thì không được coi là trigger Mark payoff kiểu hit chính.

---

## 9. Bleed

### 9.1 Mục tiêu

Bleed là hệ của:

- áp lực dài hạn,
- damage-over-time thật,
- chuyển hóa tài nguyên ở build phù hợp.

### 9.2 Rule đã chốt

- Bleed gây damage **đầu lượt**
- Bleed **bỏ qua Guard**
- Bleed **giảm dần theo lượt**
- current wording rõ nhất hiện tại là **`-1 stack mỗi cuối lượt`**

### 9.3 Identity gameplay

Bleed khác Burn ở chỗ:

- Burn là tài nguyên để consume,
- Bleed là damage-over-time thật,
- và trong đúng build, Bleed còn có thể đổi sang Guard hoặc Consumable.

### 9.4 Edge cases

- Bleed tick **không consume Stagger**.
- Bleed bypass Guard nhưng vẫn phải đọc đúng timing đầu lượt.
- Bleed stack là một dạng tài nguyên build-level, không nên bị coi chỉ là số máu chảy máu vô danh.

---

## 10. Stagger như một payoff trung gian

Dù Stagger là core combat rule, ở cấp status engine nó đóng vai trò như một “cơ hội payoff ngắn hạn”.

Current locked rules nhắc lại:

- Khi Guard từ `> 0` về `0`, mục tiêu vào Stagger.
- Chỉ **hit direct kế tiếp duy nhất** mới ăn `x1.2 tổng damage`.
- `Lightning shock` và `Bleed tick` **không consume** Stagger.
- `Burn consume` nằm trong direct-hit thì tính vào tổng damage trước khi nhân `1.2`.

Ý nghĩa thiết kế:

- Stagger thưởng cho sequencing tốt,
- tạo cầu nối giữa anti-Guard và payoff,
- khiến Guard break có giá trị chiến thuật chứ không chỉ là mất giáp.

---

## 11. Ailment system

### 11.1 Vai trò hiện tại

Ailment hiện được coi là **enemy-side system**.

Nghĩa là:

- không xem ailment là fantasy trung tâm của player build ở giai đoạn hiện tại,
- enemy là bên chủ yếu dùng ailment lên player / combat state,
- nếu trong code còn helper player → enemy thì không nên xem đó là gameplay ưu tiên.

### 11.2 Rule chance hiện tại trong context

- **enemy → player = 100% chance**

### 11.3 Ý nghĩa thiết kế

Ailment tồn tại để:

- enemy tạo pressure,
- tạo disruption,
- thử thách cách player xoay resources và sequencing,
- nhưng không nên cướp vai trò trung tâm của dice loop.

### 11.4 Guardrail

Khi mô tả system, tooltip, refactor plan hoặc content note:

- không mô tả Ailment như bộ skill tiêu chuẩn player-side,
- không đẩy game thành một combat loop phụ chỉ xoay quanh ailment nếu chưa có chủ đích rõ,
- nếu sau này mở rộng ailment cho player, phải kiểm tra lại alignment với core pillars.

---

## 12. Priority và logic resolve tổng quát

### 12.1 Direct-hit phải được ưu tiên đọc rõ

Nhiều interaction trong game này chỉ đúng nếu action là **direct-hit**:

- Mark payoff,
- Burn consume nằm trong hit,
- Stagger consume,
- một số skill payoff theo exact value.

Vì vậy về mặt spec, luôn phải phân biệt:

- direct-hit,
- tick,
- proc phụ,
- shock,
- effect tồn đọng.

### 12.2 Setup → payoff phải nhìn ra được bằng mắt

Trong UI và design, player phải đọc ra được ít nhất:

- ai đang có Burn,
- ai đang có Mark,
- ai đang Freeze / Chilled,
- ai đang Bleed,
- ai đang Guard / Stagger.

Nếu board state quá khó đọc, status engine sẽ mất giá trị chiến thuật.

---

## 13. Những gì chưa final trong file này

Các vùng sau chưa nên coi là khóa cứng ở cấp content implementation:

- full catalog các ailment type cụ thể,
- icon / VFX / SFX cuối cùng cho từng trạng thái,
- tất cả rule phụ của mọi skill content pool,
- các exception quá đặc thù của từng boss hoặc từng enemy.

Nhưng các vùng sau là **current locked rules**:

- Burn là resource để consume, baseline `+2 mỗi stack`,
- Freeze → Chilled, miễn Freeze khi đang Freeze/Chilled, Ice hit cho `+1 Focus +3 Guard`,
- Mark không stack, non-Lightning direct-hit vào Mark = `+4 direct damage`, Lightning direct-hit vào Mark = `4 damage all enemies`,
- shock phụ không chain, không proc lại, không consume Mark,
- Bleed đầu lượt, bypass Guard, giảm dần, current wording `-1 stack mỗi cuối lượt`,
- ailment là enemy-side system với current context `100% chance` từ enemy lên player.
