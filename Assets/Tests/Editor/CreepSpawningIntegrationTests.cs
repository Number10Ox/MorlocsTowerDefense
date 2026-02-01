using NUnit.Framework;
using UnityEngine;

public class CreepSpawningIntegrationTests
{
    private CreepStore store;
    private SpawnSystem spawnSystem;
    private MovementSystem movementSystem;

    [SetUp]
    public void SetUp()
    {
        store = new CreepStore();

        var spawnPositions = new[] { new Vector3(10f, 0f, 0f) };
        var basePosition = Vector3.zero;

        spawnSystem = new SpawnSystem(
            store,
            spawnPositions,
            basePosition,
            spawnInterval: 1f,
            creepsPerSpawn: 1,
            creepSpeed: 10f,
            damageToBase: 1,
            maxHealth: 3);

        movementSystem = new MovementSystem(store, arrivalThreshold: 0.5f);
    }

    [Test]
    public void SpawnMoveRemove_FullLifecycle()
    {
        // Frame 1: BeginFrame + SpawnSystem spawns a creep
        store.BeginFrame();
        spawnSystem.Tick(1.0f);
        Assert.AreEqual(1, store.ActiveCreeps.Count);
        Assert.AreEqual(1, store.SpawnedThisFrame.Count);

        // SpawnedThisFrame holds a live reference to CreepSimData (not a snapshot)
        CreepSimData spawnedRef = store.SpawnedThisFrame[0];

        // MovementSystem moves creep toward base
        movementSystem.Tick(0.5f);
        Assert.Less(store.ActiveCreeps[0].Position.x, 10f, "Creep has moved");
        Assert.AreSame(spawnedRef, store.ActiveCreeps[0], "SpawnedThisFrame holds same instance as ActiveCreeps");

        // Tick movement repeatedly until creep reaches base
        for (int i = 0; i < 100; i++)
        {
            if (spawnedRef.ReachedBase)
            {
                break;
            }
            movementSystem.Tick(0.1f);
        }
        Assert.IsTrue(spawnedRef.ReachedBase, "Creep should have reached base");
        Assert.AreEqual(Vector3.zero, spawnedRef.Position, "Position snapped to target");

        // Creep is still in active list this frame (deferred removal)
        Assert.AreEqual(1, store.ActiveCreeps.Count);

        // Next frame: BeginFrame flushes the removal
        store.BeginFrame();
        Assert.AreEqual(0, store.ActiveCreeps.Count, "Creep removed after BeginFrame");
        Assert.AreEqual(1, store.RemovedIdsThisFrame.Count, "Removal reported in change list");
    }

    [Test]
    public void MultipleCreeps_IndependentLifecycles()
    {
        var twoPointStore = new CreepStore();
        var twoPointSpawn = new SpawnSystem(
            twoPointStore,
            new[] { new Vector3(5f, 0f, 0f), new Vector3(20f, 0f, 0f) },
            Vector3.zero,
            spawnInterval: 1f,
            creepsPerSpawn: 1,
            creepSpeed: 10f,
            damageToBase: 1,
            maxHealth: 3);
        var twoPointMovement = new MovementSystem(twoPointStore, arrivalThreshold: 0.5f);

        // Frame 1: Spawn both creeps
        twoPointStore.BeginFrame();
        twoPointSpawn.Tick(1.0f);
        Assert.AreEqual(2, twoPointStore.ActiveCreeps.Count);

        // Identify creeps by spawn position (don't assume ID assignment order)
        int closeId = -1;
        int farId = -1;
        for (int i = 0; i < twoPointStore.ActiveCreeps.Count; i++)
        {
            var c = twoPointStore.ActiveCreeps[i];
            float distToBase = Vector3.Distance(c.Position, Vector3.zero);
            if (distToBase < 10f) closeId = c.Id;
            else farId = c.Id;
        }
        Assert.AreNotEqual(-1, closeId, "Should find close creep");
        Assert.AreNotEqual(-1, farId, "Should find far creep");

        // Closer creep (5 units away) should arrive before the far one (20 units away)
        // At speed 10, close creep arrives in ~0.5s, far creep in ~2.0s
        bool closeArrived = false;
        bool farArrived = false;

        for (int frame = 0; frame < 50; frame++)
        {
            twoPointStore.BeginFrame();
            twoPointMovement.Tick(0.1f);

            for (int i = 0; i < twoPointStore.ActiveCreeps.Count; i++)
            {
                var creep = twoPointStore.ActiveCreeps[i];
                if (creep.Id == closeId && creep.ReachedBase) closeArrived = true;
                if (creep.Id == farId && creep.ReachedBase) farArrived = true;
            }

            if (closeArrived && !farArrived)
            {
                break;
            }
        }

        Assert.IsTrue(closeArrived, "Close creep should arrive first");
        Assert.IsFalse(farArrived, "Far creep should not have arrived yet");

        // Continue ticking until far creep arrives
        for (int frame = 0; frame < 50; frame++)
        {
            twoPointStore.BeginFrame();
            twoPointMovement.Tick(0.1f);

            if (twoPointStore.ActiveCreeps.Count == 0)
            {
                break;
            }

            for (int i = 0; i < twoPointStore.ActiveCreeps.Count; i++)
            {
                if (twoPointStore.ActiveCreeps[i].Id == farId && twoPointStore.ActiveCreeps[i].ReachedBase)
                {
                    farArrived = true;
                }
            }

            if (farArrived) break;
        }

        Assert.IsTrue(farArrived, "Far creep should eventually arrive");
    }
}
