using System;
using UnityEngine;

public class ProjectileSystem : IGameSystem
{
    private const int NO_TARGET = -1;
    private const float HIT_THRESHOLD = 0.5f;

    private readonly TurretStore turretStore;
    private readonly CreepStore creepStore;
    private readonly ProjectileStore projectileStore;
    private readonly float hitThresholdSq;

    private int nextProjectileId;

    public ProjectileSystem(
        TurretStore turretStore,
        CreepStore creepStore,
        ProjectileStore projectileStore)
    {
        this.turretStore = turretStore ?? throw new ArgumentNullException(nameof(turretStore));
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.projectileStore = projectileStore ?? throw new ArgumentNullException(nameof(projectileStore));

        hitThresholdSq = HIT_THRESHOLD * HIT_THRESHOLD;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        UpdateFireTimers(deltaTime);
        MoveProjectiles(deltaTime);
    }

    public void Reset()
    {
        nextProjectileId = 0;
    }

    private void UpdateFireTimers(float deltaTime)
    {
        var turrets = turretStore.ActiveTurrets;
        for (int i = 0; i < turrets.Count; i++)
        {
            TurretSimData turret = turrets[i];
            turret.FireCooldown -= deltaTime;

            if (turret.FireCooldown > 0f) continue;

            int targetId = FindNearestCreepInRange(turret.Position, turret.Range);
            if (targetId == NO_TARGET) continue;

            var projectile = new ProjectileSimData(nextProjectileId++)
            {
                Position = turret.Position,
                TargetCreepId = targetId,
                Damage = turret.Damage,
                Speed = turret.ProjectileSpeed
            };
            projectileStore.Add(projectile);

            turret.FireCooldown = turret.FireInterval;
        }
    }

    private int FindNearestCreepInRange(Vector3 turretPos, float range)
    {
        float rangeSq = range * range;
        float bestDistSq = float.MaxValue;
        int bestId = NO_TARGET;

        var creeps = creepStore.ActiveCreeps;
        for (int i = 0; i < creeps.Count; i++)
        {
            CreepSimData creep = creeps[i];
            if (creep.ReachedBase) continue;
            if (creep.Health <= 0) continue;

            float distSq = (creep.Position - turretPos).sqrMagnitude;
            if (distSq <= rangeSq && distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestId = creep.Id;
            }
        }

        return bestId;
    }

    private void MoveProjectiles(float deltaTime)
    {
        var projectiles = projectileStore.ActiveProjectiles;
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            ProjectileSimData proj = projectiles[i];

            if (!creepStore.TryGetCreep(proj.TargetCreepId, out CreepSimData target)
                || target.Health <= 0)
            {
                projectileStore.MarkForRemoval(proj.Id);
                continue;
            }

            Vector3 direction = target.Position - proj.Position;
            float distSq = direction.sqrMagnitude;

            if (distSq <= hitThresholdSq)
            {
                projectileStore.RecordHit(new ProjectileHit(proj.TargetCreepId, proj.Damage));
                projectileStore.MarkForRemoval(proj.Id);
                continue;
            }

            float distance = Mathf.Sqrt(distSq);
            float step = proj.Speed * deltaTime;

            if (step >= distance)
            {
                projectileStore.RecordHit(new ProjectileHit(proj.TargetCreepId, proj.Damage));
                projectileStore.MarkForRemoval(proj.Id);
                continue;
            }

            proj.Position += direction / distance * step;
        }
    }
}
