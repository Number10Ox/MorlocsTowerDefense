# Architecture Diagrams

Visual companion to TDD.md Section 2 (Detailed Design). Render with any Mermaid-capable viewer.

---

## Class Diagram — Story 1 Foundation

```mermaid
classDiagram
    class GameBootstrap {
        <<MonoBehaviour>>
        -GameStateMachine stateMachine
        -SystemScheduler systemScheduler
        -PresentationAdapter presentationAdapter
        -HomeBaseComponent homeBase
        +Awake()
        +Start()
        +Update()
    }

    class GameStateMachine {
        -Dictionary~GameState‚ IGameState~ states
        -Dictionary~StateAndTrigger‚ GameState~ transitions
        -IGameState currentState
        -GameState currentStateId
        -GameTrigger? pendingTrigger
        +GameState CurrentStateId
        +event Action~GameState‚ GameState~ OnStateChanged
        +AddState(GameState, IGameState)
        +AddTransition(GameState, GameTrigger, GameState)
        +Start(GameState)
        +Tick(float deltaTime)
        +Fire(GameTrigger)
    }

    class IGameState {
        <<interface>>
        +Enter()
        +Tick(float deltaTime)
        +Exit()
    }

    class GameState {
        <<enumeration>>
        Init
        Playing
        Win
        Lose
    }

    class GameTrigger {
        <<enumeration>>
        SceneValidated
        BaseDestroyed
        AllWavesCleared
        RestartRequested
    }

    class InitState {
        -Action~GameTrigger~ fire
        +Enter()
        +Tick(float deltaTime)
        +Exit()
    }

    class PlayingState {
        -Action~GameTrigger~ fire
        +Enter()
        +Tick(float deltaTime)
        +Exit()
    }

    class SystemScheduler {
        -IGameSystem[] systems
        +Tick(float deltaTime)
    }

    class IGameSystem {
        <<interface>>
        +Tick(float deltaTime)
    }

    class HomeBaseComponent {
        <<MonoBehaviour>>
    }

    class PresentationAdapter {
        +CollectInput()
        +SyncVisuals()
    }

    GameBootstrap --> GameStateMachine : owns
    GameBootstrap --> SystemScheduler : owns
    GameBootstrap --> PresentationAdapter : owns
    GameBootstrap --> HomeBaseComponent : serialized ref
    GameStateMachine --> "0..*" IGameState : manages
    GameStateMachine --> GameState : indexes by
    GameStateMachine --> GameTrigger : transitions by
    InitState ..|> IGameState
    PlayingState ..|> IGameState
    SystemScheduler --> "0..*" IGameSystem : ticks in order
```

**Notes:**
- `GameBootstrap` is the only "god-level" MonoBehaviour. It is the composition root — creates the state machine, system scheduler, states, and systems. Configures the transition table and wires references.
- `GameStateMachine` and all `IGameState` implementations are **plain C# classes**, not MonoBehaviours.
- `HomeBaseComponent` is a thin MonoBehaviour on the Base GameObject in the scene. It holds no logic — just identifies the object for system discovery.
- States receive an `Action<GameTrigger>` delegate at construction. They fire semantic triggers (`SceneValidated`, `BaseDestroyed`, etc.) without knowing which state the trigger leads to. The transition table in `GameBootstrap` maps `(state, trigger) → destination`.
- **States are flow-only** — they manage enter/exit lifecycle and fire triggers. States do not own or tick systems.
- `SystemScheduler` is a **plain C# class** owned by `GameBootstrap`. It holds the ordered `IGameSystem[]` array and ticks them sequentially. `GameBootstrap.Update()` gates the scheduler — systems only tick when the state machine is in a gameplay state (e.g., `Playing`). This separates flow control (states) from system execution (scheduler).
- `IGameSystem` provides a uniform `Tick()` contract for gameplay systems. Systems are global — they exist independently of game states.
- `PresentationAdapter` is a **plain C# class** owned by `GameBootstrap`. It is the only place that calls Unity input and rendering APIs. Systems never reference it directly — they read input structs it produces and write sim data it consumes. Stub in Story 1; gains responsibilities as systems are added.
- `Win` and `Lose` states appear in the enum but are implemented in later stories.
- `GameTrigger` values are added incrementally as stories introduce new transitions.

