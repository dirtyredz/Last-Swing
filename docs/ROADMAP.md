# ROADMAP — Last Swing

The mod is **shipped and stable** (v1.0.0 on Nexus). There is no active development push; work is
opportunistic. Phases below are "if/when", not scheduled.

## Now — release the verified WIP

- ✅ **Rocks-after-grid killing-blow feature verified in game** (owner, 2026-08-22).
- **Decide on a 1.0.1 release** of the verified feature: bump `<Version>` in the csproj, update
  CHANGELOG, `pack.ps1`, and publish via the nexus-publish skill. (Optional: fold in the P2
  `DestructibleSource` de-leak first — see [BACKLOG](BACKLOG.md).)

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
