# Morlocs Tower Defense - Technical Design Document

## 1. Core Functionality

A tower defense game where creeps spawn from fixed points on the battlefield and move in a straight line toward a central base. The player places turrets on the battlefield to shoot and destroy creeps before they reach the base. Creeps arrive in waves of increasing difficulty. The player earns coins from killing creeps and spends coins to place turrets. The game is won by surviving all waves; the game is lost when the base takes too much damage.

### Key Systems

- **Creep Spawning & Movement** - Creeps spawn from pre-placed SpawnPoints and move in a straight line toward the Base (no pathfinding). Spawn timing, count, and behavior are data-driven and easy to tune.
- **Base Health & Lose Condition** - The Base has a health pool. Creeps that reach it deal damage. When health reaches zero, the LosePopup is displayed.
- **Turret Placement** - The player clicks on the battlefield to place turrets. Placement costs coins from the economy system.
- **Turret Targeting & Projectiles** - Turrets detect creeps within range and fire projectiles that deal damage on hit. Two turret types: regular (damage) and freezing (slow effect).
- **Creep Variety** - Two creep types (small, big) with differing speed and hit points.
- **Economy** - Turrets cost coins to build. Creeps award coins on death. Starting coins provided.
- **Wave System** - Creeps arrive in defined waves. Clearing all creeps in a wave triggers the next. Surviving all waves displays the WinPopup.
- **Game Reset** - Full restart without leaving Play Mode. All state (enemies, towers, resources, wave progress) resets cleanly.

---

## 2. Architecture

### Guiding Principles

These are architectural directions that will inform the detailed design. Not full ECS (Unity DOTS is out of scope for this project and Unity version), but borrowing key ideas:

- **Data-oriented design** - Separate data from behavior. Game state lives in plain data structures (structs, ScriptableObjects); systems operate on that data. Prefer structs for hot-path data. Minimize scattered state across MonoBehaviours.
- **System-and-component thinking** - MonoBehaviours act as thin components that hold references and wire into Unity lifecycle. Logic lives in systems/managers that process components, not in the components themselves.
- **MonoBehaviour minimalism** - Only inherit from MonoBehaviour when Unity requires it (scene presence, coroutines, serialized inspector references, collision callbacks). Pure logic, data models, state machines, and utility classes should be plain C# classes or structs. Avoid an architecture dominated by per-object `Update()` calls; prefer centralized system ticks that iterate over data.
- **MVU-style UI architecture** - UI follows a Model-View-Update pattern: a model (state) drives what the view displays; user actions produce messages/events that update the model; the view re-renders from the model. No direct UI-to-game-state mutation.
- **State machine state management** - Game flow (start, playing, wave transitions, win/lose, reset) managed by an explicit state machine. Lightweight custom implementation -- no Stateless library (avoids LINQ and allocation concerns). States are data, transitions are explicit.

### Detailed Design

_To be filled out through discussion and manual editing._

