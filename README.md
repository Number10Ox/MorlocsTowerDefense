# Morlocs Tower Defense

A tower defense game built in Unity. Creeps spawn from fixed points and move toward a central base. The player places turrets to shoot and destroy creeps before they reach the base.

## Controls

| Input | Action |
|-------|--------|
| Left Click | Place turret at terrain position |
| 1 | Select Regular turret |
| 2 | Select Freezing turret |
| R | Restart game (from Win/Lose screen) |

## Turret Types

| Type | Effect | Cost |
|------|--------|------|
| Regular | Direct damage | Configured via TurretDefinitions SO |
| Freezing | Damage + slow effect (reduces creep speed for a duration) | Configured via TurretDefinitions SO |

Slow duration and multiplier are configured on the TurretDefinitions ScriptableObject. The freezing effect reduces creep movement speed by the configured multiplier for the configured duration. Multiple hits refresh the slow timer.

## Creep Types

| Type | Characteristics |
|------|----------------|
| Small | Faster, lower HP |
| Big | Slower, higher HP |

Creep attributes (speed, HP, damage to base, coin reward) are configured via the CreepDefinitions ScriptableObject. Types and counts are defined per wave in the WaveConfig ScriptableObject. Spawn positions are assigned round-robin across SpawnPoints.

## Project Structure

- `Assets/Scripts/` - All game code (role-based folder organization)
  - `App/` - Composition root (`GameFlowController`), `GameSession`
  - `Framework/` - State machine, system scheduler, interfaces
  - `States/` - Game state implementations (Init, Playing, Win, Lose)
  - `Stores/` - Authoritative data stores (CreepStore, TurretStore, EconomyStore, etc.)
  - `SimData/` - Simulation data structs (CreepSimData, TurretSimData, etc.)
  - `Systems/` - Game logic systems (SpawnSystem, MovementSystem, DamageSystem, etc.)
  - `Components/` - Thin MonoBehaviour components for scene objects
  - `Input/` - Input bridge classes (PlacementInput)
  - `Presentation/` - Visual sync and UI (PresentationAdapter, HUDs, GameUiCoordinator)
  - `Data/` - ScriptableObject definitions (CreepDefinitions, TurretDefinitions, WaveConfig, etc.)
- `Assets/Tests/Editor/` - Edit Mode unit and integration tests
- `Docs/` - TDD, architecture diagrams, project status

## Setup

1. Open the project in Unity
2. Open `MainScene`
3. Enter Play Mode

All scene references and ScriptableObject assignments are pre-configured on the `GameFlowController` GameObject in MainScene.

## Running Tests

Window > General > Test Runner > Edit Mode > Run All

## Documentation

- [Technical Design Document](Docs/TDD.md)
- [Architecture Diagrams](Docs/Architecture-Diagrams.md)
- [Project Status](Docs/STATUS.md)
