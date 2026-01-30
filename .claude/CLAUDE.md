# Claude Code Rules

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

- **Namespaces**: Always use a project namespace
- **Folder structure**: Feature-based organization under `Assets/Scripts/`
- **Using statements**: System first, then UnityEngine, then project namespaces
- **Member order in classes**: Fields, then lifecycle methods (`Awake`/`Start`/`Update`/`OnDestroy`), then public methods, then private methods

### Field & Access Patterns

- Prefer `[SerializeField] private` over `public` fields for editor-exposed data
- Use expression-bodied properties for read-only access: `public Transform Root => root;`
- Use `private` by default; only expose what is needed

### Code Clarity

- Self-documenting method names preferred over comments
- Minimal inline comments; use only where logic is non-obvious
- No XML doc comments (`///`) unless defining a public API consumed by other assemblies
- Use string interpolation: `Debug.LogError($"Failed to load: {assetName}");`
- Use `new()` target-typed syntax: `private Dictionary<string, int> lookup = new();`

### Error Handling

- Null-check serialized dependencies in `Awake()` with `Debug.LogError` and `enabled = false`
- Use `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` consistently
- Prefer early returns over deep nesting

### Event Patterns

- Use C# `event Action<T>` or custom delegates for system-level events
- Use `?.Invoke()` for safe invocation
- Avoid UnityEvents in code-driven systems (reserve for designer-facing inspector hookups)

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
