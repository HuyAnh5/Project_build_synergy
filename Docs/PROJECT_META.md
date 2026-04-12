# PROJECT_META.md

> Đây là **meta context file** của project.
> File này không phải gameplay spec chi tiết. Nó tồn tại để mô tả:
> - bộ tài liệu hiện tại được chia như thế nào,
> - file nào là source of truth cho từng mảng,
> - code architecture hiện tại,
> - refactor state,
> - vùng nào ổn và không nên đụng,
> - vùng nào còn mở,
> - guardrail cho AI / người tiếp tục sửa project.
>
> Khi có mâu thuẫn giữa suy đoán cá nhân và file này, ưu tiên file này để xác định **nên đọc tài liệu nào** và **nên sửa theo hướng nào**.
> Khi có mâu thuẫn giữa gameplay detail trong file này và các file spec chuyên trách, **file spec chuyên trách thắng**.
>
> **Quan trọng:** `PROJECT_META.md` không lặp lại gameplay design summary nếu phần đó đã có chỗ rõ ràng trong `GDD_VISION.md` và các file spec. Meta file này chỉ giữ mức orientation ngắn gọn và mapping tài liệu.

---

## 1. Mục đích của file này

File này tồn tại để làm 5 việc:

1. **Document map** — cho biết project hiện có những tài liệu nào và tài liệu nào phụ trách mảng nào.
2. **Implementation context** — mô tả source of truth, class chính, cấu trúc thư mục, hướng refactor đã chọn.
3. **Current state** — mô tả những vùng đã ổn, những vùng còn mở và những vùng dễ gây vỡ nếu chạm sai.
4. **Handoff document** — để gửi cho AI khác hoặc người khác tiếp tục hỗ trợ project mà không cần kể lại toàn bộ lịch sử.
5. **Guardrail document** — xác định cách đọc spec, cách sửa code và cách tránh làm lệch project.

File này không dùng để lưu:

- combat rule chi tiết,
- status interaction đầy đủ,
- toàn bộ skill/passive content,
- economy/progression detail,
- UI/UX spec chi tiết.

Những phần đó đã có tài liệu riêng.

---

## 2. Bộ tài liệu hiện tại và source of truth theo mảng

### 2.1 Tài liệu định hướng

- `GDD_VISION.md`
  - Vai trò: khóa ý đồ cốt lõi, pillar, anti-pillar, core loop tổng quan và định hướng hệ thống.
  - Dùng khi cần trả lời: game này là gì, cảm giác đúng là gì, ý tưởng mới có lệch hướng không.

### 2.2 Tài liệu gameplay spec chi tiết

- `COMBAT_CORE_SPEC.md`
  - Source of truth cho: turn flow, dice pipeline, focus economy, damage/guard/stagger, targeting flow, lane/reorder core rule, preview/tooltip direction ở cấp combat core.

- `ELEMENTS_STATUS_SPEC.md`
  - Source of truth cho: Physical, Fire/Burn, Ice/Freeze/Chilled, Lightning/Mark, Bleed, ailment system, status interaction và payoff rule.

- `SKILLS_PASSIVES_SPEC.md`
  - Source of truth cho: skill list, passive list, rarity/content direction, anti-synergy note, combo engines, lenticular content philosophy.

- `SKILL_GRAMMAR_SPEC.md`
  - Source of truth cho: grammar thiết kế skill, condition/scope, effect modules, source model và template thêm skill mới.

- `LOADOUT_AND_BUILD_SPEC.md`
  - Source of truth cho: 6 skill slot, 1 passive slot, basic actions, equip/swap structure, build identity và nguyên tắc loadout.

- `RELICS_AND_DICE_PROGRESSION_SPEC.md`
  - Source of truth cho: relic framework, dice customization, dice progression, unlock relation, progression role của relic/dice.
  - Bao gồm cả direction mới: combat và shop dùng **cùng logic dice edit**, với **single-die combat overlay** và **multi-die shop/loadout overlay**.

- `RUN_STRUCTURE_AND_ECONOMY_SPEC.md`
  - Source of truth cho: run flow ngoài combat, reward, shop/event structure, economy/progression ở cấp run, fail state, unlock state.

- `ENEMIES_BOSSES_ENCOUNTERS_SPEC.md`
  - Source of truth cho: enemy roles, intent framework, encounter pressure, boss philosophy, hidden boss direction.

