using UnityEngine;

[CreateAssetMenu(fileName = "NewSpawnConfig", menuName = "Game/Spawn Config")]
public class SpawnConfig : ScriptableObject
{
    [Tooltip("Seconds between spawn bursts.")]
    [Min(0.01f)] [SerializeField] private float spawnInterval = 2f;

    [Tooltip("Number of creeps spawned per burst at each spawn point.")]
    [Min(1)] [SerializeField] private int creepsPerSpawn = 1;

    public float SpawnInterval => spawnInterval;
    public int CreepsPerSpawn => creepsPerSpawn;

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        creepsPerSpawn = Mathf.Max(1, creepsPerSpawn);
    }
}
