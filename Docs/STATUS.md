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
| Story 4: Turret Placement | Not started | |
| Story 5: Turret Shooting & Creep Damage | Not started | |
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

## Open Questions

- Addressables loading infrastructure (deferred until extensibility is needed, likely Story 8)
- Remaining Data Configuration Strategy entries (turret defs, wave defs, economy config)
- **Extract presentation concerns from GameBootstrap** — popup instantiation, HUD visibility toggling, and health event forwarding should move to a `PresentationController` or similar class. GameBootstrap should be startup wiring + Update pump only. Good time: when more presentation concerns arrive (turret placement UI, economy HUD).
