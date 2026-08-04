using System;
using Chicken.Utilities;
using UnityEngine;

namespace LastSwing
{
    /// <summary>
    /// What the equipped axe or pickaxe is aimed at right now.
    ///
    /// <b>This is the whole "only while actively attacking" gate, and the game hands it over
    /// public.</b> <c>SwingToolView</c> recomputes its target every frame in
    /// <c>ProcessEquippedUpdate</c> and exposes <c>Target</c> and <c>TargetIsInRange</c> — the
    /// same target the game is already drawing its grid cursor on. No raycast, no hover system
    /// and no Harmony patch, which is a sharp contrast with Chest Labels and Plant Peek, both
    /// of which had to build world hover from scratch.
    ///
    /// It also carries the gating for free. The view only exists while a swing tool is
    /// equipped, and <c>GrabbedItemView</c> stops updating when the "PlayerToolInteractor"
    /// blocker is set — which is what menus, cutscenes and confinement do. So the bar cannot
    /// linger over a menu the way a self-drawn overlay can.
    /// </summary>
    internal static class SwingTarget
    {
        private static SwingToolView cachedView;

        /// <summary>
        /// The current swing-tool target, or null when nothing is aimed at. <paramref
        /// name="inRange"/> is only meaningful when the return value is non-null.
        /// </summary>
        internal static IInteractable Find(out bool inRange)
        {
            inRange = false;

            var view = ResolveView();
            if (view == null)
            {
                return null;
            }

            var target = view.Target;
            if (target == null || target.IsDestroyed)
            {
                return null;
            }

            // Read only when there is a target. SwingToolView.SetTarget clears Target on the
            // null path but leaves TargetIsInRange holding its previous value, so this flag is
            // stale whenever Target is null.
            inRange = view.TargetIsInRange;
            return target;
        }

        /// <summary>
        /// The equipped tool's damage per swing, or 0 when nothing damaging is held.
        /// </summary>
        internal static int SwingDamage()
        {
            try
            {
                var addon = GameInventory.GrabbedToolAddon;
                return addon == null ? 0 : addon.Damage;
            }
            catch (Exception)
            {
                // GrabbedToolAddon walks GamePersistence, which is null before a save loads.
                return 0;
            }
        }

        /// <summary>
        /// Whether the equipped tool can hurt this target at all.
        ///
        /// Mirrors the test in <c>InteractionSwingToolState.HandleInteraction</c>: fail either
        /// half and the game calls ResolveFailedInteraction and shouts at the player *after*
        /// they have swung. Checking it here is what lets the bar say so first.
        /// </summary>
        internal static bool CanDamage(IInteractable interactable)
        {
            try
            {
                var addon = GameInventory.GrabbedToolAddon;
                if (addon == null)
                {
                    return false;
                }

                if (interactable.InteractionToolDamageRequired > addon.Damage)
                {
                    return false;
                }

                var requirements = interactable.ToolRequirements;
                if (requirements == null || requirements.Count == 0)
                {
                    // No stated requirement is not a failed requirement - the game's own test
                    // is All(...), which is vacuously true on an empty list, so an empty list
                    // means "anything goes".
                    return true;
                }

                // Deliberately not FitsRequirement(ToolAsset): that wants the SwingToolAsset,
                // and reaching it means another private hop. The damage test above is the half
                // that actually fires in play - a pickaxe pointed at a tree is not offered as a
                // target in the first place, because TryFindInteractable filters by tool type.
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// The equipped SwingToolView, if a swing tool is equipped at all.
        ///
        /// PlayerEquipment.GrabbedItemView is the ItemView of whatever is in hand; the tool
        /// behaviour is a component on the same object. It is replaced whenever the player
        /// switches tools, so the cache is validated rather than trusted.
        /// </summary>
        private static SwingToolView ResolveView()
        {
            if (cachedView != null && cachedView.isActiveAndEnabled)
            {
                return cachedView;
            }

            cachedView = null;

            try
            {
                if (!MonoBehaviourSingleton<PlayerView>.Exists)
                {
                    return null;
                }

                var equipment = MonoBehaviourSingleton<PlayerView>.Instance.Equipment;
                var held = equipment == null ? null : equipment.GrabbedItemView;
                if (held == null)
                {
                    return null;
                }

                cachedView = held.GetComponent<SwingToolView>()
                             ?? held.GetComponentInChildren<SwingToolView>();
            }
            catch (Exception e)
            {
                LastSwingPlugin.Log.LogWarning($"Could not reach the equipped tool: {e.Message}");
            }

            return cachedView;
        }
    }
}
