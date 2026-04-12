# COMBAT_CHANGES_2026.md

> Tai lieu ghi lai nhung thay doi design da duoc thao luan va chot trong nam 2026.
> File nay la ghi chu tam thoi de cap nhat lai spec chinh sau.

---

## 1. Muc tieu cua thay doi 2026

Huong 2026 khong doi core combat rule.

Nhung thu GIU NGUYEN:
- Base Value / Added Value
- Crit / Fail
- Guard / Stagger
- Focus economy
- Multi-slot skill logic goc
- Roll dice system hien tai

Nhung thu DOI:
- interaction grammar cua player turn
- cach dung skill
- cach consume dice trong turn
- nhin/feel cua action economy

Huong moi muon combat co cam giac:
- nhanh hon
- ro hon
- hop PC hon
- tactile hon
- dice giong "ammo" hon la object dung de gan tay vao tung skill

---

## 2. Turn Flow Moi

Flow moi cua player turn:

```text
Roll Dice -> Player Phase -> End Phase -> Enemy Phase
```

Trong do:

### 2.1 Roll Dice

- Giu nguyen he thong roll hien tai.
- Chua dung vao phan roll dice trong batch thay doi nay.
- Player van roll nhu flow hien co cua project.

### 2.2 Player Phase

Trong player phase, player co 2 hanh vi chinh:

1. Reorder dice neu muon
2. Keo skill icon tu skill slot vao enemy de cast

Status implementation hien tai:
- Reorder da hoat dong theo huong moi
- Drag skill vao target de cast da hoat dong
- Self-target da co vung cast rieng de tha vao

Khong con flow:
- equip skill vao planning slot
- lock planning
- execute phase rieng

Khong con grammar:
- plan xong roi bam nut de chuyen sang execute

Thay vao do:
- player nhin thu tu dice hien tai
- neu muon thi reorder dice truoc
- keo skill tu skill slot vao target
- skill duoc cast ngay lap tuc
- dice bi consume ngay sau khi cast

### 2.3 End Phase

- Khi player khong muon dung them skill nua, player bam End Phase / End Turn.
- Sau do sang enemy phase.

### 2.4 Enemy Phase

- Enemy action nhu flow combat hien co.
- Sau enemy phase thi bat dau turn moi cua player.

---

## 3. Skill Use Grammar Moi

### 3.1 Player khong equip skill vao board nua

- Skill van nam trong skill slot / skill bar.
- Player khong gan skill vao action slot tren board truoc khi dung.
- Skill duoc dung truc tiep bang thao tac drag skill vao target.

### 3.2 Cast skill

- Player keo icon skill tu skill slot vao enemy.
- Khi tha dung target hop le, skill duoc cast ngay.
- Khong can lock phase.
- Khong can execute phase rieng.
- Khong can target selection sau khi da commit.

### 3.3 Rule consume dice

- Moi skill se consume dice dua theo so slot ma skill chiem.
- Skill 1 slot -> consume 1 dice
- Skill 2 slot -> consume 2 dice
- Skill 3 slot -> consume 3 dice

Rule consume:
- Luon consume theo thu tu dice hien tai cua player
- Doc tu trai sang phai
- Neu player reorder truoc khi cast thi thu tu consume thay doi theo reorder do

Y nghia:
- Reorder dice van la quyet dinh chien thuat that
- Nhung player khong can gan tay tung die vao tung skill nua

Status implementation hien tai:
- Rule consume 1/2/3 dice da duoc lam theo slot cost
- Consume doc tu trai sang phai theo thu tu dice hien tai da hoat dong
- Reorder truoc khi cast da anh huong truc tiep den bo dice bi consume

### 3.4 Multi-slot skill

- Khong doi logic goc cua multi-slot skill
- Dieu duy nhat doi la grammar dieu khien
- Thay vi chiem board planning roi moi execute
- Skill se consume lien tiep N dice dau tien con lai theo thu tu hien tai

Vi du:
- Hellfire 3 slot
- Khi cast Hellfire, game consume 3 dice dau tien con lai
- Resolve skill dua tren 3 dice do

Status implementation hien tai:
- Multi-slot skill da di theo grammar moi
- Khong con phu thuoc vao planning board / execute phase cu de moi cast

---

## 4. Dice Consumption Rule Moi

### 4.1 Khong con "dim 50%"

Ban chot lai:
- Dice sau khi dung KHONG con o tren board voi alpha 50%
- Dice sau khi dung se bi consume han
- Tuc la bien mat khoi hang dice cua turn do

Status implementation hien tai:
- Logic consume da xong
- Presentation tam thoi van dang de dice da dung mo 50% de de doc va debug
- Day la state tam, khong phai visual final cua huong 2026
- Muc tieu final van la dice da dung phai bien mat / roi khoi hang dice cua turn do

### 4.2 Dice la ammo

Huong feel moi:
- Moi dice giong 1 vien dan
- Moi skill la cach tieu hao dan
- Player quan ly thu tu dan bang reorder
- Skill manh hon se an nhieu dice hon

Day la hinh anh/feel can giu trong implementation va animation.

### 4.3 Turn moi = nap dan lai

- Sang turn moi, 3 dice xuat hien lai day du
- Thu tu dice duoc giu nguyen
- Co animation "day ra / nap ra" tu phai sang trai
- Day chi la presentation / animation feel
- Khong phai thay doi core logic thu tu dice

