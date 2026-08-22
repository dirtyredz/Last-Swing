# ROADMAP — Last Swing

The mod is **shipped and stable** (v1.0.0 on Nexus). There is no active development push; work is
opportunistic. Phases below are "if/when", not scheduled.

## Now — stabilize the WIP

- **Finish + verify the rocks-after-grid killing-blow feature** (committed WIP on top of 1.0.0).
  Re-run the [`TESTING.md`](../TESTING.md) killing-blow checklist for rocks/ore, then decide on a
  1.0.1 release. Do **not** publish until verified in game.

## Next — structural

- ✅ **Split `HealthBar` into controller + `HealthBarView`** — done 2026-08-22.
- **De-leak `Target.DestructibleSource`** (P2) — do it when the WIP kill-frame path is next verified
  in game; unverifiable without a run. See [BACKLOG](BACKLOG.md).

## Later — investigations that could open features

- Confirm `EventBus.OnGameCameraPositionChanged` frequency → possibly migrate to `GameWorldUIScreen`
  (ADR-003).
- Determine whether ethereal tools route through `SwingToolView` → possible ethereal-tool support.
- Find the writer (if any) of `DamagePersistence.DayRegeneratedLast` → document overnight
  regeneration on the mod page if it exists.
