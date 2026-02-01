using UnityEngine;

// Inspector-tunable turret stats: damage, range, fire interval, projectile speed. One asset per type.
[CreateAssetMenu(fileName = "NewTurretDef", menuName = "Game/Turret Definition")]
public class TurretDef : ScriptableObject
{
    [Tooltip("Damage dealt per projectile hit.")]
    [Min(1)] [SerializeField] private int damage = 1;

    [Tooltip("Detection and firing range in world units.")]
    [Min(0.1f)] [SerializeField] private float range = 10f;

    [Tooltip("Seconds between shots.")]
    [Min(0.01f)] [SerializeField] private float fireInterval = 1f;

    [Tooltip("Projectile travel speed in units per second.")]
    [Min(0.1f)] [SerializeField] private float projectileSpeed = 15f;

    [Tooltip("Coin cost to place this turret type.")]
    [Min(0)] [SerializeField] private int cost = 5;

    public int Damage => damage;
    public float Range => range;
    public float FireInterval => fireInterval;
    public float ProjectileSpeed => projectileSpeed;
    public int Cost => cost;

    private void OnValidate()
    {
        damage = Mathf.Max(1, damage);
        range = Mathf.Max(0.1f, range);
        fireInterval = Mathf.Max(0.01f, fireInterval);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        cost = Mathf.Max(0, cost);
    }
}
