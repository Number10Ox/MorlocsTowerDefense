using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class InitStateTests
{
    [Test]
    public void Enter_WithValidHomeBase_FiresSceneValidated()
    {
        GameTrigger? firedTrigger = null;
        Action<GameTrigger> mockFire = (trigger) => firedTrigger = trigger;
        var homeBase = new GameObject("Base").AddComponent<HomeBaseComponent>();
        var initState = new InitState(mockFire, homeBase);

        initState.Enter();

        Assert.AreEqual(GameTrigger.SceneValidated, firedTrigger);
        UnityEngine.Object.DestroyImmediate(homeBase.gameObject);
    }

    [Test]
    public void Enter_WithNullHomeBase_LogsErrorAndDoesNotFire()
    {
        GameTrigger? firedTrigger = null;
        Action<GameTrigger> mockFire = (trigger) => firedTrigger = trigger;
        var initState = new InitState(mockFire, null);

        LogAssert.Expect(LogType.Error, new Regex(@"HomeBaseComponent reference is null"));
        initState.Enter();

        Assert.IsNull(firedTrigger);
    }
}
