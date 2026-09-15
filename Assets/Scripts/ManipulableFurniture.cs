using System.Collections.Generic;
using UnityEngine;

public class ManipulableFurniture : MonoBehaviour
{
    [Header("Furniture Info")]
    [SerializeField] private string furnitureLabel = "Virtual Table";
    [SerializeField] private Vector3 size = Vector3.one;
    [SerializeField] private Vector3 centerOffset = Vector3.zero;
    [SerializeField] private bool isOnWorkbench = false;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool isSelected = false;

    [Header("Visual Components")]
    [SerializeField] private GameObject visualBodyRoot;
    [SerializeField] private BoxCollider bodyCollider;
    [SerializeField] private LineRenderer wireframeRenderer;
    [SerializeField] private Transform handlesRoot;

    [Header("UI & Feedback")]
    [SerializeField] private TextMesh dimensionLabel;

    private List<FurnitureGizmoHandle> handles = new List<FurnitureGizmoHandle>();
    private int currentStyleIndex = 0;
    private Material bodyMaterial;

    private static readonly Color[] StyleColors = new Color[]
    {
        new Color(0.18f, 0.68f, 0.98f, 0.90f), // AR Cyan / Blue
        new Color(0.78f, 0.54f, 0.35f, 0.95f), // Warm Oak Wood
        new Color(0.22f, 0.24f, 0.28f, 0.95f), // Charcoal Dark
        new Color(0.88f, 0.88f, 0.90f, 0.85f), // Modern White / Silver
        new Color(0.95f, 0.68f, 0.22f, 0.90f)  // Warm Amber / Gold
    };

    public string FurnitureLabel => furnitureLabel;
    public Vector3 Size => size;
    public bool IsOnWorkbench => isOnWorkbench;
    public bool IsLocked => isLocked;
    public bool IsSelected => isSelected;

    public void Initialize(string label, Vector3 initialSize, Vector3 center, Material baseMat = null)
    {
        furnitureLabel = label;
        size = new Vector3(
            Mathf.Max(initialSize.x, 0.15f),
            Mathf.Max(initialSize.y, 0.15f),
            Mathf.Max(initialSize.z, 0.15f)
        );
        centerOffset = center;

        SetupMaterial();
        RebuildVisualMesh();
        SetupCollider();
        SetupWireframe();
        SetupHandles();
        SetupDimensionLabel();
        SetStyle(0);
        SetSelected(true);
    }

    public void SetIsOnWorkbench(bool onWorkbench)
    {
        isOnWorkbench = onWorkbench;
        if (onWorkbench)
        {
            isLocked = false;
            SetSelected(true);
        }
        UpdateDimensionLabel();
    }

