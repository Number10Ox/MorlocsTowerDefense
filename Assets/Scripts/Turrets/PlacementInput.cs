using UnityEngine;

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
