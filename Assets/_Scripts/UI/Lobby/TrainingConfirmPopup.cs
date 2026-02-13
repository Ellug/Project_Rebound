using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 훈련 확인 팝업 창
public class TrainingConfirmPopup : UIPopup
{
    [Header("Preview")]
    [SerializeField] private Image _imgPreview;              // 출력 이미지
    [SerializeField] private Sprite _defaultPreview;         // 기본 이미지

    [Header("Texts")]
    [SerializeField] private TMP_Text _txtName;              // 훈련 이름
    [SerializeField] private TMP_Text _txtConditionModifier; // 컨디션 가감치 표시
    [SerializeField] private TMP_Text _txtDesc;              // 설명

    [Header("Buttons")]
    [SerializeField] private Button _btnCancel;              // 취소 버튼
    [SerializeField] private Button _btnStart;               // 시작 버튼

    private string _trainingKey;                             // 현재 설정된 훈련의 고유 키
    public event Action<string> OnConfirm;                   // 확인(시작) 버튼 클릭 시 호출되는 이벤트

    public override void Init()
    {
        base.Init();

        // 취소 버튼 → 공통 닫기 로직 재사용
        if (_btnCancel != null)
        {
            _btnCancel.onClick.RemoveAllListeners();
            _btnCancel.onClick.AddListener(CloseAndDestroy);
        }

        // 시작 버튼
        if (_btnStart != null)
        {
            _btnStart.onClick.RemoveAllListeners();
            _btnStart.onClick.AddListener(() =>
            {
                OnConfirm?.Invoke(_trainingKey);
                CloseAndDestroy();
            });
        }
    }

    // 팝업 내용 설정
    public void Setup(string trainingKey, string name, int conditionDelta, string desc, Sprite previewSprite)
    {
        _trainingKey = trainingKey;

        // 이름 출력
        if (_txtName != null)
        {
            _txtName.text = name ?? "";
        }

        // 컨디션 가감치 출력
        if (_txtConditionModifier != null)
        {
            if (conditionDelta == 0)
            {
                _txtConditionModifier.gameObject.SetActive(false);
            }
            else
            {
                _txtConditionModifier.gameObject.SetActive(true);
                string signValue = conditionDelta > 0 ? $"+{conditionDelta}" : conditionDelta.ToString();
                _txtConditionModifier.text = $"컨디션 {signValue}";
            }
        }

        // 설명 출력
        if (_txtDesc != null)
        {
            bool hasDesc = !string.IsNullOrEmpty(desc);
            _txtDesc.gameObject.SetActive(hasDesc);
            if (hasDesc) _txtDesc.text = desc;
        }

        // 이미지 출력
        if (_imgPreview != null)
        {
            Sprite sp = previewSprite != null ? previewSprite : _defaultPreview;
            _imgPreview.sprite = sp;
            _imgPreview.enabled = (sp != null);
        }
    }

    // X 버튼 (UIPopup 공통) → 닫기 + 파괴
    protected override void OnCloseButtonClicked()
    {
        CloseAndDestroy();
    }

    // 닫기 공통 처리 (취소, X, 시작 후 모두 여기로)
    private void CloseAndDestroy()
    {
        OnConfirm = null; // 이벤트 정리
        Close();
        Destroy(gameObject);
    }
}