    private void SetupMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            bodyMaterial = new Material(shader);
            if (bodyMaterial.HasProperty("_Surface"))
            {
                bodyMaterial.SetFloat("_Surface", 0); // Opaque / Solid
                bodyMaterial.SetFloat("_Smoothness", 0.6f);
            }
        }
    }

    private void RebuildVisualMesh()
    {
        if (visualBodyRoot == null)
        {
            visualBodyRoot = transform.Find("VisualBody")?.gameObject;
            if (visualBodyRoot == null)
            {
                visualBodyRoot = new GameObject("VisualBody");
                visualBodyRoot.transform.SetParent(transform, false);
                visualBodyRoot.transform.localPosition = centerOffset;
            }
        }

        FurnitureProceduralMeshGenerator.GenerateModel(visualBodyRoot, furnitureLabel, size, bodyMaterial);
    }

    private void SetupCollider()
    {
        bodyCollider = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
        bodyCollider.size = size;
        bodyCollider.center = centerOffset;
        bodyCollider.isTrigger = false;
    }

    private void SetupWireframe()
    {
        GameObject wireObj = transform.Find("Wireframe")?.gameObject;
        if (wireObj == null)
        {
            wireObj = new GameObject("Wireframe");
            wireObj.transform.SetParent(transform, false);
            wireObj.transform.localPosition = centerOffset;
        }

        wireframeRenderer = wireObj.GetComponent<LineRenderer>() ?? wireObj.AddComponent<LineRenderer>();
        wireframeRenderer.useWorldSpace = false;
        wireframeRenderer.startWidth = 0.008f;
        wireframeRenderer.endWidth = 0.008f;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            Material wireMat = new Material(shader);
            wireMat.color = new Color(0.3f, 0.9f, 1.0f, 0.85f);
            wireframeRenderer.material = wireMat;
        }

        UpdateWireframe();
    }

    private void UpdateWireframe()
    {
        if (wireframeRenderer == null) return;

        Vector3 ext = size * 0.5f;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-ext.x, -ext.y, -ext.z),
            new Vector3( ext.x, -ext.y, -ext.z),
            new Vector3( ext.x,  ext.y, -ext.z),
            new Vector3(-ext.x,  ext.y, -ext.z),
            new Vector3(-ext.x, -ext.y, -ext.z),
            new Vector3(-ext.x, -ext.y,  ext.z),
            new Vector3( ext.x, -ext.y,  ext.z),
            new Vector3( ext.x,  ext.y,  ext.z),
            new Vector3(-ext.x,  ext.y,  ext.z),
            new Vector3(-ext.x, -ext.y,  ext.z),
            new Vector3( ext.x, -ext.y,  ext.z),
            new Vector3( ext.x, -ext.y, -ext.z),
            new Vector3( ext.x,  ext.y, -ext.z),
            new Vector3( ext.x,  ext.y,  ext.z),
            new Vector3(-ext.x,  ext.y,  ext.z),
            new Vector3(-ext.x,  ext.y, -ext.z)
        };

        wireframeRenderer.positionCount = corners.Length;
        wireframeRenderer.SetPositions(corners);
    }

    private void SetupHandles()
    {
        if (handlesRoot == null)
        {
            GameObject rootObj = transform.Find("Handles")?.gameObject;
            if (rootObj == null)
            {
                rootObj = new GameObject("Handles");
                rootObj.transform.SetParent(transform, false);
                rootObj.transform.localPosition = centerOffset;
            }
            handlesRoot = rootObj.transform;
        }

        foreach (Transform child in handlesRoot)
        {
            Destroy(child.gameObject);
        }
        handles.Clear();

        // 6 Face stretch grab handles
        CreateHandle("Handle_Right", HandleAxis.PositiveX);
        CreateHandle("Handle_Left", HandleAxis.NegativeX);
        CreateHandle("Handle_Top", HandleAxis.PositiveY);
        CreateHandle("Handle_Bottom", HandleAxis.NegativeY);
        CreateHandle("Handle_Front", HandleAxis.PositiveZ);
        CreateHandle("Handle_Back", HandleAxis.NegativeZ);

        // Corner handle for uniform scale
        CreateHandle("Handle_UniformCorner", HandleAxis.UniformScale);

        UpdateHandlePositions();
    }

    private void CreateHandle(string name, HandleAxis axis)
    {
        GameObject handleObj = new GameObject(name);
        handleObj.transform.SetParent(handlesRoot, false);
        FurnitureGizmoHandle handle = handleObj.AddComponent<FurnitureGizmoHandle>();
        handle.Initialize(this, axis);
        handles.Add(handle);
    }

    private void UpdateHandlePositions()
    {
        if (handlesRoot == null) return;

        Vector3 ext = size * 0.5f;

        foreach (var handle in handles)
        {
            if (handle == null) continue;

            switch (handle.AxisType)
            {
                case HandleAxis.PositiveX:
                    handle.transform.localPosition = new Vector3(ext.x, 0, 0);
                    break;
                case HandleAxis.NegativeX:
                    handle.transform.localPosition = new Vector3(-ext.x, 0, 0);
                    break;
                case HandleAxis.PositiveY:
                    handle.transform.localPosition = new Vector3(0, ext.y, 0);
                    break;
                case HandleAxis.NegativeY:
                    handle.transform.localPosition = new Vector3(0, -ext.y, 0);
                    break;
                case HandleAxis.PositiveZ:
                    handle.transform.localPosition = new Vector3(0, 0, ext.z);
                    break;
                case HandleAxis.NegativeZ:
                    handle.transform.localPosition = new Vector3(0, 0, -ext.z);
                    break;
                case HandleAxis.UniformScale:
                    handle.transform.localPosition = new Vector3(ext.x, ext.y, ext.z);
                    break;
                case HandleAxis.RotateYaw:
                    handle.transform.localPosition = new Vector3(0, ext.y + 0.15f, 0);
                    break;
            }
        }
    }

    private void SetupDimensionLabel()
    {
        GameObject labelObj = transform.Find("DimensionLabel")?.gameObject;
        if (labelObj == null)
        {
            labelObj = new GameObject("DimensionLabel");
            labelObj.transform.SetParent(transform, false);
        }

        dimensionLabel = labelObj.GetComponent<TextMesh>() ?? labelObj.AddComponent<TextMesh>();
        dimensionLabel.alignment = TextAlignment.Center;
        dimensionLabel.anchor = TextAnchor.MiddleCenter;
        dimensionLabel.characterSize = 0.035f;
        dimensionLabel.fontSize = 32;
        dimensionLabel.color = Color.white;

        UpdateDimensionLabel();
    }

    private void UpdateDimensionLabel()
    {
        if (dimensionLabel == null) return;

        dimensionLabel.transform.localPosition = centerOffset + new Vector3(0, size.y * 0.5f + 0.15f, 0);
        string locTag = isOnWorkbench ? "[On Workbench]" : "[Placed in Room]";
        dimensionLabel.text = $"{furnitureLabel} {locTag}\n{size.x:F2}m × {size.y:F2}m × {size.z:F2}m";
    }

    private void LateUpdate()
    {
        if (dimensionLabel != null && Camera.main != null && dimensionLabel.gameObject.activeSelf)
        {
            dimensionLabel.transform.rotation = Quaternion.LookRotation(
                dimensionLabel.transform.position - Camera.main.transform.position
            );
        }
    }

    public void StretchByHandle(HandleAxis axis, float deltaMovement)
    {
        if (isLocked) return;

        Vector3 newSize = size;
        Vector3 newPos = transform.position;
        Vector3 right = transform.right;
        Vector3 up = transform.up;
        Vector3 forward = transform.forward;

        switch (axis)
        {
            case HandleAxis.PositiveX:
                newSize.x = Mathf.Max(0.15f, size.x + deltaMovement);
                newPos += right * ((newSize.x - size.x) * 0.5f);
                break;

            case HandleAxis.NegativeX:
                newSize.x = Mathf.Max(0.15f, size.x + deltaMovement);
                newPos -= right * ((newSize.x - size.x) * 0.5f);
                break;

            case HandleAxis.PositiveY:
                newSize.y = Mathf.Max(0.15f, size.y + deltaMovement);
                newPos += up * ((newSize.y - size.y) * 0.5f);
                break;

            case HandleAxis.NegativeY:
                newSize.y = Mathf.Max(0.15f, size.y + deltaMovement);
                newPos -= up * ((newSize.y - size.y) * 0.5f);
                break;

            case HandleAxis.PositiveZ:
                newSize.z = Mathf.Max(0.15f, size.z + deltaMovement);
                newPos += forward * ((newSize.z - size.z) * 0.5f);
                break;

            case HandleAxis.NegativeZ:
                newSize.z = Mathf.Max(0.15f, size.z + deltaMovement);
                newPos -= forward * ((newSize.z - size.z) * 0.5f);
                break;

            case HandleAxis.UniformScale:
                float scaleFactor = 1.0f + (deltaMovement / Mathf.Max(size.magnitude, 0.1f));
                newSize = new Vector3(
                    Mathf.Max(0.15f, size.x * scaleFactor),
                    Mathf.Max(0.15f, size.y * scaleFactor),
                    Mathf.Max(0.15f, size.z * scaleFactor)
                );
                break;
        }

        if (!isOnWorkbench)
        {
            transform.position = newPos;
        }
        else
        {
            transform.localPosition = new Vector3(0, newSize.y * 0.5f, 0);
        }

        SetDimensions(newSize);

        if (isOnWorkbench && ARWorkbench.Instance != null)
        {
            ARWorkbench.Instance.UpdateInfoUI();
        }
    }

    public void SetDimensions(Vector3 newSize)
    {
        size = new Vector3(
            Mathf.Max(0.15f, newSize.x),
            Mathf.Max(0.15f, newSize.y),
            Mathf.Max(0.15f, newSize.z)
        );

        if (visualBodyRoot != null)
        {
            FurnitureProceduralMeshGenerator.GenerateModel(visualBodyRoot, furnitureLabel, size, bodyMaterial);
        }

        if (bodyCollider != null)
        {
            bodyCollider.size = size;
        }

        UpdateWireframe();
        UpdateHandlePositions();
        UpdateDimensionLabel();
    }

    public void RotateYaw(float angleDelta)
    {
        if (isLocked) return;
        transform.Rotate(Vector3.up, angleDelta, Space.World);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (handlesRoot != null)
        {
            handlesRoot.gameObject.SetActive(selected && !isLocked);
        }
        if (wireframeRenderer != null)
        {
            wireframeRenderer.enabled = selected;
        }
        if (dimensionLabel != null)
        {
            dimensionLabel.gameObject.SetActive(selected);
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (handlesRoot != null)
        {
            handlesRoot.gameObject.SetActive(isSelected && !isLocked);
        }
        UpdateDimensionLabel();
    }

    public void CycleStyle()
    {
        currentStyleIndex = (currentStyleIndex + 1) % StyleColors.Length;
        SetStyle(currentStyleIndex);
    }

    public void SetStyle(int styleIdx)
    {
        currentStyleIndex = Mathf.Clamp(styleIdx, 0, StyleColors.Length - 1);
        if (bodyMaterial != null)
        {
            bodyMaterial.color = StyleColors[currentStyleIndex];
        }

        if (visualBodyRoot != null)
        {
            var renderers = visualBodyRoot.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.material = bodyMaterial;
            }
        }
    }

    public ManipulableFurniture DuplicateSelf()
    {
        GameObject cloneGo = new GameObject($"{furnitureLabel} (Copy)");
        cloneGo.transform.position = transform.position + transform.right * 0.4f + Vector3.up * 0.05f;
        cloneGo.transform.rotation = transform.rotation;

        ManipulableFurniture clone = cloneGo.AddComponent<ManipulableFurniture>();
        clone.Initialize(furnitureLabel, size, centerOffset);
        clone.SetStyle(currentStyleIndex);
        clone.SetIsOnWorkbench(false);
        return clone;
    }

    public void DeleteSelf()
    {
        Destroy(gameObject);
    }
}


