using System.Collections.Generic;
using UnityEngine;

public class ARWorkbench : MonoBehaviour
{
    public static ARWorkbench Instance { get; private set; }

    [Header("Workbench Dimensions")]
    [SerializeField] private Vector3 workbenchSize = new Vector3(1.1f, 0.75f, 0.7f);
    [SerializeField] private Transform stageMountPoint;

    [Header("Placement State")]
    [SerializeField] private bool isPlacedOnFloor = false;

    [Header("Current Staged Item")]
    [SerializeField] private ManipulableFurniture currentItem;

    [Header("UI Display")]
    [SerializeField] private TextMesh infoLabel;
    [SerializeField] private TextMesh instructionsLabel;

    private LineRenderer gridBorderRenderer;
    private GameObject workbenchVisuals;
    private BoxCollider workbenchCollider;

    public Transform StageMountPoint => stageMountPoint;
    public ManipulableFurniture CurrentItem => currentItem;
    public bool HasItem => currentItem != null;
    public bool IsPlacedOnFloor => isPlacedOnFloor;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetupWorkbenchVisuals();
        SetupUI();
    }

    private void Start()
    {
        // Start in Floor Placement mode so user can look at their floor and place it where they want
        isPlacedOnFloor = false;
        UpdateInfoUI();
    }

    private void Update()
    {
        // If not placed yet, follow player gaze / raycast onto floor
        if (!isPlacedOnFloor)
        {
            UpdateFloorPlacementPreview();
        }
    }

    public void UpdateFloorPlacementPreview()
    {
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null) return;

        Vector3 origin = cam.position;
        Vector3 forward = cam.forward;

        // Cast ray downwards in front of player to find floor
        Vector3 targetPos = origin + forward * 1.8f;

        if (Physics.Raycast(origin + forward * 1.5f + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 4f))
        {
            targetPos = hit.point;
        }
        else
        {
            // Default floor level
            targetPos.y = 0.0f;
        }

        Vector3 lookDir = targetPos - origin;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude < 0.01f) lookDir = cam.forward;
        lookDir.y = 0;
        lookDir.Normalize();

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 15f);
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookDir, Vector3.up), Time.deltaTime * 15f);
        }
    }

    public void ConfirmFloorPlacement()
    {
        isPlacedOnFloor = true;
        UpdateInfoUI();
        Debug.Log("[ARWorkbench] Workbench anchored to floor!");
    }

    public void ResetFloorPlacement()
    {
        isPlacedOnFloor = false;
        UpdateInfoUI();
    }

    private void SetupWorkbenchVisuals()
    {
        workbenchVisuals = new GameObject("WorkbenchVisuals");
        workbenchVisuals.transform.SetParent(transform, false);

        float tableHeight = workbenchSize.y;
        float legHeight = tableHeight - 0.04f;

        // 1. Table Top Glass / Hologram Plate
        GameObject topPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topPlate.name = "TableTopPlate";
        topPlate.transform.SetParent(workbenchVisuals.transform, false);
        topPlate.transform.localPosition = new Vector3(0, tableHeight - 0.02f, 0);
        topPlate.transform.localScale = new Vector3(workbenchSize.x, 0.04f, workbenchSize.z);

        Collider c = topPlate.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        if (litShader != null)
        {
            Material tableMat = new Material(litShader);
            tableMat.color = new Color(0.10f, 0.16f, 0.24f, 0.92f);
            topPlate.GetComponent<MeshRenderer>().material = tableMat;
        }

        // 2. 4 Table Legs resting on the floor (y = 0 to tableHeight)
        float legRadius = 0.035f;
        float legX = workbenchSize.x * 0.46f;
        float legZ = workbenchSize.z * 0.46f;
        float legY = legHeight * 0.5f;

        CreateLeg(workbenchVisuals.transform, new Vector3( legX, legY,  legZ), legHeight, legRadius);
        CreateLeg(workbenchVisuals.transform, new Vector3(-legX, legY,  legZ), legHeight, legRadius);
        CreateLeg(workbenchVisuals.transform, new Vector3( legX, legY, -legZ), legHeight, legRadius);
        CreateLeg(workbenchVisuals.transform, new Vector3(-legX, legY, -legZ), legHeight, legRadius);

        // 3. Stage Mount Point on the top of the table
        GameObject mountObj = new GameObject("StageMountPoint");
        mountObj.transform.SetParent(transform, false);
        mountObj.transform.localPosition = new Vector3(0, tableHeight, 0);
        stageMountPoint = mountObj.transform;

        // 4. Overall Workbench Box Collider
        workbenchCollider = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
        workbenchCollider.size = new Vector3(workbenchSize.x, tableHeight, workbenchSize.z);
        workbenchCollider.center = new Vector3(0, tableHeight * 0.5f, 0);
        workbenchCollider.isTrigger = false;

        // 5. Holographic Grid Border on top surface
        GameObject gridObj = new GameObject("GridBorder");
        gridObj.transform.SetParent(workbenchVisuals.transform, false);
        gridObj.transform.localPosition = new Vector3(0, tableHeight + 0.002f, 0);

        gridBorderRenderer = gridObj.AddComponent<LineRenderer>();
        gridBorderRenderer.useWorldSpace = false;
        gridBorderRenderer.startWidth = 0.008f;
        gridBorderRenderer.endWidth = 0.008f;
        gridBorderRenderer.positionCount = 5;
        gridBorderRenderer.loop = true;

        float hx = workbenchSize.x * 0.5f - 0.02f;
        float hz = workbenchSize.z * 0.5f - 0.02f;
        gridBorderRenderer.SetPositions(new Vector3[]
        {
            new Vector3(-hx, 0, -hz),
            new Vector3( hx, 0, -hz),
            new Vector3( hx, 0,  hz),
            new Vector3(-hx, 0,  hz),
            new Vector3(-hx, 0, -hz)
        });

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        if (unlitShader != null)
        {
            Material lineMat = new Material(unlitShader);
            lineMat.color = new Color(0.1f, 0.85f, 1.0f, 0.9f);
            gridBorderRenderer.material = lineMat;
        }
    }

    private void CreateLeg(Transform parent, Vector3 localPos, float height, float radius)
    {
        GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leg.name = "WorkbenchLeg";
        leg.transform.SetParent(parent, false);
        leg.transform.localPosition = localPos;
        leg.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

        Collider c = leg.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (litShader != null)
        {
            Material legMat = new Material(litShader);
            legMat.color = new Color(0.18f, 0.24f, 0.32f, 1f);
            leg.GetComponent<MeshRenderer>().material = legMat;
        }
    }

    private void SetupUI()
    {
        float tableHeight = workbenchSize.y;

        // 1. Info Header Label
        GameObject infoObj = new GameObject("WorkbenchHeader");
        infoObj.transform.SetParent(transform, false);
        infoObj.transform.localPosition = new Vector3(0, tableHeight + 0.42f, -workbenchSize.z * 0.48f);
        infoObj.transform.localRotation = Quaternion.Euler(20f, 0, 0);

        infoLabel = infoObj.AddComponent<TextMesh>();
        infoLabel.alignment = TextAlignment.Center;
        infoLabel.anchor = TextAnchor.MiddleCenter;
        infoLabel.characterSize = 0.03f;
        infoLabel.fontSize = 32;
        infoLabel.color = new Color(0.3f, 0.9f, 1f, 1f);

        // 2. Instructions footer label
        GameObject instrObj = new GameObject("WorkbenchInstructions");
        instrObj.transform.SetParent(transform, false);
        instrObj.transform.localPosition = new Vector3(0, tableHeight + 0.08f, -workbenchSize.z * 0.52f);
        instrObj.transform.localRotation = Quaternion.Euler(35f, 0, 0);

        instructionsLabel = instrObj.AddComponent<TextMesh>();
        instructionsLabel.alignment = TextAlignment.Center;
        instructionsLabel.anchor = TextAnchor.UpperCenter;
        instructionsLabel.characterSize = 0.018f;
        instructionsLabel.fontSize = 26;
        instructionsLabel.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);
        instructionsLabel.text = "[Grab & Stretch Handles]: Pull to resize | [A / Space]: Color Style\n[Grip Body]: Pick up & Place anywhere in room";

        UpdateInfoUI();
    }

    public void StageFurniture(string label, Vector3 targetDimensions, Vector3 centerOffset)
    {
        if (currentItem != null)
        {
            Destroy(currentItem.gameObject);
            currentItem = null;
        }

        GameObject itemObj = new GameObject($"Virtual_{label}");
        itemObj.transform.SetParent(stageMountPoint, false);
        itemObj.transform.localPosition = Vector3.up * (targetDimensions.y * 0.5f);
        itemObj.transform.localRotation = Quaternion.identity;

        currentItem = itemObj.AddComponent<ManipulableFurniture>();
        currentItem.Initialize(label, targetDimensions, centerOffset);
        currentItem.SetIsOnWorkbench(true);

        UpdateInfoUI();
        Debug.Log($"[ARWorkbench] Staged {label} on workbench. Ready for direct hand editing!");
    }

    public void ReleaseItemFromWorkbench()
    {
        if (currentItem != null)
        {
            currentItem.transform.SetParent(null);
            currentItem.SetIsOnWorkbench(false);
            currentItem = null;
        }
        UpdateInfoUI();
    }

    public void UpdateInfoUI()
    {
        if (infoLabel == null) return;

        if (!isPlacedOnFloor)
        {
            infoLabel.text = "CHOOSE A PLACE FOR YOUR WORKBENCH\nLook at clear floor space & Press [Trigger / Click] to Place";
            if (instructionsLabel != null) instructionsLabel.gameObject.SetActive(false);
        }
        else
        {
            if (instructionsLabel != null) instructionsLabel.gameObject.SetActive(true);

            if (currentItem != null)
            {
                infoLabel.text = $"EDITING: {currentItem.FurnitureLabel.ToUpperInvariant()}\nSize: {currentItem.Size.x:F2}m (W) × {currentItem.Size.y:F2}m (H) × {currentItem.Size.z:F2}m (D)";
            }
            else
            {
                infoLabel.text = "AR WORKBENCH\nPoint at any room furniture & press [Trigger] to Duplicate";
            }
        }
    }
}