- `UX_UI_FEEDBACK_SPEC.md`
  - Source of truth cho: readability, target highlight, preview/tooltip behavior ở cấp UX, combat HUD direction, feedback priorities.
  - Bao gồm cả interaction grammar cho **inspect / selected / Use / Confirm** của dice-edit flow và sự khác nhau giữa combat overlay với shop/loadout overlay.

### 2.3 Meta / implementation / archive

- `PROJECT_META.md`
  - File hiện tại.
  - Dùng để hiểu cấu trúc tài liệu, architecture, refactor state, guardrail và handoff rule.

- `MASTER_CONTEXT.md`
  - Bản context đầy đủ dạng archive / reference sâu.
  - Dùng khi cần tra lại lịch sử hợp nhất hoặc ngữ cảnh cũ chưa được tách đẹp vào bộ spec mới.

### 2.4 Rule đọc khi có chồng chéo

Khi cùng một vấn đề xuất hiện ở nhiều file, thứ tự ưu tiên là:

1. file spec chuyên trách của đúng mảng,
2. `GDD_VISION.md` nếu vấn đề là định hướng / pillar / anti-pillar,
3. `PROJECT_META.md` nếu vấn đề là architecture / refactor state / guardrail / cách đọc tài liệu,
4. `MASTER_CONTEXT.md` nếu cần tra lịch sử hoặc ngữ cảnh cũ.

---

## 3. Gameplay design summary có cần ở đây không?

**Không cần nhét lại vào `PROJECT_META.md` nếu đã có trong bộ tài liệu chuyên trách.**

Hiện tại gameplay design summary đã có chỗ đứng rõ ràng:

- định hướng tổng thể và core loop tổng quan đã có trong `GDD_VISION.md`,
- combat/system detail đã có trong `COMBAT_CORE_SPEC.md` và `ELEMENTS_STATUS_SPEC.md`,
- build/content đã có trong `SKILLS_PASSIVES_SPEC.md` và `LOADOUT_AND_BUILD_SPEC.md`,
- run/economy/progression đã có trong `RUN_STRUCTURE_AND_ECONOMY_SPEC.md` và `RELICS_AND_DICE_PROGRESSION_SPEC.md`,
- encounter/boss/UX đã có trong `ENEMIES_BOSSES_ENCOUNTERS_SPEC.md` và `UX_UI_FEEDBACK_SPEC.md`.

Vì vậy `PROJECT_META.md` chỉ giữ:

- orientation ngắn gọn,
- mapping file,
- implementation state,
- guardrail.

Không lặp nguyên gameplay summary thêm lần nữa để tránh:

- một rule nằm ở 2–3 nơi,
- sửa một nơi quên sửa nơi còn lại,
- AI khác nhầm `PROJECT_META.md` là source of truth cho combat rule.

---

## 4. Orientation ngắn gọn về project

Đây là project **Dice-driven Tactical Combat Roguelike**.

Nguồn cảm hứng lớn:

- **Balatro** — build engine, passive kiểu joker, consumable bẻ hướng run, endless pursuit.
- **Slay the Spire** — turn rhythm, intent readability, boss như bức tường cơ chế.
- **Persona / Expedition 33** — skill loadout rõ ràng, decision-driven combat, emphasis vào sequencing hơn spam nút.
- **D&D** — crit/fail, nhiều loại dice, exact value matters, mặt xúc xắc là identity thật của hành động.

### 4.1 Định hướng platform hiện tại

**PC trước, mobile sau.**

Combat feel, readability, sequencing clarity và hệ thống phải được chốt đủ chắc trên PC trước. Sau khi combat đủ rõ và đủ chắc, project mới tối ưu hóa để port / adapt sang mobile. Không hy sinh chiều sâu combat quá sớm chỉ để fit mobile UI.

### 4.2 Triết lý rất ngắn cần luôn nhớ

- Dice là trung tâm.
- Combat phải đọc được bằng mắt.
- Setup -> payoff là nhịp cốt lõi.
- Build identity quan trọng hơn thêm thật nhiều content nông.
- Không đắp chồng quá nhiều modifier buộc player phải tính nhẩm liên tục.

Phần giải thích đầy đủ đã có ở `GDD_VISION.md`.

---

## 5. Code architecture — current direction

