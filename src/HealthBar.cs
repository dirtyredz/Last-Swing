using System;
using Chicken.Utilities;
using UnityEngine;

namespace LastSwing
{
    /// <summary>
    /// The bar's controller: the per-frame loop, the "only while actively attacking" gate, the
    /// arming state machine, and the linger/fade timing. Owns no GameObjects — it decides
    /// <i>when</i> the bar shows, what it shows, and where it sits, and hands each of those to
    /// <see cref="HealthBarView"/>, which owns the <i>how</i> (canvas, drawing, projection).
    /// </summary>
    internal sealed class HealthBar : MonoBehaviour
    {
        private const float PollInterval = 0.05f;

        private readonly HealthBarView view = new HealthBarView();

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

            view.Show(transform, target, SwingTarget.CanDamage(interactable));
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

            view.Show(transform, synthetic, canDamage: true);
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

            if (!view.IsActive)
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

            view.Hide();
        }

        private void Reposition()
        {
            if (!view.IsActive)
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
                view.SetAlpha(Mathf.Clamp01(remaining / (linger * 0.5f)));
            }
            else
            {
                view.SetAlpha(1f);
            }

            var anchor = current != null ? current.Anchor : lastAnchor;
            if (anchor == null)
            {
                Clear();
                return;
            }

            var world = anchor.position + Vector3.up * LastSwingPlugin.WorldHeight.Value;
            view.PositionAt(world);
        }
    }
}
