# SPEC_CLEANUP_CHANGELOG.md

> Tóm tắt các thay đổi trong bản source package hiện tại.

## Đã dọn

- Loại khỏi package: `COMBAT_CHANGES_2026.md`, `COMBAT_UI_PREVIEW_FINAL_ORDERED_SPEC.md`, `VALIDATION_REPORT.md`.
- Thêm `CODEX_UPDATE_GUARDRAILS.md` để Codex/dev biết rule nào không được hồi sinh.
- Thêm `TODO_IMPLEMENTATION_STATUS.md` để phân biệt design direction với phần chưa implement / chưa final.
- Sửa `PROJECT_META.md` thành file map sạch, không trỏ tới file không tồn tại.
- Combat flow hiện tại: `Player Phase → End Phase → Enemy Phase`.
- Dice tự roll đầu `Player Phase`.
- Player reorder dice rồi click/drag skill vào target để cast ngay.
- Dice used visual: hạ Y + đổi background, không biến mất.
- Resource thống nhất: `AP`.
- Relic là collection nhặt nhiều, không còn 1 slot duy nhất.
- Consumable groups: `Fate / Seal / Rune`.
- Access Type: `Melee / Range`.
- Element payoff: skill-text driven.
- Blue value: chỉ số xanh mới nhận Added Value / modifier hợp lệ.

## Source chính sau cleanup

- `GDD_VISION.md`: hướng đi / pillar / anti-pillar.
- `PROJECT_META.md`: map tài liệu / source ownership.
- `CODEX_UPDATE_GUARDRAILS.md`: rule cập nhật / rule cũ bị cấm hồi sinh.
- `TODO_IMPLEMENTATION_STATUS.md`: phần đã chốt design và phần chưa làm.
- Các file `*_SPEC.md`: source chuyên trách cho từng hệ.

## Claude Review Patch

- Sửa conflict Relic: Relic hiện là passive-effect collection; consumable là Fate / Seal / Rune.
- Thêm clarification: enemy debuff có thể deterministic/100% vì đã telegraph qua intent; player status và enemy dice debuff là 2 hệ riêng.
- Thêm playtest risk: dice reorder depth phải được tạo bởi skill/relic/enemy content; Burn/Mark/Basic Attack là risk cần test, không tự động sửa rule.
- Thêm guardrail chống over-documentation: sau cleanup contradiction, ưu tiên prototype/playtest thay vì mở rộng spec.
- Thêm note: exact tuning numbers có thể nằm trong game data / implementation nếu chưa locked trong docs.
