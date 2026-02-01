using NUnit.Framework;
using UnityEngine;

public class ProjectileStoreTests
{
    private ProjectileStore store;

    [SetUp]
    public void SetUp()
    {
        store = new ProjectileStore();
    }

    // --- Add ---

    [Test]
    public void Add_ProjectileAppearsInActiveProjectiles()
    {
        var proj = MakeProjectile(1);

        store.Add(proj);

        Assert.AreEqual(1, store.ActiveProjectiles.Count);
        Assert.AreEqual(1, store.ActiveProjectiles[0].Id);
    }

    [Test]
    public void Add_ProjectileAppearsInSpawnedThisFrame()
    {
        var proj = MakeProjectile(1);

        store.Add(proj);

        Assert.AreEqual(1, store.SpawnedThisFrame.Count);
        Assert.AreSame(proj, store.SpawnedThisFrame[0]);
    }

    [Test]
    public void Add_NullThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => store.Add(null));
    }

    [Test]
    public void Add_MultipleProjectiles_AllPresent()
    {
        store.Add(MakeProjectile(1));
        store.Add(MakeProjectile(2));
        store.Add(MakeProjectile(3));

        Assert.AreEqual(3, store.ActiveProjectiles.Count);
        Assert.AreEqual(3, store.SpawnedThisFrame.Count);
    }

    // --- MarkForRemoval / BeginFrame ---

    [Test]
    public void MarkForRemoval_ProjectileStillActiveBeforeFlush()
    {
        store.Add(MakeProjectile(1));

        store.MarkForRemoval(1);

        Assert.AreEqual(1, store.ActiveProjectiles.Count);
    }

    [Test]
    public void BeginFrame_FlushesMarkedRemovals()
    {
        store.Add(MakeProjectile(1));
        store.MarkForRemoval(1);

        store.BeginFrame();

        Assert.AreEqual(0, store.ActiveProjectiles.Count);
    }

    [Test]
    public void BeginFrame_PopulatesRemovedIdsThisFrame()
    {
        store.Add(MakeProjectile(1));
        store.MarkForRemoval(1);

        store.BeginFrame();

        Assert.AreEqual(1, store.RemovedIdsThisFrame.Count);
        Assert.AreEqual(1, store.RemovedIdsThisFrame[0]);
    }

    [Test]
    public void BeginFrame_ClearsSpawnedThisFrame()
    {
        store.Add(MakeProjectile(1));
        Assert.AreEqual(1, store.SpawnedThisFrame.Count);

        store.BeginFrame();

        Assert.AreEqual(0, store.SpawnedThisFrame.Count);
    }

    [Test]
    public void BeginFrame_ClearsHitsThisFrame()
    {
        store.RecordHit(new ProjectileHit(1, 5));
        Assert.AreEqual(1, store.HitsThisFrame.Count);

        store.BeginFrame();

        Assert.AreEqual(0, store.HitsThisFrame.Count);
    }

    [Test]
    public void BeginFrame_ClearsRemovedIdsThisFrame_FromPriorFrame()
    {
        store.Add(MakeProjectile(1));
        store.MarkForRemoval(1);
        store.BeginFrame();
        Assert.AreEqual(1, store.RemovedIdsThisFrame.Count);

        store.BeginFrame();

        Assert.AreEqual(0, store.RemovedIdsThisFrame.Count);
    }

    [Test]
    public void BeginFrame_DoesNotClearUnmarkedProjectiles()
    {
        store.Add(MakeProjectile(1));
        store.Add(MakeProjectile(2));
        store.MarkForRemoval(1);

        store.BeginFrame();

        Assert.AreEqual(1, store.ActiveProjectiles.Count);
        Assert.AreEqual(2, store.ActiveProjectiles[0].Id);
    }

    // --- RecordHit ---

    [Test]
    public void RecordHit_AppearsInHitsThisFrame()
    {
        var hit = new ProjectileHit(5, 10);

        store.RecordHit(hit);

        Assert.AreEqual(1, store.HitsThisFrame.Count);
        Assert.AreEqual(5, store.HitsThisFrame[0].TargetCreepId);
        Assert.AreEqual(10, store.HitsThisFrame[0].Damage);
    }

    [Test]
    public void RecordHit_MultipleHits_AllRecorded()
    {
        store.RecordHit(new ProjectileHit(1, 5));
        store.RecordHit(new ProjectileHit(2, 10));
        store.RecordHit(new ProjectileHit(1, 5));

        Assert.AreEqual(3, store.HitsThisFrame.Count);
    }

    // --- Edge cases ---

    [Test]
    public void MarkForRemoval_InvalidId_DoesNotCrash()
    {
        store.Add(MakeProjectile(1));

        Assert.DoesNotThrow(() => store.MarkForRemoval(999));

        store.BeginFrame();

        Assert.AreEqual(1, store.ActiveProjectiles.Count);
        Assert.AreEqual(0, store.RemovedIdsThisFrame.Count);
    }

    [Test]
    public void MarkForRemoval_SameIdTwice_RemovesOnce()
    {
        store.Add(MakeProjectile(1));
        store.MarkForRemoval(1);
        store.MarkForRemoval(1);

        store.BeginFrame();

        Assert.AreEqual(0, store.ActiveProjectiles.Count);
        Assert.AreEqual(1, store.RemovedIdsThisFrame.Count);
    }

    // --- Reset ---

    [Test]
    public void Reset_ClearsAllState()
    {
        store.Add(MakeProjectile(1));
        store.Add(MakeProjectile(2));
        store.MarkForRemoval(1);
        store.RecordHit(new ProjectileHit(1, 5));

        store.Reset();

        Assert.AreEqual(0, store.ActiveProjectiles.Count);
        Assert.AreEqual(0, store.SpawnedThisFrame.Count);
        Assert.AreEqual(0, store.RemovedIdsThisFrame.Count);
        Assert.AreEqual(0, store.HitsThisFrame.Count);
    }

    // --- ProjectilesPersistAcrossFrames ---

    [Test]
    public void ProjectilesPersistAcrossFrames()
    {
        store.Add(MakeProjectile(1));
        store.BeginFrame();
        store.Add(MakeProjectile(2));
        store.BeginFrame();

        Assert.AreEqual(2, store.ActiveProjectiles.Count);
        Assert.AreEqual(0, store.SpawnedThisFrame.Count);
    }

    private static ProjectileSimData MakeProjectile(int id)
    {
        return new ProjectileSimData(id)
        {
            Position = Vector3.zero,
            TargetCreepId = 0,
            Damage = 1,
            Speed = 10f
        };
    }
}
