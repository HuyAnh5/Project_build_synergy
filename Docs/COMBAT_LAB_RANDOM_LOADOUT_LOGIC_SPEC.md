# Combat Lab Random Loadout Prototype — Logic Spec

> Bản này chỉ mô tả **logic thiết kế và flow vận hành**.  
> Không viết code, không pseudocode, không class C#.  
> File dùng để đưa cho Codex/dev hiểu cần tạo hệ thống gì.

---

## 1. Mục tiêu của prototype

Prototype này là một **Combat Lab** để test gameplay combat nhanh.

Mỗi lần bắt đầu lại hoặc làm mới combat, player sẽ nhận một bộ setup mới gồm:

1. **Dice**
2. **Skill**
3. **Consumable**

Không có reward, không có shop, không có map, không có passive random.

Mục tiêu là để người chơi nhìn vào bộ dice / skill / consumable hiện tại rồi tự tìm cách đánh tốt nhất trong một combat ngắn.

---

## 2. Scope hiện tại

### Có trong prototype

- 1 player.
- 3 enemy.
- Player có 3 dice.
- Player có một số skill được random từ pool đã chọn.
- Player có 3 consumable.
- Mỗi lần refresh combat sẽ random lại dice / skill / consumable.
- Dice có thể trùng loại.
- Skill chỉ random từ danh sách được cho phép.
- Consumable chỉ random từ danh sách được cho phép.
- Trong 3 consumable luôn chắc chắn có consumable `Adjust Face +`.

### Không có trong prototype

- Không reward sau combat.
- Không shop.
- Không map.
- Không event.
- Không run progression.
- Không unlock.
- Không passive random.
- Không relic.
- Không inventory dài hạn.
- Không cần build progression qua nhiều trận.

---

## 3. Nguyên tắc source dữ liệu

Toàn bộ dữ liệu dùng để random phải được điền trong **một ScriptableObject config duy nhất**.

ScriptableObject này là nơi designer/dev thả vào:

- dice prefab pool;
- skill pool;
- consumable pool;
- consumable chắc chắn xuất hiện;
- số lượng skill cần random;
- số lượng consumable cần random;
- các rule cho phép trùng hay không.

Không được random từ dữ liệu nằm rải rác ở scene, object khác, prefab khác hoặc tự quét toàn bộ project.

Nói ngắn:

```text
Random cái gì thì chỉ được lấy từ ScriptableObject config đó.
```

---

## 4. ScriptableObject config cần chứa những nhóm dữ liệu nào

ScriptableObject config cần có ít nhất 3 nhóm chính.

### 4.1 Dice Pool

Đây là danh sách các dice prefab mà hệ thống được phép chọn cho player.

Ví dụ designer có thể thả vào:

- d4 prefab;
- d6 prefab;
- d8 prefab;
- d12 prefab;
- d20 prefab;
- custom dice prefab nếu có.

Mỗi lần refresh, hệ thống chọn 3 dice từ danh sách này.

### 4.2 Skill Pool

Đây là danh sách các skill được phép xuất hiện trong prototype.

Skill random chỉ được lấy từ danh sách này.

Nếu một skill không nằm trong danh sách này thì tuyệt đối không được random ra, dù skill đó có tồn tại trong project.

### 4.3 Consumable Pool

Đây là danh sách consumable được phép random.

Ngoài ra config cần chỉ định một consumable bắt buộc xuất hiện, hiện tại là:

```text
Adjust Face +
```

Mỗi lần refresh, player nhận 3 consumable:

- 1 slot chắc chắn là `Adjust Face +`;
- các slot còn lại random từ consumable pool.

---

## 5. Dice random logic

### 5.1 Mỗi lần refresh chọn 3 dice

Khi combat được làm mới, player luôn nhận đúng 3 dice.

3 dice này được chọn ngẫu nhiên từ Dice Pool trong ScriptableObject config.

### 5.2 Dice có thể trùng loại

Không giới hạn việc trùng dice.

Các trường hợp sau đều hợp lệ:

