# STRUCTURE — Last Swing

Where things live in the code. For *why* the mod works the way it does, read [README.md](README.md)
(unusually thorough — it doubles as the architecture/decisions writeup); this file is the map.

> **Last full review: 2026-08-22** — full-depth structural pass (componentization + abstraction
> lenses + Codex cross-model). Findings in [Structural debt](#structural-debt) and
> [docs/BACKLOG.md](docs/BACKLOG.md).

## What it is

A single BepInEx 5 / HarmonyX plugin (netstandard2.1, Unity Mono). One `MonoBehaviour` draws a
health bar above whatever the equipped axe/pickaxe is aimed at, and only while you are swinging at
it. Strictly read-only — nothing is written to the save. No Harmony patches: the game exposes the
swing target publicly, so the mod reads rather than hooks.

## Layout

```
LastSwing/
├── pack.ps1                 # packaging script — workspace convention: lives at the mod root
├── Directory.Build.props    # workspace-synced canonical: game paths + ModBuildInfo generation
├── STRUCTURE.md, CLAUDE.md, README.md, TESTING.md, CHANGELOG.md, NEXUS.md, LICENSE
├── .github/workflows/       # CI — formatting check over `git ls-files '*.cs'` (no code files)
├── docs/                    # the living-doc set (ARCHITECTURE, DECISIONS, FEATURES, ...)
├── screenshots/             # Nexus page art
├── scripts/                 # repo tooling — git-hook installer + pre-commit formatter (shell)
└── src/
    ├── LastSwing.csproj
    ├── Plugin.cs            # BepInEx entry point — must sit beside the .csproj, not in a folder
    ├── game/                # interop with the live game — strictly read-only, no Harmony patches
    │   ├── SwingTarget.cs        # what the equipped tool is aimed at, and its per-swing damage
    │   ├── DamageReader.cs       # normalises the game's 4 damageable types into one Target DTO
    │   ├── GameFonts.cs          # locates the game's Gelica font + outline material by name
    │   └── GamePalette.cs        # the game's colour constants + this mod's 5 bar states
    ├── ui/                  # the bar's own view and its runtime-generated art
    │   ├── HealthBarView.cs      # owns every GameObject: canvas, segments, label, projection
    │   └── BarSprite.cs          # generated 9-sliced rounded segment + plate sprites
    └── core/                # the mod's own state and timing — no GameObjects, no game reads
        └── HealthBar.cs          # controller: poll, arming state machine, linger/fade, gating
```

There is deliberately **no `tests/`** — every path touches Unity/game types, so
[`TESTING.md`](TESTING.md) is the manual checklist instead.

**Enforced homes:**

- `src/game/` — Harmony patches and live-game bridges: anything that reads or intercepts the running game
- `src/ui/` — panels, widgets, views and runtime-generated sprites
- `src/core/` — the mod's own domain logic, state, timing and diagnostics
- `src/Plugin.cs` — the BepInEx entry point; must sit beside the `.csproj` at the `src/` root
- `pack.ps1` — packaging script; workspace convention puts it at the mod root beside the docs
- `scripts/` — repo tooling: the git-hook installer and the pre-commit formatter

Config binding lives in `Plugin.cs` rather than `core/` because BepInEx `ConfigEntry` binding is part
of the plugin lifecycle; the entry point is the only place that may reference BepInEx types directly.

`GameFonts.cs` and `GamePalette.cs` sit in `game/` rather than `ui/` — they are named for what they
are, bridges that resolve the *game's* own shipped assets and sampled colours, not mod-authored UI.
This matches the sibling mods they are verbatim copies of.

## Components

| File | Responsibility | Depends on |
|---|---|---|
| [`src/Plugin.cs`](src/Plugin.cs) | Composition root. BepInEx entry point; binds ~17 `ConfigEntry` settings; adds the `HealthBar` component. | everything (correct direction) |
| [`src/game/SwingTarget.cs`](src/game/SwingTarget.cs) | *What* the equipped tool is aimed at. Resolves `SwingToolView.Target`, per-swing damage, and the can-damage test. Static. | game types |
| [`src/game/DamageReader.cs`](src/game/DamageReader.cs) | *How much* damage a target has taken / can take. Normalizes the game's **4 damageable component types** (no shared interface) into one `Target` DTO, via cached reflection. Static. | game types |
| [`src/core/HealthBar.cs`](src/core/HealthBar.cs) | **Controller.** `MonoBehaviour` owning the runtime loop: per-frame `Update`, 50 ms poll, the arming state machine, target-loss/linger/fade timing, and the menu/cutscene gate. Decides *when* the bar shows, *what* it shows, and *where* — hands each to the view. Owns no GameObjects. | `SwingTarget`, `DamageReader`, `HealthBarView`, plugin config |
| [`src/ui/HealthBarView.cs`](src/ui/HealthBarView.cs) | **View.** Plain class owning every GameObject the bar is made of: one-time canvas construction, drawing (segments + colour ramp + label), `WorldToScreenPoint` projection, and camera resolution. Knows nothing about polling/arming/timing. | `BarSprite`, `GamePalette`, `GameFonts`, `SwingTarget`, plugin config |
| [`src/ui/BarSprite.cs`](src/ui/BarSprite.cs) | Runtime-generated rounded 9-sliced sprites (no shipped art). Static. | Unity |
| [`src/game/GamePalette.cs`](src/game/GamePalette.cs) | The game's colours + this mod's 5 bar states. **Verbatim shared copy** across sibling mods. | Unity |
| [`src/game/GameFonts.cs`](src/game/GameFonts.cs) | Locates the game's Gelica font/material by name. **Verbatim shared copy** across sibling mods. | Unity, TMP |

**Dependency shape:** a clean tree. `Plugin` (composition root) → `HealthBar` (controller) → leaf
static helpers. No cycles. Config/log are reached as `static` off `LastSwingPlugin` — upward
coupling, but acceptable for a single-component, single-plugin mod with no test seam.

## Build / release

- **Version** is single-sourced from `<Version>` in [`src/LastSwing.csproj`](src/LastSwing.csproj);
  `Directory.Build.props`'s `GenerateModBuildInfo` target emits `ModBuildInfo.Version` at compile
  time so `[BepInPlugin]` never drifts. Never hardcode a version in `Plugin.cs`.
- `Directory.Build.props` and `pack.ps1` are **workspace-synced canonicals** — generated by
  `../../tools/sync-mod-files.ps1`. Do not hand-edit them here.
- Build: `dotnet build src/LastSwing.csproj` (deploys the DLL to the local BepInEx plugins dir).
  Release archive: `pack.ps1` → `dist/LastSwing-<version>.zip`. No test project — every path
  touches Unity/game types; [`TESTING.md`](TESTING.md) is the manual checklist instead.

## Structural debt

The 2026-08-22 full review found **no P0** issues. The structure is sound for a mod this size:
target discovery, damage reading, sprite generation, and config binding are already separated.
Open items, triaged in [docs/BACKLOG.md](docs/BACKLOG.md):

- **P2 — `Target.DestructibleSource` leaks a concrete `DestructibleView`** into `HealthBar` so a
  rock's killing blow can be detected after it leaves the grid. Codex flags it as a leaky DTO;
  the abstraction lens judged it justified (one documented purpose, one caller). Deferred — the
  rock/ore kill-frame path is subtle and under active WIP, and can't be verified without a game
  run; revisit when that WIP is verified in game.
- **P2 — `GameFonts.Search` repeats a name-search loop 3×.** A `FindByName<T>` helper would fold
  it, but the file is a verbatim cross-mod copy — any fix must land in all copies via the workspace
  sync tool, out of scope for this repo alone.

**Resolved in the 2026-08-22 review pass:**

- ✅ **P1 — split `HealthBar` into controller + view.** The 726-line file that fused the
  poll/arm/linger state machine with the whole Unity view is now
  [`HealthBar.cs`](src/core/HealthBar.cs) (377 lines, controller) +
  [`HealthBarView.cs`](src/ui/HealthBarView.cs) (408 lines, view). Behaviour-preserving; build
  verified.
- ✅ Duplicated segment `RectTransform` setup folded into a single `PlaceSegment` helper (now in
  the view).
