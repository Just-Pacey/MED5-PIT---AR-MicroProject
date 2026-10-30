using UnityEngine;

public class FurnitureManager : MonoBehaviour
{
    public static FurnitureManager Instance { get; private set; }

    [Header("Ray Settings")]
    [Tooltip("Leave empty to use the right controller anchor of the OVRCameraRig.")]
    [SerializeField] private Transform rayOrigin;
    [Tooltip("Layer(s) of the room's floor colliders. Must not be 'Nothing'.")]
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private float maxRayDistance = 10f;
    [Tooltip("When the ray isn't pointing at the floor, the preview floats this far (metres) in front of the controller.")]
    [SerializeField] private float floatDistance = 1.5f;

    [Header("Placing")]
    [Tooltip("Press this button to drop the copy where the ray hits the floor.")]
    [SerializeField] private bool placeWithButton = true;
    [SerializeField] private OVRInput.Button placeButton = OVRInput.Button.PrimaryIndexTrigger;
    [SerializeField] private OVRInput.Controller placeController = OVRInput.Controller.RTouch;
    [Tooltip("The preview can't be placed until this many seconds have passed AND the place button has been released. Stops the press that selected the furniture from also placing it.")]
    [SerializeField] private float minHoverTime = 0.3f;

    [Header("Debugging")]
    [SerializeField] private bool debugLogging = true;

    private GameObject previewObject;
    private bool isPlacing;
    private bool armed;
    private bool hasFloorHit;
    private bool lastHasFloorHit = true;
    private float placementStartTime;

    private Collider[] previewColliders;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (floorLayer.value == 0)
            Debug.LogWarning("[FurnitureManager] Floor Layer is 'Nothing', so the ray can never hit the floor.", this);
    }

    private void Update()
    {
        if (!isPlacing || previewObject == null)
            return;

        UpdatePreviewPosition();

        if (!armed)
        {
            // Arm only after the select press has been let go, so selecting never places in the same press.
            if (Time.time - placementStartTime >= minHoverTime
                && !OVRInput.Get(placeButton, placeController))
            {
                armed = true;
                if (debugLogging)
                    Debug.Log("[FurnitureManager] Preview is hovering. Point at the floor and press the trigger to place it.");
            }
            return;
        }

        if (placeWithButton && hasFloorHit && OVRInput.GetDown(placeButton, placeController))
            PlaceCurrentPreview();
    }

    // Moves the preview to the floor hit point, or floats it at the end of the ray.
    private void UpdatePreviewPosition()
    {
        if (rayOrigin == null)
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
                rayOrigin = rig.rightControllerAnchor;
            if (rayOrigin == null)
                return;
        }

        hasFloorHit = Physics.Raycast(
            rayOrigin.position,
            rayOrigin.forward,
            out RaycastHit hit,
            maxRayDistance,
            floorLayer);

        previewObject.transform.position = hasFloorHit
            ? hit.point
            : rayOrigin.position + rayOrigin.forward * floatDistance;

        if (debugLogging && hasFloorHit != lastHasFloorHit)
        {
            lastHasFloorHit = hasFloorHit;
            Debug.Log(hasFloorHit
                ? "[FurnitureManager] Ray hit the floor: " + hit.collider.name
                : "[FurnitureManager] Ray is not hitting anything on the Floor Layer. Check the layer and that the floor has a collider.");
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
        armed = false;
        placementStartTime = Time.time;
        lastHasFloorHit = true;

        // Jump to the ray straight away instead of waiting a frame at the original's position.
        UpdatePreviewPosition();

        if (debugLogging)
            Debug.Log("[FurnitureManager] Furniture preview created: " + selectedFurniture.name);
    }

    // Also callable from a UnityEvent (e.g. a floor interactable). Ignored until the preview has been hovering for a moment.
    public void PlaceCurrentPreview()
    {
        if (!isPlacing || previewObject == null)
            return;

        if (!armed)
        {
            if (debugLogging)
                Debug.Log("[FurnitureManager] PlaceCurrentPreview was called too early and ignored. Something other than the trigger press is calling it.");
            return;
        }

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

        if (debugLogging)
            Debug.Log("[FurnitureManager] Furniture placed!");
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