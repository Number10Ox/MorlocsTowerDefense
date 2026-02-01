using UnityEngine;
using ObjectPooling;

[DisallowMultipleComponent]
public sealed class ProjectileComponent : MonoBehaviour, IPoolable
{
    private int projectileId;

    public int ProjectileId => projectileId;

    public void Initialize(int id)
    {
        if (projectileId != default)
        {
            Debug.LogWarning($"ProjectileComponent re-initialized. Previous id={projectileId}, new id={id}.");
        }

        projectileId = id;
    }

    public void OnPoolGet()
    {
        gameObject.SetActive(true);
    }

    public void OnPoolReturn()
    {
        projectileId = default;
        gameObject.SetActive(false);
    }
}
