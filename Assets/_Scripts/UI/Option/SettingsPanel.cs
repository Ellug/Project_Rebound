using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : UIBase
{
    [SerializeField] private Button _btnClose;

    public override void Init()
    {
        base.Init();

        if (_btnClose != null)
            _btnClose.onClick.AddListener(() =>
            {
                UIManager.Instance.Close(this);
            });
    }
}