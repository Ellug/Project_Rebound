using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 훈련 진행 전체 화면 게이지 UI
// TrainingFlowController가 직접 제어
public class TrainingProgressUI : UIBase
{
    [Header("UI")]
    [SerializeField] private Image _imgBackground;   // 배경 이미지
    [SerializeField] private Image _imgGaugeFill;    // 게이지 Fill
    [SerializeField] private TMP_Text _txtPercent;   // 퍼센트 표시
    [SerializeField] private TMP_Text _txtStatus;    // 상태 텍스트

    // UI 표시 및 초기화
    public void Show(Sprite backgroundSprite = null)
    {
        gameObject.SetActive(true);

        SetProgress01(0f);
        SetStatus("진행중..");
    }

    // 진행률 설정 (0~1)
    public void SetProgress01(float fill01)
    {
        fill01 = Mathf.Clamp01(fill01);

        if (_imgGaugeFill != null)
            _imgGaugeFill.fillAmount = fill01;

        if (_txtPercent != null)
            _txtPercent.text = Mathf.RoundToInt(fill01 * 100f).ToString();
    }

    // 상태 텍스트 변경
    public void SetStatus(string status)
    {
        if (_txtStatus != null)
            _txtStatus.text = status;
    }

    // UI 숨김
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}