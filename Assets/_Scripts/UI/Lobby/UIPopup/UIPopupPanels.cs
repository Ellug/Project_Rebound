using UnityEngine;

public sealed class UIPopupPanels : MonoBehaviour
{
    [SerializeField] private GameObject _panelSimple;
    [SerializeField] private GameObject _panelDefault;
    [SerializeField] private GameObject _panelGuide;

    public void SetAllPanels(bool active)
    {
        if (_panelSimple != null) _panelSimple.SetActive(active);
        if (_panelDefault != null) _panelDefault.SetActive(active);
        if (_panelGuide != null) _panelGuide.SetActive(active);
    }

    public void Activate(UIPopupRequest.PanelType type)
    {
        SetAllPanels(false);

        switch (type)
        {
            case UIPopupRequest.PanelType.Simple:
                if (_panelSimple != null) _panelSimple.SetActive(true);
                break;

            case UIPopupRequest.PanelType.Default:
                if (_panelDefault != null) _panelDefault.SetActive(true);
                break;

            case UIPopupRequest.PanelType.Guide:
                if (_panelGuide != null) _panelGuide.SetActive(true);
                break;
        }
    }
}