```text
d8 / d8 / d8
d6 / d6 / d20
d4 / d12 / d12
```

Không cần ép mỗi dice phải khác nhau.

Không cần ép phải có đủ small / medium / large dice.

### 5.3 Dice prefab chỉ là template

Dice prefab được thả vào config chỉ là nguồn để tạo dice runtime cho player.

Khi random, hệ thống phải tạo bản dice runtime riêng cho combat hiện tại.

Không được làm thay đổi dữ liệu gốc của prefab.

Lý do:

- mỗi lần refresh cần dice mới;
- prefab gốc phải giữ nguyên;
- tránh việc test nhiều lần làm hỏng dữ liệu asset.

### 5.4 Random Base Value cho từng mặt

Sau khi chọn dice, từng mặt của dice đó sẽ được random Base Value.

Rule:

```text
Base Value ban đầu của mỗi mặt phải nằm trong khoảng 1 đến số mặt của dice đó.
```

Ví dụ:

```text
d4: mỗi mặt random từ 1 đến 4
d6: mỗi mặt random từ 1 đến 6
d8: mỗi mặt random từ 1 đến 8
d12: mỗi mặt random từ 1 đến 12
d20: mỗi mặt random từ 1 đến 20
```

Ví dụ d6 hợp lệ:

```text
d6 = [2, 1, 5, 4, 4, 2]
```

Ví dụ d6 không hợp lệ ở lúc spawn ban đầu:

```text
d6 = [2, 1, 7, 4, 4, 2]
```

Vì d6 không được random ra Base Value 7 lúc khởi tạo.

### 5.5 Mặt dice được phép trùng value

Không cần ép các mặt phải khác nhau.

Ví dụ hợp lệ:

```text
d6 = [4, 4, 4, 1, 6, 2]
d8 = [1, 1, 1, 7, 2, 6, 6, 8]
```

Việc nhiều mặt trùng nhau là hợp lệ vì nó tạo identity riêng cho dice.

---

## 6. Skill random logic

### 6.1 Skill chỉ lấy từ pool được bật

Mỗi lần refresh, hệ thống random skill từ Skill Pool trong ScriptableObject config.

Không random toàn bộ skill trong project.

Không lấy skill ngoài danh sách được chọn.

### 6.2 Số lượng skill

Prototype nên dùng số lượng skill được config quy định.

Mặc định nên là 4 skill, vì combat hiện tại xoay quanh 4 skill slot.

Nếu sau này muốn test ít hơn hoặc nhiều hơn, chỉnh trong config.

### 6.3 Có nên cho trùng skill không

Mặc định nên không cho trùng skill trong cùng một setup.

Ví dụ không nên xảy ra ở mặc định:

```text
Fire Slash / Fire Slash / Fire Slash / Hellfire
```

Nếu sau này muốn test duplicate skill, có thể thêm option trong config.

Nhưng prototype đầu nên để mỗi skill xuất hiện tối đa một lần trong một setup.

### 6.4 Không random passive

Bản này không đụng vào passive.

Player chỉ cần đọc 3 thứ:

```text
Dice
Skill
Consumable
```

Passive không nằm trong random setup của prototype này.

---

## 7. Consumable random logic

### 7.1 Player luôn có 3 consumable

Mỗi lần refresh, player nhận 3 consumable.

Trong đó chắc chắn có:

```text
Adjust Face +
```

Các consumable còn lại random từ Consumable Pool trong ScriptableObject config.

### 7.2 Guaranteed consumable

`Adjust Face +` không được tìm bằng tên string trong logic.

Nó phải là một object được thả trực tiếp vào field “Guaranteed Consumable” trong ScriptableObject config.

Như vậy nếu sau này đổi tên item, logic vẫn đúng.

### 7.3 Có nên cho trùng consumable không

Mặc định nên không cho trùng consumable.

Ví dụ không nên xảy ra ở mặc định:

```text
Adjust Face+ / Adjust Face+ / Restore Focus
```

Vì `Adjust Face +` đã là consumable chắc chắn có, pool random còn lại không nên lặp lại nó nếu duplicate bị tắt.

