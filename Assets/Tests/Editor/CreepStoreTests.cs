using NUnit.Framework;
using UnityEngine;

public class CreepStoreTests
{
    private CreepStore store;

    [SetUp]
    public void SetUp()
    {
        store = new CreepStore();
    }

    [Test]
    public void Add_CreepAppearsInActiveCreeps()
    {
        var creep = MakeCreep(1);

        store.Add(creep);

        Assert.AreEqual(1, store.ActiveCreeps.Count);
        Assert.AreEqual(1, store.ActiveCreeps[0].Id);
    }

    [Test]
    public void Add_CreepAppearsInSpawnedThisFrame()
    {
        var creep = MakeCreep(1);

        store.Add(creep);

        Assert.AreEqual(1, store.SpawnedThisFrame.Count);
        Assert.AreSame(creep, store.SpawnedThisFrame[0]);
    }

    [Test]
    public void MarkForRemoval_CreepStillActiveBeforeFlush()
    {
        store.Add(MakeCreep(1));

        store.MarkForRemoval(1);

        Assert.AreEqual(1, store.ActiveCreeps.Count);
    }

    [Test]
    public void BeginFrame_FlushesMarkedRemovals()
    {
        store.Add(MakeCreep(1));
        store.MarkForRemoval(1);

        store.BeginFrame();

        Assert.AreEqual(0, store.ActiveCreeps.Count);
    }

    [Test]
    public void BeginFrame_PopulatesRemovedIdsThisFrame()
    {
        store.Add(MakeCreep(1));
        store.MarkForRemoval(1);

        store.BeginFrame();

        Assert.AreEqual(1, store.RemovedIdsThisFrame.Count);
        Assert.AreEqual(1, store.RemovedIdsThisFrame[0]);
    }

    [Test]
    public void BeginFrame_ClearsSpawnedThisFrame()
    {
        store.Add(MakeCreep(1));
        Assert.AreEqual(1, store.SpawnedThisFrame.Count);

        store.BeginFrame();

        Assert.AreEqual(0, store.SpawnedThisFrame.Count);
    }

    [Test]
    public void BeginFrame_ClearsRemovedIdsThisFrame_FromPriorFrame()
    {
        store.Add(MakeCreep(1));
        store.MarkForRemoval(1);
        store.BeginFrame();
        Assert.AreEqual(1, store.RemovedIdsThisFrame.Count);

        store.BeginFrame();

        Assert.AreEqual(0, store.RemovedIdsThisFrame.Count);
    }

    [Test]
    public void MarkForRemoval_InvalidId_DoesNotCrash()
    {
        store.Add(MakeCreep(1));

        store.MarkForRemoval(999);
        store.BeginFrame();

        Assert.AreEqual(1, store.ActiveCreeps.Count);
        Assert.AreEqual(0, store.RemovedIdsThisFrame.Count);
    }

    [Test]
    public void Reset_ClearsAllState()
    {
        store.Add(MakeCreep(1));
        store.Add(MakeCreep(2));
        store.MarkForRemoval(1);

        store.Reset();

        Assert.AreEqual(0, store.ActiveCreeps.Count);
        Assert.AreEqual(0, store.SpawnedThisFrame.Count);
        Assert.AreEqual(0, store.RemovedIdsThisFrame.Count);
    }

    [Test]
    public void MultipleAddsAndRemovals_CorrectState()
    {
        store.Add(MakeCreep(1));
        store.Add(MakeCreep(2));
        store.Add(MakeCreep(3));
        Assert.AreEqual(3, store.ActiveCreeps.Count);
        Assert.AreEqual(3, store.SpawnedThisFrame.Count);

        store.MarkForRemoval(1);
        store.MarkForRemoval(3);
        store.BeginFrame();

        Assert.AreEqual(1, store.ActiveCreeps.Count);
        Assert.IsTrue(ContainsCreepWithId(store.ActiveCreeps, 2), "Remaining creep should have Id 2");
        Assert.AreEqual(2, store.RemovedIdsThisFrame.Count);
        Assert.IsTrue(ContainsRemovedId(store.RemovedIdsThisFrame, 1), "Should report removal of Id 1");
        Assert.IsTrue(ContainsRemovedId(store.RemovedIdsThisFrame, 3), "Should report removal of Id 3");
        Assert.AreEqual(0, store.SpawnedThisFrame.Count);
    }

    private static bool ContainsCreepWithId(System.Collections.Generic.IReadOnlyList<CreepSimData> creeps, int id)
    {
        for (int i = 0; i < creeps.Count; i++)
        {
            if (creeps[i].Id == id) return true;
        }
        return false;
    }

    private static bool ContainsRemovedId(System.Collections.Generic.IReadOnlyList<int> ids, int id)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == id) return true;
        }
        return false;
    }

    private static CreepSimData MakeCreep(int id)
    {
        return new CreepSimData(id)
        {
            Position = Vector3.zero,
            Target = Vector3.one,
            Speed = 5f
        };
    }
}