### 5.1 Source of truth

Đây là rule kiến trúc phải giữ:

- `DiceSlotRig` = source of truth cho **dice math / resolved dice state**
- `SkillPlanBoard` = source of truth cho **lane planning / planning state**
- **UI không được trở thành source of truth thứ hai**

Nếu một bug liên quan tới hiển thị mà logic đang đúng, sửa ở UI layer trước. Không để patch UI làm lệch truth ở gameplay layer.

### 5.2 Các class / system quan trọng hiện tại

Các hệ thống chính hiện được xem là lõi của project:

- `TurnManager`
- `SkillPlanBoard`
- `SkillExecutor`
- `CombatActor`
- `StatusController`
- `BattlePartyManager2D`
- `DiceSlotRig`
- `DiceSpinnerGeneric`
- `RunInventoryManager`
- `PassiveSystem`
- `DamagePopupSystem`
- `CombatHUD`
- `ActorWorldUI`
- `TargetClickable2D`
- `EnemyBrainController`

### 5.3 Hướng data skill hiện tại

Project hiện đi theo hướng tách data skill thành 3 loại chính:

- `SkillDamageSO`
- `SkillBuffDebuffSO`
- `SkillPassiveSO`

Legacy pipeline đã bị loại khỏi hướng phát triển chính:

- `SkillSO_Legacy`
- `SkillConditionalOverrides`
- pipeline `SkillSO` cũ

Rule khi sửa code mới:

- không tự revive pipeline cũ,
- không lấy logic cũ làm source of truth nếu đã có spec mới,
- chỉ giữ compatibility với scene / prefab cũ khi thực sự cần.

---

## 6. Cấu trúc thư mục hiện tại

Code chính hiện nằm dưới `Assets/Scripts`.
Không coi `Assets/Scripts/Demo` là nơi chứa code chính nữa.

Cấu trúc domain hiện tại:

- `Assets/Scripts/Combat/Actors`
- `Assets/Scripts/Combat/Execution`
- `Assets/Scripts/Combat/Status`
- `Assets/Scripts/Combat/Turn`
- `Assets/Scripts/Dice`
- `Assets/Scripts/Enemies`
- `Assets/Scripts/Inventory`
- `Assets/Scripts/Skills/Basic`
- `Assets/Scripts/Skills/Buff`
- `Assets/Scripts/Skills/Damage`
- `Assets/Scripts/Skills/Debuff`
- `Assets/Scripts/Skills/Definitions`
- `Assets/Scripts/Skills/Effect`
- `Assets/Scripts/Skills/Legacy`
- `Assets/Scripts/Skills/Passive`
- `Assets/Scripts/Skills/Planning`
- `Assets/Scripts/Skills/Runtime`
- `Assets/Scripts/UI/Combat`
- `Assets/Scripts/UI/Loadout/Dice`
- `Assets/Scripts/UI/Loadout/Passive`
- `Assets/Scripts/UI/Planning`

Ghi nhớ:

- không dùng lại path cũ kiểu `Assets/Scripts/Demo/...`,
- không mặc định `Assets/Scripts/Combat/Core/...` nếu cây thư mục hiện tại đã khác,
- khi handoff file path phải ưu tiên đúng cây thư mục đang dùng trong project hiện tại.

---

## 7. Current implementation state

### 7.1 Những vùng combat core đã được xem là cập nhật theo spec mới

Các phần sau hiện được xem là đã cập nhật và không nên tiếp tục mô tả như “còn thiếu core behavior” nữa:

- Focus đầu combat = 2, turn 1 thực tế = 3
- Burn consume baseline = `+2 / stack`
- Freeze / Chilled immunity và Ice reward = `+1 Focus +3 Guard`
- Bleed tick đầu lượt bỏ qua Guard
- Mark / Lightning theo rule direct-hit + shock proc
- shock Lightning chạy tuần tự với delay `0.2s`
- `Stagger` đã được implement và hiển thị như một status thật
- Ailment direction hiện là enemy-side, enemy -> player = 100%
- reorder + lane mapping được xem là ổn ở mức nền tảng
- phase lock hiện cần được hiểu là khóa **reorder / skill assignment**, không đồng nghĩa khóa mọi dạng dice edit

Chi tiết gameplay của các rule trên phải tra ở file spec chuyên trách, không tra ở đây.

