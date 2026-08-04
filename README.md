# Last Swing

A health bar on the tree, stump or rock you are swinging at — and only while you are swinging
at it.

**Status:** v0.2.0 — **confirmed working in game.** The bar appears on the right object, at the
right moment, and tracks damage as you chop.

Unpublished. What is confirmed is that it works; several specific claims below are still only
as good as the decompile they came from, and the ones that would change the mod are listed
under [Not confirmed yet](#not-confirmed-yet).

**Nexus title:** `Last Swing - Health Bars for Trees and Rocks`

The subtitle carries the search; the name is what someone repeats in a Discord. Alternates
considered and dropped: *Timber* (collides with lumber-automation mods in search), *Death Toll*
(too grim for a cozy game's mod list). Internals stay literal — `HealthBar`, `DamageReader`,
`SwingTarget` — because a joke in a type name stops being funny at 2am.

## The problem

Chopping a tree gives you a shake, a puff of leaves and a sound. Hit one gives you exactly the
same shake, puff and sound as hit four. The game tracks damage per object and never shows it,
so the only way to know whether a rock is nearly done is to keep swinging and find out.

And when your tool is simply too weak, the game tells you *after* the swing — after the energy
is spent, via a shout.

## What it does

Land a hit on something breakable and a segmented bar appears above it. One notch per hit.
Notches empty left to right as you chop, and the remaining run runs **yellow → orange → red**
as the target gets closer to coming down.

- **Only while you are actually attacking, and only once you have started.** Aiming is not
  attacking: the bar stays hidden until a swing lands, *however damaged the target already is*.
  A tree someone half-chopped three days ago says nothing until you swing at it again.
- **"3 swings"** under the bar, computed from the damage your equipped tool actually deals, not
  from raw hit points. Turn text off entirely for a bar and nothing else.
- **"tool too weak"** *before* you swing, when the target's `InteractionToolDamageRequired`
  exceeds your tool's damage.
- **Writes nothing.** Every read is a read. Save-safe, removable at any time.

### "Only once you have started" is not "has it ever been hit"

The distinction matters and it is what `RequireHitFirst` implements. Testing
`DamagePersistence.DamageValue > 0` would light up every partly-chopped tree on the farm the
moment it was aimed at — the opposite of what the setting is for.

Instead, aiming at an object records the damage it already carries, and the bar arms only when
damage rises **above that baseline**. Arming survives the linger window, so glancing away
mid-chop and coming straight back does not demand another swing first; a full hide clears it,
so walking off and returning later starts silent again.

Comfort, not a cheat: it shows a number the game already simulated. It does not change one.

## What it covers

All four of the game's damageable component types:

| Component | In play | Where its maximum lives |
|---|---|---|
| `DestructibleView` | rocks, ore, breakables | `ItemAsset.DestructibleAddon.TotalHealthPoints` — public |
| `ChopTreeGridComponent` | full trees | private `healthTree` (default 5) |
| `DestructableTreeGridComponent` | small trees | private `healthTree` (default 3) |
| `ChopStumpGridComponent` | stumps | private `healthStump` (default 3) |

## How it works

### Finding what you are attacking

This is the part the game hands over for free, and it is why this mod is much smaller than
Chest Labels or Plant Peek. `SwingToolView` — the equipped-axe/pickaxe view — recomputes its
target every frame in `ProcessEquippedUpdate` and exposes it publicly:

```csharp
public IInteractable Target { get; private set; }
public bool TargetIsInRange { get; private set; }
```

That is the same object the game is already drawing its grid cursor on. **No raycast, no hover
system, no Harmony patch.** Both of the previous world-UI mods here had to build hover from
scratch and then reconcile their reach with the game's; this one cannot disagree with the game
because it is reading the game's own answer.

It carries the gating too. The view only exists while a swing tool is equipped, and
`GrabbedItemView` stops updating when the `"PlayerToolInteractor"` blocker is set — which is
what menus, cutscenes and confinement do. So there is no `PlayerCursorInteractionScreen` gate to
work out, the way Chest Labels needed.

> ⚠️ `SwingToolView.SetTarget` clears `Target` on its null path but leaves `TargetIsInRange`
> holding its previous value. Read the flag only when `Target` is non-null.

### Reading the damage

`DamagePersistence` is public, lives in `GamePersistence.Instance.CurrentRoom.DamagePersistences`
and is keyed by the grid object's GUID — so identity is stable across moves and the problem that
dominated Chest Labels does not arise here.

> ⚠️ **`TryGetByGuid`, never `FindOrCreate`.** The game's `FindOrCreate` writes a damage record
> for any object it is asked about. A mod that inspects whatever the axe points at would seed a
> record for every tree the player walks past. Plant Peek established this rule; this mod goes
> further and never queries the collection directly at all — it reads the `DamagePersistence`
> the component is already holding.

### The off-by-one that would have shipped

Three of the four components die on `DamagePersistence.IsDead`, which is `Damage >= health`.
**`ChopTreeGridComponent` does not:**

```csharp
public bool Chop() {
    ...
    if (DamagePersistence.Damage > healthTree.GetValue(this)) { ... }   // strictly greater
}
```

A health-5 tree survives damage 5 and only falls on the sixth point. Computing that bar with
`healthTree` as the denominator empties it a full swing before the tree comes down — at exactly
the moment the player is staring at it. So the threshold for this one type is `healthTree + 1`.

Nothing in the method name says so. This is the same shape of trap as
`GetFinalGrowStage()` hanging on regrowing crops: only the body tells you.

### Two gates on a full tree, not one

`ChopTreeGridComponent.Chop()` never destroys the tree. It dispatches `RequestGrowstageCheck`,
and the transition into the stump stage is gated by a `DamageTakenRequirement`
(`DamageValue >= DamageAmount`) hanging off one of the current grow stage's paths.

So **both** gates have to pass, and the felling threshold is the larger of the two.
`DamageReader.ReadChopTree` computes `max(healthTree + 1, ceil(DamageAmount))`. Reading the
requirement directly rather than calling `IsRequirementCompleted` matters — that method uses
`FindOrCreate` and would write to the save.

### One reflection hop, and why it is not just hardcoded

Three of the four health fields are private `ItemParameterRef<int>`. `GetValue()` on the
reference is public, so a cached `FieldInfo` per type is enough to get back onto supported API.

Hardcoding 3 and 5 would have been shorter and wrong: health can be overridden per item asset
through `ItemParametersAddon`, so higher-tier or modded trees would report the wrong number. If
the reflection ever fails, the mod logs **once** and falls back to the defaults rather than
flooding the log at the poll rate.

### Drawing it

A private screen-space overlay canvas, positioned every frame with `WorldToScreenPoint` — the
approach Chest Labels established and Plant Peek reuses, including the `Camera.main is null`
workaround (the gameplay camera is not tagged `MainCamera`; scan `Camera.allCameras` for the
highest-depth active one).

**The game does have its own world-space UI system**, and it is the better long-term home:
`GameWorldUIScreen.InstantiateWidget<T>(prefab, target)` handles projection, re-projection,
depth sorting between widgets, off-screen clamping, and hiding during cutscenes — it even
declares `showDuringPlayerConfinement => false`.

It is deliberately not used in v0.1.0 for one specific reason: `GameWorldUIWidget` only
re-projects when `EventBus.OnGameCameraPositionChanged` fires, and how often that fires while
the player walks is **unverified**. `GameWorldUIScreen.LateUpdate` only re-sorts sibling
indices; it never calls `UpdatePosition`. If that event is not effectively per-frame, the bar
visibly lags the tree it is attached to — which is the mod's most-watched pixel. Confirm the
frequency in a run, then switch.

### Colours

The bar runs **yellow → orange → red** as it depletes, switching at `OrangeBelow` (0.6) and
`RedBelow` (0.3) of health remaining.

This is a deliberate departure from `10-visual-integration.md`, which says mod UI should only
use colours sampled from the game. The game has no health bar and so no health palette to
borrow, and a bar that holds one colour throughout makes you *count notches* rather than
glance — which defeats having a bar instead of a number.

The departure is kept as small as it can be: all three are warm and desaturated and pulled
toward the game's own gold rather than being screen-bright signal colours. Yellow starts from
`CountGold`'s hue and red is a dusty brick, not a pure red, so the ramp still sits inside the
game's art direction even though no single value is sampled from it.

Two things stay inside the palette:

- **Spent** notches are the dark purple from under the nameplate banner, so an emptied notch
  reads as a hole in the plate rather than as a fourth colour competing with the ramp.
- **Too weak** drops out of the ramp entirely and goes pale. That is what keeps the state
  legible in bar-only mode, where the caption is suppressed.

The steps are hard, not a gradient. A continuous lerp reads as one slowly-shifting colour and
never quite says *you are in the red now* — the value of a ramp is the moment it changes, and
that only lands if it changes at once. It also means a five-notch tree shows three
distinguishable states rather than five muddy in-betweens.

Above 12 segments the bar switches to a continuous fill. Twenty slivers do not read as "twenty
swings"; they read as noise.

## Configuration

Everything is `Config.Bind`, so [Mod Menu](https://www.nexusmods.com/moonlightpeaks/mods/102)
and ConfigurationManager pick it up. There is no hotkey — nothing here is worth a keybind that
could collide, and F1 in particular is taken by Serena's Grimoire and ConfigurationManager.

| Setting | Default | Notes |
|---|---|---|
| `ShowBar` | `true` | Master switch. |
| `RequireHitFirst` | `true` | Stay hidden until you land a hit on this target. See below. |
| `RequireInRange` | `true` | Hide when the target is out of reach. |
| `LingerSeconds` | `1.5` | Stops the bar strobing as the weighted target flickers mid-turn. |
| **`ShowText`** | `true` | **Off gives the bar on its own, with no text at all.** |
| `ShowSwingsLeft` | `true` | The `"3 swings"` line. Needs `ShowText`. |
| `WarnWeakTool` | `true` | The `"tool too weak"` line. Needs `ShowText`. |
| `OrangeBelow` | `0.6` | Bar turns orange below this fraction remaining. |
| `RedBelow` | `0.3` | Bar turns red below this fraction remaining. |
| `WorldHeight` | `1.6` | Height above the target, world units. |
| `BarWidth` / `BarHeight` | `96` / `10` | Pixels. |
| `PlateAlpha` | `0.55` | Backing opacity. |
| `LabelFontSize` | `18` | |
| `VerboseLogging` | `false` | Logs the first target and how its health resolved. |

Display settings are re-read every draw, so tuning them takes effect without a restart.

## Relationship to Plant Peek

Plant Peek already reports `chopped 47%` when you hover a tree, from the same
`DamageTakenRequirement` this mod reads. They do not conflict and they answer different
questions:

- **Plant Peek** — mouse hover, any growable, text, percentage. Trees only; it is built on
  `GrowableView`.
- **Last Swing** — swing target, includes **rocks and ore** (no `GrowableView` anywhere), a
  segmented bar, and swings-remaining for the tool actually in your hand.

Running both is fine. If both are visible at once the hover panel sits where Plant Peek puts it
and the bar sits at `WorldHeight` above the target.

## Confirmed in game

- The bar appears on the object being swung at, positioned above it, and tracks damage as it
  is chopped. `SwingToolView.Target` is the right hook, and the private overlay canvas
  positions correctly against a moving camera.

## Not confirmed yet

Still open. Ranked by how much they would change the mod, and **the first two are the ones
that gate a Nexus release** — see [TESTING.md](TESTING.md) for how to run them.

1. **Nothing is written to the save.** TESTING.md §6: back up a save, aim at a lot of trees and
   rocks without hitting any, sleep, reload, diff. The mod claims *save-safe* on its page and
   that claim needs the diff behind it, not just an audit of the source. The whole
   `TryGetByGuid`-never-`FindOrCreate` discipline exists for this.
2. **The full tree falls exactly as the bar empties, not one swing after.** TESTING.md §3. This
   is the `ChopTreeGridComponent` off-by-one, and it is the one bug a player would notice
   immediately and rate the mod down for. Small trees, stumps and rocks all use the ordinary
   `>=` rule and would not catch it.
3. **`DamagePersistence.DayRegeneratedLast` exists and nothing found so far writes it.** If
   damage regenerates overnight, a partial chop resets. The bar reads live so it will be
   correct either way, but the Nexus page has to say so. Find the writer.
4. **Do ethereal tools route through `SwingToolView`?** `EtherealAxesToolView` and
   `EtherealPickaxesToolView` are separate types. If they do not share the swing view, they
   need a second target source — and Better Ethereal Tools is a popular mod.
5. **Is `healthTree + 1` or the `DamageTakenRequirement` the binding gate in practice?** The
   code takes the max of both, which is right either way, but knowing which one actually binds
   would let the comment stop hedging.
6. **Does it survive a room change and a save reload?** The canvas is `DontDestroyOnLoad`; the
   cached camera and swing view are both validated before use, but that is untested.

## Layout

```
src/
  LastSwing.csproj  the only project
  Plugin.cs         BepInEx entry point and config
  SwingTarget.cs    what the equipped tool is aimed at
  DamageReader.cs   the four component types -> damage and threshold
  HealthBar.cs      polling, drawing and positioning
  BarSprite.cs      runtime-generated rounded shapes
  GamePalette.cs    shared, plus this mod's three bar states
  GameFonts.cs      shared, copied verbatim
Directory.Build.props  the game path, set once
pack.ps1               release archive
TESTING.md             manual verification checklist
```

`GamePalette.cs` and `GameFonts.cs` are copies shared with
[Chest Labels](https://www.nexusmods.com/moonlightpeaks/mods/119) and Plant Peek. Fix bugs in
all copies.

## Building

Requires the .NET SDK and a local copy of Moonlight Peaks with BepInEx 5 installed.

```bash
dotnet build src/LastSwing.csproj
```

The game path is set once in [`Directory.Build.props`](Directory.Build.props) and
defaults to the usual Steam location — **edit it if yours differs.** Nothing is fetched from
NuGet; every reference resolves against the game's own `Managed` folder, and none of those
assemblies are copied next to the plugin.

A successful build deploys straight to
`BepInEx/plugins/MoonlightPeaksMods/LastSwing/`. That dev path is deliberately not the one
players get — `pack.ps1` produces `dist/LastSwing-<version>.zip` laid out as
`BepInEx/plugins/LastSwing/`, so a hand-built DLL never collides with a Vortex-managed install.

There is no test project, on purpose: every code path reads Unity or game types, so a runner
outside the game could not exercise anything real. [TESTING.md](TESTING.md) is the checklist
instead.

## Design notes

Several comments here cite files like `10-visual-integration.md`, `12-versioning-and-release.md`
and `08-mod-ideas.md`. Those are the shared research notes behind all of these mods and they
live in [dirtyredz/chest-labels](https://github.com/dirtyredz/chest-labels), not in this repo —
`08-mod-ideas.md` in particular carries the gap check and the decompile findings this mod was
built from.

## Licence

MIT. See [LICENSE](LICENSE).
