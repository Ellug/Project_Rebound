using UnityEngine;

public sealed class UIPopupPanels : MonoBehaviour
{
    [SerializeField] private GameObject _panelSimple;   // Simple 패널 루트
    [SerializeField] private GameObject _panelDefault;  // Default 패널 루트
    [SerializeField] private GameObject _panelGuide;    // Guide 패널 루트

    // 모든 패널을 일괄 활성/비활성
    public void SetAllPanels(bool active)
    {
        if (_panelSimple != null) _panelSimple.SetActive(active);
        if (_panelDefault != null) _panelDefault.SetActive(active);
        if (_panelGuide != null) _panelGuide.SetActive(active);
    }

    // 타입에 맞는 패널만 활성화
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