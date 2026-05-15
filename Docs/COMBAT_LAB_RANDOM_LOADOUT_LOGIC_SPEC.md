# Combat Lab Random Loadout Prototype - Logic Spec

> File nay mo ta logic cua Combat Lab prototype.
> Day la file rule cho prototype test combat, khong phai rule cua ca game.
> Neu prototype va game core khac nhau, prototype chi duoc override trong pham vi lab nay.


> **CURRENT SOURCE UPDATE:** Bản này đã nhập các rule hiện tại từ các draft cũ. Không dùng file `archived combat change draft` làm source nữa. Resource hiện tại là **AP**. Combat flow hiện tại là **Player Phase**: dice tự roll đầu phase → player reorder → click/drag skill vào target để cast ngay → dice chuyển used state → End Phase → Enemy Phase.

---
---

## 1. Muc tieu

Combat Lab dung de:

- reset combat nhanh;
- test encounter cu the;
- test 4 skill slot trong nhieu setup;
- test dice-driven combat voi random co kiem soat;
- de debug combat ma khong can map/shop/run progression.

Moi lan `Reset Game`, prototype phai tao 1 combat moi sach.

---

## 2. Rule da chot

1. Co `Reset Game`.
2. Enemy het HP thi bi tat khoi combat.
3. Player het HP thi bi tat khoi combat, icon player bien mat, khoa moi thu tru `Reset Game`.
4. Enemy roster va row duoc author bang `1 ScriptableObject config`.
5. 4 skill slot cua player duoc random bang cach chon **2 skill pair**.
6. Dice duoc random prefab giua `d4` va `d8`.
7. Moi mat dice duoc random gia tri, nhung gia tri spawn ban dau khong vuot qua so mat cua dice do.
8. Consumable duoc dua thang vao, khong random.

---

## 3. Skill random rule

Prototype nay khong dung rule `6 skill -> 3 pair -> lay 3 skill` nua.

Rule moi:

- Player co `4 owned skill slot`.
- Designer author tung `skill pair`, moi pair chi co `2 skill`.
- Moi lan reset, he thong random `2 pair khac nhau`.
- Tong `4 skill` cua 2 pair do duoc dua vao 4 slot.

Vi du cac pair hop le:

- `Fire Pair = Ignite / Fire Slash`
- `Ice Pair = Deep Freeze / Shatter`
- `Lightning Pair = Spark Brand / Static Conduit`

Tu do he thong co the random thanh:

```text
Fire + Ice
Fire + Lightning
Lightning + Ice
```

Noi ngan:

```text
Inspector chi author tung pair 2 skill.
Moi reset = random 2 pair khac nhau -> thanh 4 skill.
```

---

## 4. Dice random rule

### 4.1 Loai dice

Prototype chi random giua:

- `d4`
- `d8`

Moi lan reset, player co 3 dice.

3 dice nay co the trung nhau tu do:

```text
d4 / d4 / d4
d8 / d8 / d8
d4 / d8 / d8
d4 / d4 / d8
```

Khong can ep khac nhau.

### 4.2 Gia tri mat luc spawn

Sau khi random prefab, tung mat cua runtime die duoc random lai gia tri.

Rule spawn:

- `d4`: moi mat chi duoc random trong `1..4`
- `d8`: moi mat chi duoc random trong `1..8`

Vi du hop le:

```text
d4 = [1, 4, 2, 2]
d8 = [8, 1, 3, 8, 5, 2, 7, 4]
```

Vi du khong hop le luc spawn:

```text
d4 = [1, 5, 2, 2]
d8 = [8, 9, 3, 8, 5, 2, 7, 4]
```

### 4.3 Day chi la rule spawn cua prototype

Rule `khong vuot qua so mat` chi dung luc prototype random setup ban dau.

Rule nay **khong override core game rule**.

Nghia la sau khi vao combat:

- consumable `Adjust Face +`
- hoac logic khac cua game

van duoc phep day gia tri mat len cao hon so mat, toi da `99`, theo rule chinh cua game.

