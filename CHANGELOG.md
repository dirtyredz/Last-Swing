# Changelog

## 1.0.1 — 2026-08-04

**Fixes the bar staying on top of the pause menu and other full-screen UI.**

1.0.0 assumed the bar would gate itself, on the reasoning that `GrabbedItemView` reacts to the
`"PlayerToolInteractor"` blocker when menus open. It does react — by calling `HideUI()` on the
game's own tool UI — but it never clears `isEquipped`, so `ProcessEquippedUpdate` keeps running,
`SwingToolView.Target` stays resolved behind the menu, and the bar had no reason to disappear.
The assumption was never tested.

- Every frame, the bar now hides when `PlayerToolInteractor.AllowShowingGridCursor` is false or
  `Cutscene.IsInCutscene` is true. That property is the same `Blocker` instance
  `GrabbedItemView` reads, so the bar hides exactly when the game hides its own grid cursor —
  no list of screens to keep current.
- It hides immediately rather than fading over `LingerSeconds`. A menu opening should take the
  bar with it, not leave it dissolving over the UI.
- Logs once the first time each reason hides it, so "why is nothing showing?" has an answer in
  the log instead of requiring a debug build.

Added the mod page banner and thumbnail. **The banner misspells the game as "Moonlignt Peaks"**
and needs regenerating before the page goes live; the thumbnail is correct.

## 1.0.0 — 2026-08-04

First release. No functional change from 0.2.0 — this is that build, verified in game and
versioned for publication.

- Repo layout flattened: `src/*.cs` with `Directory.Build.props` at the root.
- `NEXUS.md` added with the mod page copy and the screenshot list.

**Known unverified at release**, both listed in the README and reproducible from `TESTING.md`:

- The save-diff test (§6) has not been run. The save-safe claim rests on a source audit — no
  `FindOrCreate` on any path — rather than on a before/after comparison.
- The full-tree off-by-one (§3) has not been observed directly. `ChopTreeGridComponent` dies on
  `Damage > health` where the other three types use `>=`, and the threshold takes the max of
  that and the grow-stage `DamageTakenRequirement`. Correct by construction, unconfirmed by
  play.

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