### 7.2 Hướng refactor file lớn

Refactor hiện tại đi theo hướng:

- giữ class Unity gốc để tránh vỡ reference scene / prefab,
- tách logic nặng ra utility / helper mới,
- giảm rủi ro khi chạm vào file lớn.

Các cụm đã tách:

#### `TurnManager`
- `Assets/Scripts/Combat/Turn/TurnManagerCombatUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnManagerLifecycleUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnManagerPlanningUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnManagerTargetingUtility.cs`
- `Assets/Scripts/Combat/Turn/TurnManagerViewUtility.cs`

#### `SkillExecutor`
- `Assets/Scripts/Combat/Execution/AttackPreviewCalculator.cs`
- `Assets/Scripts/Combat/Execution/SkillAttackResolutionUtility.cs`
- `Assets/Scripts/Combat/Execution/SkillTargetResolver.cs`

#### `StatusController`
- `Assets/Scripts/Combat/Status/StatusRuntimeEntries.cs`
- `Assets/Scripts/Combat/Status/StatusBuffDebuffUtility.cs`
- `Assets/Scripts/Combat/Status/StatusAilmentUtility.cs`
- `Assets/Scripts/Combat/Status/StatusStateUtility.cs`

#### `SkillPlanBoard`
- `Assets/Scripts/Skills/Planning/SkillPlanBoardStateUtility.cs`
- `Assets/Scripts/Skills/Planning/SkillPlanRuntimeUtility.cs`

#### `RunInventoryManager`
- `Assets/Scripts/Inventory/RunInventoryBindingUtility.cs`
- `Assets/Scripts/Inventory/RunInventorySetupUtility.cs`
- `Assets/Scripts/Inventory/RunInventoryLoadoutUtility.cs`

#### `EnemyBrainController`
- `Assets/Scripts/Enemies/EnemyIntentSelectionUtility.cs`
- `Assets/Scripts/Enemies/EnemyIntentPreviewUtility.cs`

#### Loadout UI - Dice
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipLayoutUtility.cs`
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipWorldSyncUtility.cs`
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipStateUtility.cs`
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipPresentationUtility.cs`
- `Assets/Scripts/UI/Loadout/Dice/DiceEquipWorldFollowUtility.cs`

#### Loadout UI - Passive
- `Assets/Scripts/UI/Loadout/Passive/PassiveEquipLayoutUtility.cs`
- `Assets/Scripts/UI/Loadout/Passive/PassiveEquipWorldSyncUtility.cs`
- `Assets/Scripts/UI/Loadout/Passive/PassiveEquipStateUtility.cs`
- `Assets/Scripts/UI/Loadout/Passive/PassiveEquipPresentationUtility.cs`

### 7.3 Những vùng đang ổn và không nên đụng nếu không cần

Các vùng sau hiện được xem là khá ổn:

- flow runtime moi `roll -> reorder neu can -> drag skill cast ngay -> End Turn -> enemy turn`
- reorder dice trong player phase
- execution order / damage order theo lane hiện tại
- lane mapping giữa pair identity và lane hiện tại
- consume rule nền tảng
- state spent / consume sau khi cast

Nếu task không nhắm đúng bug ở các vùng này, ưu tiên **không đụng**.

### 7.4 Những việc còn lại hợp lý cho bước sau

Nếu tiếp tục làm việc sau này, các hướng hợp lý là:

- tiếp tục tách file nếu còn file nào thật sự ôm quá nhiều logic,
- chuẩn hóa skill data / content layer,
- làm tooltip / runtime preview formatter sau khi skill data ổn,
- polish thêm dice feedback UI.

Nếu combat core đã ổn và file đã ở mức chấp nhận được, **không cần tách thêm chỉ vì số dòng**.

---

## 8. Những vùng còn mở / chưa final

Phần này phải giữ lại để tránh AI khác tưởng mọi thứ đã chốt hết.

Các vùng hiện vẫn mở hoặc chưa populate đầy đủ:

- regular enemy design chi tiết và full roster,
- full boss roster / stat table / encounter distribution,
- consumable / relic pool chi tiết ngoài framework cơ bản,
- tooltip / runtime preview formatter final,
- hidden boss placement cụ thể trong endless,
- skill content populate hoàn chỉnh vào toàn bộ SO runtime,
- full unlock tree và pacing cuối cùng,
- economy/drop-rate/shop-rate final,
- toàn bộ UI polish cuối cùng.

