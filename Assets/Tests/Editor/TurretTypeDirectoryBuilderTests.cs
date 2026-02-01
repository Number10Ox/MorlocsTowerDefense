using NUnit.Framework;
using UnityEngine;

public class TurretTypeDirectoryBuilderTests
{
    private const float DEFAULT_RANGE = 10f;
    private const float DEFAULT_FIRE_INTERVAL = 1f;
    private const int DEFAULT_DAMAGE = 1;
    private const float DEFAULT_PROJECTILE_SPEED = 15f;
    private const int DEFAULT_COST = 5;

    // --- Validation: Empty/Null ---

    [Test]
    public void TryBuild_NullEntries_ReturnsFalse()
    {
        bool result = TurretTypeDirectoryBuilder.TryBuild(
            null,
            out _, out string error);

        Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }

    [Test]
    public void TryBuild_EmptyEntries_ReturnsFalse()
    {
        bool result = TurretTypeDirectoryBuilder.TryBuild(
            new TurretTypeDefinition[0],
            out _, out string error);

        Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }

    // --- Validation: Null Prefab ---

    [Test]
    public void TryBuild_NullPrefab_ReturnsFalseWithIndexAndType()
    {
        var entries = new TurretTypeDefinition[] { MakeEntry(TurretType.Regular, null) };

        bool result = TurretTypeDirectoryBuilder.TryBuild(
            entries,
            out _, out string error);

        Assert.IsFalse(result);
        Assert.That(error, Does.Contain("Regular"));
        Assert.That(error, Does.Contain("entries[0]"));
    }

    // --- Validation: Duplicate Types ---

    [Test]
    public void TryBuild_DuplicateType_ReturnsFalseWithIndexAndType()
    {
        var prefab = new GameObject("Turret");
        try
        {
            var entries = new TurretTypeDefinition[]
            {
                MakeEntry(TurretType.Regular, prefab),
                MakeEntry(TurretType.Regular, prefab)
            };

            bool result = TurretTypeDirectoryBuilder.TryBuild(
                entries,
                out _, out string error);

            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("Duplicate"));
            Assert.That(error, Does.Contain("Regular"));
            Assert.That(error, Does.Contain("entries[1]"));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }

    // --- Valid Entries ---

