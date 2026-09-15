using UnityEngine;
using Meta.XR.MRUtilityKit;

public class FurnitureSelector : MonoBehaviour
{
    [Header("Raycast & Pointer")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float maxRayDistance = 20f;
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private Transform reticleVisual;
    [SerializeField] private LayerMask raycastMask = ~0;

    [Header("Direct Proximity Grab")]
    [SerializeField] private float directGrabRadius = 0.25f;

    [Header("UI & Tooltip")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TextMesh tooltipText;

    [Header("Materials & Prefabs")]
    [SerializeField] private Material virtualFurnitureBaseMat;

    // Interaction State
    private RoomFurnitureAnchor hoveredRoomFurniture;
    private ManipulableFurniture hoveredVirtualFurniture;
    private ManipulableFurniture selectedVirtualFurniture;
    private FurnitureGizmoHandle hoveredHandle;
    private FurnitureGizmoHandle activeDraggingHandle;

    private bool isDirectGrabbingHandle = false;
    private bool isCarryingFurniture = false;
    private float carryDistance = 2.0f;
    private Vector3 initialDragRayPoint;
    private Vector3 initialHandGrabPos;

    // Input state tracking
    private bool prevTriggerPressed = false;
    private bool prevGripPressed = false;

    private void Awake()
    {
        if (rayOrigin == null) rayOrigin = transform;

        SetupLaserAndReticle();
        SetupTooltip();
    }

    private void SetupLaserAndReticle()
    {
        if (laserLine == null)
        {
            GameObject laserObj = new GameObject("LaserPointer");
            laserObj.transform.SetParent(transform, false);
            laserLine = laserObj.AddComponent<LineRenderer>();
            laserLine.useWorldSpace = true;
            laserLine.startWidth = 0.006f;
            laserLine.endWidth = 0.002f;
            laserLine.positionCount = 2;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material lineMat = new Material(shader);
                lineMat.color = new Color(0.2f, 0.8f, 1.0f, 0.75f);
                laserLine.material = lineMat;
            }
        }

        if (reticleVisual == null)
        {
            GameObject reticleObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleObj.name = "ReticleCursor";
            reticleObj.transform.SetParent(transform, true);
            reticleObj.transform.localScale = Vector3.one * 0.035f;

            Collider col = reticleObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material reticleMat = new Material(shader);
                reticleMat.color = new Color(0.2f, 0.9f, 1.0f, 0.95f);
                reticleObj.GetComponent<MeshRenderer>().material = reticleMat;
            }

            reticleVisual = reticleObj.transform;
            reticleVisual.gameObject.SetActive(false);
        }
    }

    private void SetupTooltip()
    {
        if (tooltipRoot == null)
        {
            tooltipRoot = new GameObject("HoverTooltip");
            tooltipRoot.transform.SetParent(null);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tooltipRoot.transform, false);

            tooltipText = textObj.AddComponent<TextMesh>();
            tooltipText.alignment = TextAlignment.Center;
            tooltipText.anchor = TextAnchor.MiddleCenter;
            tooltipText.characterSize = 0.022f;
            tooltipText.fontSize = 28;
            tooltipText.color = Color.white;

            tooltipRoot.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateProximityAndRaycast();
        ProcessInputs();
        UpdateLaserVisual();
        UpdateTooltipPosition();
    }

