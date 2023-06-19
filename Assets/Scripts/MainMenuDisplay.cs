using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class MainMenuDisplay : MonoBehaviour
{

    public void StartHost()
    {
        ServerManager.Instance.StartHost();
    }


    public void StartServer()
    {
        ServerManager.Instance.StartServer();
    }


    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }


}
