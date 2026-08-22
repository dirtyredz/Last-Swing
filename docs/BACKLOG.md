# BACKLOG — Last Swing

Prioritized trough. P0 = do now / blocks; P1 = worth doing; P2 = nice-to-have. Newest concerns
first within each band. Structural items come from the 2026-08-22 full review (see
[STRUCTURE.md](../STRUCTURE.md#structural-debt)).

## P0

_None._

## P1

_None open._ (The `HealthBar` controller/view split — the standing P1 — is done; see Done below.)

## P2

- **De-leak `Target.DestructibleSource`.** Replace the exposed `DestructibleView` with a semantic
  property (e.g. `HasBecomeDestructed`) and have the view take a `forceEmpty` render flag instead of
  `HealthBar` synthesizing a mutable `Target`. **Held:** reviewers split on whether it's a real
  leak, and it touches the rock/ore kill-frame path — subtle, under WIP, and unverifiable without a
  game run. Do it when that WIP is next verified in game, and re-run the killing-blow checklist.
- **Fold `GameFonts.Search`'s 3 name-search loops** into a `FindByName<T>` helper. Blocked on the
  cross-mod sync: `GameFonts.cs` is a verbatim copy shared with Chest Labels / Plant Peek, so the
  fix belongs in the workspace canonical + all copies, not here alone.

## Done

- ✅ **Split `HealthBar` into controller + `HealthBarView`** (2026-08-22). 726-line file → controller
  `HealthBar.cs` (377) + view `HealthBarView.cs` (408). Behaviour-preserving, build verified.
- ✅ **`PlaceSegment` helper** — deduped the segment `RectTransform` setup in the draw path.

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
