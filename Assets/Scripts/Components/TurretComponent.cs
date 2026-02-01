using UnityEngine;
using ObjectPooling;

// Prefab component for turret GameObjects. Holds sim-to-GO ID mapping and IPoolable lifecycle.
[DisallowMultipleComponent]
public sealed class TurretComponent : MonoBehaviour, IPoolable
{
    private int turretId;

    public int TurretId => turretId;

    public void Initialize(int id)
    {
        turretId = id;
    }

    public void OnPoolGet()
    {
        gameObject.SetActive(true);
    }

    public void OnPoolReturn()
    {
        turretId = default;
        gameObject.SetActive(false);
    }
}
