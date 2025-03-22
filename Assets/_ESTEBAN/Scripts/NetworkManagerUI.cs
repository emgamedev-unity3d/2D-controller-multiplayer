using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField]
    private Button m_hostButton;

    [SerializeField]
    private Button m_clientButton;

    public void StartHost()
    {
        if(NetworkManager.Singleton.StartHost())
        {
            m_hostButton.enabled = false;
            m_clientButton.enabled = false;
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            m_hostButton.enabled = false;
            m_clientButton.enabled = false;
        }
    }
}