---

## Game State Diagram

```mermaid
stateDiagram-v2
    [*] --> Init : Play pressed

    Init --> Playing : Scene validated

    Playing --> Win : All waves cleared & base alive
    Playing --> Lose : Base health ≤ 0

    Win --> Init : Restart
    Lose --> Init : Restart

    state Init {
        [*] --> ValidateScene
        ValidateScene --> FireSceneValidated
    }

    state Playing {
        [*] --> GameplayActive
        Note right of GameplayActive : Systems ticked by SystemScheduler (gated by GameBootstrap)
    }
```

**Story 1 scope:** Only `Init` and `Playing` are implemented. `Win` and `Lose` are placeholders in the enum — their `IGameState` classes come in Stories 3 and 9.

**Reset path:** Restart from Win/Lose transitions back to `Init`. `PlayingState.Exit()` tears down spawned objects and system state. `InitState.Enter()` re-validates and sets up a fresh game. No residual state.

---

## Startup Sequence

```mermaid
sequenceDiagram
    participant Unity
    participant Bootstrap as GameBootstrap
    participant SM as GameStateMachine
    participant Sched as SystemScheduler
    participant Init as InitState
    participant Playing as PlayingState
    participant Base as HomeBaseComponent

    Note over Unity: Play pressed → MainScene loads

    Unity->>Bootstrap: Awake()
    Bootstrap->>SM: new GameStateMachine()
    Bootstrap->>Sched: new SystemScheduler(systems[])
    Bootstrap->>Init: new InitState(sm.Fire, homeBase)
    Bootstrap->>Playing: new PlayingState(sm.Fire)
    Bootstrap->>SM: AddState(Init, initState)
    Bootstrap->>SM: AddState(Playing, playingState)
    Bootstrap->>SM: AddTransition(Init, SceneValidated, Playing)
    Bootstrap->>SM: AddTransition(Playing, BaseDestroyed, Lose)
    Note over SM: ... remaining transitions
    Bootstrap->>Base: Validate serialized reference

    Unity->>Bootstrap: Start()
    Bootstrap->>SM: Start(GameState.Init)
    SM->>Init: Enter()
    Init->>Init: Validate scene setup
    Init->>SM: Fire(SceneValidated)

    Note over SM: Pending trigger set

    Unity->>Bootstrap: Update() — first frame
    Bootstrap->>SM: Tick(deltaTime)
    Note over SM: Resolve (Init, SceneValidated) → Playing
    SM->>Init: Exit()
    SM-->>SM: Switch currentState
    SM->>Playing: Enter()
    SM->>Playing: Tick(deltaTime)

    Note over Bootstrap: CurrentStateId == Playing
    Bootstrap->>Sched: Tick(deltaTime)
    Note over Sched: Systems tick in phase order (empty in Story 1)
```

**Key points:**
- `GameBootstrap.Awake()` constructs everything — state machine, system scheduler, states — and configures the transition table. `Start()` kicks off the state machine.
- `InitState.Enter()` fires `SceneValidated` — it does not know the destination. The trigger is **pending** — not resolved until the next `Tick()`.
- The state machine resolves triggers at the **start** of `Tick()`: lookup `(currentState, trigger)` in transition table → `Exit()` old → switch → `Enter()` new → `Tick()` new. This guarantees one clean frame boundary between states.
- States only depend on `Action<GameTrigger>` — no reference to other states or to `GameStateMachine` itself. This makes states independently testable.
- System ticking is separate from state ticking. `GameBootstrap` gates the scheduler based on the current state — systems only run during gameplay.

---

## Per-Frame Tick Flow

```mermaid
sequenceDiagram
    participant Unity
    participant Bootstrap as GameBootstrap
    participant Pres as PresentationAdapter
    participant SM as GameStateMachine
    participant State as Current IGameState
    participant Sched as SystemScheduler

    loop Every Frame
        Unity->>Bootstrap: Update()

        Bootstrap->>Pres: CollectInput()
        Note over Pres: Raycast, mouse pos → input structs

        Bootstrap->>SM: Tick(Time.deltaTime)

        alt Pending Trigger Exists
            Note over SM: Resolve (state, trigger) → destination
            SM->>State: Exit()
            SM-->>SM: Switch to new state
            SM->>State: Enter()
        end

        SM->>State: Tick(deltaTime)

        alt CurrentStateId == Playing
            Bootstrap->>Sched: Tick(deltaTime)
            Note over Sched: Systems tick in phase order
        end

        Bootstrap->>Pres: SyncVisuals()
        Note over Pres: Sim state → Transforms, GameObjects, UI
    end
```

