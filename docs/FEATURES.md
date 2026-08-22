# FEATURES — Last Swing

Capability inventory. Status: ✅ shipped · 🚧 WIP (committed, unreleased) · 💡 idea.

| Feature | Status | Notes |
|---|---|---|
| Health bar above the swing target | ✅ | Segmented, one notch per hit; continuous fill above 12 hits. |
| Show only while actively attacking | ✅ | `RequireHitFirst` — arms when damage rises above the baseline taken when first aimed at, not `Damage > 0`. |
| Covers all 4 damageable component types | ✅ | Rocks/ore (`DestructibleView`), full trees, small trees, stumps. |
| Correct full-tree threshold | ✅ | `max(healthTree + 1, ceil(DamageTakenRequirement))` — verified in game (tree falls as the bar empties). |
| "N swings" remaining | ✅ | From the equipped tool's real damage, not raw hit points. `ShowSwingsLeft`. |
| "tool too weak" *before* the swing | ✅ | Bar also drops out of the colour ramp and goes pale. `WarnWeakTool`. |
| yellow → orange → red depletion ramp | ✅ | Thresholds `OrangeBelow` (0.6) / `RedBelow` (0.3). Hard steps. |
| Bar-only mode | ✅ | `ShowText` off. |
| Menu / pause / cutscene suppression | ✅ | Every-frame gate on the game's own grid-cursor blocker. |
| Linger + fade on target loss | ✅ | `LingerSeconds` (1.5) stops strobing as the target flickers mid-turn. |
| Fully configurable (Mod Menu / ConfigurationManager) | ✅ | ~17 settings, re-read every draw (live tuning). No hotkey by design. |
| Read-only / save-safe | ✅ | No `FindOrCreate` anywhere; verified by save diff. |
| Detect killing blows on rocks/ore after they leave the grid | 🚧 | Committed WIP on top of live 1.0.0 (`pendingDestructibleCheck`). Not released; do not publish. |
| Migrate to `GameWorldUIScreen` world-UI | 💡 | Blocked on verifying re-projection frequency. See [DECISIONS](DECISIONS.md) ADR-003. |
| Ethereal-tool support | 💡 | Only if ethereal tools don't route through `SwingToolView`. See [BACKLOG](BACKLOG.md). |

**Released:** v1.0.0 on Nexus ([mod 122](https://www.nexusmods.com/moonlightpeaks/mods/122)).
