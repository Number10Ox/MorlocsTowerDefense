# Project Status

## TDD Progress

| Section | Status | Notes |
|---------|--------|-------|
| 1. Core Functionality | Complete | All key systems documented |
| 2. Architecture - Guiding Principles | Complete | 6 principles established |
| 2. Architecture - Detailed Design | In progress | Composition root, state machine, system scheduler, store pattern, economy, wave system documented |
| 3. Constraints & Ground Rules | Complete | 9 constraints from spec + tool constraint |
| 4. Tech Package Choices | Complete | Input System, UI Toolkit, UGUI (provided popups), Addressables (SO data), Cinemachine dropped |
| 5. Data Configuration Strategy | Complete | CreepTypeDefinition/CreepDefinitions SO, BaseConfig, EconomyConfig, TurretDefinitions SOs; WaveConfig SO with WaveDefinition/WaveEntryDefinition structs (Story 9). SpawnConfig retired. |
| 6. Provided Assets Reference | Complete | All prefabs, scene, terrain, materials cataloged |
| 7. Deliverables - User Stories | Complete | Stories 1-10 with acceptance criteria |

## Implementation Progress

| Story | Status | Notes |
|-------|--------|-------|
| Story 1: Project Foundation | Complete | GameFlowController (composition root), state machine, system scheduler, folder structure, test infrastructure |
| Story 2: Creep Spawning & Movement | Complete | CreepStore, SpawnSystem, MovementSystem, object pooling, PresentationAdapter sync |
| Story 3: Base Health & Lose Condition | Complete | BaseStore, DamageSystem, LoseState, BaseConfig SO, BaseHealthHud (UI Toolkit), health HUD event-driven |
| Story 4: Turret Placement | Complete | TurretStore (minimal, no removal pipeline), PlacementSystem, PlacementInput bridge, TurretComponent, PresentationAdapter input collection (raycast) + turret visual sync, turret pool |
| Story 5: Turret Shooting & Creep Damage | Complete | ProjectileSystem (inline targeting), DamageSystem extended with projectile hits + OnCreepKilled, homing projectiles, dead-creep guards, TurretTypeDefinition data, ProjectileStore |
| Story 6: Economy System | Complete | EconomyStore, EconomySystem (Phase 3), EconomyConfig SO, CoinHud (UI Toolkit), PlacementSystem affordability gate, DamageSystem OnCreepKilled carries reward, CoinReward on CreepSimData/CreepDef, cost on TurretTypeDefinition |
| Story 7: Turret Types (Regular & Freezing) | Complete | TurretType enum, TurretTypeStats struct (with Type field), TurretSelectionStore (defaultType constructor), TurretSelectionHud (dynamic generation from TurretTypeStats[]), slow effect system (DamageSystem writes, MovementSystem reads), per-type costs via IReadOnlyDictionary, data-driven keyboard selection (1-9), dictionary-based turret pools. Refactored to data-driven: TurretDefinitions SO + TurretTypeDirectoryBuilder/TurretTypeDirectory. Adding a new turret type requires zero code changes. PresentationAdapter refactored to use TurretVisual struct dictionary instead of separate turretMap/turretSourcePool. TurretDefinitions.OnValidate detects duplicate TurretType entries. |
| Story 8: Creep Variety | Complete | CreepType enum, CreepTypeStats struct, CreepTypeDefinition struct, CreepDefinitions SO, CreepTypeDirectoryBuilder/CreepTypeDirectory (mirrors turret pattern), SpawnSystem round-robin per-creep cycling, PresentationAdapter per-type creep pools with CreepVisual struct, Type field on CreepSimData. Old CreepDef SO deleted. |
| Story 9: Wave System | Complete | WaveSystem (wave progression, phase machine), WaveStore (spawn queue + wave state), WaveConfig SO (WaveDefinition/WaveEntryDefinition structs), SpawnSystem refactored to queue consumer, WinState, PlayingState dual end-condition polling (lose priority), GameUiCoordinator win popup, pool sizing derived from wave data. SpawnConfig retired. |
| Story 10: Game Reset | Complete | State machine trigger-during-Enter bug fix, RestartRequested input via PlacementInput bridge, R key in PresentationAdapter, restart transitions (Win/Lose → Init), reset handler (visuals → stores → systems → UI refresh), RestartHintHud (UI Toolkit), GameUiCoordinator restart hint visibility |

