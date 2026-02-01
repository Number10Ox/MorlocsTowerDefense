using UnityEngine;

// Inspector-tunable creep stats: speed, damage, health. One asset per creep type.
[CreateAssetMenu(fileName = "NewCreepDef", menuName = "Game/Creep Definition")]
public class CreepDef : ScriptableObject
{
    [Tooltip("Movement speed in units per second.")]
    [Min(0.01f)] [SerializeField] private float speed = 3f;

    [Tooltip("Damage dealt to the base when this creep arrives.")]
    [Min(1)] [SerializeField] private int damageToBase = 1;

    [Tooltip("Maximum health points for this creep type.")]
    [Min(1)] [SerializeField] private int maxHealth = 3;

    [Tooltip("Coins awarded to the player when this creep is killed.")]
    [Min(0)] [SerializeField] private int coinReward = 1;

    public float Speed => speed;
    public int DamageToBase => damageToBase;
    public int MaxHealth => maxHealth;
    public int CoinReward => coinReward;

    private void OnValidate()
    {
        speed = Mathf.Max(0.01f, speed);
        damageToBase = Mathf.Max(1, damageToBase);
        maxHealth = Mathf.Max(1, maxHealth);
        coinReward = Mathf.Max(0, coinReward);
    }
}
