using NUnit.Framework;
using UnityEngine;

public class PlacementSystemTests
{
    private const float DEFAULT_RANGE = 10f;
    private const float DEFAULT_FIRE_INTERVAL = 1f;
    private const int DEFAULT_DAMAGE = 1;
    private const float DEFAULT_PROJECTILE_SPEED = 15f;

    private TurretStore turretStore;
    private PlacementInput placementInput;
    private PlacementSystem system;

    [SetUp]
    public void SetUp()
    {
        turretStore = new TurretStore();
        placementInput = new PlacementInput();
        system = new PlacementSystem(
            turretStore,
            placementInput,
            DEFAULT_RANGE,
            DEFAULT_FIRE_INTERVAL,
            DEFAULT_DAMAGE,
            DEFAULT_PROJECTILE_SPEED);
    }

    [Test]
    public void Tick_PlaceRequested_AddsTurretToStore()
    {
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = new Vector3(5f, 0f, 10f);

        system.Tick(0.016f);

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
    }

    [Test]
    public void Tick_NoPlaceRequested_DoesNothing()
    {
        system.Tick(0.016f);

        Assert.AreEqual(0, turretStore.ActiveTurrets.Count);
    }

    [Test]
    public void Tick_PlacementPosition_MatchesInputPosition()
    {
        var pos = new Vector3(12f, 0f, -3f);
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = pos;

        system.Tick(0.016f);

        Assert.AreEqual(pos, turretStore.ActiveTurrets[0].Position);
    }

    [Test]
    public void Tick_MultiplePlacements_IncrementsIds()
    {
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;
        system.Tick(0.016f);

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.one;
        system.Tick(0.016f);

        Assert.AreEqual(2, turretStore.ActiveTurrets.Count);
        Assert.AreEqual(0, turretStore.ActiveTurrets[0].Id);
        Assert.AreEqual(1, turretStore.ActiveTurrets[1].Id);
    }

    [Test]
    public void Tick_ClearsInputAfterConsuming()
    {
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = new Vector3(1f, 0f, 1f);

        system.Tick(0.016f);

        Assert.IsFalse(placementInput.PlaceRequested);
    }

    [Test]
    public void Tick_StalePlaceRequested_DoesNotDoublePlaceAfterClear()
    {
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;
        system.Tick(0.016f);

        // Second tick without new input should not place again
        system.Tick(0.016f);

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
    }

    [Test]
    public void Reset_ResetsIdCounter()
    {
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;
        system.Tick(0.016f);

        system.Reset();

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.one;
        system.Tick(0.016f);

        Assert.AreEqual(0, turretStore.ActiveTurrets[1].Id);
    }
}
