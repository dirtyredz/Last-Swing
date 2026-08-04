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

Nexus splits the page into named fields. Each heading below is one of them, and the text under
it is meant to be pasted as-is. Written as plain prose rather than BBCode, so it survives both
the rich-text editor and the raw one.

## Field: Description

Chopping a tree in Moonlight Peaks tells you nothing. Hit one looks and sounds exactly like hit
four — the same shake, the same puff of leaves, the same thud. So the only way to know whether
a rock is nearly broken is to keep swinging and find out.

The game is already tracking the damage on every tree, stump and rock. It just never shows you.

Last Swing puts it on screen, and only when you want it there.

Land a hit on something and a small bar appears above it — one notch per hit, so a glance tells
you how much is left without reading a number. It runs yellow, then orange, then red as the
thing gets closer to coming down. Underneath, it tells you how many more swings your current
tool needs.

And the part I cared about most: it stays out of the way. The bar only appears once you have
actually hit something. Not once it has ever been hit — a tree you half-chopped three days ago
stays silent until you swing at it again. Point your axe at a forest and nothing happens. Look
away and it fades.

It shows a number the game already worked out. It does not change one. Trees do not fall
faster, tools do not hit harder, nothing is made cheaper. Comfort, not a cheat.

## Field: Installation instructions

With Vortex: open the Files tab, click the Vortex button, enable it. Done.

Manually:

1. Install BepInEx 5 for Moonlight Peaks if you have not already, and run the game once so it
   creates its folders.
2. Download the archive from the Files tab.
3. Extract it over your Moonlight Peaks folder. The DLL should end up at
   `Moonlight Peaks/BepInEx/plugins/LastSwing/LastSwing.dll`.
4. Launch the game.

To uninstall, delete the `BepInEx/plugins/LastSwing` folder. That is all — there are no save
edits to undo.

Settings live in `BepInEx/config/com.dirtyredz.moonlightpeaks.lastswing.cfg`, written on first
launch. If you have Mod Menu installed you never need to open it: everything appears in the
pause menu under Mods, and changes apply immediately without a restart.

## Field: Main features

- A health bar on any tree, stump or rock you are chopping or mining. One notch per hit, so you
  can read it at a glance instead of counting.
- It only appears once you land a hit. Aiming at something shows nothing, however damaged it
  already is, so the world stays clean until you commit.
- Yellow, then orange, then red as it depletes. Both thresholds are yours to set.
- "3 swings" — how many more hits your equipped tool needs, worked out from its real damage
  rather than raw hit points. Upgrade your axe and the count drops.
- "tool too weak" before you swing, when your tool cannot hurt the target at all. The game only
  tells you afterwards, once the energy is gone.
- Works on rocks and ore, not just trees. Stumps too.
- Bar-only mode: one setting turns off all the text and leaves the bar on its own.
- Fades out when you look away, with a short linger so it does not flicker while you reposition
  mid-chop.
- Configurable through Mod Menu — bar width, thickness, height above the target, opacity, font
  size, colour thresholds, linger time, and every toggle above.
- Save-safe. The mod only reads. It writes nothing to your save, patches nothing with Harmony,
  and can be removed at any time.

## Field: Requirements

- BepInEx 5.4.23.5
- Moonlight Peaks, tested on the current build as of 4 August 2026

Optional, but it makes life easier:

- Mod Menu by Elsiabeth — adds a Mods button to the pause menu so you can change every setting
  in game instead of editing a config file.

No other mods are needed and none are known to conflict. Plant Peek in particular sits
alongside it happily: that one tells you how a crop is doing when you hover it, this one tells
you how many swings are left while you chop, and it covers rocks and ore, which Plant Peek does
not.

## Field: Shout outs

- Little Chicken Game Company, for shipping a build you can actually read. Last Swing needs no
  Harmony patch at all, because the game already exposes everything it wants — the object your
  tool is aimed at is public, and so is the damage on every tree and rock. That is not luck,
  that is a tidy codebase.
- MissLarifari1, whose Barn Butler page framed its mod as "comfort, not a cheat". That is the
  right test for a mod in a cozy game and I have borrowed the framing outright.
- Elsiabeth, for Mod Menu. It is the reason this mod ships with no hotkey to remember and no
  config file you are obliged to open.
- Far Sight, for stating plainly on its page that it changes nothing in your save. More mod
  pages should be that clear about it.
- The BepInEx team.

## Field: Version

1.0.0

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

`banner.png` and `thumbnail.png` are done and live in [screenshots/](screenshots/).

⚠️ **The banner reads "Moonlignt Peaks"** — missing `h`, confirmed at 3× magnification. The
thumbnail is correct. Regenerate the banner before the page goes live; misspelling the game's
own name on your header is the kind of thing the first commenter points out.
