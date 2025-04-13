using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField]
    private Button m_hostButton;

    [SerializeField]
    private Button m_clientButton;

    [SerializeField]
    private Button m_leaveSessionButton;

    public void StartHost()
    {
        if(NetworkManager.Singleton.StartHost())
        {
            ChangeUItoInNetworkSession();
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            ChangeUItoInNetworkSession();
        }
    }

    public void ShutdownNetworkManager()
    {
        NetworkManager.Singleton.Shutdown();

        ChangeUItoDefault();
    }

    private void ChangeUItoInNetworkSession()
    {
        m_hostButton.interactable = false;
        m_clientButton.interactable = false;
        m_leaveSessionButton.interactable = true;
    }

    private void ChangeUItoDefault()
    {
        m_hostButton.interactable = true;
        m_clientButton.interactable = true;
        m_leaveSessionButton.interactable = false;
    }
}
