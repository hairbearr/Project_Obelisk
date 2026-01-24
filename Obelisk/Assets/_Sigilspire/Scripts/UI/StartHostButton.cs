using UnityEngine;
using Unity.Netcode;

public class StartHostButton : MonoBehaviour
{
    [SerializeField] private GameObject rootToDisable;

    private void Awake()
    {
        // Default: disable after host starts (safety)
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            Hide();
        }
    }

    public void StartAsHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("No NetworkManager in scene!");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
            return;

        Debug.Log("[BOOT] Starting Host");
        NetworkManager.Singleton.StartHost();

        Hide();
    }

    private void Hide()
    {
        if (rootToDisable != null)
            rootToDisable.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}

