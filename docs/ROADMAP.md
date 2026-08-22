# ROADMAP — Last Swing

The mod is **shipped and stable** (v1.0.1 on Nexus). There is no active development push; work is
opportunistic. Phases below are "if/when", not scheduled.

## Now — nothing outstanding

- ✅ **Rocks-after-grid killing-blow feature verified in game** (owner, 2026-08-22).
- ✅ **Released as v1.0.1** (2026-08-22) — version bump + CHANGELOG + pack + Nexus publish. The P2
  `DestructibleSource` de-leak was left out (optional polish — see [BACKLOG](BACKLOG.md)).

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
