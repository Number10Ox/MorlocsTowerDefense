using System;

// Three-phase damage: tick slow effects, projectile hits, then base damage.
// Single writer for CreepSimData.Health, CreepSimData.SlowRemainingTime/SlowMultiplier, and BaseStore.
// Fires OnCreepKilled.
public class DamageSystem : IGameSystem
{
    private readonly CreepStore creepStore;
    private readonly BaseStore baseStore;
    private readonly ProjectileStore projectileStore;

    public event Action<int, int> OnCreepKilled;

    public DamageSystem(CreepStore creepStore, BaseStore baseStore, ProjectileStore projectileStore)
    {
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.baseStore = baseStore ?? throw new ArgumentNullException(nameof(baseStore));
        this.projectileStore = projectileStore ?? throw new ArgumentNullException(nameof(projectileStore));
    }

    public void Tick(float deltaTime)
    {
        TickSlowEffects(deltaTime);
        ProcessProjectileHits();
        ProcessBaseDamage();
    }

    private void TickSlowEffects(float deltaTime)
    {
        var creeps = creepStore.ActiveCreeps;
        for (int i = 0; i < creeps.Count; i++)
        {
            CreepSimData creep = creeps[i];
            if (creep.Health <= 0) continue;
            if (creep.SlowRemainingTime <= 0f) continue;

            creep.SlowRemainingTime -= deltaTime;
            if (creep.SlowRemainingTime < 0f)
            {
                creep.SlowRemainingTime = 0f;
            }
        }
    }

    private void ProcessProjectileHits()
    {
        var hits = projectileStore.HitsThisFrame;
        for (int i = 0; i < hits.Count; i++)
        {
            ProjectileHit hit = hits[i];
            if (!creepStore.TryGetCreep(hit.TargetCreepId, out CreepSimData creep)) continue;
            if (creep.Health <= 0) continue;

            creep.Health -= hit.Damage;
            if (creep.Health < 0) creep.Health = 0;

            if (creep.Health <= 0)
            {
                creepStore.MarkForRemoval(creep.Id);
                OnCreepKilled?.Invoke(creep.Id, creep.CoinReward);
            }
            else if (hit.SlowDuration > 0f)
            {
                creep.SlowRemainingTime = hit.SlowDuration;
                creep.SlowMultiplier = hit.SlowMultiplier;
            }
        }
    }

    private void ProcessBaseDamage()
    {
        var creeps = creepStore.ActiveCreeps;
        for (int i = 0; i < creeps.Count; i++)
        {
            CreepSimData creep = creeps[i];
            if (creep.Health <= 0) continue;
            if (creep.ReachedBase && !creep.HasDealtBaseDamage)
            {
                baseStore.ApplyDamage(creep.DamageToBase);
                creep.HasDealtBaseDamage = true;
            }
        }
    }
}
