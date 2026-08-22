# BACKLOG — Last Swing

Prioritized trough. P0 = do now / blocks; P1 = worth doing; P2 = nice-to-have. Newest concerns
first within each band. Structural items come from the 2026-08-22 full review (see
[STRUCTURE.md](../STRUCTURE.md#structural-debt)).

## P0

_None._

## P1

- **Split `HealthBar.cs` into controller + `HealthBarView`.** 726 lines fusing the
  poll/arm/linger state machine with the whole Unity view (canvas construction, `Draw`, projection,
  camera). Extract a plain `HealthBarView` class owning the canvas fields + `EnsureUi`/`Draw`/
  `Reposition`; keep `HealthBar : MonoBehaviour` as lifecycle/controller. **Trigger:** do it the
  next time this area is edited — before the rocks-after-grid WIP grows the file, or before adopting
  `GameWorldUIScreen`. Not a standalone change on shipped code (moves lifecycle-sensitive Unity code
  with no test seam). Re-run [`TESTING.md`](../TESTING.md) after.

## P2

- **De-leak `Target.DestructibleSource`.** Replace the exposed `DestructibleView` with a semantic
  property (e.g. `HasBecomeDestructed`) and have the view take a `forceEmpty` render flag instead of
  `HealthBar` synthesizing a mutable `Target`. Do it alongside the P1 split; the rock/ore kill-frame
  path is subtle and under WIP, so re-run the killing-blow checklist. (Reviewers split on whether
  this is a real leak — low urgency.)
- **Fold `GameFonts.Search`'s 3 name-search loops** into a `FindByName<T>` helper. Blocked on the
  cross-mod sync: `GameFonts.cs` is a verbatim copy shared with Chest Labels / Plant Peek, so the
  fix belongs in the workspace canonical + all copies, not here alone.

## Open questions (from the decompile, not yet answered by play)

Carried from [README.md → Still open](../README.md#still-open):

1. **Does damage regenerate overnight?** `DamagePersistence.DayRegeneratedLast` exists; no writer
   found yet. The bar reads live so it stays correct either way, but the mod page should say so if
   it does. Find the writer.
2. **Do ethereal tools route through `SwingToolView`?** `EtherealAxesToolView` /
   `EtherealPickaxesToolView` are separate types; if they don't share the swing view they need a
   second target source (Better Ethereal Tools is popular).
3. **Which gate binds a full tree** — `healthTree + 1` or the `DamageTakenRequirement`? The code
   takes `max` of both (correct either way); knowing which binds would let the comment stop hedging.
