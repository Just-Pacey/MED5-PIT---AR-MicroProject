using UnityEngine;
using Meta.XR.MRUtilityKit;

public class RoomFurnitureAnchor : MonoBehaviour
{
    [Header("Furniture Info")]
    [SerializeField] private string furnitureLabel = "Furniture";
    [SerializeField] private Vector3 dimensions = Vector3.one;
    [SerializeField] private Vector3 centerOffset = Vector3.zero;
    [SerializeField] private bool isSimulated = false;

    private MRUKAnchor mrukAnchor;
    private BoxCollider boxCollider;
    private LineRenderer wireframeRenderer;
    private GameObject visualModelRoot;
    private bool isHovered = false;

    public string FurnitureLabel => furnitureLabel;
    public Vector3 Dimensions => dimensions;
    public Vector3 CenterOffset => centerOffset;
    public bool IsSimulated => isSimulated;
    public MRUKAnchor MRUKAnchorRef => mrukAnchor;

    public void InitializeFromMRUKAnchor(MRUKAnchor anchor)
    {
        mrukAnchor = anchor;
        isSimulated = false;

        if (anchor.AnchorLabels != null && anchor.AnchorLabels.Count > 0)
        {
            furnitureLabel = FormatLabel(anchor.AnchorLabels[0]);
        }
        else
        {
            furnitureLabel = "Furniture";
        }

        if (anchor.VolumeBounds.HasValue)
        {
            dimensions = anchor.VolumeBounds.Value.size;
            centerOffset = anchor.VolumeBounds.Value.center;
        }
        else if (anchor.PlaneRect.HasValue)
        {
            dimensions = new Vector3(anchor.PlaneRect.Value.width, 0.05f, anchor.PlaneRect.Value.height);
            centerOffset = Vector3.zero;
        }
        else
        {
            dimensions = Vector3.one;
            centerOffset = Vector3.zero;
        }

        EnsureCollider();
        SetupWireframe();
        SetupVisualPreview();
    }

    public void InitializeSimulated(string name, string label, Vector3 size, Vector3 center)
    {
        isSimulated = true;
        furnitureLabel = FormatLabel(label);
        dimensions = size;
        centerOffset = center;

        EnsureCollider();
        SetupWireframe();
        SetupVisualPreview();
    }

    private void EnsureCollider()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        boxCollider.size = dimensions;
        boxCollider.center = centerOffset;
        boxCollider.isTrigger = false;
    }

    private void SetupVisualPreview()
    {
        if (visualModelRoot == null)
        {
            visualModelRoot = transform.Find("VisualPreview")?.gameObject;
            if (visualModelRoot == null)
            {
                visualModelRoot = new GameObject("VisualPreview");
                visualModelRoot.transform.SetParent(transform, false);
                visualModelRoot.transform.localPosition = centerOffset;
            }
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material previewMat = null;
        if (shader != null)
        {
            previewMat = new Material(shader);
            // Subtle translucent ghost material so real furniture in Passthrough is visible behind it
            previewMat.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        }

        FurnitureProceduralMeshGenerator.GenerateModel(visualModelRoot, furnitureLabel, dimensions, previewMat);
    }

    private void SetupWireframe()
    {
        GameObject wireObj = transform.Find("HighlightWireframe")?.gameObject;
        if (wireObj == null)
        {
            wireObj = new GameObject("HighlightWireframe");
            wireObj.transform.SetParent(transform, false);
            wireObj.transform.localPosition = centerOffset;
            wireObj.transform.localRotation = Quaternion.identity;
            wireObj.transform.localScale = Vector3.one;
        }

        wireframeRenderer = wireObj.GetComponent<LineRenderer>();
        if (wireframeRenderer == null)
        {
            wireframeRenderer = wireObj.AddComponent<LineRenderer>();
        }

        wireframeRenderer.useWorldSpace = false;
        wireframeRenderer.startWidth = 0.015f;
        wireframeRenderer.endWidth = 0.015f;
        wireframeRenderer.positionCount = 16;
        wireframeRenderer.loop = false;

        // Create standard wireframe line material
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            Material wireMat = new Material(shader);
            wireMat.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            wireframeRenderer.material = wireMat;
        }

        UpdateWireframePoints();
        wireframeRenderer.enabled = false;
    }

    private void UpdateWireframePoints()
    {
        if (wireframeRenderer == null) return;

        Vector3 ext = dimensions * 0.5f;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-ext.x, -ext.y, -ext.z), // 0
            new Vector3( ext.x, -ext.y, -ext.z), // 1
            new Vector3( ext.x,  ext.y, -ext.z), // 2
            new Vector3(-ext.x,  ext.y, -ext.z), // 3
            new Vector3(-ext.x, -ext.y, -ext.z), // 4 (back to 0)
            new Vector3(-ext.x, -ext.y,  ext.z), // 5
            new Vector3( ext.x, -ext.y,  ext.z), // 6
            new Vector3( ext.x,  ext.y,  ext.z), // 7
            new Vector3(-ext.x,  ext.y,  ext.z), // 8
            new Vector3(-ext.x, -ext.y,  ext.z), // 9 (back to 5)
            new Vector3( ext.x, -ext.y,  ext.z), // 10 (to 6)
            new Vector3( ext.x, -ext.y, -ext.z), // 11 (to 1)
            new Vector3( ext.x,  ext.y, -ext.z), // 12 (to 2)
            new Vector3( ext.x,  ext.y,  ext.z), // 13 (to 7)
            new Vector3(-ext.x,  ext.y,  ext.z), // 14 (to 8)
            new Vector3(-ext.x,  ext.y, -ext.z)  // 15 (to 3)
        };

        wireframeRenderer.positionCount = corners.Length;
        wireframeRenderer.SetPositions(corners);
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        if (wireframeRenderer != null)
        {
            wireframeRenderer.enabled = hovered;
            if (hovered)
            {
                wireframeRenderer.startColor = new Color(0.2f, 0.9f, 1.0f, 0.9f);
                wireframeRenderer.endColor = new Color(0.2f, 0.9f, 1.0f, 0.9f);
            }
        }
    }

    private string FormatLabel(string rawLabel)
    {
        if (string.IsNullOrEmpty(rawLabel)) return "Furniture";
        string cleaned = rawLabel.Replace("_", " ").ToLowerInvariant();
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned);
    }
}

