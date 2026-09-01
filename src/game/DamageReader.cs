using System;
using System.Reflection;
using UnityEngine;

namespace LastSwing
{
    /// <summary>
    /// Reads how much damage a world object has taken and how much it can take.
    ///
    /// <b>Strictly read-only.</b> Every lookup below uses <c>TryGetByGuid</c> and never
    /// <c>FindOrCreate</c>. The game's own <c>FindOrCreate</c> writes a damage record into the
    /// save for any object it is asked about — which for a mod that inspects whatever the axe
    /// happens to point at would mean seeding a record for every tree the player walks past.
    /// Plant Peek established this rule; see GrowthReader.ReadChoppedPercent.
    ///
    /// There are four damageable component types and they share no interface. Each keeps its
    /// maximum somewhere different, and one of them counts differently from the other three —
    /// see <see cref="Read"/>.
    /// </summary>
    internal static class DamageReader
    {
        internal sealed class Target
        {
            /// <summary>What the bar hangs above.</summary>
            internal Transform Anchor;

            /// <summary>Damage already taken.</summary>
            internal int Damage;

            /// <summary>Damage value at which the object is destroyed.</summary>
            internal int Threshold;

            /// <summary>Component type name, for logging only.</summary>
            internal string Kind;

            /// <summary>
            /// Set only when <see cref="Kind"/> is <c>DestructibleView</c> (rocks/ore). Lets the
            /// caller keep checking <c>IsDestructed</c> directly by reference after the object
            /// has dropped off the grid and <c>SwingTarget.Find</c> can no longer see it - see
            /// the note on <c>DamageReader.ReadDestructible</c>.
            /// </summary>
            internal DestructibleView DestructibleSource;

            /// <summary>Damage still to be dealt. Never negative.</summary>
            internal int Remaining => Mathf.Max(0, Threshold - Damage);

            internal bool IsUsable => Anchor != null && Threshold > 0;
        }

        // One FieldInfo per type, resolved on first use. The fields are private, but the
        // ItemParameterRef<int> they hold has a public GetValue - so a single reflection hop
        // gets us onto supported API for the part that actually matters (per-item overrides).
        private static FieldInfo chopTreeHealth;
        private static FieldInfo destructableTreeHealth;
        private static FieldInfo stumpHealth;
        private static bool loggedReflectionFailure;

        /// <summary>
        /// Resolve the interactable the player's tool is aimed at into damage numbers, or null
        /// if it is not something that takes damage.
        /// </summary>
        internal static Target Read(IInteractable interactable)
        {
            // Every IInteractable in the game derives from Interactable : MonoBehaviour.
            if (!(interactable is Component component) || component == null)
            {
                return null;
            }

            // Order matters only for speed; nothing carries two of these.
            //
            // TreeChopInteractable does GetComponent<ITreeGridComponent>() on itself, so the
            // grid component is a sibling. DestructibleHitInteractable does
            // GetComponentInParent<DestructibleView>(). Search self, then parents, then
            // children, so both layouts resolve without caring which is which.
            var chopTree = Find<ChopTreeGridComponent>(component);
            if (chopTree != null)
            {
                return ReadChopTree(chopTree);
            }

            var smallTree = Find<DestructableTreeGridComponent>(component);
            if (smallTree != null)
            {
                return new Target
                {
                    Anchor = smallTree.transform,
                    Damage = DamageOf(smallTree.DamagePersistence),
                    // Chop() ends with IsDead(healthTree), i.e. Damage >= health.
                    Threshold = ReadParameter(smallTree, ref destructableTreeHealth,
                        typeof(DestructableTreeGridComponent), "healthTree", 3),
                    Kind = nameof(DestructableTreeGridComponent),
                };
            }

            var stump = Find<ChopStumpGridComponent>(component);
            if (stump != null)
            {
                return new Target
                {
                    Anchor = stump.transform,
                    Damage = DamageOf(stump.DamagePersistence),
                    Threshold = ReadParameter(stump, ref stumpHealth,
                        typeof(ChopStumpGridComponent), "healthStump", 3),
                    Kind = nameof(ChopStumpGridComponent),
                };
            }

            var destructible = Find<DestructibleView>(component);
            if (destructible != null)
            {
                return ReadDestructible(destructible);
            }

            return null;
        }

        /// <summary>
        /// Rocks, ore and the rest of the breakables. The only one of the four whose maximum is
        /// public: <c>ItemAsset.DestructibleAddon.TotalHealthPoints</c>.
        /// </summary>
        private static Target ReadDestructible(DestructibleView view)
        {
            // Deliberately not gating on view.IsDestructed. Decompiling DestructibleView.Hit()
            // shows it flips IsDestructed to true synchronously, in the same call that applies
            // the killing blow's damage - so a check here would throw the Remaining-hits-0
            // frame away before the bar ever got to draw it, on every rock and ore node.
            // GridObjectPersistence is not cleared until the separate Delete() call, which
            // happens later once the object actually leaves the grid, so reading through it
            // here is still safe and gives the bar one real empty frame.
            var addon = view.GridObjectPersistence?.ItemAsset?.DestructibleAddon;
            if (addon == null)
            {
                return null;
            }

            return new Target
            {
                Anchor = view.transform,
                Damage = DamageOf(view.DamagePersistence),
                // Hit() tests IsAlive/IsDead, so it dies on Damage >= TotalHealthPoints.
                Threshold = addon.TotalHealthPoints,
                Kind = nameof(DestructibleView),
                DestructibleSource = view,
            };
        }