Nếu sau này muốn test duplicate consumable, có thể thêm option trong config.

---

## 8. Logic của Adjust Face +

Consumable `Adjust Face +` là item bắt buộc xuất hiện trong mọi setup.

Logic prototype:

```text
Chọn tối đa 3 mặt trên cùng 1 dice.
Mỗi mặt được chọn nhận +1 Base Value.
```

Trong prototype hiện tại, sau khi tăng, Base Value nên bị giới hạn bởi số mặt của dice.

Ví dụ d6:

```text
d6 = [2, 1, 5, 4, 4, 2]
```

Nếu dùng `Adjust Face +` lên ba mặt:

```text
1 -> 2
5 -> 6
4 -> 5
```

Kết quả:

```text
d6 = [2, 2, 6, 5, 4, 2]
```

Nếu một mặt d6 đang là 6:

```text
6 + 1 = 6
```

Trong prototype đầu, không cho thành 7 để tránh người chơi mới thắc mắc vì sao d6 có mặt 7.

Sau này nếu muốn đi sâu vào dice customization, có thể mở rule cho dice edit vượt quá số mặt. Nhưng bản prototype combat đầu chưa cần.

---

## 9. Flow refresh setup

Mỗi lần bấm refresh hoặc bắt đầu lại combat:

1. Xóa setup runtime hiện tại của player.
2. Random 3 dice từ Dice Pool.
3. Tạo bản runtime cho từng dice.
4. Random Base Value cho từng mặt của từng dice.
5. Gắn 3 dice đó vào player.
6. Random skill từ Skill Pool.
7. Gắn skill vào skill slot của player.
8. Thêm `Adjust Face +` vào consumable slot.
9. Random các consumable còn lại từ Consumable Pool.
10. Gắn consumable vào player.
11. Refresh UI.
12. Bắt đầu hoặc reset combat với setup mới.

Flow này chỉ xử lý dice / skill / consumable.

Không xử lý passive, reward, shop, map hoặc run progression.

---

## 10. UI cần đọc được gì

Prototype không cần UI đẹp, nhưng phải đọc rõ.

Player phải luôn thấy 3 nhóm:

```text
Dice
Skill
Consumable
```

### 10.1 Dice UI

Mỗi dice cần cho player đọc được:

- dice đó là loại gì;
- các Base Value trên mặt dice;
- kết quả roll hiện tại;
- dice còn dùng được hay đã bị consume;
- nếu có hover skill, dice nào sẽ bị skill đó consume.

Ví dụ hiển thị debug hợp lệ:

```text
Dice 1: d8 [2, 8, 1, 4, 4, 7, 3, 6]
Rolled: 7
State: Ready
```

### 10.2 Skill UI

Mỗi skill cần cho player đọc được:

- tên skill;
- số dice cần consume;
- Focus cost;
- target hợp lệ;
- preview skill sẽ consume dice nào nếu cast ngay.

Ví dụ:

```text
Hellfire
Cost: 3 dice / 2 Focus
Target: Enemy
Will consume: Dice 1, Dice 2, Dice 3
```

### 10.3 Consumable UI

Mỗi consumable cần cho player đọc được:

- tên consumable;
- dùng vào đối tượng nào;
- điều kiện nào để nút Use sáng;
- vì sao chưa dùng được nếu thiếu target.

Ví dụ:

```text
Adjust Face +
Select up to 3 faces on 1 dice.
Use available only after valid face selection.
```

---

## 11. Enemy scope

Prototype hiện tại chỉ cần 3 enemy.

Enemy có thể cố định trong scene hoặc spawn từ một encounter đơn giản.

Không cần random enemy nếu chưa cần.

Mục tiêu của enemy trong bản này là tạo áp lực để player dùng dice / skill / consumable, không phải test full encounter system.

Gợi ý 3 enemy đơn giản:

```text
1. Bruiser
- đánh rõ
- ép player phòng thủ hoặc giết nhanh

2. Guard enemy
- có Guard hoặc tự tạo Guard
- dạy sequencing / Stagger / anti-Guard

3. Caster
- setup big move hoặc áp status nhẹ
- ép player chọn target ưu tiên
```

