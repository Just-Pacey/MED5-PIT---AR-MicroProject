
using UnityEngine;

public class FloorPlacementSelection : MonoBehaviour
{
    public void OnSelected()
    {
        Debug.Log("FLOOR SELECTED");

        if (FurnitureManager.Instance == null)
        {
            Debug.LogError("FurniturePlacementManager not found!");
            return;
        }

        FurnitureManager.Instance.PlaceCurrentPreview();
    }
}
