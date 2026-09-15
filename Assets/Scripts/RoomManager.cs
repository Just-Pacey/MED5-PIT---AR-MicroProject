using UnityEngine;
using Meta.XR.MRUtilityKit;

public class RoomManager : MonoBehaviour
{
    [SerializeField]
    private GameObject whiteRoom;

    private void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogError("MRUK instance not found.");
            return;
        }

        // Wait until MRUK has loaded the room
        MRUK.Instance.SceneLoadedEvent.AddListener(OnRoomLoaded);
    }

    private void OnRoomLoaded()
    {
        Debug.Log("[RoomManager] MRUK room loaded!");

        MRUKRoom room = MRUK.Instance.GetCurrentRoom();

        if (room == null)
        {
            Debug.LogError("[RoomManager] MRUK loaded, but no room was found.");
            return;
        }

        Debug.Log("[RoomManager] Room contains " + room.Anchors.Count + " anchors.");

        // We have successfully found a room.
        if (whiteRoom != null)
        {
            whiteRoom.SetActive(false);
        }

        if (RoomFurnitureScanner.Instance != null)
        {
            RoomFurnitureScanner.Instance.ScanRoomFurniture();
        }
    }

    private void OnDestroy()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnRoomLoaded);
        }
    }
}