using System;

public class DamageSystem : IGameSystem
{
    private readonly CreepStore creepStore;
    private readonly BaseStore baseStore;

    public DamageSystem(CreepStore creepStore, BaseStore baseStore)
    {
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.baseStore = baseStore ?? throw new ArgumentNullException(nameof(baseStore));
    }

    public void Tick(float deltaTime)
    {
        var creeps = creepStore.ActiveCreeps;
        for (int i = 0; i < creeps.Count; i++)
        {
            CreepSimData creep = creeps[i];
            if (creep.ReachedBase && !creep.HasDealtBaseDamage)
            {
                baseStore.ApplyDamage(creep.DamageToBase);
                creep.HasDealtBaseDamage = true;
            }
        }
    }
}
