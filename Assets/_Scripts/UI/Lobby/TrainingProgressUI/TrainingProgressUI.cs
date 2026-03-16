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

    private string _currentBackgroundImageId;        // 현재 로드된 배경 이미지 ID (해제용)

    // UI 표시 및 초기화
    public void Show(string backgroundImageId = null)
    {
        gameObject.SetActive(true);

        SetProgress01(0f);
        SetStatus("진행중..");

        // 배경 이미지 ID가 있으면 Addressable로 비동기 로드
        LoadBackgroundImage(backgroundImageId);
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
        ReleaseBackgroundImage();
        gameObject.SetActive(false);
    }

    // 이미지 ID 기준으로 Addressable 비동기 로드
    private void LoadBackgroundImage(string imageId)
    {
        // 기존 이미지 해제
        ReleaseBackgroundImage();

        if (_imgBackground == null) return;

        if (string.IsNullOrEmpty(imageId))
        {
            _imgBackground.gameObject.SetActive(false);
            return;
        }

        _imgBackground.gameObject.SetActive(false);
        _currentBackgroundImageId = imageId;

        AddressableImageManager.Instance.LoadSprite(imageId, sprite =>
        {
            if (_imgBackground == null) return;

            if (sprite != null)
            {
                _imgBackground.sprite = sprite;
                _imgBackground.preserveAspect = true;
                _imgBackground.gameObject.SetActive(true);
            }
            else
            {
                _imgBackground.gameObject.SetActive(false);
            }
        });
    }

    // 현재 로드된 배경 이미지 해제
    private void ReleaseBackgroundImage()
    {
        if (string.IsNullOrEmpty(_currentBackgroundImageId)) return;

        AddressableImageManager.Instance.ReleaseSprite(_currentBackgroundImageId);
        _currentBackgroundImageId = null;
    }
}