Nếu user không yêu cầu chốt các phần này, không được tự “đóng spec” thay user.

---

## 9. Guardrail khi AI / người khác sửa code

### 9.1 Nguyên tắc bắt buộc

- đừng phá những phần reorder / lane / execution đang ổn,
- execute, preview, tooltip không được nói ba kiểu nếu có sửa dice math,
- effect phải bám spec mới, không bám logic cũ nếu logic cũ lệch spec,
- ưu tiên patch nhỏ, dễ review,
- nếu refactor lớn, phải nói rõ vì sao,
- luôn nêu file nào sửa và vì sao,
- nếu có thể, đưa checklist test thủ công.

### 9.2 Khi sửa combat

- ưu tiên bảo toàn behavior đúng trước, đẹp code tính sau,
- utility mới chỉ nên tách khi thực sự giúp code dễ đọc / dễ bảo trì hơn,
- giữ `SkillPlanBoard` là source of truth cho lane planning,
- giữ `DiceSlotRig` là source of truth cho resolved dice math,
- không để UI thành source of truth thứ hai.

### 9.3 Điều không nên làm

- không đập lại lane mapping chỉ vì muốn code “sạch” hơn,
- không tự revive `SkillSO` cũ nếu pipeline mới đang là hướng chính,
- không coi toàn bộ content pool chưa chốt là final spec,
- không sửa combat core chỉ để refactor nếu không có bug hoặc yêu cầu rõ,
- không nhét gameplay summary quay lại `PROJECT_META.md` nếu đã có file chuyên trách.

---

## 10. Cách đọc và cách trả lời mong muốn khi handoff

Nếu AI khác đọc file này để tiếp tục hỗ trợ project, style mong muốn là:

- ưu tiên tiếng Việt,
- ngắn, rõ, thẳng vào vấn đề trong chat trả lời,
- nhưng khi viết tài liệu / patch note / handoff thì phải đủ ngữ cảnh,
- luôn phân biệt rõ: cái gì đã chốt, cái gì là direction, cái gì là implementation state, cái gì là open issue.

Nếu sửa code, nên trả lời theo cấu trúc:

1. nguyên nhân,
2. file nào sửa,
3. vì sao sửa như vậy,
4. checklist test.

Nếu không chắc một rule đã chốt hay chưa:

1. tra file spec chuyên trách,
2. tra `GDD_VISION.md` nếu câu hỏi là định hướng,
3. tra `PROJECT_META.md` nếu câu hỏi là architecture / guardrail / file mapping,
4. chỉ tra `MASTER_CONTEXT.md` khi cần lịch sử sâu hơn.

---

## 11. Tóm tắt cực ngắn

Nếu chỉ nhớ vài điểm thì phải nhớ:

1. Đây là project **Dice-driven Tactical Combat Roguelike**.
2. `GDD_VISION.md` giữ ý đồ cốt lõi; các file `*_SPEC.md` giữ gameplay detail; `PROJECT_META.md` giữ meta/architecture/guardrail.
3. `DiceSlotRig` là source of truth của dice math.
4. `SkillPlanBoard` là source of truth của lane planning.
5. UI không được trở thành source of truth thứ hai.
6. Đừng phá reorder / lane mapping / execution order nếu chúng đang ổn.
7. Không revive pipeline `SkillSO` cũ nếu không có yêu cầu rõ.
8. Phần gameplay summary đã có chỗ đứng rõ trong bộ file spec; không lặp lại ở meta file này.
---

## 12. Progress Update - 2026-03-22

Doan nay la progress note de handoff cho chat sau. Khong thay the cac phan tren, chi bo sung implementation state va huong di dang dung.

### 12.1 Huong hien tai da chot cho skill/passive runtime

Project dang di theo huong:

- code for engine
- data for content
- custom hooks for exceptions

Y nghia:

- engine resolve, turn flow, preview, status, passive hook van nam trong code
- content identity va config co gang dat trong SO / asset / inspector
- nhung mechanic dac thu chua generic duoc thi di qua behavior hook ro rang

### 12.2 Nhung gi da lam gan day

