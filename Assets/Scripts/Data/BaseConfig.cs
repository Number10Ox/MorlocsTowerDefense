using UnityEngine;

[CreateAssetMenu(fileName = "NewBaseConfig", menuName = "Game/Base Config")]
public class BaseConfig : ScriptableObject
{
    [Tooltip("Maximum health for the base.")]
    [Min(1)] [SerializeField] private int maxHealth = 100;

    public int MaxHealth => maxHealth;

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }
}
