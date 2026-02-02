using NUnit.Framework;
using UnityEngine;

public class CreepTypeDirectoryBuilderTests
{
    private const float DEFAULT_SPEED = 5f;
    private const int DEFAULT_DAMAGE_TO_BASE = 1;
    private const int DEFAULT_MAX_HEALTH = 3;
    private const int DEFAULT_COIN_REWARD = 1;

    // --- Validation: Empty/Null ---

    [Test]
    public void TryBuild_NullEntries_ReturnsFalse()
    {
        bool result = CreepTypeDirectoryBuilder.TryBuild(
            null,
            out _, out string error);

        Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }

    [Test]
    public void TryBuild_EmptyEntries_ReturnsFalse()
    {
        bool result = CreepTypeDirectoryBuilder.TryBuild(
            new CreepTypeDefinition[0],
            out _, out string error);

        Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }

    // --- Validation: Null Prefab ---

    [Test]
    public void TryBuild_NullPrefab_ReturnsFalseWithIndexAndType()
    {
        var entries = new CreepTypeDefinition[] { MakeEntry(CreepType.Small, null) };

        bool result = CreepTypeDirectoryBuilder.TryBuild(
            entries,
            out _, out string error);

        Assert.IsFalse(result);
        Assert.That(error, Does.Contain("Small"));
        Assert.That(error, Does.Contain("entries[0]"));
    }

    // --- Validation: Duplicate Types ---

    [Test]
    public void TryBuild_DuplicateType_ReturnsFalseWithIndexAndType()
    {
        var prefab = new GameObject("Creep");
        try
        {
            var entries = new CreepTypeDefinition[]
            {
                MakeEntry(CreepType.Small, prefab),
                MakeEntry(CreepType.Small, prefab)
            };

            bool result = CreepTypeDirectoryBuilder.TryBuild(
                entries,
                out _, out string error);

            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("Duplicate"));
            Assert.That(error, Does.Contain("Small"));
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
        var prefab = new GameObject("Creep");
        try
        {
            var entries = new CreepTypeDefinition[] { MakeEntry(CreepType.Small, prefab) };

            bool result = CreepTypeDirectoryBuilder.TryBuild(
                entries,
                out CreepTypeDirectory directory,
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
        var smallPrefab = new GameObject("Small");
        var bigPrefab = new GameObject("Big");
        try
        {
            var entries = new CreepTypeDefinition[]
            {
                MakeEntry(CreepType.Small, smallPrefab, speed: 8f, maxHealth: 2),
                MakeEntry(CreepType.Big, bigPrefab, speed: 3f, maxHealth: 10)
            };

            bool result = CreepTypeDirectoryBuilder.TryBuild(
                entries,
                out CreepTypeDirectory directory,
                out string error);

            Assert.IsTrue(result);
            Assert.AreEqual(2, directory.StatsByType.Count);
            Assert.AreEqual(8f, directory.StatsByType[CreepType.Small].Speed, 0.001f);
            Assert.AreEqual(2, directory.StatsByType[CreepType.Small].MaxHealth);
            Assert.AreEqual(3f, directory.StatsByType[CreepType.Big].Speed, 0.001f);
            Assert.AreEqual(10, directory.StatsByType[CreepType.Big].MaxHealth);
            Assert.AreEqual(smallPrefab, directory.PrefabsByType[CreepType.Small]);
            Assert.AreEqual(bigPrefab, directory.PrefabsByType[CreepType.Big]);
        }
        finally
        {
            Object.DestroyImmediate(smallPrefab);
            Object.DestroyImmediate(bigPrefab);
        }
    }

    // --- Ordering Contract ---

    [Test]
    public void TryBuild_OrderedTypes_PreservesEntryOrder()
    {
        var smallPrefab = new GameObject("Small");
        var bigPrefab = new GameObject("Big");
        try
        {
            // Intentionally Big first, then Small
            var entries = new CreepTypeDefinition[]
            {
                MakeEntry(CreepType.Big, bigPrefab),
                MakeEntry(CreepType.Small, smallPrefab)
            };

            CreepTypeDirectoryBuilder.TryBuild(entries, out CreepTypeDirectory directory, out _);

            Assert.AreEqual(CreepType.Big, directory.OrderedTypes[0]);
            Assert.AreEqual(CreepType.Small, directory.OrderedTypes[1]);
        }
        finally
        {
            Object.DestroyImmediate(smallPrefab);
            Object.DestroyImmediate(bigPrefab);
        }
    }

    [Test]
    public void TryBuild_OrderedStatsAndTypes_InSync()
    {
        var smallPrefab = new GameObject("Small");
        var bigPrefab = new GameObject("Big");
        try
        {
            var entries = new CreepTypeDefinition[]
            {
                MakeEntry(CreepType.Small, smallPrefab),
                MakeEntry(CreepType.Big, bigPrefab)
            };

            CreepTypeDirectoryBuilder.TryBuild(entries, out CreepTypeDirectory directory, out _);

            for (int i = 0; i < directory.OrderedTypes.Length; i++)
            {
                Assert.AreEqual(directory.OrderedTypes[i], directory.OrderedStats[i].Type,
                    $"orderedStats[{i}].Type does not match orderedTypes[{i}]");
            }
        }
        finally
        {
            Object.DestroyImmediate(smallPrefab);
            Object.DestroyImmediate(bigPrefab);
        }
    }

    // --- Helpers ---

    private CreepTypeDefinition MakeEntry(
        CreepType type,
        GameObject prefab,
        float speed = DEFAULT_SPEED,
        int damageToBase = DEFAULT_DAMAGE_TO_BASE,
        int maxHealth = DEFAULT_MAX_HEALTH,
        int coinReward = DEFAULT_COIN_REWARD)
    {
        var def = new CreepTypeDefinition();
        SetField(ref def, "creepType", type);
        SetField(ref def, "prefab", prefab);
        SetField(ref def, "speed", speed);
        SetField(ref def, "damageToBase", damageToBase);
        SetField(ref def, "maxHealth", maxHealth);
        SetField(ref def, "coinReward", coinReward);
        return def;
    }

    private static void SetField<T>(ref CreepTypeDefinition def, string fieldName, T value)
    {
        var field = typeof(CreepTypeDefinition).GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        object boxed = def;
        field.SetValue(boxed, value);
        def = (CreepTypeDefinition)boxed;
    }
}
