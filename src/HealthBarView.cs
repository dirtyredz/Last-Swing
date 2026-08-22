using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSwing
{
    /// <summary>
    /// The bar's Unity view: builds the private overlay canvas once, draws a target onto it, and
    /// projects it to screen. Owns every GameObject the bar is made of and the camera cache;
    /// knows nothing about polling, arming or linger timing — <see cref="HealthBar"/> drives it,
    /// handing over a target to draw and a world point to sit above.
    ///
    /// <b>Positioning is a per-frame WorldToScreenPoint on a private overlay canvas</b>, the
    /// approach Chest Labels established and Plant Peek reuses. The game does have its own
    /// world-space UI system — <c>GameWorldUIScreen.InstantiateWidget</c> handles projection,
    /// depth sorting and cutscene hiding — and it is the right long-term home for this. It is
    /// not used yet for one specific reason: <c>GameWorldUIWidget</c> only re-projects when
    /// <c>EventBus.OnGameCameraPositionChanged</c> fires, and how often that fires while the
    /// player walks is unverified. Getting that wrong means a bar that visibly lags the tree it
    /// is attached to, which is the mod's most-watched pixel. Confirm the event's frequency in
    /// a run, then switch. See 08-mod-ideas.md, Rung 6.
    /// </summary>
    internal sealed class HealthBarView
    {
        /// <summary>
        /// Above this many segments the bar switches to a continuous fill. Twenty slivers do
        /// not read as "twenty swings"; they read as noise.
        /// </summary>
        private const int MaxSegments = 12;

        private const float SegmentGap = 3f;

        private Canvas canvas;
        private CanvasGroup group;
        private RectTransform plateRect;
        private Image plateImage;
        private RectTransform trackRect;
        private TextMeshProUGUI label;

        private readonly List<Image> segments = new List<Image>();

        private Camera cachedCamera;
        private bool warnedNoCamera;

        /// <summary>Whether the canvas exists and is currently shown.</summary>
        internal bool IsActive => canvas != null && canvas.gameObject.activeSelf;

        /// <summary>
        /// Build (if needed), draw <paramref name="target"/>, and reveal the bar — the one
        /// operation the controller ever needs to show something. Keeping build-then-draw-then-
        /// reveal inside the view means the ordering contract can't be got wrong at a call site:
        /// <see cref="Draw"/> writes straight to the built objects and would throw if it ran
        /// before <see cref="EnsureUi"/>.
        /// </summary>
        internal void Show(Transform parent, DamageReader.Target target, bool canDamage)
        {
            EnsureUi(parent);
            Draw(target, canDamage);
            SetActive(true);
        }

        /// <summary>Hide the bar. Safe before the canvas is built (a no-op then).</summary>
        internal void Hide()
        {
            SetActive(false);
        }

        private void SetActive(bool active)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(active);
            }
        }

        /// <summary>Set the whole bar's opacity, for the linger fade the controller drives.</summary>
        internal void SetAlpha(float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }

        /// <summary>
        /// Project <paramref name="world"/> onto the overlay and move the bar there. Leaves the
        /// bar where it was if no camera can be found, and fully transparent when the point is
        /// behind the camera.
        /// </summary>
        internal void PositionAt(Vector3 world)
        {
            var camera = ResolveCamera();
            if (camera == null)
            {
                if (!warnedNoCamera)
                {
                    warnedNoCamera = true;
                    LastSwingPlugin.Log.LogWarning(
                        "No usable camera found - the health bar cannot position itself.");
                }
                return;
            }

            var screenPoint = camera.WorldToScreenPoint(world);

            if (screenPoint.z < 0f)
            {
                // Behind the camera.
                SetAlpha(0f);
                return;
            }

            plateRect.position = screenPoint;
        }

        private void Draw(DamageReader.Target target, bool canDamage)
        {
            var width = Mathf.Max(24f, LastSwingPlugin.BarWidth.Value);
            var height = Mathf.Max(4f, LastSwingPlugin.BarHeight.Value);

            plateRect.sizeDelta = new Vector2(width + 8f, height + 8f);
            trackRect.sizeDelta = new Vector2(width, height);

            // Re-read on every draw so editing the .cfg - or moving a slider in Mod Menu -
            // takes effect without a restart. The UI objects themselves are built once.
            ApplyStyle();

            var count = target.Threshold <= MaxSegments ? target.Threshold : 1;
            EnsureSegments(count);

            var spent = target.Threshold - target.Remaining;

            // One colour for the whole remaining run, chosen from how much is left overall -
            // not per segment. Shading each notch separately would turn a five-hit tree into a
            // gradient, and the point of the ramp is that a glance at the colour answers "am I
            // nearly there?" without counting anything.
            var remainingColour = canDamage
                ? RampColour(target.Remaining / (float)target.Threshold)
                : (Color)GamePalette.SegmentBlocked;

            // Segments anchor to the track's left edge - anchorMin.x of 0 makes anchoredPosition
            // relative to that edge, so x counts rightward from 0 and needs no half-width
            // offset of its own. The track is what is centred, not each segment.
            if (count == 1)
            {
                // Continuous fill: one segment sized to the remaining fraction. Pinned to the
                // left so it empties toward the right, the direction every meter in every game
                // empties, rather than shrinking about its centre.
                var fraction = Mathf.Clamp01(target.Remaining / (float)target.Threshold);
                PlaceSegment(segments[0].rectTransform, 0f, width * fraction, height);
                segments[0].color = remainingColour;
            }
            else
            {
                var segmentWidth = (width - SegmentGap * (count - 1)) / count;

                for (var i = 0; i < count; i++)
                {
                    PlaceSegment(
                        segments[i].rectTransform, i * (segmentWidth + SegmentGap), segmentWidth, height);

                    // Segments empty left to right, so the leftmost ones go first.
                    var isSpent = i < spent;
                    segments[i].color = isSpent
                        ? (Color)GamePalette.SegmentSpent
                        : remainingColour;
                }
            }

            DrawLabel(target, canDamage);
        }

        /// <summary>
        /// Yellow above <c>OrangeBelow</c>, orange down to <c>RedBelow</c>, red under that.
        ///
        /// Hard steps rather than a lerp between the three. A continuous gradient reads as one
        /// slowly-shifting colour and never quite says "you are in the red now" — the whole
        /// value of the ramp is the moment it changes, and that only lands if it changes at
        /// once. It also means a five-notch tree shows three distinguishable states rather than
        /// five muddy in-betweens.
        ///
        /// Both thresholds are fractions of health <i>remaining</i>, so a lower number means
        /// closer to felled. Guarded so a config file with red above orange still behaves.
        /// </summary>
        private static Color RampColour(float remainingFraction)
        {
            var red = Mathf.Clamp01(LastSwingPlugin.RedBelow.Value);
            var orange = Mathf.Clamp01(LastSwingPlugin.OrangeBelow.Value);

            if (orange < red)
            {
                orange = red;
            }

            if (remainingFraction <= red)
            {
                return GamePalette.SegmentLow;
            }

            return remainingFraction <= orange
                ? (Color)GamePalette.SegmentMedium
                : (Color)GamePalette.SegmentHigh;
        }

        private void DrawLabel(DamageReader.Target target, bool canDamage)
        {
            if (label == null)
            {
                return;
            }

            // Bar-only mode. The too-weak state is still readable without its caption, because
            // the bar itself goes pale and drops out of the yellow-orange-red ramp entirely.
            if (!LastSwingPlugin.ShowText.Value)
            {
                label.gameObject.SetActive(false);
                return;
            }

            if (!canDamage && LastSwingPlugin.WarnWeakTool.Value)
            {
                // The one thing worth interrupting for. InteractionSwingToolState only tells
                // the player this *after* they swing and spend the energy.
                label.text = "tool too weak";
                label.color = GamePalette.SegmentBlocked;
                label.gameObject.SetActive(true);
                return;
            }

            if (!LastSwingPlugin.ShowSwingsLeft.Value)
            {
                label.gameObject.SetActive(false);
                return;
            }

            var perSwing = SwingTarget.SwingDamage();
            if (perSwing <= 0)
            {
                label.gameObject.SetActive(false);
                return;
            }

            // Ceiling, not round: 3 remaining at 2 damage a swing is two swings, not one.
            var swings = Mathf.CeilToInt(target.Remaining / (float)perSwing);
            label.text = swings == 1 ? "1 swing" : $"{swings} swings";
            label.color = GamePalette.NameCream;
            label.gameObject.SetActive(true);
        }

        /// <summary>
        /// Push the values a player can tune back onto the built objects. Colours live in the
        /// palette and are applied per-segment in <see cref="Draw"/>; only the two settings
        /// that are genuinely user-facing are re-read here.
        /// </summary>
        private void ApplyStyle()
        {
            if (plateImage != null)
            {
                var alpha = Mathf.Clamp01(LastSwingPlugin.PlateAlpha.Value);
                plateImage.color = new Color(
                    GamePalette.PanelPlum.r / 255f,
                    GamePalette.PanelPlum.g / 255f,
                    GamePalette.PanelPlum.b / 255f,
                    alpha);
                plateImage.enabled = alpha > 0.003f;
            }

            if (label != null)
            {
                label.fontSize = LastSwingPlugin.LabelFontSize.Value;
            }
        }

        /// <summary>
        /// Lay a segment out relative to the track's <b>left edge</b>: left-anchored so x counts
        /// rightward from 0 with no half-width offset of its own, and vertically centred. Both
        /// fill modes place segments the same way — only the x offset and width differ — so the
        /// anchoring lives here rather than being repeated per branch in <see cref="Draw"/>.
        /// </summary>
        private static void PlaceSegment(RectTransform rect, float x, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void EnsureSegments(int count)
        {
            while (segments.Count < count)
            {
                var go = new GameObject($"Segment{segments.Count}");
                go.transform.SetParent(trackRect, false);

                var image = go.AddComponent<Image>();
                image.sprite = BarSprite.Segment();
                image.type = Image.Type.Sliced;
                image.raycastTarget = false;

                segments.Add(image);
            }

            for (var i = 0; i < segments.Count; i++)
            {
                segments[i].gameObject.SetActive(i < count);
            }
        }

        /// <summary>
        /// Camera.main is null in this game - the gameplay camera is not tagged "MainCamera",
        /// which is normal for a Cinemachine setup. Fall back to the highest-depth active
        /// camera that renders to the screen. Established by Chest Labels.
        /// </summary>
        private Camera ResolveCamera()
        {
            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            {
                return cachedCamera;
            }

            var main = Camera.main;
            if (main != null)
            {
                cachedCamera = main;
                return cachedCamera;
            }

            Camera best = null;
            foreach (var candidate in Camera.allCameras)
            {
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.targetTexture != null)
                {
                    continue;
                }

                if (best == null || candidate.depth > best.depth)
                {
                    best = candidate;
                }
            }

            if (best != null && cachedCamera == null)
            {
                LastSwingPlugin.Log.LogInfo($"Health bar using camera '{best.name}'.");
            }

            cachedCamera = best;
            return cachedCamera;
        }

        /// <summary>
        /// Build the overlay canvas and every child of the bar, once. Parented to
        /// <paramref name="parent"/> (the driving component's transform) and kept across scene
        /// loads. Starts hidden; <see cref="SetActive"/> reveals it.
        /// </summary>
        private void EnsureUi(Transform parent)
        {
            if (canvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("LastSwing_BarCanvas");
            canvasGo.transform.SetParent(parent, false);
            Object.DontDestroyOnLoad(canvasGo);

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above normal UI but below anything that deliberately claims the top. Matches the
            // value Chest Labels and Plant Peek use, so the three mods stack predictably.
            canvas.sortingOrder = 500;
            canvasGo.AddComponent<CanvasScaler>();
            group = canvasGo.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var plate = new GameObject("Plate");
            plate.transform.SetParent(canvasGo.transform, false);
            plateRect = plate.AddComponent<RectTransform>();
            // Anchors and pivot are set explicitly rather than left at whatever AddComponent
            // defaults to; the whole layout below assumes centre-anchored boxes.
            plateRect.anchorMin = new Vector2(0.5f, 0.5f);
            plateRect.anchorMax = new Vector2(0.5f, 0.5f);
            plateRect.pivot = new Vector2(0.5f, 0.5f);

            plateImage = plate.AddComponent<Image>();
            plateImage.sprite = BarSprite.Plate();
            plateImage.type = Image.Type.Sliced;
            plateImage.raycastTarget = false;

            var track = new GameObject("Track");
            track.transform.SetParent(plate.transform, false);
            trackRect = track.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.5f, 0.5f);
            trackRect.anchorMax = new Vector2(0.5f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = Vector2.zero;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(plate.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 2f);
            labelRect.sizeDelta = new Vector2(160f, 24f);

            label = labelGo.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.enableAutoSizing = false;
            label.color = GamePalette.NameCream;
            label.outlineColor = GamePalette.Ink;

            // Gelica plus the game's own outline preset. Chest Labels shipped to Nexus with the
            // stock TMP font on exactly this kind of element, because a private canvas has no
            // neighbour to inherit from. See 10-visual-integration.md.
            GameFonts.Apply(label, preferOutline: true);

            canvasGo.SetActive(false);
            LastSwingPlugin.Log.LogInfo("Health bar canvas created.");
        }
    }
}