## Key Decisions Made

- UI Toolkit for new UI only; provided UGUI popups (WinPopup, LosePopup) used as-is
- Cinemachine dropped (no camera work needed)
- Addressables used for ScriptableObject tuning data (extensibility); visual prefabs via direct serialized field references
- Coding style and testing strategy in `.claude/CLAUDE.md` (reusable across projects)
- Architectural preferences also in `.claude/CLAUDE.md`; TDD section 2 captures project-specific guiding principles
- No LINQ in runtime code (global rule)
- Custom state machine (no Stateless library)
- **Store pattern for simulation data**: Data lives in stores (e.g., `CreepStore`), not in systems. Systems read/write stores via their public API. `GameSession` owns all stores and manages per-frame lifecycle (`BeginFrame()` flushes deferred removals, clears per-frame change lists).
- **No system-to-system dependencies**: Systems depend on stores, not on each other. Eliminates coupling chains.
- **Buffered change lists**: Stores expose `SpawnedThisFrame` and `RemovedIdsThisFrame` instead of inter-system events for presentation sync.
- **Object pooling**: `ObjectPooling` namespace with `IPoolable` interface and `GameObjectPool`. Position set before activation to avoid visual pop.
- **Systems take primitives**: Systems receive plain values (float, int, Vector3) from SOs at construction — not SO references. Keeps systems purely testable.
- **BaseStore for base health**: `BaseStore` follows the same pattern as `CreepStore` (BeginFrame/Reset, owned by GameSession). `DamageSystem` is the single writer. `ApplyDamage` is idempotent after destruction (no event re-fire).
- **One-shot base damage guard**: `CreepSimData.HasDealtBaseDamage` prevents double-damage if a creep survives multiple ticks due to deferred removal. `DamageSystem` gates on `ReachedBase && !HasDealtBaseDamage`.
- **Per-frame damage tracking**: `BaseStore.DamageTakenThisFrame` justifies the `BeginFrame()` API and enables UI effects/test assertions.
- **PlayingState polls for end conditions**: Event handler discipline forbids firing game triggers from event handlers. `PlayingState.Tick()` polls `baseStore.IsDestroyed` instead.
- **HUD event-driven**: `BaseHealthHud` updates via `BaseStore.OnBaseHealthChanged` event (pure presentation, no mutation).
- **LosePopup via GameUiCoordinator**: `GameUiCoordinator.OnStateChanged` instantiates the LosePopup from a prefab reference on enter, destroys it on exit. Presentation concern only.
- **PlacementInput as shared bridge**: `PlacementInput` class created by `GameFlowController` and passed to both `PresentationAdapter` (writer in `CollectInput`) and `PlacementSystem` (reader in `Tick`). Neither depends on the other. Consume-and-clear pattern: `PlacementSystem` clears input after consuming to prevent double-placement.
- **TurretStore minimal for Story 4**: No removal pipeline (`MarkForRemoval`, `RemovedIdsThisFrame`). Only `ActiveTurrets`, `PlacedThisFrame`, `BeginFrame` (clears placed list), `Reset`. Removal deferred to a story that needs turret destruction/selling.
- **Terrain raycast via LayerMask**: `GameFlowController` exposes `LayerMask terrainLayerMask` serialized field, passed to `PresentationAdapter`. `CollectInput()` raycasts against this layer using Input System (`Mouse.current`).
- **No TargetingSystem**: Target selection merged into `ProjectileSystem` at fire time. Eliminates a whole system class and cross-system coupling field (`TargetCreepId` on turret). Targeting is ephemeral — nearest alive creep in range is found at the moment of firing.
- **Homing projectiles**: Projectiles track their target's current position each frame. If target dies or is removed before impact, projectile is discarded. Hit = distance < threshold or overshoot.
- **Hit recording via store**: `ProjectileStore.HitsThisFrame` (list of `ProjectileHit` structs) bridges `ProjectileSystem` → `DamageSystem`. DamageSystem remains the single writer for creep health.
- **Dead-creep guards**: `MovementSystem` and `DamageSystem.ProcessBaseDamage` skip creeps with `Health <= 0`. Prevents dead creeps from moving or dealing base damage after being killed by projectiles.
- **OnCreepKilled event with reward**: `DamageSystem` fires `event Action<int, int>` (creepId, coinReward) on creep death. Reward passed through event so EconomySystem doesn't need CreepStore dependency.
- **FireInterval naming**: Consistent use of `FireInterval` (seconds between shots) across all code and data. No `FireRate`.
- **TurretTypeDefinition data**: Turret stats (damage, range, fireInterval, projectileSpeed) defined as `TurretTypeDefinition` structs inside `TurretDefinitions` SO. Systems receive primitives at bootstrap, never SO references.
- **ProjectileStore with deferred removal**: Mirrors CreepStore pattern — `Add`, `MarkForRemoval`, `BeginFrame` (flush + clear frame lists). Plus `HitsThisFrame` for cross-system hit communication.
- **GameBootstrap renamed to GameFlowController**: Composition root + game loop pump only. Class suffix taxonomy established (Controller, Coordinator, Adapter, System, Store, State).
- **GameUiCoordinator extracted**: Presentation state decisions (popup lifecycle, HUD visibility, health forwarding) extracted from GameFlowController into `GameUiCoordinator`. Subscribes to state machine and store events. No simulation writes. Idempotent Teardown.
- **Folder restructure: domain-based → role-based**: Reorganized from feature folders (Core/, Creeps/, Turrets/, Combat/) to role-based folders (App/, Framework/, States/, Stores/, SimData/, Systems/, Components/, Input/, Presentation/, Data/). All within single `Game.asmdef`.

