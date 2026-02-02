using System.Collections.Generic;
using NUnit.Framework;

public class WaveStoreTests
{
    private WaveStore store;

    [SetUp]
    public void SetUp()
    {
        store = new WaveStore();
    }

    [Test]
    public void InitialState_QueueEmpty()
    {
        Assert.AreEqual(0, store.SpawnQueue.Count);
    }

    [Test]
    public void InitialState_WaveIndexNegativeOne()
    {
        Assert.AreEqual(-1, store.CurrentWaveIndex);
    }

    [Test]
    public void InitialState_AllWavesClearedFalse()
    {
        Assert.IsFalse(store.AllWavesCleared);
    }

    [Test]
    public void InitialState_WaveActiveFalse()
    {
        Assert.IsFalse(store.WaveActive);
    }

    [Test]
    public void EnqueueSpawn_AddsToQueue()
    {
        store.EnqueueSpawn(CreepType.Small);

        Assert.AreEqual(1, store.SpawnQueue.Count);
        Assert.AreEqual(CreepType.Small, store.SpawnQueue[0]);
    }

    [Test]
    public void EnqueueSpawn_MultipleAdds_AllPresent()
    {
        store.EnqueueSpawn(CreepType.Small);
        store.EnqueueSpawn(CreepType.Big);
        store.EnqueueSpawn(CreepType.Small);

        Assert.AreEqual(3, store.SpawnQueue.Count);
        Assert.AreEqual(CreepType.Small, store.SpawnQueue[0]);
        Assert.AreEqual(CreepType.Big, store.SpawnQueue[1]);
        Assert.AreEqual(CreepType.Small, store.SpawnQueue[2]);
    }

    [Test]
    public void ConsumeSpawnQueue_CopiesAndClears()
    {
        store.EnqueueSpawn(CreepType.Small);
        store.EnqueueSpawn(CreepType.Big);

        var destination = new List<CreepType>();
        store.ConsumeSpawnQueue(destination);

        Assert.AreEqual(2, destination.Count);
        Assert.AreEqual(CreepType.Small, destination[0]);
        Assert.AreEqual(CreepType.Big, destination[1]);
        Assert.AreEqual(0, store.SpawnQueue.Count);
    }

    [Test]
    public void ConsumeSpawnQueue_EmptyQueue_NoCrash()
    {
        var destination = new List<CreepType>();

        Assert.DoesNotThrow(() => store.ConsumeSpawnQueue(destination));
        Assert.AreEqual(0, destination.Count);
    }

    [Test]
    public void ConsumeSpawnQueue_AppendsToDestination()
    {
        store.EnqueueSpawn(CreepType.Small);

        var destination = new List<CreepType> { CreepType.Big };
        store.ConsumeSpawnQueue(destination);

        Assert.AreEqual(2, destination.Count);
        Assert.AreEqual(CreepType.Big, destination[0]);
        Assert.AreEqual(CreepType.Small, destination[1]);
    }

    [Test]
    public void StartWave_SetsCurrentWaveIndex()
    {
        store.StartWave(2);

        Assert.AreEqual(2, store.CurrentWaveIndex);
    }

    [Test]
    public void StartWave_SetsWaveActiveTrue()
    {
        store.StartWave(0);

        Assert.IsTrue(store.WaveActive);
    }

    [Test]
    public void StartWave_FiresOnWaveStarted()
    {
        int capturedIndex = -1;
        store.OnWaveStarted += index => capturedIndex = index;

        store.StartWave(3);

        Assert.AreEqual(3, capturedIndex);
    }

    [Test]
    public void ClearWave_SetsWaveActiveFalse()
    {
        store.StartWave(0);

        store.ClearWave(0);

        Assert.IsFalse(store.WaveActive);
    }

    [Test]
    public void ClearWave_FiresOnWaveCleared()
    {
        int capturedIndex = -1;
        store.OnWaveCleared += index => capturedIndex = index;

        store.ClearWave(1);

        Assert.AreEqual(1, capturedIndex);
    }

    [Test]
    public void MarkAllWavesCleared_SetsFlag()
    {
        store.MarkAllWavesCleared();

        Assert.IsTrue(store.AllWavesCleared);
    }

    [Test]
    public void Reset_ClearsQueue()
    {
        store.EnqueueSpawn(CreepType.Small);

        store.Reset();

        Assert.AreEqual(0, store.SpawnQueue.Count);
    }

    [Test]
    public void Reset_ResetsWaveIndex()
    {
        store.StartWave(2);

        store.Reset();

        Assert.AreEqual(-1, store.CurrentWaveIndex);
    }

    [Test]
    public void Reset_ClearsAllWavesCleared()
    {
        store.MarkAllWavesCleared();

        store.Reset();

        Assert.IsFalse(store.AllWavesCleared);
    }

    [Test]
    public void Reset_ClearsWaveActive()
    {
        store.StartWave(0);

        store.Reset();

        Assert.IsFalse(store.WaveActive);
    }
}