Muc tieu cam xuc:
- player co cam giac duoc nap dan moi turn
- nhin thay ro tai nguyen dang duoc xai dan trong turn

---

## 5. Vai tro cua Reorder trong huong moi

Reorder van rat quan trong.

Reorder khong con phuc vu cho:
- gan skill vao lane truoc

Reorder gio phuc vu cho:
- quyet dinh skill tiep theo se an dice nao
- setup dice cao/thap cho skill sap dung
- doi vi tri de combo dung exact / highest / lowest / parity / crit-fail

Noi cach khac:
- assignment puzzle giam
- sequencing puzzle tang len

Day la tradeoff co chu dich.

---

## 6. So sanh voi huong truoc do

### 6.1 Huong truoc do

```text
Roll -> Equip skill vao planning slot -> Reorder neu can -> Chon target -> Cast
```

Van de:
- nhieu buoc hon
- thao tac cham hon
- tren PC cam thay co them mot lop friction

### 6.2 Huong moi da chot

```text
Roll -> Reorder neu can -> Drag skill vao target -> Consume dice -> End Phase
```

Loi ich:
- nhanh hon
- ro hon
- hop chuot/PC hon
- de doc board state hon
- tao fantasy "dice = ammo" ro hon

Tradeoff:
- giam bot layer "gan die cu the vao skill cu the"
- tang tam quan trong cua thu tu dice

---

## 7. Thu khong thay doi

Nhung diem nay khong doi trong file thay doi 2026:

- Khong sua logic roll dice o batch nay
- Khong sua multi-slot core rule
- Khong doi combat formula
- Khong doi targeting rule co ban cua tung skill
- Khong doi passive slot da chot moi: build state hien tai chi con 1 passive

---

## 8. Tong ket 2026 Direction

Cau chot cua huong 2026:

> Combat se di theo huong STS-like drag-to-target o cap dieu khien,
> nhung van giu dice order la trung tam cua chien thuat.

Tom lai:
- Roll dice giu nguyen
- Khong equip skill vao planning slot nua
- Player co the reorder dice trong player phase
- Drag skill icon tu skill slot vao enemy de cast ngay
- Skill consume dice theo slot cost, doc tu trai sang phai theo thu tu hien tai
- Dice da dung se bien mat, khong dim 50%
- Turn moi se "nap lai" 3 dice bang animation day ra tu phai sang trai
- End Phase xong thi sang enemy phase

---

## 9. Viec can lam tiep

- [x] Reorder trong player phase anh huong den dice se bi consume
- [x] Skill consume dice theo slot cost, doc tu trai sang phai theo thu tu hien tai
- [x] Drag skill vao target de cast ngay
- [x] Co self-cast zone rieng cho skill target Self
- [x] Cap nhat COMBAT_CORE_SPEC.md theo grammar moi
- [x] Cap nhat UX/UI spec theo huong drag skill vao target
- [ ] Xac dinh visual feedback khi skill consume 1/2/3 dice
- [ ] Xac dinh animation consume dice
- [ ] Xac dinh animation "nap lai" dice dau turn
- [ ] Test xem reorder trong player phase du signal hay chua
- [ ] Test multi-slot skill trong huong consume dice tu trai sang phai
- [ ] Ra soat cac skill/payoff co phu thuoc local dice group de dam bao van doc dung

---

## 10. Note Rieng - Y tuong Cooldown thay / bo sung Focus (CHUA CHOT)

Muc nay chi la note de giu y tuong lai.
Khong phai thay doi da duoc approve.
Khong duoc coi la source of truth.

### 10.1 Y tuong

Co kha nang ve sau se thu mot trong 2 huong:

1. Cooldown thay hoan toan Focus
2. Cooldown bo sung len tren Focus

### 10.2 Mo ta huong Cooldown-only

Neu bo Focus va doi sang cooldown:

- Moi skill co chi so cooldown rieng
- Cooldown = 0:
  - co the dung nhieu lan trong 1 turn
  - chi can du dice de consume
- Cooldown = 1:
  - dung xong thi turn sau moi dung lai duoc
- Cooldown = 2/3:
  - phai cho them 2-3 turn moi reset

Luc do rang buoc chinh cua player turn se la:
- con bao nhieu dice
- skill nao dang cooldown

### 10.3 Loi ich tiem nang

- de hieu hon
- hop voi flow drag skill -> target
- ro identity tung skill hon
- de gioi han spam cua nhom skill manh

### 10.4 Rui ro lon

- neu bo Focus hoan toan, game co the mat bot economy layer
- Basic Attack / Basic Guard can viet lai vai tro
- cac skill / passive doc Focus se mat gia tri hoac can rework
- game de bi flatten thanh "co dice thi spam skill nao dang available"

### 10.5 Danh gia tam thoi

Danh gia tam thoi hien tai:

- Cooldown la huong dang can note lai
- Nhung khong nen coi la thay doi da chot
- Huong an toan hon neu test sau nay:
  - giu Focus
  - them cooldown cho mot so skill dac biet / skill manh

### 10.6 Ket luan cua note nay

Tam thoi:
- KHONG cap nhat vao spec chinh
- KHONG sua runtime theo huong nay
- CHI giu lai de quyet dinh sau
