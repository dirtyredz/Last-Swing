# Changelog

## 1.0.0 — 2026-08-04

First release. **The `TESTING.md` checklist has been worked through in game and passes**,
including the save diff and the full-tree hit count.

A health bar above the tree, stump or rock you are swinging at, driven by
`SwingToolView.Target` — the object the game is already drawing its grid cursor on. No raycast,
no hover system, no Harmony patch.

- One notch per hit, emptying left to right, running yellow → orange → red at `OrangeBelow`
  (0.6) and `RedBelow` (0.3) of health remaining. Hard steps, not a gradient, and the whole
  remaining run is one colour rather than shaded per notch.
- Hidden until a hit lands on the target, however damaged it already is. Arming compares damage
  against a baseline taken when the object was first aimed at, rather than testing
  `DamageValue > 0` — which would light up every part-chopped tree on the farm.
- "3 swings", computed from the equipped tool's real damage rather than raw hit points.
- "tool too weak" *before* the swing, where the game only shouts after the energy is spent.
- `ShowText` turns all text off, leaving the bar alone. Too-weak targets drop out of the colour
  ramp and go pale, which is what keeps that state readable with text off.
- Covers all four damageable component types: `DestructibleView`, `ChopTreeGridComponent`,
  `DestructableTreeGridComponent`, `ChopStumpGridComponent`. Full trees use
  `max(healthTree + 1, ceil(DamageTakenRequirement.DamageAmount))`, because
  `ChopTreeGridComponent.Chop()` tests `Damage > health` where the other three use `IsDead`'s
  `>=`, and the tree must also satisfy the grow-stage requirement before it falls.
- Hides immediately for menus, pause and cutscenes, checked every frame against
  `PlayerToolInteractor.AllowShowingGridCursor` and `Cutscene.IsInCutscene`. That property
  reads the same `Blocker` instance `GrabbedItemView` uses, so the bar hides exactly when the
  game hides its own grid cursor. **This was the one real bug found in testing** — the earlier
  assumption that the swing view would gate itself was wrong, and is written up in the README.
- Logs once per hide reason, so "why is nothing showing?" is answerable from the log.
- Above 12 hit points the bar becomes a continuous fill rather than a row of slivers.
- Read-only. No `FindOrCreate` on any path, so no damage records are seeded into the save for
  objects the tool merely points at — confirmed by save diff, not just by reading the source.
- Display settings are re-read every draw, so config edits and Mod Menu changes apply live.

Ships with the mod page banner and thumbnail in `screenshots/`, and the page copy in
`NEXUS.md`.

---

*0.1.0 and 0.2.0 were development builds on the same day and never left this machine. Their
notes are folded into the 1.0.0 entry above rather than kept as a release history nobody
could have installed.*