- da them `behaviorId` vao cac SO lien quan den skill va passive de khong con doan mechanic bang display name
- da dua mot so skill/passive vao flow inspector-driven theo behavior id thay vi string match
- da chinh inspector cua skill theo huong summary luon hien ben ngoai, tab chi chua phan config
- da xu ly passive equip UI theo huong giong dice equip UI o muc drag/reorder/layout
- da sap xep lai asset trong `Assets/Scripts/Skills/Damage` thanh cac nhom `Fire / Ice / Lightning / Bleed / Physical` de de tim va de quan ly content

### 12.3 Runtime slice da migrate

Skill dac thu da migrate truoc:

- Ignite
- Hellfire
- Ember Weapon
- Cinderbrand

Passive da hook vao runtime truoc:

- Clear Mind
- Even Resonance
- Elemental Catalyst
- Fail Forward
- Iron Stance
- Crit Escalation
- Dice Forging

### 12.4 Note quan trong ve Hellfire

Rule dang dung hien tai:

- Hellfire chi co phan reapply Burn neu target da co Burn san truoc hit
- sau khi consume Burn, moi die trong local group co Base Value = 7 se them 7 Burn
- 1 die ra 7 = 7 Burn, 2 dice ra 7 = 14 Burn, 3 dice ra 7 = 21 Burn

### 12.5 Next direction

Buoc tiep theo khong phai hardcode tung skill mai mai, nhung cung khong ep 100% moi thu vao inspector bang mot dong field roi rac.

Huong tiep theo nen la:

- giu `behaviorId` cho exception / mechanic dac thu
- mo rong dan generic effect modules va condition modules
- de phan lon skill/passive thuong co the author bang data
- chi de so it mechanic hiem / exotic dung custom code hook

Thu tu uu tien gan nhat cho prototype:

1. lam `consumable / relic pool` khung chay duoc
2. playtest them mot vong combat + build loop sau khi co consumable
3. roi moi lam `tooltip / preview formatter` de tranh sua lai nhieu lan khi grammar va progression con dang doi

### 12.6 Technical debt hien tai

Nhung no ky thuat con lai sau dot chuan hoa skill/guard:

- chua co pass verify day du chuoi `inspector -> preview -> execution` cho cac edge case co `Fail`, `minimum 1`, `fixed output + Added Value`, `split-role`, va cac branch element dac thu
- mot so skill van resolve qua special runtime path thay vi grammar generic hoan toan, tieu bieu la cac branch Fire nhu `Hellfire`, `Cauterize` va fire module lien quan
- con asset / field legacy de giu compatibility du lieu cu; Guard multiplier da bi day ve legacy mode nhung chua xoa cung khoi serialization

Noi ngan gon:

- prototype co the di tiep
- nhung chua phai trang thai polish / clean final

### 12.7 Verify status

- chua co CLI build verify on dinh vi may hien tai khong co .NET SDK / MSBuild tren PATH
- can test trong Unity sau moi dot thay doi runtime lon

---

## Runtime Update Note (2026-04)

- Passive loadout runtime hien tai da giam tu `3 passive slot` xuong `1 passive slot`.
- Combat runtime hien tai dang dung grammar dieu khien moi:
  - `roll dice`
  - `reorder neu can`
  - `drag skill icon tu skill slot vao enemy de cast truc tiep`
  - `bam End Turn de sang enemy turn`
- Flow cu theo planning/continue khong con la cach dieu khien chinh can mo ta trong docs handoff nua.
- Roll dice hien tai van giu nguyen.
- Consume dice kieu "bien mat that" chua duoc dua vao runtime; tam thoi dice da dung van duoc bieu dien bang state mo `50%`.
- Source of truth cho huong thay doi nay nam o [COMBAT_CHANGES_2026.md](/C:/Users/huyan/Desktop/GameProject/Project_build_synergy/Docs/Detail/COMBAT_CHANGES_2026.md).

## Runtime Update Note (2026-04-05)

### Dice progression runtime state

- `DiceFace` hien tai da co them field `enchant`.
- `DiceSpinnerGeneric` hien tai da co them `wholeDieTag`.
- `DiceSpinnerGeneric` hien tai co 2 lop doc state rieng:
  - `face enchant` o cap mat dice
  - `whole-die tag` o cap ca vien dice

### File moi da them de giam rui ro vo file goc

