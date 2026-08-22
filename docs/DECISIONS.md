# DECISIONS — Last Swing

Design/architecture decisions and their rationale, newest first. The long-form reasoning is in
[README.md](../README.md); this is the index.

## ADR-006 — Keep controller and view together for now (2026-08-22)
The full review flagged `HealthBar` fusing its state machine with its Unity view (P1). Decision:
**backlog, don't split standalone.** It moves lifecycle-sensitive Unity code with no test seam on a
shipped mod; the payoff comes when the area is next changed. See [BACKLOG](BACKLOG.md).

## ADR-005 — Version single-sourced from the csproj
`[BepInPlugin]` reads a compile-time `ModBuildInfo.Version` generated from `<Version>`, so the
plugin version can never drift from the archive name. Never hardcode it in `Plugin.cs`.

## ADR-004 — No Harmony patch; read the game's own swing target
`SwingToolView.Target` is public and recomputed every frame. Reading it (vs. building a hover
system + patching) is why this mod is a fraction of the size of Chest Labels / Plant Peek, and why
it can never disagree with the game about what's targeted. **Rejected:** raycast/hover from scratch.

## ADR-003 — `WorldToScreenPoint` on a private canvas, not `GameWorldUIScreen`
The game's world-UI system is the better long-term home but re-projects only on
`OnGameCameraPositionChanged`, whose per-frame-ness is unverified. A lagging bar is the worst
failure for this mod, so the proven per-frame `WorldToScreenPoint` approach ships first. Revisit
after confirming the event frequency in play.

## ADR-002 — A yellow→orange→red colour ramp, departing from the palette rule
`10-visual-integration.md` says mod UI should use only sampled game colours. The game has no health
bar and no health palette to borrow, and a one-colour bar makes the player count notches instead of
glancing. Departure kept minimal: all three ramp colours are warm, desaturated, pulled toward the
game's gold; spent notches and the too-weak state stay inside the palette. Hard steps, not a
gradient — the value of a ramp is the moment it changes.

## ADR-001 — Read health via one reflection hop, don't hardcode 3/5
Three of four health fields are private `ItemParameterRef<int>`; `GetValue` on the reference is
public API, so a cached `FieldInfo` reaches supported ground. Hardcoding defaults would report wrong
numbers for items (or mods) that override health via `ItemParametersAddon`. Fails safe: logs once,
falls back to defaults.