        /// <summary>
        /// Full trees, and the one case that does not follow <c>DamagePersistence.IsDead</c>.
        ///
        /// <b>Two separate gates have to pass before a full tree comes down, and the bar has to
        /// respect the later of them.</b>
        ///
        /// 1. <c>ChopTreeGridComponent.Chop()</c> tests <c>Damage &gt; healthTree</c> —
        ///    <i>strictly</i> greater, where the other three components use <c>IsDead</c>'s
        ///    <c>&gt;=</c>. A health-5 tree survives damage 5 and only falls on the sixth
        ///    point, so the threshold is <c>healthTree + 1</c>. Using <c>healthTree</c> would
        ///    empty the bar a whole swing early, at exactly the moment the player is watching
        ///    it.
        /// 2. Chop() does not destroy the tree itself; it dispatches RequestGrowstageCheck, and
        ///    the transition to the stump stage is gated by a <c>DamageTakenRequirement</c>
        ///    (<c>DamageValue &gt;= DamageAmount</c>) hanging off one of the current stage's
        ///    grow paths.
        ///
        /// Both must hold, so the true felling threshold is the larger of the two.
        /// </summary>
        private static Target ReadChopTree(ChopTreeGridComponent tree)
        {
            var health = ReadParameter(tree, ref chopTreeHealth,
                typeof(ChopTreeGridComponent), "healthTree", 5);

            var threshold = health + 1;

            var required = ReadStageDamageRequirement(tree);
            if (required > threshold)
            {
                threshold = required;
            }

            return new Target
            {
                Anchor = tree.transform,
                Damage = DamageOf(tree.DamagePersistence),
                Threshold = threshold,
                Kind = nameof(ChopTreeGridComponent),
            };
        }

        /// <summary>
        /// The damage the current grow stage wants before it will turn into a stump, or 0 if
        /// the stage has no damage-gated path.
        ///
        /// Read off the requirement rather than by calling
        /// <c>DamageTakenRequirement.IsRequirementCompleted</c>, which uses FindOrCreate and
        /// would write to the save. Both halves are public: the requirement's DamageAmount and
        /// DamagePersistence.DamageValue.
        /// </summary>
        private static int ReadStageDamageRequirement(Component component)
        {
            try
            {
                var view = component.GetComponentInParent<GrowableView>();
                var grid = view?.GridObjectPersistence;
                var growable = view?.GrowablePersistence;
                if (grid == null || growable == null)
                {
                    return 0;
                }

                var stages = grid.ItemAsset?.GrowableAddon?.GrowStageContainer?.CachedGrowStages;
                if (stages == null || string.IsNullOrEmpty(growable.GrowStageGuid))
                {
                    return 0;
                }

                GrowStage current = null;
                for (var i = 0; i < stages.Count; i++)
                {
                    if (stages[i] != null && stages[i].Guid == growable.GrowStageGuid)
                    {
                        current = stages[i];
                        break;
                    }
                }

                if (current == null)
                {
                    return 0;
                }

                foreach (var path in current.GrowPaths)
                {
                    var requirement = path == null ? null : path.GetComponent<DamageTakenRequirement>();
                    if (requirement == null)
                    {
                        continue;
                    }

                    var amount = requirement.DamageAmount.GetValue(requirement, throwError: false);
                    // DamageAmount is a float and the comparison is >=, so a fractional
                    // requirement is met by the first integer at or above it.
                    return Mathf.CeilToInt(amount);
                }
            }
            catch (Exception e)
            {
                LastSwingPlugin.Log.LogWarning(
                    $"Could not read a tree's grow-stage damage requirement: {e.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Search self, then parents, then children. The four component types sit at different
        /// depths relative to their interactable and this saves caring which.
        /// </summary>
        private static T Find<T>(Component from) where T : Component
        {
            var found = from.GetComponent<T>();
            if (found != null)
            {
                return found;
            }

            found = from.GetComponentInParent<T>();
            return found != null ? found : from.GetComponentInChildren<T>();
        }

        /// <summary>
        /// No damage record simply means nothing has hit it yet — that is 0, not an error.
        /// </summary>
        private static int DamageOf(DamagePersistence persistence)
        {
            return persistence == null ? 0 : Mathf.Max(0, persistence.DamageValue);
        }

        /// <summary>
        /// Read one of the private <c>ItemParameterRef&lt;int&gt;</c> health fields.
        ///
        /// The reflection is only to reach the field; <c>GetValue</c> on the reference itself is
        /// public API. Going through it rather than hardcoding 3 and 5 is what makes the bar
        /// correct for items that override their health through ItemParametersAddon — including
        /// anything another mod adds.
        /// </summary>
        private static int ReadParameter(
            Component owner, ref FieldInfo cached, Type type, string fieldName, int fallback)
        {
            try
            {
                if (cached == null)
                {
                    cached = type.GetField(
                        fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

                    if (cached == null)
                    {
                        WarnOnce($"{type.Name}.{fieldName} not found");
                        return fallback;
                    }
                }

                if (cached.GetValue(owner) is ItemParameterRef<int> reference)
                {
                    var value = reference.GetValue(owner, throwError: false);
                    return value > 0 ? value : fallback;
                }
            }
            catch (Exception e)
            {
                WarnOnce($"{type.Name}.{fieldName} unreadable: {e.Message}");
            }

            return fallback;
        }

        /// <summary>
        /// One warning, not one per frame. A missing field would otherwise flood the log at the
        /// poll rate for as long as the player keeps swinging.
        /// </summary>
        private static void WarnOnce(string message)
        {
            if (loggedReflectionFailure)
            {
                return;
            }

            loggedReflectionFailure = true;
            LastSwingPlugin.Log.LogWarning(
                $"Falling back to the game's default health values: {message}. " +
                "Bars will still show, but may be wrong for items that override their health.");
        }
    }
}
