using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class RoomFurnitureScanner : MonoBehaviour
{
    public static RoomFurnitureScanner Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool autoScanOnRoomLoaded = true;
    [SerializeField] private bool createEditorSimulatedFurnitureIfEmpty = true;

    [Header("Detected Furniture")]
    [SerializeField] private List<RoomFurnitureAnchor> detectedFurniture = new List<RoomFurnitureAnchor>();

    public System.Action<List<RoomFurnitureAnchor>> OnFurnitureScanComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.AddListener(OnRoomLoaded);
        }
    }

    private void OnDestroy()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnRoomLoaded);
        }
    }

    private void OnRoomLoaded()
    {
        if (autoScanOnRoomLoaded)
        {
            ScanRoomFurniture();
        }
    }

    public void ScanRoomFurniture()
    {
        detectedFurniture.Clear();

        if (MRUK.Instance == null)
        {
            Debug.LogWarning("[RoomFurnitureScanner] MRUK Instance not found.");
            return;
        }

        MRUKRoom room = MRUK.Instance.GetCurrentRoom();
        if (room == null)
        {
            Debug.LogWarning("[RoomFurnitureScanner] No current room found in MRUK.");
            if (Application.isEditor && createEditorSimulatedFurnitureIfEmpty)
            {
                CreateSimulatedFurniture();
            }
            return;
        }

        Debug.Log($"[RoomFurnitureScanner] Scanning room with {room.Anchors.Count} anchors...");

        foreach (MRUKAnchor anchor in room.Anchors)
        {
            if (anchor == null) continue;

            if (IsFurnitureAnchor(anchor))
            {
                RoomFurnitureAnchor furniture = anchor.gameObject.GetComponent<RoomFurnitureAnchor>();
                if (furniture == null)
                {
                    furniture = anchor.gameObject.AddComponent<RoomFurnitureAnchor>();
                }

                furniture.InitializeFromMRUKAnchor(anchor);
                detectedFurniture.Add(furniture);
            }
        }

        Debug.Log($"[RoomFurnitureScanner] Found {detectedFurniture.Count} furniture pieces in the room.");

        if (detectedFurniture.Count == 0 && Application.isEditor && createEditorSimulatedFurnitureIfEmpty)
        {
            CreateSimulatedFurniture();
        }

        OnFurnitureScanComplete?.Invoke(detectedFurniture);
    }

    private bool IsFurnitureAnchor(MRUKAnchor anchor)
    {
        string labelStr = anchor.name;
        if (anchor.VolumeBounds.HasValue)
        {
            // Recognize furniture vs walls/floor
            string upper = anchor.name.ToUpperInvariant();
            if (upper.Contains("WALL") || upper.Contains("FLOOR") || upper.Contains("CEILING"))
            {
                return false;
            }
            return true;
        }

        return false;
    }

    private void CreateSimulatedFurniture()
    {
        Debug.Log("[RoomFurnitureScanner] Creating simulated furniture for Editor testing...");
        GameObject simRoot = GameObject.Find("Simulated Room Furniture");
        if (simRoot != null) Destroy(simRoot);

        simRoot = new GameObject("Simulated Room Furniture");

        // Create sample Table
        CreateSimulatedItem(simRoot.transform, "Coffee Table", "TABLE", new Vector3(0, 0.4f, 1.8f), Quaternion.identity, new Vector3(1.2f, 0.45f, 0.7f));
        // Create sample Couch
        CreateSimulatedItem(simRoot.transform, "Living Room Couch", "COUCH", new Vector3(-1.8f, 0.5f, 1.8f), Quaternion.Euler(0, 45, 0), new Vector3(2.0f, 0.85f, 0.9f));
        // Create sample Desk
        CreateSimulatedItem(simRoot.transform, "Work Desk", "DESK", new Vector3(1.8f, 0.75f, 1.5f), Quaternion.Euler(0, -30, 0), new Vector3(1.4f, 0.75f, 0.8f));
        // Create sample Storage/Bookshelf
        CreateSimulatedItem(simRoot.transform, "Bookshelf", "STORAGE", new Vector3(0, 0.9f, 2.8f), Quaternion.identity, new Vector3(0.9f, 1.8f, 0.4f));
    }

    private void CreateSimulatedItem(Transform parent, string name, string label, Vector3 localPos, Quaternion localRot, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        BoxCollider col = go.AddComponent<BoxCollider>();
        col.size = size;
        col.center = Vector3.zero;

        RoomFurnitureAnchor anchor = go.AddComponent<RoomFurnitureAnchor>();
        anchor.InitializeSimulated(name, label, size, Vector3.zero);
        detectedFurniture.Add(anchor);
    }

    public List<RoomFurnitureAnchor> GetDetectedFurniture()
    {
        return detectedFurniture;
    }
}
