// Copied verbatim from mods/ChestLabels/src/ChestLabels/GamePalette.cs, namespace aside,
// plus the two entries below that this mod adds.
// Shared look-and-feel: see 10-visual-integration.md. Fix bugs in all copies.
using UnityEngine;

namespace LastSwing
{
    /// <summary>
    /// The game's colours, in one place, so no element invents its own.
    ///
    /// See <c>10-visual-integration.md</c> at the repo root for why this matters.
    ///
    /// Values are read off the game's own UI. Where a value is estimated from a screenshot
    /// rather than sampled from the shipped asset it says so — those are the ones to verify
    /// if something looks slightly off.
    /// </summary>
    internal static class GamePalette
    {
        /// <summary>
        /// Label text. The pale cream used on the game's world nameplates — noticeably lighter
        /// than the gold of inventory item counts, and the right colour for a name.
        /// Estimated from a screenshot of the "Chester" nameplate.
        /// </summary>
        internal static readonly Color32 NameCream = new Color32(0xF8, 0xF0, 0xC6, 0xFF);

        /// <summary>Warm gold, as used for item quantities in inventory panels.</summary>
        internal static readonly Color32 CountGold = new Color32(0xF7, 0xD9, 0x94, 0xFF);

        /// <summary>Mid royal purple of the world nameplate banner. Estimated.</summary>
        internal static readonly Color32 NameplatePurple = new Color32(0x5A, 0x2D, 0x91, 0xFF);

        /// <summary>Darker purple rim beneath the nameplate banner. Estimated.</summary>
        internal static readonly Color32 NameplateRim = new Color32(0x3B, 0x1B, 0x63, 0xFF);

        /// <summary>Near-black plum of the inventory and chest windows.</summary>
        internal static readonly Color32 PanelPlum = new Color32(0x1B, 0x0F, 0x2E, 0xFF);

        /// <summary>Dark ink used for outlines throughout the game's art.</summary>
        internal static readonly Color32 Ink = new Color32(0x2A, 0x1B, 0x3D, 0xFF);

        // ---- Added by Last Swing -------------------------------------------------------
        //
        // The bar runs yellow → orange → red as it depletes, which is a deliberate departure
        // from 10-visual-integration.md's rule that mod UI should only use shipped colours.
        // The game has no health bar and therefore no health palette to borrow, and a bar that
        // stays one colour throughout makes the player read the notches rather than glance at
        // it — which defeats the point of a bar over a number.
        //
        // The departure is kept as small as it can be. All three are warm, desaturated and
        // pulled toward the game's own gold rather than being screen-bright signal colours:
        // Yellow starts from CountGold's hue, and Red is a dusty brick rather than a pure red,
        // so the ramp still sits inside the game's art direction even though no single value
        // is sampled from it.
        //
        // Spent segments stay the dark purple from under the nameplate banner, so an emptied
        // notch reads as a hole in the plate rather than as a fourth colour competing with the
        // ramp.

        /// <summary>Plenty left. Starts from the gold the game uses for numbers that matter.</summary>
        internal static readonly Color32 SegmentHigh = new Color32(0xF2, 0xC9, 0x4C, 0xFF);

        /// <summary>Getting there.</summary>
        internal static readonly Color32 SegmentMedium = new Color32(0xE8, 0x85, 0x3A, 0xFF);

        /// <summary>Nearly down. Dusty brick, not a signal red.</summary>
        internal static readonly Color32 SegmentLow = new Color32(0xD1, 0x4B, 0x42, 0xFF);

        /// <summary>A segment already chopped or mined away.</summary>
        internal static readonly Color32 SegmentSpent = new Color32(0x3B, 0x1B, 0x63, 0xB0);

        /// <summary>
        /// Shown when the equipped tool cannot damage the target at all. Desaturated toward the
        /// panel plum rather than a saturated warning red, which would be the one element on
        /// screen that no shipped pixel matches.
        /// </summary>
        internal static readonly Color32 SegmentBlocked = new Color32(0x8A, 0x74, 0x9A, 0xFF);
    }
}
