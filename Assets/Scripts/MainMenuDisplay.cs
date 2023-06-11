using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
public class MainMenuDisplay : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Gameplay";
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single); 
    }
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
