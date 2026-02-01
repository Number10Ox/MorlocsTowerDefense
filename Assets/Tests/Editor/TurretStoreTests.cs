using NUnit.Framework;
using UnityEngine;

public class TurretStoreTests
{
    private TurretStore store;

    [SetUp]
    public void SetUp()
    {
        store = new TurretStore();
    }

    [Test]
    public void Add_TurretAppearsInActiveTurrets()
    {
        var turret = MakeTurret(1);

        store.Add(turret);

        Assert.AreEqual(1, store.ActiveTurrets.Count);
        Assert.AreEqual(1, store.ActiveTurrets[0].Id);
    }

    [Test]
    public void Add_TurretAppearsInPlacedThisFrame()
    {
        var turret = MakeTurret(1);

        store.Add(turret);

        Assert.AreEqual(1, store.PlacedThisFrame.Count);
        Assert.AreSame(turret, store.PlacedThisFrame[0]);
    }

    [Test]
    public void Add_NullThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => store.Add(null));
    }

    [Test]
    public void BeginFrame_ClearsPlacedThisFrame()
    {
        store.Add(MakeTurret(1));
        Assert.AreEqual(1, store.PlacedThisFrame.Count);

        store.BeginFrame();

        Assert.AreEqual(0, store.PlacedThisFrame.Count);
    }

    [Test]
    public void BeginFrame_DoesNotClearActiveTurrets()
    {
        store.Add(MakeTurret(1));

        store.BeginFrame();

        Assert.AreEqual(1, store.ActiveTurrets.Count);
    }

    [Test]
    public void Reset_ClearsAllState()
    {
        store.Add(MakeTurret(1));
        store.Add(MakeTurret(2));

        store.Reset();

        Assert.AreEqual(0, store.ActiveTurrets.Count);
        Assert.AreEqual(0, store.PlacedThisFrame.Count);
    }

    [Test]
    public void MultipleAdds_AllPresentInActiveTurrets()
    {
        store.Add(MakeTurret(1));
        store.Add(MakeTurret(2));
        store.Add(MakeTurret(3));

        Assert.AreEqual(3, store.ActiveTurrets.Count);
        Assert.AreEqual(3, store.PlacedThisFrame.Count);
    }

    [Test]
    public void TurretsPersistAcrossFrames()
    {
        store.Add(MakeTurret(1));
        store.BeginFrame();
        store.Add(MakeTurret(2));
        store.BeginFrame();

        Assert.AreEqual(2, store.ActiveTurrets.Count);
        Assert.AreEqual(0, store.PlacedThisFrame.Count);
    }

    private static TurretSimData MakeTurret(int id)
    {
        return new TurretSimData(id)
        {
            Position = new Vector3(id, 0f, id)
        };
    }
}
