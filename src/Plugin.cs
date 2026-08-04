using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace LastSwing
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Moonlight Peaks.exe")]
    public sealed class LastSwingPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.dirtyredz.moonlightpeaks.lastswing";
        public const string PluginName = "Last Swing";
        // Keep in step with <Version> in the csproj - pack.ps1 names the archive from that one
        // and BepInEx reports this one. See 12-versioning-and-release.md.
        public const string PluginVersion = "1.0.1";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> ShowBar;
        internal static ConfigEntry<bool> RequireHitFirst;
        internal static ConfigEntry<bool> RequireInRange;
        internal static ConfigEntry<float> LingerSeconds;

        internal static ConfigEntry<bool> ShowText;
        internal static ConfigEntry<bool> ShowSwingsLeft;
        internal static ConfigEntry<bool> WarnWeakTool;

        internal static ConfigEntry<float> OrangeBelow;
        internal static ConfigEntry<float> RedBelow;

        internal static ConfigEntry<float> WorldHeight;
        internal static ConfigEntry<float> BarWidth;
        internal static ConfigEntry<float> BarHeight;
        internal static ConfigEntry<float> PlateAlpha;
        internal static ConfigEntry<float> LabelFontSize;

        internal static ConfigEntry<bool> VerboseLogging;

        // Mod Menu reads these out of ConfigDescription.Tags to title its sections. Display
        // names only - the .cfg section keys are untouched, since renaming one there would
        // orphan every saved value.
        private const string BarSection = "ModMenu.Section=When the bar shows";
        private const string DisplaySection = "ModMenu.Section=Appearance";
        private const string DiagnosticsSection = "ModMenu.Section=Diagnostics";

        private void Awake()
        {
            Log = Logger;

            // Descriptions are kept to one short line: Mod Menu renders them in a settings row
            // and overflows on anything longer. The reasoning behind each one lives in the
            // mod's README rather than here.

            ShowBar = Config.Bind(
                "Bar", "ShowBar", true,
                new ConfigDescription(
                    "Show a health bar on the tree or rock your tool is aimed at.",
                    null,
                    BarSection, "ModMenu.Label=Show the bar"));

            RequireHitFirst = Config.Bind(
                "Bar", "RequireHitFirst", true,
                new ConfigDescription(
                    "Stay hidden until you land a hit on this target, however damaged it already is.",
                    null,
                    BarSection, "ModMenu.Label=Only after the first hit"));

            RequireInRange = Config.Bind(
                "Bar", "RequireInRange", true,
                new ConfigDescription(
                    "Hide the bar when the target is out of reach.",
                    null,
                    BarSection, "ModMenu.Label=Only when in range"));

            LingerSeconds = Config.Bind(
                "Bar", "LingerSeconds", 1.5f,
                new ConfigDescription(
                    "Seconds the bar stays up after you look away. 0 hides it at once.",
                    new AcceptableValueRange<float>(0f, 10f),
                    BarSection, "ModMenu.Label=Stays up for"));

            ShowText = Config.Bind(
                "Display", "ShowText", true,
                new ConfigDescription(
                    "Show text above the bar. Off leaves the bar on its own.",
                    null,
                    DisplaySection, "ModMenu.Label=Show text"));

            ShowSwingsLeft = Config.Bind(
                "Display", "ShowSwingsLeft", true,
                new ConfigDescription(
                    "Show how many more swings your current tool needs. Needs ShowText.",
                    null,
                    DisplaySection, "ModMenu.Label=Show swings remaining"));

            WarnWeakTool = Config.Bind(
                "Display", "WarnWeakTool", true,
                new ConfigDescription(
                    "Say when your tool is too weak, before you spend the energy. Needs ShowText.",
                    null,
                    DisplaySection, "ModMenu.Label=Warn when the tool is too weak"));

            OrangeBelow = Config.Bind(
                "Display", "OrangeBelow", 0.6f,
                new ConfigDescription(
                    "Bar turns orange below this much health remaining, 0-1.",
                    new AcceptableValueRange<float>(0f, 1f),
                    DisplaySection, "ModMenu.Label=Orange below"));

            RedBelow = Config.Bind(
                "Display", "RedBelow", 0.3f,
                new ConfigDescription(
                    "Bar turns red below this much health remaining, 0-1.",
                    new AcceptableValueRange<float>(0f, 1f),
                    DisplaySection, "ModMenu.Label=Red below"));

            WorldHeight = Config.Bind(
                "Display", "WorldHeight", 1.6f,
                new ConfigDescription(
                    "Height above the target, in world units.",
                    new AcceptableValueRange<float>(0f, 5f),
                    DisplaySection, "ModMenu.Label=Height above the target"));

            BarWidth = Config.Bind(
                "Display", "BarWidth", 96f,
                new ConfigDescription(
                    "Bar width in pixels.",
                    new AcceptableValueRange<float>(24f, 400f),
                    DisplaySection, "ModMenu.Label=Bar width"));

            BarHeight = Config.Bind(
                "Display", "BarHeight", 10f,
                new ConfigDescription(
                    "Bar thickness in pixels.",
                    new AcceptableValueRange<float>(2f, 40f),
                    DisplaySection, "ModMenu.Label=Bar thickness"));

            PlateAlpha = Config.Bind(
                "Display", "PlateAlpha", 0.55f,
                new ConfigDescription(
                    "Opacity of the plate behind the bar, 0-1.",
                    new AcceptableValueRange<float>(0f, 1f),
                    DisplaySection, "ModMenu.Label=Background opacity"));

            LabelFontSize = Config.Bind(
                "Display", "LabelFontSize", 18f,
                new ConfigDescription(
                    "Font size of the text above the bar.",
                    new AcceptableValueRange<float>(8f, 48f),
                    DisplaySection, "ModMenu.Label=Text size"));

            VerboseLogging = Config.Bind(
                "Diagnostics", "VerboseLogging", false,
                new ConfigDescription(
                    "Log the first target found and how its health was resolved.",
                    null,
                    DiagnosticsSection, "ModMenu.Label=Verbose logging"));

            gameObject.AddComponent<HealthBar>();

            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Read-only: nothing is written to your save.");
        }
    }
}