Noi ngan:

```text
spawn prototype: d4 toi da 4, d8 toi da 8
sau do trong runtime game: van co the len 99
```

---

## 5. Consumable rule

Consumable trong prototype:

- khong random;
- duoc author san trong config;
- giu thu tu slot theo config neu UI can thu tu.

Vi du:

```text
Slot 1 = Adjust Face +
Slot 2 = Restore AP
Slot 3 = Final Verdict
```

Moi lan reset player nhan dung cac item do.

---

## 6. ScriptableObject config can chua gi

Config prototype can chua it nhat 4 nhom:

### 6.1 Enemy entries

Moi entry can co:

- enemy prefab nao;
- enemy nay o `Front Row` hay `Back Row`;
- order trong row neu can;
- co bat/tat entry nay hay khong.

### 6.2 Dice prefab pool

Config can tham chieu:

- `d4 prefab`
- `d8 prefab`

Prototype random 3 slot dice tu 2 prefab nay.

### 6.3 Skill pair list

Config can chua danh sach `skill pair`.

Moi pair chi co:

- `skillA`
- `skillB`

Vi du:

- `Fire Pair = Ignite / Fire Slash`
- `Ice Pair = Deep Freeze / Shatter`
- `Lightning Pair = Spark Brand / Static Conduit`

Moi lan reset:

- random 2 pair khac nhau;
- lay tong 4 skill cua 2 pair do;
- co the shuffle thu tu 4 skill truoc khi gan vao slot.

### 6.4 Fixed consumables

Config chua danh sach consumable co dinh de dua vao player.

---

## 7. Reset Game rule

`Reset Game` trong prototype nay duoc hieu la:

```text
tao lai 1 combat moi sach
```

Moi lan reset phai:

1. clear combat cu;
2. spawn lai player/enemy;
3. doc lai config encounter;
4. random lai 2 skill pair;
5. random lai 3 dice prefab tu d4/d8;
6. random lai gia tri mat cua tung die;
7. gan lai consumable co dinh;
8. reset HP, Guard, status, intent, UI.

Prototype co the implement reset bang full scene reload neu cach do giup state sach va on dinh hon.

---

## 8. Death handling

### 8.1 Enemy death

Khi enemy HP ve `0` hoac thap hon:

- enemy bi xem la dead ngay;
- enemy bi tat khoi combat runtime;
- khong con target duoc;
- khong con intent;
- khong con tick turn;
- world icon/UI cua no cung bien mat.

### 8.2 Player death

Khi player HP ve `0` hoac thap hon:

- player bi xem la dead ngay;
- actor player bi tat khoi combat runtime;
- icon player bien mat;
- lock moi input combat;
- khong duoc roll;
- khong duoc cast skill;
- khong duoc dung consumable;
- chi con `Reset Game` duoc dung.

---

## 9. Pham vi prototype

### Co trong prototype

- 1 player
- toi da 3 enemy
- enemy roster co dinh theo config
- row enemy co dinh theo config
- 4 owned skill slot random theo 2 pair
- 3 dice random giua d4/d8
- gia tri mat dice random theo rule prototype
- consumable co dinh
- reset nhanh

### Khong co trong prototype

- map
- shop
- reward
- unlock
- relic random
- progression qua nhieu tran
- dice pool rong hon d4/d8
- random consumable

---

## 10. Ban chot thuc dung

Ban prototype tot nhat cho nhu cau hien tai la:

1. `1 config SO` author enemy + row + skill preset + consumable + d4/d8 refs.
2. `1 reset button` de vao tran moi ngay.
3. `4 skill slot` random theo `2 skill pair`.
4. `3 dice` random giua `d4` va `d8`.
5. Moi mat dice random dung theo tran `1..4` hoac `1..8` luc spawn.
6. Sau khi vao tran, consumable `+1` va logic core van duoc day mat dice len toi `99`.
7. Enemy/player chet thi bien mat khoi combat runtime.
8. Player chet thi khoa moi thu tru `Reset Game`.