    [Test]
    public void TryBuild_SingleEntry_Succeeds()
    {
        var prefab = new GameObject("Turret");
        try
        {
            var entries = new TurretTypeDefinition[] { MakeEntry(TurretType.Regular, prefab) };

            bool result = TurretTypeDirectoryBuilder.TryBuild(
                entries,
                out TurretTypeDirectory directory,
                out string error);

            Assert.IsTrue(result);
            Assert.IsNull(error);
            Assert.AreEqual(1, directory.OrderedTypes.Length);
            Assert.AreEqual(1, directory.OrderedStats.Length);
            Assert.AreEqual(1, directory.StatsByType.Count);
            Assert.AreEqual(1, directory.PrefabsByType.Count);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void TryBuild_MultipleEntries_BuildsCorrectDictionaries()
    {
        var regularPrefab = new GameObject("Regular");
        var freezingPrefab = new GameObject("Freezing");
        try
        {
            var entries = new TurretTypeDefinition[]
            {
                MakeEntry(TurretType.Regular, regularPrefab, cost: 5),
                MakeEntry(TurretType.Freezing, freezingPrefab, cost: 8)
            };

            bool result = TurretTypeDirectoryBuilder.TryBuild(
                entries,
                out TurretTypeDirectory directory,
                out string error);

            Assert.IsTrue(result);
            Assert.AreEqual(2, directory.StatsByType.Count);
            Assert.AreEqual(5, directory.StatsByType[TurretType.Regular].Cost);
            Assert.AreEqual(8, directory.StatsByType[TurretType.Freezing].Cost);
            Assert.AreEqual(regularPrefab, directory.PrefabsByType[TurretType.Regular]);
            Assert.AreEqual(freezingPrefab, directory.PrefabsByType[TurretType.Freezing]);
        }
        finally
        {
            Object.DestroyImmediate(regularPrefab);
            Object.DestroyImmediate(freezingPrefab);
        }
    }

    // --- Ordering Contract ---

    [Test]
    public void TryBuild_OrderedTypes_PreservesEntryOrder()
    {
        var regularPrefab = new GameObject("Regular");
        var freezingPrefab = new GameObject("Freezing");
        try
        {
            var entries = new TurretTypeDefinition[]
            {
                MakeEntry(TurretType.Freezing, freezingPrefab),
                MakeEntry(TurretType.Regular, regularPrefab)
            };

            TurretTypeDirectoryBuilder.TryBuild(entries, out TurretTypeDirectory directory, out _);

            Assert.AreEqual(TurretType.Freezing, directory.OrderedTypes[0]);
            Assert.AreEqual(TurretType.Regular, directory.OrderedTypes[1]);
        }
        finally
        {
            Object.DestroyImmediate(regularPrefab);
            Object.DestroyImmediate(freezingPrefab);
        }
    }

    [Test]
    public void TryBuild_OrderedStatsAndTypes_InSync()
    {
        var regularPrefab = new GameObject("Regular");
        var freezingPrefab = new GameObject("Freezing");
        try
        {
            var entries = new TurretTypeDefinition[]
            {
                MakeEntry(TurretType.Regular, regularPrefab),
                MakeEntry(TurretType.Freezing, freezingPrefab)
            };

            TurretTypeDirectoryBuilder.TryBuild(entries, out TurretTypeDirectory directory, out _);

            for (int i = 0; i < directory.OrderedTypes.Length; i++)
            {
                Assert.AreEqual(directory.OrderedTypes[i], directory.OrderedStats[i].Type,
                    $"orderedStats[{i}].Type does not match orderedTypes[{i}]");
            }
        }
        finally
        {
            Object.DestroyImmediate(regularPrefab);
            Object.DestroyImmediate(freezingPrefab);
        }
    }

    [Test]
    public void TryBuild_DefaultType_IsFirstEntry()
    {
        var regularPrefab = new GameObject("Regular");
        var freezingPrefab = new GameObject("Freezing");
        try
        {
            var entries = new TurretTypeDefinition[]
            {
                MakeEntry(TurretType.Freezing, freezingPrefab),
                MakeEntry(TurretType.Regular, regularPrefab)
            };

            TurretTypeDirectoryBuilder.TryBuild(entries, out TurretTypeDirectory directory, out _);

            Assert.AreEqual(TurretType.Freezing, directory.DefaultType);
        }
        finally
        {
            Object.DestroyImmediate(regularPrefab);
            Object.DestroyImmediate(freezingPrefab);
        }
    }

    // --- Helpers ---

    private TurretTypeDefinition MakeEntry(TurretType type, GameObject prefab, int cost = DEFAULT_COST)
    {
        // TurretTypeDefinition is a struct with private serialized fields.
        // We use reflection to set values for testing since there's no public constructor.
        var def = new TurretTypeDefinition();
        SetField(ref def, "turretType", type);
        SetField(ref def, "prefab", prefab);
        SetField(ref def, "damage", DEFAULT_DAMAGE);
        SetField(ref def, "range", DEFAULT_RANGE);
        SetField(ref def, "fireInterval", DEFAULT_FIRE_INTERVAL);
        SetField(ref def, "projectileSpeed", DEFAULT_PROJECTILE_SPEED);
        SetField(ref def, "cost", cost);
        SetField(ref def, "slowDuration", 0f);
        SetField(ref def, "slowMultiplier", 1f);
        return def;
    }

    private static void SetField<T>(ref TurretTypeDefinition def, string fieldName, T value)
    {
        var field = typeof(TurretTypeDefinition).GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        object boxed = def;
        field.SetValue(boxed, value);
        def = (TurretTypeDefinition)boxed;
    }
}
