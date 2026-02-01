using NUnit.Framework;
using UnityEngine;

public class MovementSystemTests
{
    private CreepStore store;
    private MovementSystem movement;

    private const float THRESHOLD = 0.5f;

    [SetUp]
    public void SetUp()
    {
        store = new CreepStore();
        movement = new MovementSystem(store, THRESHOLD);
    }

    private CreepSimData AddCreep(int id, Vector3 position, Vector3 target, float speed, int health = 3)
    {
        var creep = new CreepSimData(id)
        {
            Position = position,
            Target = target,
            Speed = speed,
            Health = health,
            MaxHealth = health
        };
        store.Add(creep);
        return creep;
    }

    [Test]
    public void Tick_MovesCreepTowardTarget()
    {
        var creep = AddCreep(0, new Vector3(10f, 0f, 0f), Vector3.zero, 10f);

        movement.Tick(0.5f);

        Assert.Less(creep.Position.x, 10f);
        Assert.Greater(creep.Position.x, 0f);
    }

    [Test]
    public void Tick_MovementScaledByDeltaTime()
    {
        var creep = AddCreep(0, new Vector3(10f, 0f, 0f), Vector3.zero, 10f);

        movement.Tick(0.5f);

        float expectedX = 10f - (10f * 0.5f);
        Assert.AreEqual(expectedX, creep.Position.x, 0.01f);
    }

    [Test]
    public void Tick_CreepReachesBase_ReachedBaseFlagSet()
    {
        var creep = AddCreep(0, new Vector3(0.3f, 0f, 0f), Vector3.zero, 10f);

        movement.Tick(0.1f);

        Assert.IsTrue(creep.ReachedBase);
    }

    [Test]
    public void Tick_CreepReachesBase_PositionSnappedToTarget()
    {
        var creep = AddCreep(0, new Vector3(0.3f, 0f, 0f), Vector3.zero, 10f);

        movement.Tick(0.1f);

        Assert.AreEqual(Vector3.zero, creep.Position);
    }

    [Test]
    public void Tick_CreepReachesBase_MarkedForRemoval()
    {
        AddCreep(0, new Vector3(0.3f, 0f, 0f), Vector3.zero, 10f);

        movement.Tick(0.1f);
        store.BeginFrame();

        Assert.AreEqual(0, store.ActiveCreeps.Count);
        Assert.AreEqual(1, store.RemovedIdsThisFrame.Count);
        Assert.AreEqual(0, store.RemovedIdsThisFrame[0]);
    }

    [Test]
    public void Tick_CreepAlreadyReachedBase_SkipsProcessing()
    {
        var creep = AddCreep(0, new Vector3(0.3f, 0f, 0f), Vector3.zero, 10f);

        movement.Tick(0.1f);
        Assert.IsTrue(creep.ReachedBase);
        Vector3 positionAfterArrival = creep.Position;

        movement.Tick(0.1f);

        Assert.AreEqual(positionAfterArrival, creep.Position);
    }

    [Test]
    public void Tick_NoActiveCreeps_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => movement.Tick(1.0f));
    }

    [Test]
    public void Tick_MultipleCreeps_AllMoveIndependently()
    {
        var creepA = AddCreep(0, new Vector3(10f, 0f, 0f), Vector3.zero, 10f);
        var creepB = AddCreep(1, new Vector3(0f, 0f, 20f), Vector3.zero, 5f);

        movement.Tick(0.5f);

        float expectedAx = 10f - (10f * 0.5f);
        Assert.AreEqual(expectedAx, creepA.Position.x, 0.01f);
        Assert.AreEqual(0f, creepA.Position.z, 0.01f);

        float expectedBz = 20f - (5f * 0.5f);
        Assert.AreEqual(0f, creepB.Position.x, 0.01f);
        Assert.AreEqual(expectedBz, creepB.Position.z, 0.01f);
    }

    [Test]
    public void Tick_DeltaTimeZero_NoMovement()
    {
        var creep = AddCreep(0, new Vector3(10f, 0f, 0f), Vector3.zero, 10f);
        Vector3 original = creep.Position;

        movement.Tick(0f);

        Assert.AreEqual(original, creep.Position);
    }

    [Test]
    public void Tick_LargeDeltaTime_NoOvershoot()
    {
        var creep = AddCreep(0, new Vector3(1f, 0f, 0f), Vector3.zero, 100f);

        movement.Tick(10f);

        Assert.AreEqual(Vector3.zero, creep.Position);
        Assert.IsTrue(creep.ReachedBase);
    }
}
