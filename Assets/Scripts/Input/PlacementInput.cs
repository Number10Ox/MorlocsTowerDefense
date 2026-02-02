using UnityEngine;

// Shared input bridge. Writer: PresentationAdapter. Readers: PlacementSystem, GameFlowController. Consume-and-clear.
public class PlacementInput
{
    public bool PlaceRequested;
    public Vector3 WorldPosition;
    public bool RestartRequested;

    public void Clear()
    {
        PlaceRequested = false;
        WorldPosition = default;
        RestartRequested = false;
    }
}
