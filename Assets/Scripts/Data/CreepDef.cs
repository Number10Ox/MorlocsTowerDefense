using UnityEngine;

[CreateAssetMenu(fileName = "NewCreepDef", menuName = "Game/Creep Definition")]
public class CreepDef : ScriptableObject
{
    [Tooltip("Movement speed in units per second.")]
    [Min(0.01f)] [SerializeField] private float speed = 3f;

    public float Speed => speed;

    private void OnValidate()
    {
        speed = Mathf.Max(0.01f, speed);
    }
}