**Frame boundary contract:** Each frame has four phases with unidirectional data flow:
1. **Input collection** — `PresentationAdapter.CollectInput()` reads Unity inputs (mouse position, raycasts, keyboard) and writes them into sim-readable input structs. Systems never call Unity input APIs directly.
2. **State tick** — The state machine resolves pending triggers and ticks the current state. States manage flow (enter/exit, fire triggers) — not system execution.
3. **System tick** — `GameBootstrap` gates the `SystemScheduler` based on the current state. Systems tick in deterministic phase order. Systems read/write only simulation data (structs, arrays). No Unity API calls.
4. **Visual sync** — `PresentationAdapter.SyncVisuals()` reads simulation state and writes to Unity objects (`Transform.position`, enable/disable GameObjects, UI updates). The sim is unaware this step exists.

**Story 1:** The `SystemScheduler` holds an empty `IGameSystem[]` array — no systems yet. The presentation adapter is a stub. Future stories add systems to the scheduler in `GameBootstrap`.

---

## System Scheduler — System Phases & Tick Order

Shows how `IGameSystem` implementations will be ticked by `SystemScheduler` as stories are implemented. Systems are grouped into three conceptual phases. All systems are plain C# classes implementing `IGameSystem`, registered in order via `GameBootstrap`. The scheduler is gated by the state machine — systems only tick during gameplay states.

```mermaid
flowchart TD
    subgraph phase1 ["Phase 1 — World Update"]
        A["SystemScheduler.Tick(deltaTime)"] --> B["WaveSystem.Tick()
        (Story 9)"]
        B --> C["SpawnSystem.Tick()
        (Story 2)"]
        C --> D["MovementSystem.Tick()
        (Story 2)"]
        D --> E["PlacementSystem.Tick()
        (Story 4)"]
    end
    subgraph phase2 ["Phase 2 — Combat"]
        E --> F["TargetingSystem.Tick()
        (Story 5)"]
        F --> G["ProjectileSystem.Tick()
        (Story 5)"]
        G --> H["DamageSystem.Tick()
        (Story 5)"]
    end
    subgraph phase3 ["Phase 3 — Resolution"]
        H --> I["EconomySystem.Tick()
        (Story 6)"]
        I --> J{"Check End Conditions"}
    end
    J -->|"Base HP ≤ 0"| K["Fire(BaseDestroyed)"]
    J -->|"All Waves Cleared"| L["Fire(AllWavesCleared)"]
    J -->|"Continue"| M["End Tick"]
```

**System phases:**

| Phase | Systems | Purpose |
|-------|---------|---------|
| **1 — World Update** | Wave, Spawn, Movement, Placement | Bring all entities to current-frame state; process player input |
| **2 — Combat** | Targeting, Projectile, Damage | Resolve attacks using positions settled in Phase 1 |
| **3 — Resolution** | Economy, End Conditions | Process rewards and check win/lose after combat settles |

**Tick order within phases:**
1. **Waves** decide what to spawn this frame
2. **Spawn** creates new creeps from wave data
3. **Movement** advances all creeps toward the base
4. **Placement** processes player turret placement input — placed turrets are available for targeting this frame
5. **Targeting** assigns turret targets using settled positions from Phase 1
6. **Projectiles** advance in-flight projectiles, check hits
7. **Damage** applies damage from hits, removes dead creeps, triggers base damage on arrival
8. **Economy** processes coin awards from kills
9. **Conditions** check win/lose after all systems have settled

---

## Folder Structure