---

## 11. Removal Note

Section nay ton tai de sau nay nhin lai se biet:

- prototype dang nam o dau;
- file nao la file prototype thuan;
- file nao da bi patch vao core;
- muon go prototype thi phai xoa gi.

### 11.1 File prototype thuan

Day la cac file chi phuc vu Combat Lab prototype.
Neu sau nay bo tinh nang nay, day la nhom file uu tien xoa truoc:

- [CombatLabPrototypeConfigSO.cs](C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Assets/Scripts/Prototype/CombatLab/CombatLabPrototypeConfigSO.cs:1)
- [CombatLabPrototypeController.cs](C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Assets/Scripts/Prototype/CombatLab/CombatLabPrototypeController.cs:1)
- [COMBAT_LAB_RANDOM_LOADOUT_LOGIC_SPEC.md](C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Docs/COMBAT_LAB_RANDOM_LOADOUT_LOGIC_SPEC.md:1)

Neu scene test co:

- `CombatLabPrototypeRoot`
- nut `Reset Game`
- asset `CombatLabPrototypeConfig`

thi cac object/asset do cung la phan prototype va co the xoa cung dot nay.

### 11.2 File core da bi patch

De prototype nay chay dung, mot so file core da duoc sua them.
Xoa thu muc `Prototype/CombatLab` la chua du de go sach hoan toan.

Day la cac file core da bi patch:

- [CombatActor.cs](C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Assets/Scripts/Combat/Actors/CombatActor.cs:1)
- [TurnAPger.cs](C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Assets/Scripts/Combat/Turn/TurnAPger.cs:1)
- [ActorWorldUI.cs](C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Assets/Scripts/UI/Combat/ActorWorldUI.cs:1)
- [ConsumableBarUIAPger.cs](C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Assets/Scripts/UI/Combat/ConsumableBarUIAPger.cs:1)

### 11.3 Cac patch core dang lam gi

#### CombatActor.cs

Dang co patch cho:

- detect actor chet;
- tat actor runtime khi HP <= 0;
- reset lai death state khi vao tran moi.

Neu bo prototype va muon quay lai behavior cu, can go logic death-transition da them o file nay.

#### TurnAPger.cs

Dang co patch cho:

- `ArePlayerCommandsLocked`;
- lock input khi player chet;
- chan roll / continue / target click khi defeat;
- them defeat handling rieng;
- method `SetPlayerInteractionLocked`.

Neu bo prototype, can go nhom logic lock/defeat nay neu game core khong can no.

#### ActorWorldUI.cs

Dang co patch cho:

- tu tat world UI khi actor da chet hoac actor runtime da bi disable.

Neu bo prototype, can go check nay neu muon world UI quay lai behavior cu.

#### ConsumableBarUIAPger.cs

Dang co patch cho:

- khoa click/drag/use/sell consumable khi player da chet;
- tu refresh UI khi combat interaction bi khoa;
- helper `IsInteractionLocked`.

Neu bo prototype, can go nhom lock nay neu consumable UI cua game core khong can behavior do.

### 11.4 Thu tu go prototype de it loi nhat

Neu sau nay muon xoa Combat Lab prototype, thu tu an toan nen la:

1. xoa scene test / button reset / asset config prototype;
2. xoa 2 file trong `Assets/Scripts/Prototype/CombatLab`;
3. xoa file spec prototype trong `Docs`;
4. revert patch trong 4 file core:
   - `CombatActor.cs`
   - `TurnAPger.cs`
   - `ActorWorldUI.cs`
   - `ConsumableBarUIAPger.cs`
5. compile lai va test:
   - death flow
   - combat input
   - consumable UI
   - actor world UI

### 11.5 Ket luan removal

Noi ngan:

```text
Xoa prototype nhanh:
- Prototype/CombatLab/*
- scene/asset/button test

Xoa prototype sach 100%:
- xoa nhom tren
- revert 4 file core da patch
```