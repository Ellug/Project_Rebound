using UnityEngine;

public class SaveUI : MonoBehaviour
{
    [SerializeField] private GameObject _viewSavePanel;

    public void SavePanelOpen()
    {
        _viewSavePanel.SetActive(true);
    }
    public void SavePanelClose()
    {
        _viewSavePanel.SetActive(false);
    }
}
