
using UnityEngine;

public class FurnitureSelection : MonoBehaviour
{
    public void OnSelected()
    {
        Debug.Log("FURNITURE SELECTED: " + gameObject.name);

        if (FurnitureManager.Instance == null)
        {
            Debug.LogError("FurniturePlacementManager not found!");
            return;
        }

        Debug.Log("Placement manager found!");

        FurnitureManager.Instance.BeginPlacement(gameObject);
    }
}
