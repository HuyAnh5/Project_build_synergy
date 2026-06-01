# Face Enchant Runtime And Popups

This document records the current runtime rules for dice face enchants and their combat popup timing.

## Core Rules

- A face enchant only triggers when that face is actually selected to pay for a skill/action.
- Consumables that reroll, rotate, or edit a face do not trigger break effects by themselves.
- Skipping/end turn does not break a face.
- `Gum` has no runtime popup.
- Broken faces cannot be used for payment and show as `X`.
- Runtime conditions still read true Base Value unless a rule explicitly says otherwise.
- Added Value is separate from Base Value and output base.

## Value Layers

- `Base Value`: the real rolled number on the face.
- `Output Base`: the base output used by the action after special rewrite such as `Double`.
- `Added Value`: bonus value added on top of output base.
- Conditions read `Base Value`.
- Basic Attack uses its fixed damage plus total Added Value. It does not treat `Double`'s base rewrite as Added Value.

Example:

```text
Face 6 with Double rolls Crit.
Double output base = 12.
Crit Added Value = floor(12 * 0.3) = +3.
Basic Attack = 4 + 3 = 7 damage.
```

## Popup Timing

Pre-skill enchants resolve before the dice cast/throw animation starts.

The order is:

1. Self enchant popup stage.
2. Wait 0.25s if any self popup appeared.
3. Relay popup stage.
4. Wait 0.25s if any Relay popup appeared.
5. Skill/dice animation begins.
6. Skill payload resolves.
7. Post-skill enchants resolve.

This means if a die has `Power` and also receives `Relay`, the `Power` popup appears on that die first, then the Relay `+2` popup appears on that same die, then the dice animation starts.

## Enchant Rules

### Power

- Timing: pre-skill self stage.
- Effect: +2 Added Value to the selected die/action.
- Popup: `Power` on the enchanted die.
- If it also receives Relay, `Power` appears first, then `+2`.

### Guard

- Timing: pre-skill self stage.
- Effect: gain Guard equal to this face's resolved value.
- Popup: `Guard` and the Guard amount on the enchanted die.

### Charge

- Timing: pre-skill self stage.
- Effect: gain +1 AP.
- Popup: `Charge` and `+1 AP` on the enchanted die.

### Gold

- Timing: pre-skill self stage.
- Effect: mark this face for bonus Gold after combat victory.
- Popup: `Gold` and `+Gold` on the enchanted die.

### Gum

- Timing: passive roll weighting.
- Effect: makes the opposite logical face easier to roll.
- Popup: none.

### Relay

- Timing: pre-skill Relay stage, after all self enchants.
- Source: the die with `Relay`.
- Target: the die immediately to the right in the current dice rig when Relay resolves.
- Effect: target die receives +2 phase value until the end of the player phase/turn.
- The +2 buff is stored on the target `DiceSpinnerGeneric`, so it stays with that physical die if later UI/order changes move it.
- A different die that was not buffed does not gain this +2 just because it moves into that lane later.
- Popup on source die: `Relay`.
- Popup on target die: `+2`.
- If no valid target exists, popup on source die: `Relay` and `No target`.

### Double

- Timing: pre-skill self stage.
- Effect: this face's output base is doubled for the committed action.
- It does not add the doubled difference as Added Value.
- Crit bonus uses the doubled output base.
- The face breaks after the committed use.
- Popup: `Double` only.
- Hover/consume preview blinks the current face number as its doubled output, for example `6` -> `12`.

### Repeat

- Timing: pre-skill self stage and post-first-payload repeat stage.
- Effect: repeat the skill payload once without paying cost again.
- The face breaks after the committed use.
- Popup 1: `Repeat` before the first skill payload.
- Popup 2: `Again` before the repeated payload.
- No third Repeat popup.

### Reload

- Timing: post-skill.
- Effect: after the skill payload resolves, break the committed face, reroll that die, and restore that die to available use this turn.
- The broken face is the face index used at commit time, not the new face rolled after Reload.
- Popup: `Reload` once, immediately before the reroll begins.
- After the reroll completes, planning UI, skill castability, previews, and dice visual state refresh.

### Heavy

- Timing: payment and pre-skill self stage.
- Effect: contributes 2 dice toward dice-slot cost.
- The face breaks after the committed use.
- Popup: `Heavy` and `+1 dice` on the enchanted die.

### Echo

- Timing: payment/effective enchant lookup.
- Effect: copies a valid enchant from the die to the left for this committed use.
- Echo can copy: Power, Guard, Charge, Gold, Relay, Double, Repeat, Reload, Heavy, Stone.
- Echo cannot copy Gum, Echo, None, or invalid/broken source faces.
- The Echo face breaks after the committed use.
- If Echo cannot copy anything, popup after skill: `Echo` and `No copy`.

### Stone

- Timing: pre-skill self stage.
- Effect: non-numeric face. Adds +5 Added Value on use.
- Conditions do not read it as a normal number.
- If it receives Relay, Stone applies +5 first, then Relay adds +2.
- Popup: `Stone` and `+5 Value` on the enchanted die.

## Break Rules

The following break after committed use:

- Double
- Repeat
- Reload
- Heavy
- Echo

Break uses the committed face index captured at payment time. This prevents Reload from rerolling into a new face and then breaking the new face by mistake.

## Preview Rules

- Target hover preview uses the same committed payment plan as real execution.
- Preview includes Power, Stone, Double, and Relay value changes.
- Relay preview applies +2 to the actual target die instance in the committed payment plan.
- Basic Attack preview uses fixed base damage plus Added Value only.
- Double preview blinks the face text to the doubled output base.

