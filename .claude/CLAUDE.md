# Claude Code Rules

## Session Start

At the start of each session, check for and read the following files if they exist:
- `Docs/STATUS.md` -- project progress, key decisions, and open questions
- `Docs/TDD.md` -- technical design document with architecture, constraints, and user stories

---

## Unity C# Coding Style

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `WaveManager`, `AudioSystem` |
| Methods (public & private) | PascalCase | `PlayAnimation()`, `LoadAssets()` |
| Private fields | camelCase | `private float blendTimer;` |
| Serialized private fields | camelCase with attribute | `[SerializeField] private float fireRate;` |
| Properties | PascalCase | `public Transform ModelRoot => modelRoot;` |
| Constants | UPPER_SNAKE_CASE | `private const string BGM_MIXER_GROUP_KEY = "BGM";` |
| Enums | PascalCase type and values | `enum CreepType { Small, Big }` |
| Interfaces | I-prefix, PascalCase | `IDamageable`, `IPoolable` |

### Formatting

- **Brace style**: Allman (opening brace on its own line)
- **Indentation**: 4 spaces (no tabs)
- **Blank lines**: One blank line between members
- **`[SerializeField]`**: On the same line as the field declaration
- **No `#region`**: Do not use `#region`/`#endregion` blocks

### Code Organization

- **Namespaces**: No project-wide namespace on application code. Generic reusable classes that are not specific to the project (e.g., state machine infrastructure, object pooling) get a namespace appropriate to their purpose. Project-specific code (gameplay systems, components, data) does not. A namespace collision with Unity or a library is a design smell — it means you are recreating something that already exists or building something generic enough to belong in a library.
- **Folder structure**: Feature-based organization under `Assets/Scripts/`
- **Using statements**: System first, then UnityEngine, then project namespaces
- **Member order in classes**: Fields, then lifecycle methods (`Awake`/`Start`/`Update`/`OnDestroy`), then public methods, then private methods

### Field & Access Patterns

- Prefer `[SerializeField] private` over `public` fields for editor-exposed data
- Use expression-bodied properties for read-only access: `public Transform Root => root;`
- Use `private` by default; only expose what is needed
- **Properties over public fields** — Expose data through properties, not raw public fields. Exception: pure data containers (structs or classes that exist solely to hold data with no behavior) may use public fields.

### Component Reference Patterns

- **Avoid `GetComponent` in hot paths** — `GetComponent<T>()` is a runtime lookup. Never call it per-frame, in Update loops, or inside high-frequency iteration (pool Get/Return, per-creep ticks). Treat any `GetComponent` call in a loop as a performance bug.
- **Prefer direct serialized references** — Drag-and-drop in the Inspector is the cheapest and most explicit way to wire dependencies: `[SerializeField] private CreepComponent creepComponent;`
- **Cache once in Awake** — When a serialized reference isn't practical, call `GetComponent` once during `Awake`/`OnEnable` and store the result in a field. Never re-fetch what you already have.
- **Use `TryGetComponent` for fallible lookups** — When the component may legitimately be absent, use `TryGetComponent` (avoids exceptions and is clearer intent than null-checking `GetComponent`).
- **Enforce prefab contracts with `[RequireComponent]`** — If a MonoBehaviour always needs a sibling component, declare `[RequireComponent(typeof(T))]` on the class. This makes the dependency editor-enforced rather than runtime-enforced.
- **Cache in pooling infrastructure** — Object pools that call `TryGetComponent` on Get/Return should cache the component reference per instance (e.g., in a `Dictionary<GameObject, IPoolable>`) to avoid repeated lookups across the object's pooled lifetime.
- **Constructor injection for pure C# objects** — Systems, adapters, and other non-MonoBehaviour classes receive their dependencies through constructors, not through runtime lookups.

### Code Clarity

- Self-documenting method names preferred over comments
- Minimal inline comments; use only where logic is non-obvious
- No XML doc comments (`///`) unless defining a public API consumed by other assemblies
- Use string interpolation: `Debug.LogError($"Failed to load: {assetName}");`
- Use `new()` target-typed syntax: `private Dictionary<string, int> lookup = new();`
- **No magic numbers** — Define named constants for numeric literals that carry domain meaning. Trivially obvious values (0, 1, -1, dividing/multiplying by 2) and serialized field defaults in ScriptableObjects are exempt.

### Error Handling

- Null-check serialized dependencies in `Awake()` with `Debug.LogError` and `enabled = false`
- Use `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` consistently
- Prefer early returns over deep nesting

### Event Patterns

- Use C# `event Action<T>` or custom delegates for system-level events
- Use `?.Invoke()` for safe invocation
- Avoid UnityEvents in code-driven systems (reserve for designer-facing inspector hookups)

### Performance Rules

