using UnityEngine;

[CreateAssetMenu(fileName = "NewCreepDef", menuName = "Game/Creep Definition")]
public class CreepDef : ScriptableObject
{
    [Tooltip("Movement speed in units per second.")]
    [Min(0.01f)] [SerializeField] private float speed = 3f;

    [Tooltip("Damage dealt to the base when this creep arrives.")]
    [Min(1)] [SerializeField] private int damageToBase = 1;

    public float Speed => speed;
    public int DamageToBase => damageToBase;

    private void OnValidate()
    {
        speed = Mathf.Max(0.01f, speed);
        damageToBase = Mathf.Max(1, damageToBase);
    }
}