- `Assets/Scripts/Dice/DiceFaceEnchantKind.cs`
- `Assets/Scripts/Dice/DiceFaceEnchantUtility.cs`
- `Assets/Scripts/Dice/DiceWholeDieTag.cs`
- `Assets/Scripts/Dice/DiceWholeDieTagUtility.cs`
- `Assets/Scripts/Dice/DiceCombatEnchantRuntimeUtility.cs`

Huong sua dot nay van giu nguyen nguyen tac:

- utility / helper moi chua phan lon logic
- file goc chi nhan hook mong
- neu runtime nay co van de, co the go / refactor lai de hon viec da nhung het vao `TurnManager` hoac `DiceSpinnerGeneric`

### Face enchant runtime da co

Hien tai runtime da duoc doi sang pool enchant moi:

- `Value +N` -> cong `+3 Added Value`
- `Guard Boost` -> `+3 Guard`
- `Gold Proc` -> `+5 Gold`
- `Fire` -> ap `2 Burn` len `1 enemy random` con song
- `Bleed` -> ap `2 Bleed` len `1 enemy random` con song
- `Lightning` -> mat nay duoc doc la ca `Crit` va `Fail` cho condition skill / passive, nhung khong an crit bonus va khong chiu fail penalty
- `Ice` -> mat stone-like, luon cho `+5 Added Value` va khong con duoc doc nhu mat so binh thuong

Rule runtime dang dung:

- enchant trigger ngay khi face roll ra
- enchant doc lap voi skill
- face khong co skill van trigger enchant
- face co skill thi enchant trigger truoc, khong cho skill resolve xong moi chay
- `Fire / Bleed` target random trong `all alive enemies`
- `Bomb / Snipe / Mark Proc / Ice Proc` da bi loai khoi runtime hien tai

### Whole-die runtime da co

Whole-die layer hien tai moi khoa `None / Patina`.

`Patina` da duoc noi vao runtime theo huong:

- chi xet khi `thang combat`
- dice phai da duoc dung it nhat `1 lan` trong combat
- dice phai co `1 mat thap nhat duy nhat`
- neu hop le -> mat thap nhat do nhan `+1 Base vinh vien`
- neu co nhieu mat dong thap nhat hoac tat ca mat bang nhau -> khong tang

### Hook da noi vao flow hien tai

- `TurnManager`:
  - resolve on-roll face enchant ngay sau khi dice roll xong
  - khong con resolve assigned face enchant sau skill
  - apply `Patina` khi combat da duoc xem la win
- `DiceSlotRig`:
  - da tinh Added Value tu `Value +N` va `Ice`
  - giu source of truth cho crit/fail bonus, fail penalty, va non-numeric face state
- `DiceSpinnerGeneric`:
  - co debug text rieng cho mat hien tai qua `enchantText`
  - khong con hien `Bomb / Snipe` trong enum hien tai

### Dieu can verify trong Unity

- `Fire / Bleed / Guard Boost / Gold Proc` co proc ngay sau roll, ke ca khi mat khong gan skill
- `Lightning` co duoc doc dung cho crit/fail condition nhung khong doi damage theo crit/fail bonus
- `Ice` co cho `+5 Added Value` va dong thoi bi loai khoi parity / exact / highest / lowest numeric checks
- `Gold Proc` co cong dung vao `RunInventoryManager` trong scene runtime that hay khong
- `Patina` co chi proc khi win combat va buff dung mat duy nhat thap nhat hay khong

### Guardrail cho dot nay

- chua co CLI build verify on dinh; moi thay doi runtime lon van phai test trong Unity
- neu can rollback, uu tien go hook o `TurnManager` va tach khoi `DiceCombatEnchantRuntimeUtility` truoc
- chua nen gop utility moi vao file lon neu chua verify gameplay / scene runtime on dinh

### Debug/test roll hook tam thoi

De test nhanh trong scene, `DiceSlotRig` hien tai co them debug keyboard mode:

- checkbox `enableDebugRollHotkeys`
  - bat -> `Space = roll all`, `A / S / D = reroll slot 1 / 2 / 3`
  - tat -> quay ve flow binh thuong
- checkbox `allowDebugRerollThisTurn`
  - bat -> cho phep reroll trong cung 1 turn
  - tat -> van bi khoa sau lan roll dau tien

Rule quan trong:

