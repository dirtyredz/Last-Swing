> ⚠️ **Superseded — do not paste from this file.**
> The live pages were restyled on 2026-08-04 and this BBCode is the *pre-style* version.
> The live page is now the source of truth; pull its BBCode from the edit form's description
> field. Structure: [14-description-review.md](../../14-description-review.md). Look:
> [15-page-style.md](../../15-page-style.md). Mechanics: [13-nexus-page-standard.md](../../13-nexus-page-standard.md).

# Last Swing — Nexus page source

**Nexus page:** [mod 122](https://www.nexusmods.com/moonlightpeaks/mods/122)

The description field is **SCEditor with a BBCode source**, so the block below is the literal
value that gets set. Structure per [14-description-review.md](../../14-description-review.md).

Description prose and Main features wording are **yours, unchanged**.

**This page needs the most work of the six.** The live version has 77 hard line breaks and no
lists at all — the paragraphs are frozen at the width of the source file they were pasted
from, and every bullet and numbered step is literal `-` and `1.` text. It also drops the
BepInEx team from the credits while demanding BepInEx, and pins BepInEx to one exact build.

**One bullet removed from Main features** — *"Configurable through Mod Menu — bar width,
thickness, height above the target…"*. It named Mod Menu, and the new Configuration section
covers the same ground. Say the word if you want it kept.

## Other fields

| Field | Change |
|---|---|
| Name | `Last Swing` — **no change.** Adding a subtitle was considered and rejected; mod names stay clean |
| Category | User Interface — no change |
| Tags | User Interface, Quality of Life — no change |
| Short description | no change, the live one is good |

## Description source

```bbcode
[size=4][b]Description[/b][/size]
[color=#D4D4D8]Chopping a tree in Moonlight Peaks tells you nothing. Hit one looks and sounds exactly like hit four — the same shake, the same puff of leaves, the same thud. So the only way to know whether a rock is nearly broken is to keep swinging and find out.

The game is already tracking the damage on every tree, stump and rock. It just never shows you.

Last Swing puts it on screen, and only when you want it there.

Land a hit on something and a small bar appears above it — one notch per hit, so a glance tells you how much is left without reading a number. It runs yellow, then orange, then red as the thing gets closer to coming down. Underneath, it tells you how many more swings your current tool needs.

And the part I cared about most: it stays out of the way. The bar only appears once you have actually hit something. Not once it has ever been hit — a tree you half-chopped three days ago stays silent until you swing at it again. Point your axe at a forest and nothing happens. Look away and it fades.

It shows a number the game already worked out. It does not change one. Trees do not fall faster, tools do not hit harder, nothing is made cheaper. [b]Comfort, not a cheat.[/b][/color]

[size=4][b]Main features[/b][/size]
[list]
[*]A health bar on any tree, stump or rock you are chopping or mining. One notch per hit, so you can read it at a glance instead of counting.
[*]It only appears once you land a hit. Aiming at something shows nothing, however damaged it already is, so the world stays clean until you commit.
[*]Yellow, then orange, then red as it depletes. Both thresholds are yours to set.
[*]"3 swings" — how many more hits your equipped tool needs, worked out from its real damage rather than raw hit points. Upgrade your axe and the count drops.
[*]"tool too weak" before you swing, when your tool cannot hurt the target at all. The game only tells you afterwards, once the energy is gone.
[*]Works on rocks and ore, not just trees. Stumps too.
[*]Bar-only mode: one setting turns off all the text and leaves the bar on its own.
[*]Fades out when you look away, with a short linger so it does not flicker while you reposition mid-chop.
[*]Save-safe. The mod only reads. It writes nothing to your save, patches nothing with Harmony, and can be removed at any time.
[/list]

[size=4][b]Requirements[/b][/size]
[list]
[*][b]BepInEx 5 (win_x64)[/b], version 5.4.23.5 or newer — the only thing this mod needs
[/list]
[color=#D4D4D8]Moonlight Peaks, tested on the current build as of 4 August 2026. PC/Steam only — the Switch and mobile builds cannot load BepInEx.[/color]

[size=4][b]Installation[/b][/size]
[b]With Vortex[/b]
[color=#D4D4D8]Open the Files tab, click the Vortex button, and enable the mod. Done.[/color]

[b]Manually[/b]
[list=1]
[*]Install [b]BepInEx 5 (win_x64)[/b] into your Moonlight Peaks folder if you have not already. The BepInEx folder sits beside Moonlight Peaks.exe.
[*]Launch the game once, then quit, so it creates the BepInEx/plugins folder.
[*]Download the archive from the Files tab and extract it over your Moonlight Peaks folder. The file should end up at Moonlight Peaks/BepInEx/plugins/LastSwing/LastSwing.dll
[*]Launch the game.
[/list]
[color=#D4D4D8]To uninstall, delete the BepInEx/plugins/LastSwing folder. That is all — there are no save edits to undo.[/color]

[size=4][b]Configuration[/b][/size]
[color=#D4D4D8]Settings live in BepInEx/config/com.dirtyredz.moonlightpeaks.lastswing.cfg, written on first launch. The defaults are meant to be left alone.

Install [url=https://www.nexusmods.com/moonlightpeaks/mods/127][b]Mod Nook[/b][/url] and you can change them in game instead. Last Swing shows up in it on its own, and the colour thresholds are the case for it — pick the yellow and the red off a palette with a live preview rather than typing hex into a file, and set bar width, thickness and opacity on sliders you cannot push out of range. Nothing here needs it — it just makes this mod easier to live with.[/color]

[size=4][b]Compatibility[/b][/size]
[color=#D4D4D8]No other mods are needed and none are known to conflict. Plant Peek in particular sits alongside it happily: that one tells you how a crop is doing when you hover it, this one tells you how many swings are left while you chop, and it covers rocks and ore, which Plant Peek does not.[/color]

[size=4][b]Shout outs[/b][/size]
[list]
[*][b]Little Chicken Game Company[/b], for shipping a build you can actually read. Last Swing needs no Harmony patch at all, because the game already exposes everything it wants — the object your tool is aimed at is public, and so is the damage on every tree and rock. That is not luck, that is a tidy codebase.
[*]The [b]BepInEx[/b] and [b]HarmonyX[/b] teams, without whom none of this scene exists.
[*][b]MissLarifari1[/b], whose Barn Butler page framed its mod as "comfort, not a cheat". That is the right test for a mod in a cozy game and I have borrowed the framing outright.
[*][b]Far Sight[/b], for stating plainly on its page that it changes nothing in your save. More mod pages should be that clear about it.
[*][b]My Mate[/b], for being my inspiration.
[/list]
```
