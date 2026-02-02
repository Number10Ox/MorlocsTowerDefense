using System;
using UnityEngine;

// Per-wave definition: ordered entries (creep groups) and delay before the wave begins.
// Serialized as entries inside WaveConfig.
[Serializable]
public struct WaveDefinition
{
    [Tooltip("Ordered creep groups within this wave. Spawned sequentially.")]
    [SerializeField] private WaveEntryDefinition[] entries;

    [Tooltip("Seconds to wait before this wave starts spawning (inter-wave delay).")]
    [Min(0f)] [SerializeField] private float delayBeforeStart;

    public WaveEntryDefinition[] Entries => entries;
    public float DelayBeforeStart => delayBeforeStart;

    public WaveDefinition(WaveEntryDefinition[] entries, float delayBeforeStart)
    {
        this.entries = entries;
        this.delayBeforeStart = delayBeforeStart;
    }

    public void Validate()
    {
        delayBeforeStart = Mathf.Max(0f, delayBeforeStart);
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            entry.Validate();
            entries[i] = entry;
        }
    }
}
