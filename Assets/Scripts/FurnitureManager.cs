
using UnityEngine;

public class FurnitureManager : MonoBehaviour
{
    public static FurnitureManager Instance { get; private set; }

    [Header("Ray Settings")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private float maxRayDistance = 10f;

    private GameObject previewObject;
    private bool isPlacing;

    private Collider[] previewColliders;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!isPlacing || previewObject == null || rayOrigin == null)
            return;

        // Find the floor using the controller ray.
        if (Physics.Raycast(
            rayOrigin.position,
            rayOrigin.forward,
            out RaycastHit hit,
            maxRayDistance,
            floorLayer))
        {
            previewObject.transform.position = hit.point;
        }
    }

    public void BeginPlacement(GameObject selectedFurniture)
    {
        if (selectedFurniture == null)
            return;

        // Remove any previous preview.
        if (previewObject != null)
            Destroy(previewObject);

        // Create a copy of the selected furniture.
        previewObject = Instantiate(
            selectedFurniture,
            selectedFurniture.transform.position,
            selectedFurniture.transform.rotation
        );

        previewObject.name = selectedFurniture.name + "_Preview";

        // Disable colliders so the preview doesn't block the ray.
        previewColliders = previewObject.GetComponentsInChildren<Collider>(true);
        SetPreviewColliders(false);

        // Disable selection scripts on the preview.
        FurnitureSelection[] selections =
            previewObject.GetComponentsInChildren<FurnitureSelection>(true);

        foreach (FurnitureSelection selection in selections)
            selection.enabled = false;

        isPlacing = true;

        Debug.Log("Furniture preview created: " + selectedFurniture.name);
    }

    public void PlaceCurrentPreview()
    {
        if (!isPlacing || previewObject == null)
            return;

        // Make the placed furniture interactable again.
        SetPreviewColliders(true);

        FurnitureSelection[] selections =
            previewObject.GetComponentsInChildren<FurnitureSelection>(true);

        foreach (FurnitureSelection selection in selections)
            selection.enabled = true;

        previewObject.name = previewObject.name.Replace("_Preview", "");

        previewObject = null;
        previewColliders = null;
        isPlacing = false;

        Debug.Log("Furniture placed!");
    }

    private void SetPreviewColliders(bool enabled)
    {
        if (previewColliders == null)
            return;

        foreach (Collider col in previewColliders)
            col.enabled = enabled;
    }

    public void CancelPlacement()
    {
        if (previewObject != null)
            Destroy(previewObject);

        previewObject = null;
        previewColliders = null;
        isPlacing = false;
    }
}