- debug reroll doc theo `slot hien tai`, khong doc theo identity cua vien dice
- neu swap dice giua cac slot thi `A / S / D` se reroll vien dang nam o slot do tai thoi diem bam phim
- hook nay chi de test, khong phai gameplay final

### Consumable implementation state hien tai

Tinh den dot chat nay, project da co:

- spec consumable / Zodiac / Seals / Runes
- runtime face enchant moi
- runtime whole-die `Patina`
- dice-edit sandbox de test thao tac chon mat / inspect

Nhung van chua co:

- `consumable data` that trong code
- runtime cho `3 shared consumable slot`
- `use flow` day du de bam va dung consumable
- link giua `selected logical face` va Zodiac consumable cu the

Vi vay neu tiep tuc sau dot nay, batch code hop ly nhat la:

1. `consumable data`
2. `consumable slot`
3. `consumable use flow`

Khong nen mo ta project nhu da co he consumable that; hien tai moi chi co spec va mot phan nen dice/enchant.

## Runtime Update Note (2026-04-10)

### Row runtime audit theo code hien tai

Dot nay can note ro: `row` khong con o muc chi co spec.
Theo code hien tai, `row` da co runtime that o muc combat core va target resolve.

Nhung diem da co trong code:

- `CombatActor` da co `RowTag.Front / Back` va moi actor giu `row` rieng.
- `BattlePartyManager2D` da duoc cap nhat de:
  - spawn actor theo `row`,
  - giu `playerRow`,
  - filter alive enemy theo `frontOnly`,
  - layout world theo `row` qua depth / scale.
- `SkillTargetRule` da co `RowEnemies / RowAllies`.
- `TurnManagerTargetingUtility` da co rule chan `melee strike` vao `back row` neu ben dich van con `front row`.
- `TurnManagerCombatUtility` da co resolve cho:
  - `RowEnemies`,
  - `RowAllies`,
  - `AllEnemies`,
  - `AllAllies`,
  - filter target theo `clicked.row` hoac `caster.row`.
- `TurnManager` da noi `aoeTargets` vao flow cast cua ca player va enemy cho cac skill target theo row.
- `SkillExecutor` da xu ly duoc attack va buff/debuff co target rule theo row.
- `DiceCombatEnchantRuntimeUtility` da resolve `Bomb` theo `Front row` va `Snipe` theo `Back row`.

Noi ngan gon:

- `row core logic` hien tai co the xem la da noi vao runtime that.
- `row` khong con la phan "chua lam".

### BattlePartyManager2D state can ghi nho khi handoff

`BattlePartyManager2D` hien tai khong chi la spawn helper.
No da tro thanh mot phan cua runtime row/formation:

- la noi source roster song de resolve target theo row,
- cap `GetAliveEnemies(frontOnly: true/false)`,
- cap `GetAliveAllies(includePlayer: true)`,
- la diem tu do `TurnManagerTargetingUtility`, `TurnManagerCombatUtility` va `DiceCombatEnchantRuntimeUtility` doc formation hien tai.

Neu sua row runtime, phai tinh ca `BattlePartyManager2D`, khong duoc chi nhin `TurnManager`.

### Nhung gi row con thieu hoac chua thay dau vet generic day du

Theo code hien tai, nhung phan sau chua nen goi la da xong het:

- chua thay preview / intent text noi ro muc tieu dang nham `Front Row` hay `Back Row`;
- chua thay UI surfacing ro actor nao dang o `Front` hay `Back`;
- chua thay mot lop generic rieng cho `Cross-row / All-row exception`;
- chua thay runtime content hook ro rang cho cac case:
  - check player dang o `Front Row` hay `Back Row`,
  - move / swap row trong combat,
  - payoff doc target row nhu mot condition axis authoring chung.

Vi vay cach mo ta dung nhat luc nay la:

- `row combat core / target legality / row target resolve`: da co
- `row presentation / intent surfacing / content axis day du`: chua day du

### Guardrail sau khi audit code

- neu task lien quan `row`, dau tien phai phan biet dang sua `combat legality` hay `UI/presentation/content`;
- khong nen tiep tuc mo ta `row` nhu mot phan chua implement o lop nen;
- neu co bug row, phai test ca 4 diem:
  - click target hop le / khong hop le,
  - row-target skill cua player,
  - row-target move cua enemy,
  - `Bomb / Snipe` va cac effect phu doc formation.
