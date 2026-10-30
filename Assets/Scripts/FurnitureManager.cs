using Oculus.Interaction;
using UnityEngine;

public class FurnitureManager : MonoBehaviour
{
    public static FurnitureManager Instance { get; private set; }

    /// <summary>True while a copy is hovering and waiting to be placed.</summary>
    public bool IsPlacing => isPlacing;

    [Header("Ray Settings")]
    [Tooltip("The Interaction SDK RayInteractor of your right controller (inside OVRComprehensiveInteractionRig). This is the ray you select with, so the preview follows exactly what you point at. If empty, the one that is selecting when you pick a piece is used.")]
    [SerializeField] private RayInteractor rayInteractor;
    [Tooltip("Fallback only, used when no RayInteractor is found. Leave empty to use the right controller anchor of the OVRCameraRig.")]
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

    [Header("Highlight")]
    [Tooltip("The floor plane's Mesh Renderer. Turned on while you're placing something, off otherwise. Give it a see-through material.")]
    [SerializeField] private Renderer floorRenderer;
    [Tooltip("Optional flat disc shown where the ray hits the floor. Remove its collider. Hidden when the ray isn't on the floor.")]
    [SerializeField] private GameObject hitMarker;

    [Header("Debugging")]
    [SerializeField] private bool debugLogging = true;

    private GameObject previewObject;
    private bool isPlacing;
    private bool armed;
    private bool hasFloorHit;
    private bool lastHasFloorHit = true;
    private float placementStartTime;
    private RayInteractor activeRay;

    private Collider[] previewColliders;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SetHighlight(false);

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

    // Gets the ray to aim with: the Interaction SDK ray if there is one, otherwise a plain transform.
    private bool TryGetRay(out Vector3 origin, out Vector3 direction)
    {
        if (activeRay == null)
            activeRay = rayInteractor != null ? rayInteractor : FindSelectingRay();

        if (activeRay != null)
        {
            origin = activeRay.Origin;
            direction = activeRay.Forward;
            return true;
        }

        if (rayOrigin == null)
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
                rayOrigin = rig.rightControllerAnchor;
        }

        if (rayOrigin != null)
        {
            origin = rayOrigin.position;
            direction = rayOrigin.forward;
            return true;
        }

        origin = Vector3.zero;
        direction = Vector3.forward;
        return false;
    }

    // Picks the RayInteractor that is selecting right now (the one that just selected the furniture).
    private RayInteractor FindSelectingRay()
    {
        RayInteractor fallback = null;
        foreach (RayInteractor r in FindObjectsByType<RayInteractor>(FindObjectsSortMode.None))
        {
            if (r.State == InteractorState.Select)
                return r;
            if (fallback == null && r.State != InteractorState.Disabled)
                fallback = r;
        }
        return fallback;
    }

    // Moves the preview to the floor hit point, or floats it at the end of the ray.
    private void UpdatePreviewPosition()
    {
        if (!TryGetRay(out Vector3 origin, out Vector3 direction))
            return;

        // Visible in the Scene view (Gizmos on) so you can see exactly where this script is aiming.
        Debug.DrawRay(origin, direction * maxRayDistance, Color.cyan);

        hasFloorHit = Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            maxRayDistance,
            floorLayer);

        previewObject.transform.position = hasFloorHit
            ? hit.point
            : origin + direction * floatDistance;

        if (hitMarker != null)
        {
            hitMarker.SetActive(hasFloorHit);
            if (hasFloorHit)
                hitMarker.transform.position = hit.point + Vector3.up * 0.005f;
        }

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
        {
            selection.MarkAsCopy();
            selection.enabled = false;
        }

        // Aim with the ray that just selected this piece.
        activeRay = rayInteractor != null ? rayInteractor : FindSelectingRay();

        isPlacing = true;
        SetHighlight(true);
        armed = false;
        placementStartTime = Time.time;
        lastHasFloorHit = true;

        // Jump to the ray straight away instead of waiting a frame at the original's position.
        UpdatePreviewPosition();

        if (debugLogging)
        {
            string aim = activeRay != null ? "Interaction SDK ray '" + activeRay.name + "'" : "fallback transform";
            Debug.Log("[FurnitureManager] Furniture preview created: " + selectedFurniture.name + ". Aiming with " + aim + ".");
        }
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
        {
            selection.enabled = true;
            selection.MarkPlaced();
        }

        previewObject.name = previewObject.name.Replace("_Preview", "");

        previewObject = null;
        previewColliders = null;
        isPlacing = false;
        activeRay = null;
        SetHighlight(false);

        if (debugLogging)
            Debug.Log("[FurnitureManager] Furniture placed!");
    }

    private void SetHighlight(bool on)
    {
        if (floorRenderer != null)
            floorRenderer.enabled = on;
        if (hitMarker != null && !on)
            hitMarker.SetActive(false);
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
        activeRay = null;
        SetHighlight(false);
    }
}