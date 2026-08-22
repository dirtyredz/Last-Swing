using System;
using System.Collections.Generic;
using Chicken.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSwing
{
    /// <summary>
    /// Draws the bar above whatever the player's axe or pickaxe is aimed at.
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
    internal sealed class HealthBar : MonoBehaviour
    {
        /// <summary>
        /// Above this many segments the bar switches to a continuous fill. Twenty slivers do
        /// not read as "twenty swings"; they read as noise.
        /// </summary>
        private const int MaxSegments = 12;

        private const float PollInterval = 0.05f;
        private const float SegmentGap = 3f;

        private Canvas canvas;
        private CanvasGroup group;
        private RectTransform plateRect;
        private Image plateImage;
        private RectTransform trackRect;
        private TextMeshProUGUI label;

        private readonly List<Image> segments = new List<Image>();

        private float nextPollTime;

        /// <summary>What the bar is currently about. Null when nothing is aimed at.</summary>
        private DamageReader.Target current;

        /// <summary>Kept past target loss so the bar can fade rather than blink out.</summary>
        private Transform lastAnchor;
        private float hideAtTime;

        /// <summary>
        /// The target the arming state below belongs to. Compared by reference; a different
        /// transform means a different object and a fresh baseline.
        /// </summary>
        private Transform armedAnchor;

        /// <summary>
        /// Damage this target already had when it was first aimed at. The bar only appears once
        /// damage rises <i>above</i> this, so a half-chopped tree stays silent until the player
        /// swings at it — "have you hit it" rather than "has it ever been hit".
        /// </summary>
        private int baselineDamage;

        /// <summary>Set once a hit has landed on the current target.</summary>
        private bool armed;

        /// <summary>
        /// The rock/ore behind <see cref="current"/>, if any - kept so a killing blow can still
        /// be detected once the object drops off the grid.
        ///
        /// <b>Rocks need this; trees don't.</b> A tree's own <c>ChopTreeGridComponent</c> stays
        /// resolvable through <c>SwingTarget.Find</c> for a poll or two after the killing swing,
        /// so letting <see cref="Poll"/> draw its Remaining-hits-0 frame before releasing is
        /// enough. Decompiling <c>DestructibleView.Hit()</c> shows a rock has no such window:
        /// the same call that lands the killing blow also unregisters it from the grid, so by
        /// our next 50ms poll <c>SwingTarget.Find</c> already returns null and
        /// <c>DamageReader.Read</c> is never reached at all - there is no frame left where a
        /// live poll would ever see <c>Remaining == 0</c>.
        ///
        /// Holding the component reference directly sidesteps the grid lookup entirely: a
        /// destroyed rock's <c>DestructibleView</c> still exists as a plain C# object for about
        /// a second afterwards (see <c>DestroyAsGridObject(1f, ...)</c>), so <c>IsDestructed</c>
        /// can still be read off it after the game has stopped offering it as a target.
        /// </summary>
        private DestructibleView pendingDestructibleCheck;

        private Camera cachedCamera;
        private bool warnedNoCamera;
        private bool loggedFirstTarget;
        private bool loggedSuppressionFailure;
        private string loggedSuppressionReason;

        private void Update()
        {
            // Checked every frame rather than on the poll interval, and Clear rather than
            // Release: a pause menu opening must take the bar with it immediately, not fade it
            // out over the linger while sitting on top of the UI.
            if (!LastSwingPlugin.ShowBar.Value || IsSuppressed())
            {
                Clear();
                return;
            }

            if (Time.unscaledTime >= nextPollTime)
            {
                nextPollTime = Time.unscaledTime + PollInterval;

                try
                {
                    Poll();
                }
                catch (Exception e)
                {
                    LastSwingPlugin.Log.LogError($"Health bar failed; disabling it. {e}");
                    LastSwingPlugin.ShowBar.Value = false;
                    Clear();
                    return;
                }
            }

            // Every frame, not on the poll interval - the camera moves continuously and a
            // 50ms-stale screen position is visible as the bar swimming behind the tree.
            Reposition();
        }

        /// <summary>
        /// Whether the game is in a state where no world UI should be on screen — a menu, the
        /// pause screen, a cutscene, a full-screen window.
        ///
        /// <b>This is not optional, and the first version was wrong to think it was.</b> The
        /// original reasoning was that <c>SwingToolView</c> stops producing targets when the
        /// player cannot interact, so the bar would gate itself. It does not.
        /// <c>GrabbedItemView.HandleToolInteractorBlockerChanged</c> only calls
        /// <c>ShowUI()</c>/<c>HideUI()</c> — it never clears <c>isEquipped</c>, so
        /// <c>ProcessEquippedUpdate</c> keeps running and <c>Target</c> stays resolved behind an
        /// open menu. The bar then sat on top of the pause screen.
        ///
        /// The gate is the game's own: <c>PlayerToolInteractor.AllowShowingGridCursor</c> is
        /// <c>cursorVisualBlocker.IsFree</c>, and <c>Blocker</c>'s constructor registers itself
        /// by name into a static table — so that is the very same blocker
        /// <c>GrabbedItemView</c> reads via <c>Blocker.Get("PlayerToolInteractor")</c> to decide
        /// whether its own world UI may show. Asking it directly means this mod hides exactly
        /// when the game hides its grid cursor, with no list of screens to keep up to date.
        /// </summary>
        private bool IsSuppressed()
        {
            try
            {
                if (!MonoBehaviourSingleton<PlayerView>.Exists)
                {
                    return true;
                }

                if (Cutscene.IsInCutscene)
                {
                    return Suppress("cutscene");
                }

                var toolInteractor = MonoBehaviourSingleton<PlayerView>.Instance.ToolInteractor;
                if (toolInteractor == null)
                {
                    return true;
                }

                return !toolInteractor.AllowShowingGridCursor && Suppress("the game hid its grid cursor");
            }
            catch (Exception e)
            {
                // Showing a bar that should be hidden is a cosmetic bug; hiding the whole mod on
                // a transient error is a functional one. Prefer the former, but say so once.
                if (!loggedSuppressionFailure)
                {
                    loggedSuppressionFailure = true;
                    LastSwingPlugin.Log.LogWarning(
                        $"Could not read the game's cursor blocker, so the bar cannot hide itself " +
                        $"for menus: {e.Message}");
                }

                return false;
            }
        }

        /// <summary>Logs the first time each reason suppresses the bar, then stays quiet.</summary>
        private bool Suppress(string reason)
        {
            if (loggedSuppressionReason != reason)
            {
                loggedSuppressionReason = reason;
                LastSwingPlugin.Log.LogInfo($"Health bar hidden: {reason}.");
            }

            return true;
        }

        private void Poll()
        {
            var interactable = SwingTarget.Find(out var inRange);

            if (interactable == null || (LastSwingPlugin.RequireInRange.Value && !inRange))
            {
                HandleLoss();
                return;
            }

            var target = DamageReader.Read(interactable);
            if (target == null || !target.IsUsable)
            {
                HandleLoss();
                return;
            }

            pendingDestructibleCheck = target.DestructibleSource;
            TrackArming(target);

            // "Actively attacking" in its strict sense: aiming is not attacking. Note this is
            // deliberately not `target.Damage > 0` - that would light up a tree someone
            // half-chopped three days ago the moment it is aimed at, which is the case the
            // baseline exists to exclude.
            if (LastSwingPlugin.RequireHitFirst.Value && !armed)
            {
                Release();
                return;
            }

            // Note: deliberately not releasing here when target.Remaining <= 0. The object
            // often survives a poll or two past its killing blow - waiting on a destroy
            // animation or a grow-stage check - and if we bail out before drawing, the last
            // frame the player ever sees is the one-hit-remaining state from the previous
            // poll. Letting this draw shows the bar actually reach empty; the next poll picks
            // up the object's disappearance and starts the normal linger/fade from there.

            if (!loggedFirstTarget && LastSwingPlugin.VerboseLogging.Value)
            {
                loggedFirstTarget = true;
                LastSwingPlugin.Log.LogInfo(
                    $"First target: {target.Kind}, damage {target.Damage}/{target.Threshold}, " +
                    $"tool damage {SwingTarget.SwingDamage()}.");
            }

            current = target;
            lastAnchor = target.Anchor;
            hideAtTime = 0f;

            EnsureUi();
            Draw(target, SwingTarget.CanDamage(interactable));
            canvas.gameObject.SetActive(true);
        }

        /// <summary>
        /// Decide whether a hit has landed on the object currently being aimed at.
        ///
        /// Aiming at a new object records what damage it already carries and disarms; damage
        /// rising above that baseline is a hit, and arms the bar for as long as this object
        /// stays the target. Arming survives the linger window, so glancing away mid-chop and
        /// coming straight back does not demand another swing first — but a full hide clears
        /// it, so re-engaging a tree later starts silent again. See <see cref="Clear"/>.
        /// </summary>
        private void TrackArming(DamageReader.Target target)
        {
            if (target.Anchor != armedAnchor)
            {
                armedAnchor = target.Anchor;
                baselineDamage = target.Damage;
                armed = false;
            }

            if (target.Damage > baselineDamage)
            {
                armed = true;
            }
            else if (target.Damage < baselineDamage)
            {
                // Damage went down - the object regenerated, or the guid was recycled onto
                // something else. Either way the old baseline describes a different situation.
                baselineDamage = target.Damage;
            }
        }

        /// <summary>
        /// Entry point for every way <see cref="Poll"/> can lose its target: aimed away, walked
        /// out of range, or the object is simply gone.
        ///
        /// Rocks are gone from <c>SwingTarget.Find</c>'s perspective before the bar ever gets a
        /// live poll at <c>Remaining == 0</c> - see <see cref="pendingDestructibleCheck"/>. If
        /// the object we were just showing turns out to have been destroyed, draw it emptied one
        /// last time before releasing, so the bar visibly reaches zero instead of freezing on
        /// whatever hit count was left over from the last real poll.
        /// </summary>
        private void HandleLoss()
        {
            if (current != null && pendingDestructibleCheck != null && pendingDestructibleCheck.IsDestructed)
            {
                DrawEmptied(current);
            }

            pendingDestructibleCheck = null;
            Release();
        }

        /// <summary>
        /// Redraws <paramref name="previous"/> as fully spent, ignoring whatever damage value it
        /// actually carried - the point is showing the bar reach zero, not reproducing the exact
        /// number that never got read live.
        /// </summary>
        private void DrawEmptied(DamageReader.Target previous)
        {
            var synthetic = new DamageReader.Target
            {
                Anchor = previous.Anchor,
                Damage = previous.Threshold,
                Threshold = previous.Threshold,
                Kind = previous.Kind,
            };

            EnsureUi();
            Draw(synthetic, canDamage: true);
            canvas.gameObject.SetActive(true);
            lastAnchor = previous.Anchor;
        }

        /// <summary>
        /// Start the linger timer rather than hiding immediately.
        ///
        /// The swing target is recomputed every frame from a weighted angle-and-distance scan,
        /// so it flickers to null for a frame or two as the player turns mid-swing. Hiding on
        /// the first miss makes the bar strobe.
        /// </summary>
        private void Release()
        {
            current = null;

            if (canvas == null || !canvas.gameObject.activeSelf)
            {
                return;
            }

            var linger = Mathf.Max(0f, LastSwingPlugin.LingerSeconds.Value);
            if (linger <= 0f)
            {
                Clear();
                return;
            }

            if (hideAtTime <= 0f)
            {
                hideAtTime = Time.unscaledTime + linger;
            }
        }

        private void Clear()
        {
            current = null;
            lastAnchor = null;
            hideAtTime = 0f;

            // Fully disengaged, so the next approach to this same tree starts silent and needs
            // a fresh swing. Only Clear does this - Release keeps the arming alive through the
            // linger window.
            armedAnchor = null;
            armed = false;

            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
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

        private void Reposition()
        {
            if (canvas == null || !canvas.gameObject.activeSelf)
            {
                return;
            }

            if (hideAtTime > 0f)
            {
                var remaining = hideAtTime - Time.unscaledTime;
                if (remaining <= 0f)
                {
                    Clear();
                    return;
                }

                // Fade over the last half of the linger, so a brief target flicker does not
                // show as a visible dip.
                var linger = Mathf.Max(0.001f, LastSwingPlugin.LingerSeconds.Value);
                group.alpha = Mathf.Clamp01(remaining / (linger * 0.5f));
            }
            else
            {
                group.alpha = 1f;
            }

            var anchor = current != null ? current.Anchor : lastAnchor;
            if (anchor == null)
            {
                Clear();
                return;
            }

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

            var world = anchor.position + Vector3.up * LastSwingPlugin.WorldHeight.Value;
            var screenPoint = camera.WorldToScreenPoint(world);

            if (screenPoint.z < 0f)
            {
                // Behind the camera.
                group.alpha = 0f;
                return;
            }

            plateRect.position = screenPoint;
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

        private void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("LastSwing_BarCanvas");
            canvasGo.transform.SetParent(transform, false);
            DontDestroyOnLoad(canvasGo);

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
