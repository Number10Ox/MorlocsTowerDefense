using System;
using NUnit.Framework;

public class WinStateTests
{
    [Test]
    public void Constructor_NullFire_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new WinState(null));
    }

    [Test]
    public void Enter_DoesNotThrow()
    {
        var state = new WinState(trigger => { });

        Assert.DoesNotThrow(() => state.Enter());
    }

    [Test]
    public void Tick_DoesNotThrow()
    {
        var state = new WinState(trigger => { });

        Assert.DoesNotThrow(() => state.Tick(0.016f));
    }

    [Test]
    public void Exit_DoesNotThrow()
    {
        var state = new WinState(trigger => { });

        Assert.DoesNotThrow(() => state.Exit());
    }
}
