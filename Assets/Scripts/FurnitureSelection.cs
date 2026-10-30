using UnityEngine;

public class FurnitureSelection : MonoBehaviour
{
    [Tooltip("Seconds after placing during which a copy ignores selection, so the trigger press that placed it can't also delete it.")]
    [SerializeField] private float deleteCooldown = 0.5f;

    private bool isCopy;
    private float placedTime;

    // Called by FurnitureManager when this object is created as a preview copy.
    public void MarkAsCopy()
    {
        isCopy = true;
        placedTime = float.MaxValue; // can't be deleted while it's still a preview
    }

    // Called by FurnitureManager the moment the copy is placed.
    public void MarkPlaced()
    {
        placedTime = Time.time;
    }

    public void OnSelected()
    {
        if (FurnitureManager.Instance == null)
        {
            Debug.LogError("FurnitureManager not found!");
            return;
        }

        // A placed copy: selecting it again deletes it.
        if (isCopy)
        {
            if (FurnitureManager.Instance.IsPlacing) return;
            if (Time.time - placedTime < deleteCooldown) return;

            Debug.Log("FURNITURE DELETED: " + gameObject.name);
            Destroy(gameObject);
            return;
        }

        // An original piece: selecting it starts placing a copy.
        Debug.Log("FURNITURE SELECTED: " + gameObject.name);
        FurnitureManager.Instance.BeginPlacement(gameObject);
    }
}