Nếu chưa có đủ hệ enemy, chỉ cần 3 enemy cố định với HP / damage / intent đơn giản.

---

## 12. Guardrail quan trọng

### 12.1 Không lấy random source ngoài config

Mọi thứ random phải đến từ ScriptableObject config.

Không lấy dữ liệu từ:

- scene list riêng;
- hardcode trong randomizer;
- search toàn project;
- Resources load toàn bộ asset;
- prefab tự khai báo pool riêng.

### 12.2 Không mutate prefab gốc

Dice prefab gốc không bị thay đổi Base Value trong quá trình refresh.

Chỉ runtime dice instance mới được random mặt.

### 12.3 Không random passive

Passive không thuộc prototype này.

Nếu scene hiện tại cần passive để không lỗi, giữ passive mặc định hoặc để trống, nhưng không random.

### 12.4 Không thêm reward trá hình

Không có reward sau combat.

Không có màn chọn item sau combat.

Không có shop mini.

Không có map mini.

Prototype chỉ là combat setup random.

### 12.5 UI không là source of truth

UI chỉ hiển thị setup hiện tại.

Gameplay logic phải đọc từ runtime player loadout, không đọc từ text UI.

---

## 13. Test checklist

### Dice

- Refresh tạo đúng 3 dice.
- 3 dice có thể trùng loại.
- d8 / d8 / d8 có thể xuất hiện nếu d8 nằm trong Dice Pool.
- Mỗi dice random Base Value mới cho từng mặt.
- d6 không có Base Value ban đầu lớn hơn 6.
- d8 không có Base Value ban đầu lớn hơn 8.
- Các mặt được phép trùng value.
- Prefab gốc không bị thay đổi sau nhiều lần refresh.

### Skill

- Skill chỉ random từ Skill Pool trong config.
- Không lấy skill ngoài config.
- Số skill đúng theo config.
- Mặc định không trùng skill nếu duplicate bị tắt.
- Không random passive.

### Consumable

- Player luôn có 3 consumable.
- Luôn có `Adjust Face +`.
- Các consumable còn lại lấy từ Consumable Pool.
- Mặc định không trùng consumable nếu duplicate bị tắt.
- `Adjust Face +` tăng đúng tối đa 3 mặt trên cùng 1 dice.
- `Adjust Face +` không làm d6 vượt quá 6 trong prototype v1.

### Scope

- Không reward.
- Không shop.
- Không map.
- Không event.
- Không run progression.
- Không passive random.

### UI

- UI có 3 nhóm rõ: Dice / Skill / Consumable.
- Sau refresh UI update đúng.
- Player đọc được dice đang có.
- Player đọc được skill đang có.
- Player đọc được consumable đang có.
- Khi hover skill, player biết skill sẽ consume dice nào.

---

## 14. Definition of Done

Bản này hoàn thành khi:

```text
Bấm Refresh
→ player nhận 3 dice random từ config
→ dice có thể trùng loại
→ từng mặt dice có Base Value random hợp lệ theo số mặt
→ player nhận skill random từ config
→ player nhận 3 consumable từ config
→ luôn có Adjust Face +
→ không random passive
→ UI hiển thị rõ Dice / Skill / Consumable
→ combat có thể bắt đầu với setup đó
```

Nếu đạt những điều trên thì prototype random setup đã đủ cho mục tiêu combat lab.

---

## 15. Tóm tắt ngắn

Prototype này chỉ cần một hệ random setup đơn giản:

```text
Một ScriptableObject config
→ chứa Dice Pool
→ chứa Skill Pool
→ chứa Consumable Pool
→ chứa Guaranteed Consumable
```

Mỗi lần refresh:

```text
Random 3 dice
Random face value cho dice
Random skill
Random 3 consumable
Luôn có Adjust Face +
Hiển thị Dice / Skill / Consumable
Bắt đầu combat
```

Không làm gì ngoài scope đó.
