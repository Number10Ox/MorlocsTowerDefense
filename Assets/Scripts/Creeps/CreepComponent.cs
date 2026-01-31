using UnityEngine;
using ObjectPooling;

[DisallowMultipleComponent]
public sealed class CreepComponent : MonoBehaviour, IPoolable
{
    private int creepId;

    public int CreepId => creepId;

    public void Initialize(int id)
    {
        if (creepId != default)
        {
            Debug.LogWarning($"CreepComponent re-initialized. Previous id={creepId}, new id={id}.");
        }

        creepId = id;
    }

    public void OnPoolGet()
    {
        gameObject.SetActive(true);
    }

    public void OnPoolReturn()
    {
        creepId = default;
        gameObject.SetActive(false);
    }
}