- **No LINQ in runtime code** - Do not use `System.Linq` in any runtime (non-editor-tool) code. LINQ causes hidden allocations and GC pressure. Use explicit loops, arrays, and manual collection operations instead.
- **Minimize GC allocations** - Every `GC.Alloc` in a hot path is a bug. Specific rules:
  - **Boxing** - Never pass value types to `object` parameters. Avoid non-generic collections (`ArrayList`, `Hashtable`). Use generic collections and interfaces.
  - **Object pooling** - Pool frequently created/destroyed objects (creeps, projectiles). Design systems for clean teardown and rebuild without residual allocations.
  - **Pre-allocate collections** - Size lists/arrays upfront. Reuse with `Clear()` instead of `new`. No `new List<T>()` in per-frame code.
  - **Cache references** - Cache results of `GetComponent<T>()`, `FindObjectOfType<T>()`, and similar lookups. Never call them in Update or hot loops.
  - **Strings** - Avoid string concatenation (`+`) in hot paths; it allocates. Use `StringBuilder` or pre-built strings. Be cautious with `Debug.Log` string interpolation in loops.
  - **Coroutine yields** - Cache `WaitForSeconds` and other yield instruction instances in fields. `yield return new WaitForSeconds()` allocates every call.
  - **foreach** - Safe on arrays and `List<T>`. Avoid `foreach` on non-generic or custom `IEnumerable` implementations that allocate enumerators.
  - **Lambdas/closures** - Acceptable for setup, configuration, and infrequent callbacks. Avoid creating new lambdas with captures in per-frame or high-frequency code paths. Cache delegates in fields when a callback with captures is needed on a hot path.
  - **Structs over classes** - Prefer structs for hot-path data to avoid heap allocation. Be mindful of struct size and copying costs.

---

## Unity Architectural Preferences

### Architectural Principles

- **Data-oriented design** - Separate data from behavior. Game state lives in plain data structures (structs, ScriptableObjects); systems operate on that data. Prefer structs for hot-path data. Minimize scattered state across MonoBehaviours.
- **System-and-component thinking** - MonoBehaviours act as thin components that hold references and wire into Unity lifecycle. Logic lives in systems/managers that process components, not in the components themselves.
- **MonoBehaviour minimalism** - Only inherit from MonoBehaviour when Unity requires it (scene presence, coroutines, serialized inspector references, collision callbacks). Pure logic, data models, state machines, and utility classes should be plain C# classes or structs. Avoid an architecture dominated by per-object `Update()` calls; prefer centralized system ticks that iterate over data.
- **Simulation/presentation separation** - The game simulation (state, logic, rules) is independent of visuals. Simulation ticks on data and produces state changes; the presentation layer reads state and updates transforms, effects, and UI. No gameplay logic should depend on visual state.
- **MVU-style UI architecture** - UI follows a Model-View-Update pattern: a model (state) drives what the view displays; user actions produce messages/events that update the model; the view re-renders from the model. No direct UI-to-game-state mutation.
- **State machine state management** - Game flow managed by explicit state machines. Lightweight custom implementation -- no Stateless library (avoids LINQ and allocation concerns). States are data, transitions are explicit.
- **Interfaces over concrete inheritance** - Do not inherit from concrete base classes. Use interfaces for contracts and composition for code sharing. Abstract base classes are acceptable when they provide genuine shared behavior behind an interface. Concrete class inheritance leads to fragile hierarchies and hidden coupling.

### Preferred Tech Choices

| Technology | Purpose | Notes |
|-----------|---------|-------|
| **Input System** (new) | Player input | Use `UnityEngine.InputSystem`. Do not use legacy `Input` class. |
| **UI Toolkit** | Runtime UI | UXML for layout, USS for styles, C# for binding. Preferred over UGUI for new UI. |
| **Cinemachine** | Camera control | Use for camera management when the project requires camera work. |
| **ScriptableObjects** | Tuning data | Definitions, configurations, and tunable parameters. |
| **Addressables** | Asset loading | Use for extensible sets of assets including ScriptableObject collections. Enables runtime-loadable, data-driven content without hardcoded references. |

---

## Testing Strategy

Tests are required for every deliverable. Tests must be written alongside feature implementation, not deferred.

### Test Types

- **Edit Mode Tests** (`Assets/Tests/Editor/`): Unit tests for pure logic that does not require a running scene. Examples: damage calculations, economy math, state progression logic, data validation.
- **Play Mode Tests** (`Assets/Tests/Runtime/`): Integration tests that require MonoBehaviour lifecycle, scene loading, or coroutines. Examples: object movement, targeting, collision, state reset.

### Per-Deliverable Test Requirements

Each deliverable must include:
1. **Happy-path tests** verifying the acceptance criteria are met
2. **Edge case analysis** documented in the test file or as comments
3. **Coverage review** before signing off -- confirm all acceptance criteria have corresponding test assertions

### Test Conventions

- Test class naming: `[FeatureName]Tests` (e.g., `SpawningTests`, `EconomyTests`)
- Test method naming: `MethodOrBehavior_Condition_ExpectedResult` (e.g., `TakeDamage_HealthReachesZero_EntityDestroyed`)
- Use `[SetUp]` / `[TearDown]` for shared test fixtures
- Use `Assert` (NUnit) for assertions
- Use `[UnityTest]` for Play Mode coroutine-based tests
- Use `[Test]` for synchronous Edit Mode tests

### Deliverable Sign-Off Checklist

Before a deliverable is considered complete:
- All acceptance criteria have passing tests
- Edge cases identified and tested or documented as out of scope
- No regressions in previously passing tests
- TDD.md updated to reflect any architectural changes made during implementation
- Architecture-Diagrams.md updated to reflect any structural changes (new classes, changed relationships, modified sequences)
- Critical code review: systematic pass through all new/modified production files checking for bugs, single-writer violations, event lifecycle issues (subscribe/unsubscribe), frame-ordering correctness, null safety, and adherence to architectural constraints (no LINQ, no GetComponent in hot paths, simulation/presentation separation)
