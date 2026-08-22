# ARCHITECTURE — Last Swing

How the system works. The full narrative — with decompile findings and the reasoning behind each
choice — lives in [README.md](../README.md); this is the short map. Code shape is in
[STRUCTURE.md](../STRUCTURE.md).

## The loop

`HealthBar` is a `MonoBehaviour` added by `Plugin` at `Awake`. Each frame:

1. **Gate** (`Update` → `IsSuppressed`, every frame): if the master switch is off, or the game hid
   its grid cursor (menu / cutscene / full-screen UI), `Clear()` immediately.
2. **Poll** (every 50 ms): `SwingTarget.Find` → the object the equipped tool is aimed at →
   `DamageReader.Read` → a normalized `Target { Anchor, Damage, Threshold, Kind }`. Update the
   arming state, then `Draw`.
3. **Reposition** (every frame): `WorldToScreenPoint(anchor + WorldHeight)` onto a private
   screen-space overlay canvas; handle linger/fade timing.

The poll is throttled (state changes slowly); repositioning is per-frame (the camera moves
continuously, and a stale screen position shows as the bar swimming behind the tree).

## Key external interfaces (all read-only, no Harmony)

- **`SwingToolView.Target` / `TargetIsInRange`** — the game recomputes the swing target every frame
  and exposes it. This is why the mod is small: no raycast, no hover system, no patch. It's the same
  object the game draws its grid cursor on, so the mod can't disagree with the game.
- **`DamagePersistence`** (public, GUID-keyed) — damage already taken, read off the component the
  mod already holds. Never via `FindOrCreate` (writes the save) — see [GOTCHAS](GOTCHAS.md).
- **Four damageable component types** with no shared interface, each keeping its maximum somewhere
  different — `DamageReader` normalizes them. See the table in [README](../README.md#what-it-covers).
- **`PlayerToolInteractor.AllowShowingGridCursor` + `Cutscene.IsInCutscene`** — the menu/cutscene
  gate. The swing view does *not* gate itself; see [GOTCHAS](GOTCHAS.md).

## Rendering

A private `ScreenSpaceOverlay` canvas (sortingOrder 500, matching sibling mods so they stack
predictably), positioned per-frame — the approach Chest Labels established and Plant Peek reuses.
Sprites are generated at runtime (`BarSprite`, signed-distance rounded rects, 9-sliced, white +
tinted), so the archive ships only a DLL. Font/colours come from the game's own assets via
`GameFonts` / `GamePalette`.

**Not yet using the game's own world-space UI** (`GameWorldUIScreen.InstantiateWidget`), which would
be the better long-term home: its widgets only re-project on `EventBus.OnGameCameraPositionChanged`,
and whether that fires per-frame while walking is unverified. Getting it wrong means a lagging bar —
the mod's most-watched pixel. Confirm the event frequency in a run, then switch. See
[DECISIONS](DECISIONS.md).

## Release pipeline

Version single-sourced from the csproj `<Version>` (see [STRUCTURE.md](../STRUCTURE.md#build--release)).
`pack.ps1` builds `dist/LastSwing-<version>.zip` in Nexus layout. Publishing to the Nexus page uses
the workspace **nexus-publish** skill. Full chain: workspace `../../docs/ARCHITECTURE.md`.