    private void UpdateProximityAndRaycast()
    {
        // 1. Initial State: If Workbench is not placed on floor yet, pointing places workbench on floor
        if (ARWorkbench.Instance != null && !ARWorkbench.Instance.IsPlacedOnFloor)
        {
            ClearHoveredHandle();
            ClearHoveredVirtualFurniture();
            ClearHoveredRoomFurniture();
            reticleVisual.gameObject.SetActive(false);
            return;
        }

        // 2. Direct Hand Proximity Check (When standing right at the workbench)
        Collider[] nearbyCols = Physics.OverlapSphere(rayOrigin.position, directGrabRadius, raycastMask);
        FurnitureGizmoHandle closestDirectHandle = null;
        float closestDist = float.MaxValue;

        foreach (var col in nearbyCols)
        {
            FurnitureGizmoHandle h = col.GetComponent<FurnitureGizmoHandle>();
            if (h != null)
            {
                float d = Vector3.Distance(rayOrigin.position, h.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestDirectHandle = h;
                }
            }
        }

        if (closestDirectHandle != null)
        {
            SetHoveredHandle(closestDirectHandle);
            ClearHoveredRoomFurniture();
            ClearHoveredVirtualFurniture();
            reticleVisual.gameObject.SetActive(false);
            ShowTooltip(closestDirectHandle.transform.position + Vector3.up * 0.08f, $"[GRAB HANDLE]\nPinch/Grip & pull to stretch {closestDirectHandle.AxisType}");
            return;
        }

        Ray ray = GetCurrentRay();
        RaycastHit hit;

        // Handle Active Handle Dragging (Direct Grab or Pointer Stretch)
        if (activeDraggingHandle != null)
        {
            if (isDirectGrabbingHandle)
            {
                ProcessDirectHandleDragging();
            }
            else
            {
                ProcessHandleDragging(ray);
            }
            return;
        }

        // Handle Carrying & Placing Furniture
        if (isCarryingFurniture && selectedVirtualFurniture != null)
        {
            ProcessFurnitureCarrying(ray);
            return;
        }

        // 3. Standard Raycast Hover (From far away or across room)
        if (Physics.Raycast(ray, out hit, maxRayDistance, raycastMask))
        {
            reticleVisual.position = hit.point;
            reticleVisual.rotation = Quaternion.LookRotation(hit.normal);
            reticleVisual.gameObject.SetActive(true);

            // 1. Check Gizmo Handle
            FurnitureGizmoHandle handle = hit.collider.GetComponent<FurnitureGizmoHandle>();
            if (handle != null)
            {
                SetHoveredHandle(handle);
                ClearHoveredRoomFurniture();
                ClearHoveredVirtualFurniture();
                ShowTooltip(hit.point + Vector3.up * 0.12f, $"[Pull Handle] to stretch {handle.AxisType}");
                return;
            }
            ClearHoveredHandle();

            // 2. Check Virtual Manipulable Furniture
            ManipulableFurniture virtualFurn = hit.collider.GetComponentInParent<ManipulableFurniture>();
            if (virtualFurn != null)
            {
                hoveredVirtualFurniture = virtualFurn;
                ClearHoveredRoomFurniture();

                if (virtualFurn.IsOnWorkbench)
                {
                    ShowTooltip(hit.point + Vector3.up * 0.18f, $"<b>{virtualFurn.FurnitureLabel}</b> (On Workbench)\n[Grip / Direct Grab]: Pick up & Place into Room\n[Grab Handles]: Stretch with hands | [A / Space]: Style");
                }
                else
                {
                    string status = virtualFurn.IsLocked ? "[Locked]" : "[Grip: Move / Trigger: Select]";
                    ShowTooltip(hit.point + Vector3.up * 0.18f, $"<b>{virtualFurn.FurnitureLabel}</b> (Virtual)\n{status}\n[A / Space: Style | B: Clone | X: Lock | Y: Delete]");
                }
                return;
            }
            ClearHoveredVirtualFurniture();

            // 3. Check Physical Room Furniture Anchor (or MRUK Anchor)
            RoomFurnitureAnchor roomFurn = hit.collider.GetComponentInParent<RoomFurnitureAnchor>();
            if (roomFurn == null)
            {
                MRUKAnchor mrukAnchor = hit.collider.GetComponentInParent<MRUKAnchor>();
                if (mrukAnchor != null && !IsStructureAnchor(mrukAnchor))
                {
                    roomFurn = mrukAnchor.gameObject.GetComponent<RoomFurnitureAnchor>() ?? mrukAnchor.gameObject.AddComponent<RoomFurnitureAnchor>();
                    roomFurn.InitializeFromMRUKAnchor(mrukAnchor);
                }
            }

            if (roomFurn != null)
            {
                SetHoveredRoomFurniture(roomFurn);
                ShowTooltip(hit.point + Vector3.up * 0.18f, $"Physical <b>{roomFurn.FurnitureLabel}</b>\n[Trigger / Click]: Copy to AR Workbench");
                return;
            }

            // Hit environment (floor / wall)
            ClearHoveredRoomFurniture();
            HideTooltip();
        }
        else
        {
            reticleVisual.gameObject.SetActive(false);
            ClearHoveredHandle();
            ClearHoveredVirtualFurniture();
            ClearHoveredRoomFurniture();
            HideTooltip();
        }
    }