- **EconomyStore**: Authoritative store for coin balance. Single writer: `EconomySystem`. Constructor accepts `startingCoins >= 0`. `TrySpendCoins` for atomic check-and-deduct. `CanAfford` read-only check used by `PlacementSystem`. `BeginFrame/Reset` lifecycle matches other stores.
- **EconomySystem (Phase 3)**: Subscribes to `DamageSystem.OnCreepKilled`, buffers rewards locally (handler discipline), applies during `Tick()`. Reads `TurretStore.PlacedThisFrame` to deduct turret costs. No CreepStore dependency.
- **PlacementSystem affordability gate**: `PlacementSystem` now reads `EconomyStore.CanAfford()` before placing. Clears input on insufficient coins. Never writes to EconomyStore.
- **CoinReward per creep**: `CreepSimData.CoinReward` set by `SpawnSystem` at creation from `CreepTypeStats.CoinReward`. Follows `DamageToBase` precedent.
- **CoinHud**: Stateless UI Toolkit view. Shares UIDocument with `BaseHealthHud`. `GameUiCoordinator` forwards `EconomyStore.OnCoinsChanged` to `CoinHud.UpdateCoins`.
- **EconomyConfig SO**: ScriptableObject for `startingCoins` tuning. Assigned to `GameFlowController` in Inspector.
- **TurretTypeDefinition.Cost**: Per-turret-type cost field. Passed to `PlacementSystem` and `EconomySystem` as primitive.
- **Coin-timing contract (next-frame spendability)**: Kill rewards are applied in Phase 3 (`EconomySystem.Tick`). `PlacementSystem` checks `CanAfford()` in Phase 1 using the balance from the prior frame's resolution. Coins earned this frame are not spendable until next frame. This is consistent with the deferred-removal contract used throughout the project: the frame boundary is the commit point.
- **One placement per frame invariant**: `PresentationAdapter.CollectInput()` clears `PlacementInput` at the start of each frame, and `PlacementSystem` clears it after consuming. At most one placement request can exist per frame. If multi-placement is needed in the future, gate against projected spend (`cost * requestCount`).

