using NUnit.Framework;
using UnityEngine;

public class TurretPlacementIntegrationTests
{
    private TurretStore turretStore;
    private PlacementInput placementInput;
    private PlacementSystem placementSystem;

    [SetUp]
    public void SetUp()
    {
        turretStore = new TurretStore();
        placementInput = new PlacementInput();
        placementSystem = new PlacementSystem(
            turretStore,
            placementInput,
            turretRange: 10f,
            turretFireInterval: 1f,
            turretDamage: 1,
            turretProjectileSpeed: 15f);
    }

    [Test]
    public void FullPlacementFlow_TurretAppearsAtCorrectPosition()
    {
        var pos = new Vector3(10f, 0f, 20f);
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = pos;

        placementSystem.Tick(0.016f);

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
        Assert.AreEqual(1, turretStore.PlacedThisFrame.Count);
        Assert.AreEqual(pos, turretStore.ActiveTurrets[0].Position);
    }

    [Test]
    public void PlaceMultipleTurrets_AllPersistInStore()
    {
        var pos1 = new Vector3(5f, 0f, 5f);
        var pos2 = new Vector3(-3f, 0f, 8f);
        var pos3 = new Vector3(0f, 0f, 0f);

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = pos1;
        placementSystem.Tick(0.016f);

        turretStore.BeginFrame();

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = pos2;
        placementSystem.Tick(0.016f);

        turretStore.BeginFrame();

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = pos3;
        placementSystem.Tick(0.016f);

        Assert.AreEqual(3, turretStore.ActiveTurrets.Count);
        Assert.AreEqual(pos1, turretStore.ActiveTurrets[0].Position);
        Assert.AreEqual(pos2, turretStore.ActiveTurrets[1].Position);
        Assert.AreEqual(pos3, turretStore.ActiveTurrets[2].Position);
    }

    [Test]
    public void TurretsPersistAcrossFrames_BeginFrameDoesNotRemove()
    {
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.one;
        placementSystem.Tick(0.016f);

        turretStore.BeginFrame();
        turretStore.BeginFrame();
        turretStore.BeginFrame();

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
    }

    [Test]
    public void PlacedThisFrame_ClearsOnNextBeginFrame()
    {
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;
        placementSystem.Tick(0.016f);

        Assert.AreEqual(1, turretStore.PlacedThisFrame.Count);

        turretStore.BeginFrame();

        Assert.AreEqual(0, turretStore.PlacedThisFrame.Count);
        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
    }

    [Test]
    public void NoInput_NoTurretPlaced_AcrossMultipleFrames()
    {
        placementSystem.Tick(0.016f);
        turretStore.BeginFrame();
        placementSystem.Tick(0.016f);
        turretStore.BeginFrame();

        Assert.AreEqual(0, turretStore.ActiveTurrets.Count);
    }

    [Test]
    public void GameSessionBeginFrame_FlushesAllStores()
    {
        var session = new GameSession(100);
        var input = new PlacementInput();
        var system = new PlacementSystem(
            session.TurretStore,
            input,
            turretRange: 10f,
            turretFireInterval: 1f,
            turretDamage: 1,
            turretProjectileSpeed: 15f);

        input.PlaceRequested = true;
        input.WorldPosition = Vector3.forward;
        system.Tick(0.016f);

        Assert.AreEqual(1, session.TurretStore.PlacedThisFrame.Count);

        session.BeginFrame();

        Assert.AreEqual(0, session.TurretStore.PlacedThisFrame.Count);
        Assert.AreEqual(1, session.TurretStore.ActiveTurrets.Count);
    }
}