    private void ProcessInputs()
    {
        bool triggerDown = IsTriggerDown();
        bool triggerUp = IsTriggerUp();
        bool gripDown = IsGripDown();
        bool gripUp = IsGripUp();
        bool actionStyleDown = IsActionStyleDown();
        bool actionDuplicateDown = IsActionDuplicateDown();
        bool actionLockDown = IsActionLockDown();
        bool actionDeleteDown = IsActionDeleteDown();
        bool recenterDown = IsRecenterWorkbenchDown();
        Vector2 thumbstick = GetThumbstick();

        // 0. Confirm Workbench Floor Placement on start
        if (ARWorkbench.Instance != null && !ARWorkbench.Instance.IsPlacedOnFloor)
        {
            if (triggerDown || gripDown)
            {
                ARWorkbench.Instance.ConfirmFloorPlacement();
                TriggerHaptic(0.4f, 0.15f);
            }
            return;
        }

        // Recenter / Reposition Workbench in front of player
        if (recenterDown && ARWorkbench.Instance != null)
        {
            ARWorkbench.Instance.ResetFloorPlacement();
            TriggerHaptic(0.3f, 0.1f);
        }

        // 1. Direct Hand Grab or Pointer Drag on Handles (Stretching)
        if ((triggerDown || gripDown) && hoveredHandle != null)
        {
            activeDraggingHandle = hoveredHandle;
            activeDraggingHandle.SetActive(true);

            float distToHand = Vector3.Distance(rayOrigin.position, hoveredHandle.transform.position);
            isDirectGrabbingHandle = (distToHand <= directGrabRadius * 1.5f);

            initialDragRayPoint = reticleVisual.position;
            initialHandGrabPos = rayOrigin.position;
            TriggerHaptic(0.3f, 0.08f);
            return;
        }

        // Release Handle Stretch
        if ((triggerUp || gripUp) && activeDraggingHandle != null)
        {
            activeDraggingHandle.SetActive(false);
            activeDraggingHandle = null;
            isDirectGrabbingHandle = false;
        }

        // 2. Trigger on Physical Real Furniture -> Copy to AR Workbench!
        if (triggerDown && hoveredRoomFurniture != null)
        {
            CopyFurnitureToWorkbench(hoveredRoomFurniture);
            return;
        }

        // 3. Grip Down on Virtual Furniture -> Direct Grab or Laser Carry!
        if (gripDown)
        {
            ManipulableFurniture targetFurn = hoveredVirtualFurniture ?? selectedVirtualFurniture;
            if (targetFurn != null && !targetFurn.IsLocked)
            {
                SelectVirtualFurniture(targetFurn);
                isCarryingFurniture = true;

                if (targetFurn.IsOnWorkbench && ARWorkbench.Instance != null)
                {
                    ARWorkbench.Instance.ReleaseItemFromWorkbench();
                }

                carryDistance = Vector3.Distance(rayOrigin.position, targetFurn.transform.position);
                carryDistance = Mathf.Clamp(carryDistance, 0.6f, 6.0f);
                TriggerHaptic(0.35f, 0.1f);
            }
        }

        // 4. Grip Up -> Drop & Place in Room!
        if (gripUp && isCarryingFurniture)
        {
            isCarryingFurniture = false;
            TriggerHaptic(0.2f, 0.1f);
            Debug.Log($"[FurnitureSelector] Placed {selectedVirtualFurniture?.FurnitureLabel} in room!");
        }

        // 5. Trigger Down on Virtual Furniture -> Select
        if (triggerDown && hoveredVirtualFurniture != null && hoveredHandle == null)
        {
            SelectVirtualFurniture(hoveredVirtualFurniture);
        }

        // 6. Selected Virtual Furniture Hotkeys & Controls
        if (selectedVirtualFurniture != null)
        {
            // Thumbstick Rotation
            if (Mathf.Abs(thumbstick.x) > 0.15f)
            {
                selectedVirtualFurniture.RotateYaw(thumbstick.x * 120f * Time.deltaTime);
            }

            // Push / Pull Carry Distance with thumbstick Y
            if (isCarryingFurniture && Mathf.Abs(thumbstick.y) > 0.15f)
            {
                carryDistance = Mathf.Clamp(carryDistance + thumbstick.y * 2.5f * Time.deltaTime, 0.6f, 8.0f);
            }

            if (actionStyleDown)
            {
                selectedVirtualFurniture.CycleStyle();
                TriggerHaptic(0.15f, 0.08f);
            }

            if (actionDuplicateDown)
            {
                ManipulableFurniture clone = selectedVirtualFurniture.DuplicateSelf();
                SelectVirtualFurniture(clone);
                TriggerHaptic(0.2f, 0.1f);
            }

            if (actionLockDown)
            {
                selectedVirtualFurniture.SetLocked(!selectedVirtualFurniture.IsLocked);
                TriggerHaptic(0.25f, 0.1f);
            }

            if (actionDeleteDown)
            {
                selectedVirtualFurniture.DeleteSelf();
                selectedVirtualFurniture = null;
                HideTooltip();
                TriggerHaptic(0.3f, 0.15f);
            }
        }
    }