- **TurretSelectionStore (not PlacementInput)**: Turret type selection persists across frames and lives in `TurretSelectionStore` (proper Store pattern). Constructor takes `TurretType defaultType` — no hardcoded value. `PlacementInput` remains strictly ephemeral (PlaceRequested, WorldPosition, Clear). Writer: `PresentationAdapter` (data-driven keyboard shortcuts 1-9 from `turretTypeOrder`). Readers: `PlacementSystem`, `GameUiCoordinator`.
- **Data-driven turret types**: Single `TurretDefinitions` ScriptableObject contains an ordered array of `TurretTypeDefinition` structs (each with turretType, prefab, stats, slow params). `TurretTypeDirectoryBuilder.TryBuild()` validates entries and produces a `TurretTypeDirectory` (immutable result object with `OrderedTypes`, `OrderedStats`, `StatsByType`, `PrefabsByType`, `DefaultType`). Adding a new turret type requires: enum value + definitions entry + prefab — zero system/presentation code changes.
- **TurretTypeStats as single source of truth**: Readonly struct with `Type` field, built by `TurretTypeDirectoryBuilder.TryBuild()` from `TurretDefinitions` entries. Passed as `IReadOnlyDictionary<TurretType, TurretTypeStats>` to `PlacementSystem`, `EconomySystem`. Passed as `TurretTypeStats[]` to `TurretSelectionHud`. Eliminates cost duplication and type-branching across systems.
- **TurretTypeDirectoryBuilder (pure C# builder)**: Static helper validates definitions entries (null prefabs, duplicate types, suspicious slow configs) and builds a `TurretTypeDirectory` (immutable result object). `TryBuild()` returns `TurretTypeDirectory` via single `out` parameter instead of multiple `out` parameters. Keeps `GameFlowController.Awake()` clean. Unit-testable without Unity runtime.
- **Slow effect via single-writer discipline**: `DamageSystem` writes `CreepSimData.SlowRemainingTime` and `SlowMultiplier`. `MovementSystem` reads them. Slow data propagates through the system: `TurretTypeDefinition` → `TurretTypeStats` → `TurretSimData` → `ProjectileSimData` → `ProjectileHit` → `CreepSimData`.
- **Slow timer semantics**: Effective slow duration is `[duration, duration + dt]` due to Phase 1/Phase 2 ordering. Acceptable for tower defense. Tests use tolerance.
- **Dictionary-based turret pools**: `PresentationAdapter` manages `IReadOnlyDictionary<TurretType, GameObjectPool> turretPoolByType`. Pools built by `GameFlowController` from `PrefabsByType`. Uses `TurretVisual` struct dictionary (`turretVisuals`) to track each turret's GameObject and source pool together, replacing separate `turretMap` and `turretSourcePool` dictionaries. Data-driven — new types get pools automatically.
- **Per-type costs via dictionary**: `EconomySystem` reads `turret.Type` and looks up `statsByType[type].Cost`. `PlacementSystem` reads `selectionStore.SelectedType` to pick matching `TurretTypeStats` via dictionary lookup. No switch statements on TurretType.
- **TurretSelectionHud (dynamic generation)**: Builds UI elements dynamically from `TurretTypeStats[]`. Accepts `TurretType initialSelection`. Rebuilds into inner `turret-options` UXML container. Labels include keyboard shortcut and cost. USS class toggle (`turret-option--selected`) for highlighting.
- **Ordering contract**: Array order in `TurretDefinitions` determines default type (`OrderedTypes[0]` / `DefaultType`), keyboard shortcuts (1-9), and HUD layout. Documented in definitions tooltip. `TurretDefinitions.OnValidate()` also detects duplicate TurretType entries.

- **Data-driven creep types (mirrors turret pattern)**: `CreepDefinitions` SO + `CreepTypeDirectoryBuilder` + `CreepTypeDirectory` parallels the turret type infrastructure from Story 7. `CreepTypeDefinition` is a `[Serializable]` struct with `Validate()`. `OnValidate()` kept dumb (clamps ranges only) — builder is runtime validation authority. Adding a new creep type requires: enum value + definitions entry + prefab — zero code changes.
- **Per-creep round-robin cycling**: `SpawnSystem` advances `currentTypeIndex` after each individual creep spawn (not per burst). This produces visible Small/Big alternation even with a single spawn point. `Reset()` resets the index.
- **Per-type creep pools with CreepVisual struct**: `PresentationAdapter` manages `IReadOnlyDictionary<CreepType, GameObjectPool> creepPoolByType`. Uses `CreepVisual` struct (mirrors `TurretVisual`) to track each creep's source pool for correct return-to-pool. Pool budget split across types (`CeilToInt(total / typeCount)`) — not multiplied.
- **CreepDef deleted**: Replaced entirely by `CreepDefinitions` with `CreepTypeDefinition` entries. No migration needed — old asset removed.
- **SpawnSystem refactored to queue consumer (Story 9)**: Timer-driven spawning with round-robin type cycling replaced by queue consumption from `WaveStore.SpawnQueue`. `WaveSystem` decides WHAT and WHEN; `SpawnSystem` decides WHERE (round-robin spawn positions) and creates `CreepSimData`. Wave definitions are scene-independent.
- **WaveSystem scene-independent**: `WaveSystem` only enqueues `CreepType` values — no spawn positions, no `Vector3`. Wave definitions reusable across different scene layouts.
- **Wave-cleared = queue empty + creeps empty**: `SpawnQueue.Count == 0 && ActiveCreeps.Count == 0`. No per-wave creep tracking needed since one wave is active at a time. `WaveSystem` checks clear condition before enqueuing new spawns in the same tick.
- **Lose takes priority over win**: `PlayingState` checks `BaseDestroyed` first, then `AllWavesCleared`. Once either fires, subsequent ticks are no-ops (one-shot guards + early return).
- **WinPopup via GameUiCoordinator**: Mirrors LosePopup pattern. `OnStateChanged` instantiates from prefab on enter `GameState.Win`, destroys on exit.
- **WaveConfig replaces SpawnConfig**: `WaveConfig` SO contains ordered `WaveDefinition[]` array. Each wave has `WaveEntryDefinition[]` entries and `delayBeforeStart`. Each entry specifies `CreepType`, `count` (total, not per-spawn-point), and `spawnInterval`. SpawnConfig file remains in codebase but is no longer referenced.
- **Burst cap in WaveSystem**: `MAX_SPAWNS_PER_TICK = 20` prevents hitching on large deltaTime spikes (matches pattern from prior SpawnSystem).
- **Time carry-over precision**: When `WaitingToStart` delay elapses mid-tick, only excess time after the delay flows into spawning phase via return value pattern. Prevents first-tick over-spawning.
- **Pool sizing from wave data**: `MaxCreepsInAnyWave(waveConfig.Waves) * 2` replaces old SpawnConfig-based calculation. Dynamically adapts to wave content.
- **WaveStore events for future use**: `OnWaveStarted(int)` and `OnWaveCleared(int)` events exist for future wave HUD. No wave HUD in current scope.

- **Game reset via keyboard (R key)**: Input reading stays in `PresentationAdapter.CollectInput()` (architectural consistency). `PlacementInput.RestartRequested` field bridges input to `GameFlowController`, which fires `GameTrigger.RestartRequested` when in Win/Lose state. No UGUI popup modifications — restart is keyboard-only with a UI Toolkit "Press R to Restart" hint.
- **Reset ordering**: `PresentationAdapter.ResetVisuals()` first (returns pooled GameObjects while stores still have entity data for lookup), then `GameSession.Reset()` (clears all stores), then per-system `Reset()` (resets ID counters, internal phase state), then `GameUiCoordinator.Refresh()` (forces HUD values from fresh store state).
- **Two-frame restart transition**: Frame N: RestartRequested queued → Frame N+1: resolved to Init, `InitState.Enter()` fires SceneValidated (survives due to bug fix), reset handler runs → Frame N+2: SceneValidated resolved to Playing, systems tick on clean state.
- **State machine trigger-during-Enter bug fix**: `GameStateMachine.Tick()` was clearing `pendingTrigger` AFTER `ResolveTrigger()`, which wiped triggers fired during `Enter()` (e.g., `InitState` fires `SceneValidated`). Fixed by clearing BEFORE calling `ResolveTrigger()`.
- **RestartHintHud**: UI Toolkit element in shared UXML. Visible only in Win/Lose states. Mirrors CoinHud/BaseHealthHud pattern (queries named elements from UIDocument, `SetVisible(bool)` toggle).

## TODOs (post story sign-off)

- **Refactor GameUiCoordinator constructor**: Currently 11 parameters — violates the 6+ parameter smell rule added to CLAUDE.md. Group into nested `readonly record struct` bundles (e.g., `Stores`, `Huds`, `Popups`). Tests should pass `default` for irrelevant bundles instead of long `null` trails. Also audit other constructors in the codebase for the same issue.

## Open Questions

- Addressables loading infrastructure (deferred until extensibility is needed)
