# Project Status

## TDD Progress

| Section | Status | Notes |
|---------|--------|-------|
| 1. Core Functionality | Complete | All key systems documented |
| 2. Architecture - Guiding Principles | Complete | 6 principles established |
| 2. Architecture - Detailed Design | In progress | Bootstrap, state machine, system scheduler, store pattern documented |
| 3. Constraints & Ground Rules | Complete | 9 constraints from spec + tool constraint |
| 4. Tech Package Choices | Complete | Input System, UI Toolkit, UGUI (provided popups), Addressables (SO data), Cinemachine dropped |
| 5. Data Configuration Strategy | In progress | CreepDef, SpawnConfig, BaseConfig SOs defined; more SOs to come in later stories |
| 6. Provided Assets Reference | Complete | All prefabs, scene, terrain, materials cataloged |
| 7. Deliverables - User Stories | Complete | Stories 1-10 with acceptance criteria |

## Implementation Progress

| Story | Status | Notes |
|-------|--------|-------|
| Story 1: Project Foundation | Complete | Game bootstrap, state machine, system scheduler, folder structure, test infrastructure |
| Story 2: Creep Spawning & Movement | Complete | CreepStore, SpawnSystem, MovementSystem, object pooling, PresentationAdapter sync |
| Story 3: Base Health & Lose Condition | Complete | BaseStore, DamageSystem, LoseState, BaseConfig SO, BaseHealthHud (UI Toolkit), health HUD event-driven |
| Story 4: Turret Placement | Complete | TurretStore (minimal, no removal pipeline), PlacementSystem, PlacementInput bridge, TurretComponent, PresentationAdapter input collection (raycast) + turret visual sync, turret pool |
| Story 5: Turret Shooting & Creep Damage | Complete | ProjectileSystem (inline targeting), DamageSystem extended with projectile hits + OnCreepKilled, homing projectiles, dead-creep guards, TurretDef SO, ProjectileStore |
| Story 6: Economy System | Not started | |
| Story 7: Turret Types (Regular & Freezing) | Not started | |
| Story 8: Creep Variety | Not started | |
| Story 9: Wave System | Not started | |
| Story 10: Game Reset | Not started | |

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
- **LosePopup via bootstrap**: `GameBootstrap.OnStateChanged` instantiates the LosePopup from a prefab reference on enter, destroys it on exit. Presentation concern only.
- **PlacementInput as shared bridge**: `PlacementInput` class created by `GameBootstrap` and passed to both `PresentationAdapter` (writer in `CollectInput`) and `PlacementSystem` (reader in `Tick`). Neither depends on the other. Consume-and-clear pattern: `PlacementSystem` clears input after consuming to prevent double-placement.
- **TurretStore minimal for Story 4**: No removal pipeline (`MarkForRemoval`, `RemovedIdsThisFrame`). Only `ActiveTurrets`, `PlacedThisFrame`, `BeginFrame` (clears placed list), `Reset`. Removal deferred to a story that needs turret destruction/selling.
- **Terrain raycast via LayerMask**: `GameBootstrap` exposes `LayerMask terrainLayerMask` serialized field, passed to `PresentationAdapter`. `CollectInput()` raycasts against this layer using Input System (`Mouse.current`).
- **No TargetingSystem**: Target selection merged into `ProjectileSystem` at fire time. Eliminates a whole system class and cross-system coupling field (`TargetCreepId` on turret). Targeting is ephemeral — nearest alive creep in range is found at the moment of firing.
- **Homing projectiles**: Projectiles track their target's current position each frame. If target dies or is removed before impact, projectile is discarded. Hit = distance < threshold or overshoot.
- **Hit recording via store**: `ProjectileStore.HitsThisFrame` (list of `ProjectileHit` structs) bridges `ProjectileSystem` → `DamageSystem`. DamageSystem remains the single writer for creep health.
- **Dead-creep guards**: `MovementSystem` and `DamageSystem.ProcessBaseDamage` skip creeps with `Health <= 0`. Prevents dead creeps from moving or dealing base damage after being killed by projectiles.
- **OnCreepKilled event**: `DamageSystem` fires `event Action<int>` on creep death. Forward hook for Story 6 EconomySystem.
- **FireInterval naming**: Consistent use of `FireInterval` (seconds between shots) across all code and data. No `FireRate`.
- **TurretDef SO**: ScriptableObject for turret stats (damage, range, fireInterval, projectileSpeed). Systems receive primitives at bootstrap, never SO references.
- **ProjectileStore with deferred removal**: Mirrors CreepStore pattern — `Add`, `MarkForRemoval`, `BeginFrame` (flush + clear frame lists). Plus `HitsThisFrame` for cross-system hit communication.

## Open Questions

- Addressables loading infrastructure (deferred until extensibility is needed, likely Story 8)
- Remaining Data Configuration Strategy entries (turret defs, wave defs, economy config)
- **Extract presentation concerns from GameBootstrap** — popup instantiation, HUD visibility toggling, and health event forwarding should move to a `PresentationController` or similar class. GameBootstrap should be startup wiring + Update pump only. Good time: when more presentation concerns arrive (turret placement UI, economy HUD).
