using System;
using UnityEngine;

public class MovementSystem : IGameSystem
{
    private readonly CreepStore creepStore;
    private readonly float arrivalThreshold;
    private readonly float arrivalThresholdSq;

    public MovementSystem(CreepStore creepStore, float arrivalThreshold = 0.5f)
    {
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));

        if (arrivalThreshold <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(arrivalThreshold), "Must be greater than zero.");
        }

        this.arrivalThreshold = arrivalThreshold;
        arrivalThresholdSq = arrivalThreshold * arrivalThreshold;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        var creeps = creepStore.ActiveCreeps;
        for (int i = 0; i < creeps.Count; i++)
        {
            CreepSimData creep = creeps[i];
            if (creep.ReachedBase)
            {
                continue;
            }

            Vector3 direction = creep.Target - creep.Position;
            float distSq = direction.sqrMagnitude;

            if (distSq <= arrivalThresholdSq)
            {
                Arrive(creep);
                continue;
            }

            float distance = Mathf.Sqrt(distSq);
            float stepDistance = creep.Speed * deltaTime;

            if (stepDistance >= distance)
            {
                Arrive(creep);
                continue;
            }

            creep.Position += direction / distance * stepDistance;
        }
    }

    private void Arrive(CreepSimData creep)
    {
        creep.Position = creep.Target;
        creep.ReachedBase = true;
        creepStore.MarkForRemoval(creep.Id);
    }
}
