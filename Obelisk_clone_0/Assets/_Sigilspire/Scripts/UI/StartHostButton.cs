using UnityEngine;
using Unity.Netcode;

public class StartHostButton : MonoBehaviour
{
    [SerializeField] private GameObject rootToDisable;
    [SerializeField] private GameObject textRootToDisable;

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
            return;
        }

        if (NetworkManager.Singleton.IsListening)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick();
        }

        NetworkManager.Singleton.StartHost();

        Hide();
    }

    private void Hide()
    {
        if (rootToDisable != null)
            rootToDisable.SetActive(false);
        else
            gameObject.SetActive(false);

        if(textRootToDisable != null)
            textRootToDisable.SetActive(false);
    }
}

