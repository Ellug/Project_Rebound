using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 훈련 진행 게이지 UI (전체 화면)
// FlowController가 SetProgress01()로 직접 제어
public class TrainingProgressUI : UIBase
{
    [Header("UI")]
    [SerializeField] private Image _imgBackground;
    [SerializeField] private Image _imgGaugeFill;
    [SerializeField] private TMP_Text _txtPercent;
    [SerializeField] private TMP_Text _txtStatus;

    public void Show(Sprite backgroundSprite = null)
    {
        gameObject.SetActive(true);

        if (_imgBackground != null)
        {
            if (backgroundSprite != null)
            {
                _imgBackground.sprite = backgroundSprite;
                _imgBackground.enabled = true;
            }
            else
            {
                _imgBackground.enabled = false;
            }
        }

        SetProgress01(0f);
        SetStatus("진행중..");
    }

    public void SetProgress01(float fill01)
    {
        fill01 = Mathf.Clamp01(fill01);

        if (_imgGaugeFill != null)
            _imgGaugeFill.fillAmount = fill01;

        if (_txtPercent != null)
            _txtPercent.text = Mathf.RoundToInt(fill01 * 100f).ToString();
    }

    public void SetStatus(string status)
    {
        if (_txtStatus != null)
            _txtStatus.text = status;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}