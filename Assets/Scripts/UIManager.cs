using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject messagePanel;

    public void OpenMessagePanel() => messagePanel.SetActive(true);
    public void CloseMessagePanel() => messagePanel.SetActive(false);
}