```
Assets/
├── Scripts/
│   ├── Core/                   # Bootstrap, session, state machine, scheduler, game loop
│   │   ├── GameBootstrap.cs
│   │   ├── GameSession.cs
│   │   ├── GameStateMachine.cs
│   │   ├── SystemScheduler.cs
│   │   ├── PresentationAdapter.cs
│   │   ├── GameState.cs        # enum
│   │   ├── GameTrigger.cs      # enum
│   │   ├── IGameState.cs       # interface
│   │   ├── IGameSystem.cs      # interface
│   │   ├── InitState.cs
│   │   ├── PlayingState.cs
│   │   └── LoseState.cs
│   ├── ObjectPooling/          # Reusable pool infrastructure (namespaced)
│   │   ├── IPoolable.cs
│   │   └── GameObjectPool.cs
│   ├── HomeBase/               # Home base component and store
│   │   ├── HomeBaseComponent.cs
│   │   └── BaseStore.cs
│   ├── Creeps/                 # Creep store, sim data, systems, components
│   │   ├── CreepStore.cs
│   │   ├── CreepSimData.cs
│   │   ├── SpawnSystem.cs
│   │   ├── MovementSystem.cs
│   │   ├── SpawnPointComponent.cs
│   │   └── CreepComponent.cs
│   ├── Data/                   # ScriptableObject definitions
│   │   ├── CreepDef.cs
│   │   ├── SpawnConfig.cs
│   │   └── BaseConfig.cs
│   ├── Turrets/                # (Story 4+)
│   ├── Combat/                 # DamageSystem (Story 3+)
│   ├── Economy/                # (Story 6+)
│   ├── Waves/                  # (Story 9)
│   └── UI/                     # BaseHealthHud (Story 3+)
├── Tests/
│   ├── Editor/
│   │   ├── EditModeTests.asmdef
│   │   ├── GameStateMachineTests.cs
│   │   ├── InitStateTests.cs
│   │   ├── SystemSchedulerTests.cs
│   │   ├── CreepStoreTests.cs
│   │   ├── SpawnSystemTests.cs
│   │   ├── MovementSystemTests.cs
│   │   ├── GameObjectPoolTests.cs
│   │   ├── CreepSpawningIntegrationTests.cs
│   │   ├── BaseStoreTests.cs
│   │   ├── DamageSystemTests.cs
│   │   ├── PlayingStateTests.cs
│   │   ├── LoseStateTests.cs
│   │   └── BaseHealthIntegrationTests.cs
│   └── Runtime/
│       └── RuntimeTests.asmdef
├── Prefabs/                    # (provided, unchanged)
├── Scenes/                     # (provided, unchanged)
├── Materials/                  # (provided, unchanged)
└── Terrain/                    # (provided, unchanged)
```

No project-wide namespace. Feature folders group related components, systems, stores, and data. Generic reusable infrastructure (`ObjectPooling`) gets its own namespace.

---

## Story 2 — Creep Spawning & Movement

### Class Diagram

```mermaid
classDiagram
    class GameSession {
        +CreepStore CreepStore
        +BeginFrame()
        +Reset()
    }

    class CreepStore {
        -List~CreepSimData~ activeCreeps
        -List~int~ pendingRemovals
        -List~CreepSimData~ spawnedThisFrame
        -List~int~ removedIdsThisFrame
        +IReadOnlyList~CreepSimData~ ActiveCreeps
        +IReadOnlyList~CreepSimData~ SpawnedThisFrame
        +IReadOnlyList~int~ RemovedIdsThisFrame
        +Add(CreepSimData)
        +MarkForRemoval(int)
        +BeginFrame()
        +Reset()
    }

    class CreepSimData {
        +int Id
        +Vector3 Position
        +Vector3 Target
        +float Speed
        +bool ReachedBase
    }

    class SpawnSystem {
        -CreepStore creepStore
        -Vector3[] spawnPositions
        -float spawnInterval
        -int creepsPerSpawn
        -float creepSpeed
        -float spawnTimer
        -int nextCreepId
        +Tick(float deltaTime)
    }

    class MovementSystem {
        -CreepStore creepStore
        -float arrivalThreshold
        +Tick(float deltaTime)
    }

    class SpawnPointComponent {
        <<MonoBehaviour>>
    }

    class CreepComponent {
        <<MonoBehaviour>>
        -int creepId
        +int CreepId
        +Initialize(int)
        +OnPoolGet()
        +OnPoolReturn()
    }

    class GameObjectPool {
        <<ObjectPooling>>
        -Stack~GameObject~ available
        +Get(Vector3 position) GameObject
        +Return(GameObject)
        +Clear()
    }

    class IPoolable {
        <<interface>>
        <<ObjectPooling>>
        +OnPoolGet()
        +OnPoolReturn()
    }

    GameSession --> CreepStore : owns
    CreepStore --> "0..*" CreepSimData : stores
    SpawnSystem --> CreepStore : writes via Add()
    MovementSystem --> CreepStore : reads ActiveCreeps‚ writes via MarkForRemoval()
    SpawnSystem ..|> IGameSystem
    MovementSystem ..|> IGameSystem
    CreepComponent ..|> IPoolable
    PresentationAdapter --> CreepStore : reads change lists
    PresentationAdapter --> GameObjectPool : manages creep GOs
    GameBootstrap --> GameSession : owns
```

