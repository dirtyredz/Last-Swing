# Nexus Page Copy

Paste-ready copy for the mod page, plus the shot list.

**Name:** `Last Swing - Health Bars for Trees and Rocks`
**Category:** User Interface
**Tags:** Quality of Life, Utilities for Players
**Requirements:** BepInEx 5.4.23.5

---

## Summary (one line, shows in listings)

See how much is left in a tree, stump or rock — but only while you are actually swinging at it.
Shows the swings remaining for your tool, and warns when it is too weak. Save-safe.

> Listings truncate around 200 characters, so the first sentence has to stand alone. "Health
> bar", "trees" and "rocks" all sit in the mod name rather than the summary, because Nexus
> keyword search matches titles.

---

# Paste-ready page copy

Nexus splits the page into named fields. Each heading below maps to one of them.

## Field: Description

Chopping a tree in Moonlight Peaks tells you nothing. Hit one looks and sounds exactly like hit
four — same shake, same puff of leaves, same thud — so the only way to find out whether a rock
is nearly done is to keep swinging and see.

The game is tracking the damage. It just never shows you.

**Last Swing** puts that on screen, and only when you want it there.

### What you get

Land a hit on a tree, stump or rock and a small bar appears above it. **One notch per hit**, so
a glance tells you how much is left without reading a number. The remaining notches run
**yellow, then orange, then red** as it gets closer to coming down.

Underneath, **"3 swings"** — worked out from the damage your equipped tool actually deals, not
from raw hit points. Upgrade your axe and the same tree needs fewer swings.

And when your tool simply cannot hurt something, it says **"tool too weak"** *before* you swing,
instead of after you have spent the energy and been shouted at.

### It stays out of the way

This is the part I cared about most. **The bar only appears once you have actually hit
something.**

Not "once it has ever been hit" — a tree you half-chopped three days ago stays silent until you
swing at it again. Merely pointing your axe at the forest shows nothing at all. Look away and
the bar fades.

If you would rather have no text, one setting turns it off and leaves you the bar on its own.

### Comfort, not a cheat

It shows a number the game already worked out. It does not change one. Trees do not fall
faster, tools do not hit harder, nothing is made cheaper.

### Save-safe

**Nothing is written to your save, ever.** No Harmony patches either — the mod only reads. You
can remove it at any time and your save will not know it was there.

### Works with

- **Plant Peek** — no conflict. Plant Peek answers "how is this crop doing?" on hover; Last
  Swing answers "how many more swings?" while you chop, and covers rocks and ore, which Plant
  Peek does not.
- **Mod Menu** — every setting appears there, so you never have to open a config file.

### Settings

Bar: master toggle, whether a hit is required first, whether the target must be in range, and
how long the bar lingers after you look away.

Display: text on or off, swings-remaining, the too-weak warning, where the yellow/orange/red
thresholds sit, and the bar's height, width, thickness, opacity and font size.

## Field: Version

1.0.0

## Field: Requirements

BepInEx 5.4.23.5

## Field: Install

Extract the archive over your Moonlight Peaks folder, so the DLL lands in
`BepInEx/plugins/LastSwing/`. Or install it with Vortex.

To uninstall, delete that folder. Nothing else is left behind.

---

## Shot list

Ranked by how much each one sells the mod. The first is the only one that is truly required.

1. **The bar mid-chop on a full tree** — part-empty, orange, "2 swings" underneath. This is the
   whole mod in one image and should be the thumbnail.
2. **Yellow, orange and red side by side** — three shots of the same tree at different stages,
   or one composite. Shows the ramp at a glance.
3. **"tool too weak"** on a rock that needs a better pickaxe. Sells the feature nobody knows
   they want until they have been shouted at.
4. **Bar-only mode** — same scene as shot 1 with `ShowText` off, for people who dislike floating
   text.
5. **A rock or ore node**, so it is obvious this is not a trees-only mod.
6. **The Mod Menu settings page**, which the scene reads as a sign of a well-behaved mod.

Still to produce: `banner.png` and `thumbnail.png`.
