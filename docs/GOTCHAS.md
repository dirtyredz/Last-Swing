# GOTCHAS — Last Swing

Non-obvious traps, most of them hard-won and written up at length in [README.md](../README.md).
Map form here so a future session sees them before stepping on one.

- **`TryGetByGuid`, never `FindOrCreate`.** The game's `FindOrCreate` *writes* a damage record for
  any object it's asked about — a mod that inspects whatever the axe points at would seed a record
  for every tree walked past. This mod goes further and never queries the collection at all; it
  reads the `DamagePersistence` the component already holds. The *save-safe* claim is verified by a
  before/after save diff, not just source audit. (`DamageReader.cs`)

- **`ChopTreeGridComponent` dies on `Damage > health`, not `>=`.** The other three components use
  `IsDead`'s `>=`. A health-5 tree survives damage 5 and falls on the 6th point, so its threshold is
  `healthTree + 1`. Using `healthTree` empties the bar a full swing early — at the exact moment the
  player is watching. Nothing in the method name says so; only the body. (`DamageReader.ReadChopTree`)

- **A full tree has *two* felling gates.** `Chop()` doesn't destroy the tree; it dispatches
  `RequestGrowstageCheck`, and the stump transition is gated by a `DamageTakenRequirement`. Both
  must pass, so the real threshold is `max(healthTree + 1, ceil(DamageAmount))`. Read the
  requirement's fields directly — `IsRequirementCompleted` uses `FindOrCreate` and would write.

- **Rocks vanish before a live poll ever sees `Remaining == 0`.** `DestructibleView.Hit()` flips
  `IsDestructed` *and* unregisters the object from the grid in the same call, so by the next 50 ms
  poll `SwingTarget.Find` returns null and `DamageReader.Read` is never reached. The mod keeps the
  `DestructibleView` reference directly (`pendingDestructibleCheck`) so it can still read
  `IsDestructed` after the object leaves the grid and draw one honest empty frame. Trees don't need
  this; their grid component stays resolvable a poll or two longer. (`HealthBar.HandleLoss`)

- **The swing view does *not* gate itself for menus — the first build assumed it did and shipped a
  bar sitting on the pause screen.** `GrabbedItemView.HandleToolInteractorBlockerChanged` only calls
  `ShowUI()`/`HideUI()`; it never clears `isEquipped`, so `SwingToolView` keeps resolving a target
  behind an open menu. Gate on `PlayerToolInteractor.AllowShowingGridCursor` (the same `Blocker`
  instance the game reads) + `Cutscene.IsInCutscene`, checked **every frame** (not on the poll
  interval — a menu must take the bar instantly). **Lesson: a component *reacting* to a blocker is
  not the same as it going quiet.** (`HealthBar.IsSuppressed`)

- **`SwingToolView.TargetIsInRange` goes stale when `Target` is null.** `SetTarget` clears `Target`
  on the null path but leaves the range flag holding its old value. Read the flag only when `Target`
  is non-null. (`SwingTarget.Find`)

- **`Camera.main` is null in this game.** The gameplay camera isn't tagged `MainCamera`
  (normal for Cinemachine). Fall back to the highest-depth active on-screen camera in
  `Camera.allCameras`. (`HealthBarView.ResolveCamera`)

- **Reflection health fields, logged once.** Three of the four health values are private
  `ItemParameterRef<int>`; a cached `FieldInfo` per type reaches them (`GetValue` itself is public
  API). Don't hardcode 3/5 — items can override health via `ItemParametersAddon`. On failure the mod
  logs **once** and falls back, rather than flooding the log at the poll rate.

- **`GamePalette.cs` / `GameFonts.cs` are verbatim copies** shared with Chest Labels and Plant Peek.
  Fix bugs in *all* copies (via `../../tools/sync-mod-files.ps1`), not just here.

- **`Directory.Build.props` / `pack.ps1` are workspace-synced canonicals** — don't hand-edit them in
  this repo; changes get overwritten by the sync tool.