    private void ProcessDirectHandleDragging()
    {
        if (activeDraggingHandle == null || activeDraggingHandle.ParentFurniture == null)
        {
            activeDraggingHandle = null;
            return;
        }

        ManipulableFurniture parent = activeDraggingHandle.ParentFurniture;
        Vector3 handMovement = rayOrigin.position - initialHandGrabPos;

        Vector3 localDelta = parent.transform.InverseTransformDirection(handMovement);
        Vector3 axisDir = activeDraggingHandle.GetLocalDirection();
        float projDelta = Vector3.Dot(localDelta, axisDir);

        if (Mathf.Abs(projDelta) > 0.003f)
        {
            parent.StretchByHandle(activeDraggingHandle.AxisType, projDelta);
            initialHandGrabPos = rayOrigin.position;
        }
    }

    private void ProcessHandleDragging(Ray ray)
    {
        if (activeDraggingHandle == null || activeDraggingHandle.ParentFurniture == null)
        {
            activeDraggingHandle = null;
            return;
        }

        ManipulableFurniture parent = activeDraggingHandle.ParentFurniture;
        Plane dragPlane = new Plane(-ray.direction, activeDraggingHandle.transform.position);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 currentPoint = ray.GetPoint(enter);
            Vector3 worldDelta = currentPoint - initialDragRayPoint;

            Vector3 localDelta = parent.transform.InverseTransformDirection(worldDelta);
            Vector3 axisDir = activeDraggingHandle.GetLocalDirection();
            float projDelta = Vector3.Dot(localDelta, axisDir);

            if (Mathf.Abs(projDelta) > 0.003f)
            {
                parent.StretchByHandle(activeDraggingHandle.AxisType, projDelta);
                initialDragRayPoint = currentPoint;
            }

            reticleVisual.position = currentPoint;
            reticleVisual.gameObject.SetActive(true);
        }
    }

    private void ProcessFurnitureCarrying(Ray ray)
    {
        if (selectedVirtualFurniture == null)
        {
            isCarryingFurniture = false;
            return;
        }

        Vector3 targetPos = ray.GetPoint(carryDistance);

        // Raycast down to find floor to snap to
        if (Physics.Raycast(targetPos + Vector3.up * 1.5f, Vector3.down, out RaycastHit floorHit, 4.0f))
        {
            // Ignore hitting the dragged furniture itself
            if (floorHit.collider.GetComponentInParent<ManipulableFurniture>() != selectedVirtualFurniture)
            {
                targetPos.y = floorHit.point.y + selectedVirtualFurniture.Size.y * 0.5f;
            }
        }
        else
        {
            targetPos.y = Mathf.Max(targetPos.y, 0.0f + selectedVirtualFurniture.Size.y * 0.5f);
        }

        selectedVirtualFurniture.transform.position = Vector3.Lerp(
            selectedVirtualFurniture.transform.position,
            targetPos,
            Time.deltaTime * 25f
        );

        reticleVisual.position = targetPos;
        reticleVisual.gameObject.SetActive(true);
    }

    public void CopyFurnitureToWorkbench(RoomFurnitureAnchor roomFurn)
    {
        if (roomFurn == null) return;

        Debug.Log($"[FurnitureSelector] Copying {roomFurn.FurnitureLabel} to AR Workbench...");

        if (ARWorkbench.Instance == null)
        {
            GameObject wb = new GameObject("AR Workbench");
            wb.AddComponent<ARWorkbench>();
        }

        ARWorkbench.Instance.StageFurniture(
            roomFurn.FurnitureLabel,
            roomFurn.Dimensions,
            roomFurn.CenterOffset
        );

        SelectVirtualFurniture(ARWorkbench.Instance.CurrentItem);

        TriggerHaptic(0.5f, 0.18f);
    }

    public void SelectVirtualFurniture(ManipulableFurniture furn)
    {
        if (selectedVirtualFurniture != null && selectedVirtualFurniture != furn)
        {
            selectedVirtualFurniture.SetSelected(false);
        }

        selectedVirtualFurniture = furn;
        if (selectedVirtualFurniture != null)
        {
            selectedVirtualFurniture.SetSelected(true);
        }
    }

    private void TriggerHaptic(float amplitude, float duration)
    {
        OVRInput.SetControllerVibration(amplitude, amplitude, OVRInput.Controller.RTouch);
        CancelInvoke(nameof(StopHaptics));
        Invoke(nameof(StopHaptics), duration);
    }

    private void StopHaptics()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }

    private void SetHoveredHandle(FurnitureGizmoHandle handle)
    {
        if (hoveredHandle != handle)
        {
            ClearHoveredHandle();
            hoveredHandle = handle;
            hoveredHandle.SetHovered(true);
        }
    }

    private void ClearHoveredHandle()
    {
        if (hoveredHandle != null)
        {
            hoveredHandle.SetHovered(false);
            hoveredHandle = null;
        }
    }

    private void ClearHoveredVirtualFurniture()
    {
        hoveredVirtualFurniture = null;
    }

    private void SetHoveredRoomFurniture(RoomFurnitureAnchor furn)
    {
        if (hoveredRoomFurniture != null && hoveredRoomFurniture != furn)
        {
            hoveredRoomFurniture.SetHovered(false);
        }
        hoveredRoomFurniture = furn;
        if (hoveredRoomFurniture != null)
        {
            hoveredRoomFurniture.SetHovered(true);
        }
    }

    private void ClearHoveredRoomFurniture()
    {
        if (hoveredRoomFurniture != null)
        {
            hoveredRoomFurniture.SetHovered(false);
            hoveredRoomFurniture = null;
        }
    }

    private void ShowTooltip(Vector3 position, string text)
    {
        if (tooltipRoot == null) return;
        tooltipRoot.transform.position = position;
        if (tooltipText != null) tooltipText.text = text;
        tooltipRoot.SetActive(true);
    }

    private void HideTooltip()
    {
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipRoot != null && tooltipRoot.activeSelf && Camera.main != null)
        {
            tooltipRoot.transform.rotation = Quaternion.LookRotation(
                tooltipRoot.transform.position - Camera.main.transform.position
            );
        }
    }

    private void UpdateLaserVisual()
    {
        if (laserLine == null) return;

        // If workbench is not placed on floor, laser shines down towards preview
        Vector3 start = rayOrigin.position;
        Vector3 end = reticleVisual != null && reticleVisual.gameObject.activeSelf
            ? reticleVisual.position
            : start + rayOrigin.forward * maxRayDistance;

        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);
    }

    private Ray GetCurrentRay()
    {
        Vector3 origin = rayOrigin.position;
        Vector3 direction = rayOrigin.forward;

        if (Application.isEditor && Camera.main != null && (rayOrigin == Camera.main.transform || !OVRInput.IsControllerConnected(OVRInput.Controller.Active)))
        {
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                return Camera.main.ScreenPointToRay(mousePos);
            }
        }

        return new Ray(origin, direction);
    }

    private bool IsStructureAnchor(MRUKAnchor anchor)
    {
        if (anchor == null || anchor.AnchorLabels == null) return false;
        foreach (string label in anchor.AnchorLabels)
        {
            string upper = label.ToUpperInvariant();
            if (upper == "WALL_FACE" || upper == "FLOOR" || upper == "CEILING" || upper == "INVISIBLE_WALL_FACE" || upper == "WALL_BORDER")
            {
                return true;
            }
        }
        return false;
    }

    private bool IsTriggerDown()
    {
        float ovrRightAxis = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        float ovrLeftAxis = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        bool axisPressed = (ovrRightAxis > 0.45f) || (ovrLeftAxis > 0.45f);

        bool ovrButton = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) ||
                         OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) ||
                         OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);

        bool triggerJustPressed = (axisPressed && !prevTriggerPressed) || ovrButton;
        prevTriggerPressed = axisPressed;

        bool mouseLeft = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
        bool keyE = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;

        return triggerJustPressed || mouseLeft || keyE;
    }

    private bool IsTriggerUp()
    {
        float ovrRightAxis = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        float ovrLeftAxis = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        bool axisReleased = (ovrRightAxis < 0.2f) && (ovrLeftAxis < 0.2f) && prevTriggerPressed;

        bool ovrButtonUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) ||
                           OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) ||
                           OVRInput.GetUp(OVRInput.RawButton.RIndexTrigger);

        bool mouseLeftUp = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame;
        bool keyEUp = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasReleasedThisFrame;

        return axisReleased || ovrButtonUp || mouseLeftUp || keyEUp;
    }

    private bool IsGripDown()
    {
        float ovrRightGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        float ovrLeftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        bool axisPressed = (ovrRightGrip > 0.45f) || (ovrLeftGrip > 0.45f);

        bool ovrButton = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
                         OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ||
                         OVRInput.GetDown(OVRInput.RawButton.RHandTrigger);

        bool gripJustPressed = (axisPressed && !prevGripPressed) || ovrButton;
        prevGripPressed = axisPressed;

        bool mouseRight = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;
        bool keyG = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame;

        return gripJustPressed || mouseRight || keyG;
    }

    private bool IsGripUp()
    {
        float ovrRightGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        float ovrLeftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        bool axisReleased = (ovrRightGrip < 0.2f) && (ovrLeftGrip < 0.2f) && prevGripPressed;

        bool ovrButtonUp = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
                           OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ||
                           OVRInput.GetUp(OVRInput.RawButton.RHandTrigger);

        bool mouseRightUp = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasReleasedThisFrame;
        bool keyGUp = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.gKey.wasReleasedThisFrame;

        return axisReleased || ovrButtonUp || mouseRightUp || keyGUp;
    }

    private bool IsActionStyleDown()
    {
        bool kb = UnityEngine.InputSystem.Keyboard.current != null && 
                 (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame);

        return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) ||
               OVRInput.GetDown(OVRInput.RawButton.A) ||
               kb;
    }

    private bool IsActionDuplicateDown()
    {
        bool kb = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.dKey.wasPressedThisFrame;

        return OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch) ||
               OVRInput.GetDown(OVRInput.RawButton.B) ||
               kb;
    }

    private bool IsActionLockDown()
    {
        bool kb = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame;

        return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch) ||
               OVRInput.GetDown(OVRInput.RawButton.X) ||
               kb;
    }

    private bool IsActionDeleteDown()
    {
        bool kb = UnityEngine.InputSystem.Keyboard.current != null && 
                 (UnityEngine.InputSystem.Keyboard.current.deleteKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.backspaceKey.wasPressedThisFrame);

        return OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch) ||
               OVRInput.GetDown(OVRInput.RawButton.Y) ||
               kb;
    }

    private bool IsRecenterWorkbenchDown()
    {
        bool kb = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;

        return OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) ||
               OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch) ||
               kb;
    }

    private Vector2 GetThumbstick()
    {
        Vector2 ovrStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        if (ovrStick.sqrMagnitude < 0.01f)
        {
            ovrStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        }

        if (ovrStick.sqrMagnitude < 0.01f && UnityEngine.InputSystem.Keyboard.current != null)
        {
            float h = 0f;
            float v = 0f;
            if (UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed || UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) h -= 1f;
            if (UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed || UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) h += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed || UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) v += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed || UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) v -= 1f;
            return new Vector2(h, v);
        }

        return ovrStick;
    }
}

