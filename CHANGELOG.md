# Changelog

## 0.2.0 — 2026-08-04

**First build run in game, and it works** — the bar appears on the object being swung at and
tracks damage as it is chopped. 0.1.0 was never run. Save-safety and the full-tree off-by-one
are still unconfirmed; see the README.

- **The bar now stays hidden until a hit lands on the target**, and this is the default
  (`RequireHitFirst`). Aiming at a half-chopped tree shows nothing — arming compares damage
  against a baseline taken when the object was first aimed at, rather than testing
  `DamageValue > 0`, which would light up everything already damaged. Arming survives the
  linger window but not a full hide.
- **`ShowText`** turns all text off, leaving the bar on its own. `ShowSwingsLeft` and
  `WarnWeakTool` are now sub-toggles of it.
- **The bar runs yellow → orange → red** as it depletes, at `OrangeBelow` (0.6) and `RedBelow`
  (0.3) of health remaining, both configurable. Hard steps rather than a gradient. The whole
  remaining run is one colour, not shaded per notch.
- Too-weak targets drop out of the ramp and go pale, which is what keeps that state readable
  when text is off.
- Replaces `OnlyAfterFirstHit` (never released).

## 0.1.0 — 2026-08-04

First build. Compiles and deploys; **not yet verified in game.**

- Segmented health bar above the tree, stump or rock the equipped swing tool is aimed at,
  driven by `SwingToolView.Target` rather than a raycast of our own.
- Covers all four damageable component types: `DestructibleView`, `ChopTreeGridComponent`,
  `DestructableTreeGridComponent`, `ChopStumpGridComponent`.
- Full trees use `max(healthTree + 1, ceil(DamageTakenRequirement.DamageAmount))` as the
  threshold. `ChopTreeGridComponent.Chop()` tests `Damage > health` where the other three use
  `IsDead`'s `>=`, and the tree also has to satisfy the grow-stage requirement before it falls.
- "3 swings" line, computed from the equipped tool's actual damage.
- "tool too weak" line when `InteractionToolDamageRequired` exceeds the tool's damage — before
  the swing, rather than after it like the game's own shout.
- Read-only. No `FindOrCreate` anywhere, so no damage records are seeded into the save.
- Above 12 segments the bar becomes a continuous fill.
- Display settings are re-read every draw, so config edits apply live.