**Notes:**
- `SpawnSystem` and `MovementSystem` depend only on `CreepStore`, never on each other. No system-to-system coupling.
- `CreepStore` manages deferred removals: `MarkForRemoval()` buffers IDs, `BeginFrame()` flushes them and populates `RemovedIdsThisFrame`.
- `PresentationAdapter` reads `SpawnedThisFrame` and `RemovedIdsThisFrame` to efficiently manage the object pool — no O(n^2) diffing.
- `GameObjectPool.Get(position)` sets transform position before activation to avoid one-frame visual pop at origin.

### Creep Lifecycle Sequence

```mermaid
sequenceDiagram
    participant Bootstrap as GameBootstrap
    participant Session as GameSession
    participant Store as CreepStore
    participant Spawn as SpawnSystem
    participant Move as MovementSystem
    participant Pres as PresentationAdapter
    participant Pool as GameObjectPool

    Note over Bootstrap: Frame N — spawn frame

    Bootstrap->>Session: BeginFrame()
    Session->>Store: BeginFrame()
    Note over Store: Flush pending removals‚ clear frame lists

    Bootstrap->>Spawn: Tick(dt)
    Spawn->>Store: Add(creepSimData)
    Note over Store: Added to activeCreeps + spawnedThisFrame

    Bootstrap->>Move: Tick(dt)
    Note over Move: Moves creep toward base

    Bootstrap->>Pres: SyncVisuals()
    Pres->>Store: Read SpawnedThisFrame
    Pres->>Pool: Get(position)
    Note over Pres: Creates GO‚ maps to creep ID

    Pres->>Store: Read ActiveCreeps
    Note over Pres: Updates Transform.position

    Note over Bootstrap: Frame N+K — arrival frame

    Bootstrap->>Session: BeginFrame()
    Bootstrap->>Move: Tick(dt)
    Note over Move: Detects distance ≤ threshold
    Move->>Store: MarkForRemoval(id)
    Note over Store: Buffered in pendingRemovals

    Note over Bootstrap: Frame N+K+1 — removal frame

    Bootstrap->>Session: BeginFrame()
    Session->>Store: BeginFrame()
    Note over Store: Flush removal → removedIdsThisFrame

    Bootstrap->>Pres: SyncVisuals()
    Pres->>Store: Read RemovedIdsThisFrame
    Pres->>Pool: Return(GO)
    Note over Pres: Returns GO to pool‚ removes from map
```

---

## Story 3 — Base Health & Lose Condition

### Class Diagram

```mermaid
classDiagram
    class BaseStore {
        -int maxHealth
        -int currentHealth
        -int damageTakenThisFrame
        +int MaxHealth
        +int CurrentHealth
        +bool IsDestroyed
        +int DamageTakenThisFrame
        +event Action~int‚ int~ OnBaseHealthChanged
        +ApplyDamage(int amount)
        +BeginFrame()
        +Reset()
    }

    class DamageSystem {
        -CreepStore creepStore
        -BaseStore baseStore
        +Tick(float deltaTime)
    }

    class LoseState {
        -Action~GameTrigger~ fire
        +Enter()
        +Tick(float deltaTime)
        +Exit()
    }

    class BaseConfig {
        <<ScriptableObject>>
        -int maxHealth
        +int MaxHealth
    }

    class BaseHealthHud {
        -Label healthLabel
        -VisualElement healthBarFill
        -VisualElement healthContainer
        +UpdateHealth(int current‚ int max)
        +SetVisible(bool visible)
    }

    class CreepSimData {
        +int Id
        +Vector3 Position
        +Vector3 Target
        +float Speed
        +bool ReachedBase
        +int DamageToBase
        +bool HasDealtBaseDamage
    }

    GameSession --> BaseStore : owns
    GameSession --> CreepStore : owns
    DamageSystem --> CreepStore : reads ActiveCreeps
    DamageSystem --> BaseStore : writes via ApplyDamage()
    DamageSystem ..|> IGameSystem
    LoseState ..|> IGameState
    PlayingState --> BaseStore : reads IsDestroyed
    BaseHealthHud ..> BaseStore : listens OnBaseHealthChanged
    GameBootstrap --> BaseConfig : serialized ref
    GameBootstrap --> BaseHealthHud : owns
```

