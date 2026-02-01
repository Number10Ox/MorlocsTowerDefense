using UnityEngine;

// Inspector-tunable economy settings: starting coins. One asset per game configuration.
[CreateAssetMenu(fileName = "NewEconomyConfig", menuName = "Game/Economy Config")]
public class EconomyConfig : ScriptableObject
{
    [Tooltip("Number of coins the player starts with.")]
    [Min(0)] [SerializeField] private int startingCoins = 20;

    public int StartingCoins => startingCoins;

    private void OnValidate()
    {
        startingCoins = Mathf.Max(0, startingCoins);
    }
}
