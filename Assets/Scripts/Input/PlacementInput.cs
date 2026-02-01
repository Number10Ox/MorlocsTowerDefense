using UnityEngine;

// Shared input bridge. Writer: PresentationAdapter. Reader: PlacementSystem. Consume-and-clear.
public class PlacementInput
{
    public bool PlaceRequested;
    public Vector3 WorldPosition;

    public void Clear()
    {
        PlaceRequested = false;
        WorldPosition = default;
    }
}