**Notes:**
- `DamageSystem` reads `CreepStore.ActiveCreeps` and writes `BaseStore` via `ApplyDamage()`. Gates on `ReachedBase && !HasDealtBaseDamage` to prevent double-damage.
- `BaseStore` fires `OnBaseHealthChanged` for UI updates. `ApplyDamage` is idempotent after destruction (no event, no state change once health is 0).
- `PlayingState` polls `BaseStore.IsDestroyed` in `Tick()` — not event-driven — because event handlers must not fire game triggers per the event handler discipline.
- `LoseState` is empty; `GameBootstrap.OnStateChanged` toggles the LosePopup as a presentation concern.
- `BaseHealthHud` is a plain C# class (not MonoBehaviour) that binds to a `UIDocument` and updates via `OnBaseHealthChanged` event.

### Base Damage Sequence

```mermaid
sequenceDiagram
    participant Bootstrap as GameBootstrap
    participant Session as GameSession
    participant CStore as CreepStore
    participant BStore as BaseStore
    participant Move as MovementSystem
    participant Dmg as DamageSystem
    participant SM as GameStateMachine
    participant Playing as PlayingState
    participant HUD as BaseHealthHud

    Note over Bootstrap: Frame N — creep arrives at base

    Bootstrap->>Session: BeginFrame()
    Session->>CStore: BeginFrame()
    Session->>BStore: BeginFrame()
    Note over BStore: Clear DamageTakenThisFrame

    Bootstrap->>SM: Tick(dt)
    SM->>Playing: Tick(dt)
    Note over Playing: baseStore.IsDestroyed? → false (last frame's state)

    Bootstrap->>Move: Tick(dt)
    Note over Move: Creep reaches base
    Move-->>CStore: creep.ReachedBase = true
    Move->>CStore: MarkForRemoval(id)

    Bootstrap->>Dmg: Tick(dt)
    Note over Dmg: ReachedBase && !HasDealtBaseDamage
    Dmg->>BStore: ApplyDamage(creep.DamageToBase)
    Dmg-->>CStore: creep.HasDealtBaseDamage = true
    BStore-->>HUD: OnBaseHealthChanged(current‚ max)
    HUD-->>HUD: UpdateHealth(current‚ max)

    Note over Bootstrap: Frame N+1 — base destroyed check

    Bootstrap->>Session: BeginFrame()
    Session->>CStore: BeginFrame()
    Note over CStore: Flush removal

    Bootstrap->>SM: Tick(dt)
    SM->>Playing: Tick(dt)
    Note over Playing: baseStore.IsDestroyed? → true
    Playing->>SM: Fire(BaseDestroyed)
    Note over SM: Pending trigger set

    Note over Bootstrap: Frame N+2 — transition to Lose

    Bootstrap->>SM: Tick(dt)
    Note over SM: Resolve (Playing‚ BaseDestroyed) → Lose
    SM->>Playing: Exit()
    SM->>SM: Switch to LoseState
    SM-->>Bootstrap: OnStateChanged(Playing‚ Lose)
    Note over Bootstrap: losePopup.SetActive(true)
    Note over Bootstrap: baseHealthHud.SetVisible(false)
```

**Key timing:**
- **Frame N**: MovementSystem sets `ReachedBase`, DamageSystem applies damage, HUD updates via event.
- **Frame N+1**: `PlayingState.Tick()` detects `IsDestroyed`, fires `BaseDestroyed` (pending trigger).
- **Frame N+2**: State machine resolves trigger, transitions to Lose, popup appears.
- Systems do not tick in Lose state (gated by `CurrentStateId == Playing` in `GameBootstrap.Update()`).
