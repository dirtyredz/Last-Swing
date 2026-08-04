# Testing Last Swing

There is no test project. Every code path reads Unity or game types — `SwingToolView`, the four
damage components, a `Canvas` — so a console runner could not exercise anything meaningful.
This is the checklist instead.

Set `VerboseLogging = true` in
`BepInEx/config/com.dirtyredz.moonlightpeaks.lastswing.cfg` before the first run. It logs the
first target found, which component type matched, its damage and threshold, and the tool's
damage per swing — which is enough to diagnose most of what can go wrong below.

## 1. It loads

`LogOutput.log` should contain:

```
Last Swing 0.1.0 loaded. Read-only: nothing is written to your save.
```

No warning about reflection. If `Falling back to the game's default health values` appears, a
private field name has changed — the message says which one.

## 2. The bar appears, on the right thing, at the right moment

Equip the axe, walk up to a small tree, aim at it.

- **Aiming shows nothing.** With `RequireHitFirst` on (the default) the bar must stay hidden
  while you are merely pointing at the tree.
- **The first swing brings it up**, above the tree — not above the player, not at a screen
  corner.
- Turn away: it fades over ~1.5s. Turn back within that window and it should still be up
  *without* needing another swing.
- Walk right away, come back, aim again: **hidden again** until you swing.
- Sheathe the axe or open a menu: the bar goes, without a frame of it floating over the menu.
- Walk while aimed at a tree — **the bar must not lag behind the tree.** This is the specific
  risk the private canvas exists to avoid.

### 2a. The half-chopped case

This is the one the baseline exists for and the one a naive `DamageValue > 0` test fails.

1. Chop a tree part-way. Walk off until the bar clears.
2. Come back and aim at it. **Nothing should appear**, even though it is visibly damaged.
3. Swing once. The bar appears, already part-empty, showing the real remaining count.

## 2b. Bar-only mode

Set `ShowText = false`. Swing at something.

- Bar, no text — not even "tool too weak".
- The too-weak state must still be obvious: the bar goes pale and drops out of the
  yellow/orange/red ramp entirely.

## 2c. The colour ramp

Chop something with enough hit points to cross both thresholds — a full tree with a damage-1
axe is ideal.

- Above 60% remaining: yellow.
- 60% down to 30%: orange.
- Below 30%: red.
- **All remaining notches share one colour**; they do not form a gradient across the bar.
- Steps are abrupt, not a fade. That is deliberate.

Lower `OrangeBelow`/`RedBelow` in the config and the switch points should move without a
restart.

## 3. The count is right

This is the whole mod. Count the notches, then count the swings.

| Target | Expected |
|---|---|
| Small tree | notches empty one per hit; tree falls as the last one empties |
| **Full tree** | **the tree must fall exactly as the bar empties, not one swing after** |
| Stump | same |
| Rock / ore | same |

The full tree is the one to watch. `ChopTreeGridComponent` dies on `Damage > health` where the
others use `>=`, so the threshold is `healthTree + 1` — and a full tree also has to satisfy a
`DamageTakenRequirement` before the grow stage flips it to a stump. If the bar empties a swing
early, the `max(...)` in `DamageReader.ReadChopTree` is picking the wrong gate.

## 4. "N swings" matches reality

With a damage-1 tool, "3 swings" means three more hits. Upgrade the tool and the same tree
should report fewer swings without the notch count changing — the notches are hit points, the
label is swings.

## 5. Tool too weak

Aim a low-tier tool at something that needs a higher one. The bar should read **"tool too
weak"** and go pale *before* you swing. Swing anyway: the game's own
`interaction-failed-use-low-tooldamage` shout should agree with what the bar said.

If the bar says nothing and the game shouts, `SwingTarget.CanDamage` is reading the wrong half
of the test — the game checks both `InteractionToolDamageRequired` and `ToolRequirements`.

## 6. Nothing is written

The important one, and the reason `FindOrCreate` is banned in this codebase.

1. Back up the save.
2. Walk past a lot of trees and rocks with the axe out, aiming at each — **without hitting any.**
3. Sleep, quit, reload.
4. Diff the save against the backup.

No new `DamagePersistence` records should exist for anything untouched. If they do, something
is calling `FindOrCreate` — check that no code path reaches
`DamageTakenRequirement.IsRequirementCompleted` or the persistence collection directly.

## 7. Still open

- Does damage regenerate overnight? `DamagePersistence.DayRegeneratedLast` exists; nothing
  found so far writes it. Chop a tree halfway, sleep, re-aim: if the bar refills, the Nexus
  page has to say so.
- Do ethereal tools show a bar? If not, `EtherealAxesToolView` is a separate view and needs its
  own target source.
- Does the bar survive a room change and a save reload?
