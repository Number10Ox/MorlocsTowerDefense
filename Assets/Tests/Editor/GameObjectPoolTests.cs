using System.Text.RegularExpressions;
using NUnit.Framework;
using ObjectPooling;
using UnityEngine;
using UnityEngine.TestTools;

public class GameObjectPoolTests
{
    private GameObject prefab;

    [SetUp]
    public void SetUp()
    {
        prefab = new GameObject("PoolTestPrefab");
        prefab.AddComponent<TestPoolable>();
        prefab.SetActive(false);
    }

    [TearDown]
    public void TearDown()
    {
        if (prefab != null)
        {
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void Get_ReturnsGameObject()
    {
        var pool = new GameObjectPool(prefab, 1);
        var instance = pool.Get(Vector3.zero);

        Assert.IsNotNull(instance);

        Object.DestroyImmediate(instance);
    }

    [Test]
    public void Get_ActivatesGameObject()
    {
        var pool = new GameObjectPool(prefab, 1);
        var instance = pool.Get(Vector3.zero);

        Assert.IsTrue(instance.activeSelf);

        Object.DestroyImmediate(instance);
    }

    [Test]
    public void Get_SetsPositionBeforeActivation()
    {
        var pool = new GameObjectPool(prefab, 1);
        var position = new Vector3(5f, 10f, 15f);
        var instance = pool.Get(position);

        var poolable = instance.GetComponent<TestPoolable>();
        Assert.AreEqual(position, poolable.PositionAtOnPoolGet, "Position should be set before OnPoolGet callback");
        Assert.IsFalse(poolable.WasActiveBeforeOnPoolGet, "Object should not be active before OnPoolGet callback");

        Object.DestroyImmediate(instance);
    }

    [Test]
    public void Return_DeactivatesGameObject()
    {
        var pool = new GameObjectPool(prefab, 1);
        var instance = pool.Get(Vector3.zero);

        pool.Return(instance);

        Assert.IsFalse(instance.activeSelf);

        pool.Clear();
    }

    [Test]
    public void Get_AfterReturn_ReusesSameInstance()
    {
        var pool = new GameObjectPool(prefab, 1);
        var first = pool.Get(Vector3.zero);
        pool.Return(first);
        var second = pool.Get(Vector3.zero);

        Assert.AreSame(first, second);

        Object.DestroyImmediate(second);
    }

    [Test]
    public void Get_PoolExhausted_GrowsWithWarning()
    {
        var pool = new GameObjectPool(prefab, 1);
        var first = pool.Get(Vector3.zero);

        LogAssert.Expect(LogType.Warning, new Regex(@"Pool exhausted"));
        var second = pool.Get(Vector3.zero);

        Assert.IsNotNull(second);

        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
    }

    [Test]
    public void Clear_DestroysAllPooledInstances()
    {
        var pool = new GameObjectPool(prefab, 3);

        pool.Clear();

        // Pre-allocated instances should be destroyed; getting now requires new instantiation
        LogAssert.Expect(LogType.Warning, new Regex(@"Pool exhausted"));
        var instance = pool.Get(Vector3.zero);
        Assert.IsNotNull(instance);

        Object.DestroyImmediate(instance);
    }

    private class TestPoolable : MonoBehaviour, IPoolable
    {
        public Vector3 PositionAtOnPoolGet;
        public bool WasActiveBeforeOnPoolGet;

        public void OnPoolGet()
        {
            PositionAtOnPoolGet = transform.position;
            WasActiveBeforeOnPoolGet = gameObject.activeSelf;
            gameObject.SetActive(true);
        }

        public void OnPoolReturn()
        {
            gameObject.SetActive(false);
        }
    }
}
