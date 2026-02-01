# Scripts Architecture

## Folder Purposes

| Folder | Purpose |
|--------|---------|
| `App/` | Composition root (`GameFlowController`), session ownership, game state/trigger enums. |
| `Framework/` | Reusable infrastructure: state machine, system scheduler, object pooling. |
| `States/` | `IGameState` implementations that manage game flow lifecycle (enter/exit/tick). |
| `Stores/` | Authoritative data containers with deferred removal and per-frame change lists. |
| `SimData/` | Pure simulation data classes/structs. No Unity types, no behavior. |
| `Systems/` | `IGameSystem` implementations containing all game logic. Depend on stores, not on each other. |
| `Components/` | Thin MonoBehaviour prefab/scene markers. Hold ID mappings and pooling lifecycle, no logic. |
| `Input/` | Sim-readable input bridges written by presentation, consumed by systems. |
| `Presentation/` | Unity view sync, UI bindings, and coordinators. Reads sim state, writes to Unity objects. |
| `Data/` | ScriptableObject definitions for inspector-tunable configuration. |

## Per-Frame Tick Order

```
GameFlowController.Update()
  1. PresentationAdapter.CollectInput()   -- Unity input -> sim-readable structs
  2. GameStateMachine.Tick()              -- resolve pending triggers, tick current state
  3. GameSession.BeginFrame()             -- flush deferred removals, clear frame lists
  4. SystemScheduler.Tick()               -- systems tick in deterministic phase order (gated by Playing state)
  5. PresentationAdapter.SyncVisuals()    -- sim state -> Transforms, GameObjects, UI
```

Data flows in one direction through these phases. Systems never call Unity APIs; presentation never mutates sim data.

## Single-Writer Rule

Every piece of simulation data has exactly one system that writes it. No exceptions.

| Data | Writer | Readers |
|------|--------|---------|
| `CreepSimData.Position` | `MovementSystem` | `ProjectileSystem`, `PresentationAdapter` |
| `CreepSimData.Health` | `DamageSystem` | `MovementSystem`, `DamageSystem`, `ProjectileSystem` |
| `CreepSimData.ReachedBase` | `MovementSystem` | `DamageSystem` |
| `BaseStore.CurrentHealth` | `DamageSystem` | `PlayingState`, `GameUiCoordinator` |
| `TurretSimData` (placement fields) | `PlacementSystem` | `ProjectileSystem` |
| `TurretSimData.FireCooldown` | `ProjectileSystem` | `ProjectileSystem` |
| `ProjectileSimData.Position` | `ProjectileSystem` | `PresentationAdapter` |
| `ProjectileStore.HitsThisFrame` | `ProjectileSystem` | `DamageSystem` |
| `PlacementInput` | `PresentationAdapter` | `PlacementSystem` |
