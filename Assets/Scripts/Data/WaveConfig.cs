using UnityEngine;

// Single asset containing all wave definitions. Authored in Inspector; consumed at bootstrap.
// Ordering contract: array order determines wave progression sequence.
[CreateAssetMenu(fileName = "WaveConfig", menuName = "Game/Wave Config")]
public class WaveConfig : ScriptableObject
{
    [Tooltip("Ordered wave definitions. Waves are played in sequence.")]
    [SerializeField] private WaveDefinition[] waves;

    public WaveDefinition[] Waves => waves;

    private void OnValidate()
    {
        if (waves == null) return;
        for (int i = 0; i < waves.Length; i++)
        {
            var wave = waves[i];
            wave.Validate();
            waves[i] = wave;
        }
    }
}
