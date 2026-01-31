using System.Collections.Generic;
using NUnit.Framework;

public class SystemSchedulerTests
{
    private class MockSystem : IGameSystem
    {
        public int TickCount;
        public float LastDeltaTime;

        public void Tick(float deltaTime)
        {
            TickCount++;
            LastDeltaTime = deltaTime;
        }
    }

    private class OrderTrackingSystem : IGameSystem
    {
        private readonly List<int> order;
        private readonly int id;

        public OrderTrackingSystem(List<int> order, int id)
        {
            this.order = order;
            this.id = id;
        }

        public void Tick(float deltaTime)
        {
            order.Add(id);
        }
    }

    [Test]
    public void Tick_TicksAllSystemsInOrder()
    {
        var order = new List<int>();
        var system1 = new OrderTrackingSystem(order, 1);
        var system2 = new OrderTrackingSystem(order, 2);
        var system3 = new OrderTrackingSystem(order, 3);
        var scheduler = new SystemScheduler(new IGameSystem[] { system1, system2, system3 });

        scheduler.Tick(0.016f);

        Assert.AreEqual(new List<int> { 1, 2, 3 }, order);
    }

    [Test]
    public void Tick_PassesDeltaTime()
    {
        var system = new MockSystem();
        var scheduler = new SystemScheduler(new IGameSystem[] { system });

        scheduler.Tick(0.033f);

        Assert.AreEqual(0.033f, system.LastDeltaTime, 0.0001f);
    }

    [Test]
    public void Tick_EmptySystems_DoesNotThrow()
    {
        var scheduler = new SystemScheduler(new IGameSystem[0]);

        Assert.DoesNotThrow(() => scheduler.Tick(0.016f));
    }
}
