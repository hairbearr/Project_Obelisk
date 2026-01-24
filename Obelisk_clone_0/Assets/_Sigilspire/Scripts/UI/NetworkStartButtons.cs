using Unity.Netcode;
using UnityEngine;

public class NetworkStartButtons : MonoBehaviour
{
    public void StartHost() => NetworkManager.Singleton.StartHost();
    public void StartClient() => NetworkManager.Singleton.StartClient();
    public void StartServer() => NetworkManager.Singleton.StartServer();
}


