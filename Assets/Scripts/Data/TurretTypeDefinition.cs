using System;
using UnityEngine;

// Per-turret-type definition: stats, prefab, and type identifier. Serialized as entries in TurretDefinitions.
// Mutates in place via Validate() — caller must reassign if this is an array element (value-type copy).
[Serializable]
public struct TurretTypeDefinition
{
    [SerializeField] private TurretType turretType;
    [SerializeField] private GameObject prefab;

    [Tooltip("Damage dealt per projectile hit.")]
    [Min(1)] [SerializeField] private int damage;

    [Tooltip("Detection and firing range in world units.")]
    [Min(0.1f)] [SerializeField] private float range;

    [Tooltip("Seconds between shots.")]
    [Min(0.01f)] [SerializeField] private float fireInterval;

    [Tooltip("Projectile travel speed in units per second.")]
    [Min(0.1f)] [SerializeField] private float projectileSpeed;

    [Tooltip("Coin cost to place this turret type.")]
    [Min(0)] [SerializeField] private int cost;

    [Tooltip("Duration of the slow effect in seconds. 0 = no slow.")]
    [Min(0f)] [SerializeField] private float slowDuration;

    [Tooltip("Speed multiplier while slowed. 0.5 = 50% speed. Only meaningful when slowDuration > 0.")]
    [Range(0f, 1f)] [SerializeField] private float slowMultiplier;

    public TurretType TurretType => turretType;
    public GameObject Prefab => prefab;
    public int Damage => damage;
    public float Range => range;
    public float FireInterval => fireInterval;
    public float ProjectileSpeed => projectileSpeed;
    public int Cost => cost;
    public float SlowDuration => slowDuration;
    public float SlowMultiplier => slowMultiplier;

    public void Validate()
    {
        damage = Mathf.Max(1, damage);
        range = Mathf.Max(0.1f, range);
        fireInterval = Mathf.Max(0.01f, fireInterval);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        cost = Mathf.Max(0, cost);
        slowDuration = Mathf.Max(0f, slowDuration);
        slowMultiplier = Mathf.Clamp01(slowMultiplier);
    }
}
