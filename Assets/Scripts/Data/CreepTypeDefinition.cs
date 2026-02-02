using System;
using UnityEngine;

// Per-creep-type definition: stats, prefab, and type identifier. Serialized as entries in CreepDefinitions.
// Mutates in place via Validate() — caller must reassign if this is an array element (value-type copy).
[Serializable]
public struct CreepTypeDefinition
{
    [SerializeField] private CreepType creepType;
    [SerializeField] private GameObject prefab;

    [Tooltip("Movement speed in units per second.")]
    [Min(0.01f)] [SerializeField] private float speed;

    [Tooltip("Damage dealt to the base when this creep arrives.")]
    [Min(1)] [SerializeField] private int damageToBase;

    [Tooltip("Maximum health points for this creep type.")]
    [Min(1)] [SerializeField] private int maxHealth;

    [Tooltip("Coins awarded to the player when this creep is killed.")]
    [Min(0)] [SerializeField] private int coinReward;

    public CreepType CreepType => creepType;
    public GameObject Prefab => prefab;
    public float Speed => speed;
    public int DamageToBase => damageToBase;
    public int MaxHealth => maxHealth;
    public int CoinReward => coinReward;

    public void Validate()
    {
        speed = Mathf.Max(0.01f, speed);
        damageToBase = Mathf.Max(1, damageToBase);
        maxHealth = Mathf.Max(1, maxHealth);
        coinReward = Mathf.Max(0, coinReward);
    }
}