<!--
Topics to cover:
- High-level system diagram / dependency graph
- Manager vs. component responsibilities
- Event/messaging approach (C# events, UnityEvents, ScriptableObject events, etc.)
- Object pooling strategy for creeps and projectiles
- How turret placement interacts with the input system
- How wave definitions are structured and sequenced
- Game state machine (Menu -> Playing -> Win/Lose -> Reset)
- Folder / namespace organization
- Where the "model" lives relative to MonoBehaviour state
- How MVU boundaries work: what is the model, what generates update messages, what re-renders
-->

---

## 3. Constraints & Ground Rules

These constraints come directly from the spec and must be respected during implementation.

- **No pathfinding** - Creeps move in a straight line toward the Base. Do not implement or integrate NavMesh or any pathfinding system.
- **Editor-only** - The game will be tested in the Unity Editor. No platform build or deployment concerns.
- **Mouse & keyboard input** - Must support mouse and keyboard controls.
- **No extra features** - Only implement the 9 requirements listed in the spec. No bonus features.
- **No visual polish** - Code quality and architecture are evaluated, not visual fidelity. Don't invest in aesthetics.
- **Scalability focus** - Architecture must make it easy to add new unit or turret types without major refactors.
- **Use provided assets** - Use the prefabs and materials already in the project. Do not create replacement assets.
- **No Unity asset creation from code tools** - Claude Code must not create Unity assets (scenes, prefabs, ScriptableObjects, materials, etc.). These are created in the Unity Editor by the developer. Claude Code writes C# scripts, edits existing scripts, and creates non-asset files only. When a ScriptableObject class is written in code, the developer will create the corresponding `.asset` file in the Editor.
- **Unity version** - 2022.3.58f1.

---

## 4. Tech Package Choices

| Package | Purpose | Notes |
|---------|---------|-------|
| **Input System** (new) | Player input (mouse clicks, keyboard) | Use `UnityEngine.InputSystem`. Do not use legacy `Input` class. |
| **UI Toolkit** | All runtime UI (HUD, popups, overlays) | UXML for layout, USS for styles, C# for binding. No UGUI Canvas. |
| **Cinemachine** | Camera control | Use for any camera management if needed. |
| **ScriptableObjects** | Tuning data | Creep definitions, turret definitions, wave definitions, economy config. |
| **TextMeshPro** | _Not used_ | UI text handled via UI Toolkit labels. |
| **Addressables** | _TBD_ | Evaluate need. Assets in Resources only as exception. |

### Asset Loading Strategy

- Avoid `Resources/` folder where possible
- Prefer Addressables or AssetBundle references for loading prefabs at runtime
- ScriptableObject assets are referenced directly via serialized fields on MonoBehaviours
- If Addressables adds unnecessary complexity for this MVP scope, direct prefab references via serialized fields are acceptable

---

## 5. Data Configuration Strategy

The spec emphasizes making gameplay values "easy to tweak and tune." This section defines how tunable data is exposed.

_To be filled out during architecture discussion._

<!--
Considerations:
- ScriptableObjects for creep definitions (speed, HP, coin reward) and turret definitions (damage, range, fire rate, cost, effect type)
- ScriptableObjects or serialized data for wave definitions (which creeps, how many, spawn interval, delay between waves)
- Serialized fields on MonoBehaviours for per-instance overrides vs. shared SO data
- Economy starting values (initial coins)
- Base health
-->

---

## 6. Provided Assets Reference

Assets already present in the Unity project:

| Asset | Type | Location |
|-------|------|----------|
| MainScene | Scene | Assets/Scenes/ |
| Base | Prefab | Assets/Prefabs/ |
| SpawnPoint - 1 | Prefab | Assets/Prefabs/ |
| Turret-regular | Prefab | Assets/Prefabs/ |
| Turret-freezing | Prefab | Assets/Prefabs/ |
| Creep-small | Prefab | Assets/Prefabs/ |
| Creep-big | Prefab | Assets/Prefabs/ |
| WinPopup | Prefab | Assets/Prefabs/UI/ |
| LosePopup | Prefab | Assets/Prefabs/UI/ |
| Terrain | Asset | Assets/Terrain/ |
| Various materials | Materials | Assets/Materials/ |

The MainScene already contains: Terrain, Base (centered), and SpawnPoints placed on the battlefield.

---

## 7. Deliverables - User Stories

Implementation order is designed to build systems incrementally, with each story producing a testable result.

### Story 1: Project Foundation

> As a developer, the project has a working architectural skeleton so gameplay systems can be built on a solid base.

**Acceptance Criteria:**
- Game bootstrap / entry point exists and runs on Play
- Game state machine is implemented with at least `Init` and `Playing` states
- Base has a component that systems can discover
- Folder structure and namespace (`MorlocsTD`) are established
- Test infrastructure is set up (Edit Mode and Play Mode test assemblies exist and run)
- A test verifies the game state machine transitions from `Init` to `Playing`

---

### Story 2: Creep Spawning & Movement

> As a player, I see creeps spawn from the edges of the battlefield and move toward my base.

**Acceptance Criteria:**
- Creeps instantiate from the pre-placed SpawnPoint positions at configurable intervals
- Creeps move in a straight line toward the Base position (no pathfinding)
- Spawn timing, creep count per spawn, and movement speed are exposed as tunable parameters
- Creeps use the provided Creep-small prefab

---

### Story 3: Base Health & Lose Condition

> As a player, when too many creeps reach my base, I am informed I have lost.

**Acceptance Criteria:**
- The Base has a configurable health value
- When a creep reaches the Base, it deals damage to the Base and is destroyed
- When Base health reaches zero, the LosePopup prefab is displayed
- A health bar or health indicator is visible for the Base (or creeps -- spec allows either)

---

### Story 4: Turret Placement

> As a player, I can place turrets on the battlefield using my mouse.

**Acceptance Criteria:**
- Clicking on the battlefield instantiates a turret at the clicked position
- The turret uses the provided Turret-regular prefab
- Turrets remain stationary after placement
- Placement works via mouse input on the terrain

---

### Story 5: Turret Shooting & Creep Damage

> As a player, turrets I've placed automatically shoot at nearby creeps and destroy them.

**Acceptance Criteria:**
- Turrets detect creeps within a configurable range
- Turrets fire projectiles at a configurable rate toward the nearest creep in range
- Projectiles travel toward the target and deal configurable damage on hit
- Creeps are destroyed when their HP reaches zero
- Damage amount and creep HP are exposed as tunable parameters

---

### Story 6: Economy System

> As a player, I spend coins to place turrets and earn coins from killing creeps.

**Acceptance Criteria:**
- Player starts with a configurable amount of coins
- Each turret costs 5 coins to place (configurable)
- Each creep awards 1 coin on death (configurable)
- Player cannot place a turret if they lack sufficient coins
- Current coin count is displayed in the UI

---

### Story 7: Turret Types (Regular & Freezing)

> As a player, I can choose between a regular turret and a freezing turret, each with different effects.

**Acceptance Criteria:**
- Two turret types are available: Regular and Freezing
- Regular turret deals direct damage (as already implemented)
- Freezing turret applies a slow effect to creeps it hits, reducing their movement speed for a configurable duration
- Player can select which turret type to place (e.g., keyboard shortcut or UI toggle)
- Each turret type uses its corresponding provided prefab
- Both turret types cost coins to place (may differ per type)

---

### Story 8: Creep Variety

> As a player, I face different types of creeps with varying difficulty.

**Acceptance Criteria:**
- Two creep types exist: Small and Big
- Small creeps are faster with lower HP
- Big creeps are slower with higher HP
- Each type uses its corresponding provided prefab (Creep-small, Creep-big)
- Creep attributes (speed, HP, coin reward) are defined in data and easy to extend

---

### Story 9: Wave System

> As a player, I face successive waves of creeps, and I win if I survive them all.

**Acceptance Criteria:**
- Creeps arrive in defined waves
- Each wave specifies which creep types spawn, how many, and at what intervals
- A wave is considered cleared when all its creeps are destroyed or have reached the base
- The next wave starts after the current wave is cleared (with optional delay)
- After all waves are cleared with the Base still alive, the WinPopup is displayed
- Wave definitions are data-driven and easy to add/modify

---

### Story 10: Game Reset

> As a player, I can restart the game after winning or losing without leaving Play Mode.

**Acceptance Criteria:**
- A restart button/option is available on both the WinPopup and LosePopup
- Restarting resets all game state: Base health, coins, wave progress, all spawned creeps destroyed, all placed turrets removed
- The game returns to its initial starting state and begins again
- No residual state from the previous session affects the new game
- Works entirely within a single Play Mode session (no editor stop/start required)
