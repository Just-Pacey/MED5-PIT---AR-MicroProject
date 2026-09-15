using UnityEngine;

public enum HandleAxis
{
    PositiveX,
    NegativeX,
    PositiveY,
    NegativeY,
    PositiveZ,
    NegativeZ,
    UniformScale,
    RotateYaw
}

public class FurnitureGizmoHandle : MonoBehaviour
{
    [SerializeField] private HandleAxis axisType;
    [SerializeField] private ManipulableFurniture parentFurniture;
    [SerializeField] private MeshRenderer handleRenderer;
    [SerializeField] private SphereCollider handleCollider;

    private Color normalColor = new Color(0.2f, 0.7f, 1.0f, 0.85f);
    private Color hoverColor = new Color(1.0f, 0.85f, 0.2f, 1.0f);
    private Color activeColor = new Color(0.2f, 1.0f, 0.4f, 1.0f);
    private Material handleMaterial;

    public HandleAxis AxisType => axisType;
    public ManipulableFurniture ParentFurniture => parentFurniture;

    public void Initialize(ManipulableFurniture parent, HandleAxis type)
    {
        parentFurniture = parent;
        axisType = type;

        if (handleCollider == null)
        {
            handleCollider = gameObject.GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();
            handleCollider.radius = 0.05f;
            handleCollider.isTrigger = false;
        }

        if (handleRenderer == null)
        {
            handleRenderer = gameObject.GetComponent<MeshRenderer>();
            if (handleRenderer == null)
            {
                MeshFilter mf = gameObject.AddComponent<MeshFilter>();
                GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mf.sharedMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(tempSphere);
                handleRenderer = gameObject.AddComponent<MeshRenderer>();
            }
        }

        // Set distinctive colors based on axis
        switch (type)
        {
            case HandleAxis.PositiveX:
            case HandleAxis.NegativeX:
                normalColor = new Color(1.0f, 0.3f, 0.3f, 0.9f); // Red for X (Width)
                break;
            case HandleAxis.PositiveY:
            case HandleAxis.NegativeY:
                normalColor = new Color(0.3f, 1.0f, 0.3f, 0.9f); // Green for Y (Height)
                break;
            case HandleAxis.PositiveZ:
            case HandleAxis.NegativeZ:
                normalColor = new Color(0.3f, 0.5f, 1.0f, 0.9f); // Blue for Z (Depth)
                break;
            case HandleAxis.UniformScale:
                normalColor = new Color(1.0f, 0.8f, 0.1f, 0.9f); // Gold for Uniform
                break;
            case HandleAxis.RotateYaw:
                normalColor = new Color(0.8f, 0.3f, 1.0f, 0.9f); // Purple for Rotation
                break;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            handleMaterial = new Material(shader);
            handleMaterial.color = normalColor;
            handleRenderer.material = handleMaterial;
        }

        transform.localScale = Vector3.one * 0.08f;
    }

    public void SetHovered(bool hovered)
    {
        if (handleMaterial != null)
        {
            handleMaterial.color = hovered ? hoverColor : normalColor;
        }
    }

    public void SetActive(bool active)
    {
        if (handleMaterial != null)
        {
            handleMaterial.color = active ? activeColor : normalColor;
        }
    }

    public Vector3 GetLocalDirection()
    {
        switch (axisType)
        {
            case HandleAxis.PositiveX: return Vector3.right;
            case HandleAxis.NegativeX: return Vector3.left;
            case HandleAxis.PositiveY: return Vector3.up;
            case HandleAxis.NegativeY: return Vector3.down;
            case HandleAxis.PositiveZ: return Vector3.forward;
            case HandleAxis.NegativeZ: return Vector3.back;
            default: return Vector3.up;
        }
    }
}
