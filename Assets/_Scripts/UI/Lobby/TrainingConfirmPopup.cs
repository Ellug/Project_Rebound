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

    [SerializeField] private StudentSelectPopup _studentSelectPopupPrefab;

    private string _trainingKey;                             // 현재 설정된 훈련의 고유 키
    public event Action<string> OnConfirm;                   // 확인 버튼 클릭 시 호출되는 이벤트

    // 훈련 선택 팝업 참조 (확인 팝업에서 훈련 선택 팝업을 닫기 위해 사용)
    private TrainingSelectPopup _ownerSelectPopup;

    // 훈련 선택 팝업에서 Confirm 생성 직후 주입
    public void SetOwner(TrainingSelectPopup owner)
    {
        _ownerSelectPopup = owner;
    }

    public override void Init()
    {
        base.Init();

        if (_btnStart != null)
        {
            _btnStart.onClick.RemoveAllListeners();
            _btnStart.onClick.AddListener(OnClickStart);
        }

        if (_btnCancel != null)
        {
            _btnCancel.onClick.RemoveAllListeners();
            _btnCancel.onClick.AddListener(CloseAndDestroy);
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

    // 닫기 공통 처리
    private void CloseAndDestroy()
    {
        OnConfirm = null; // 이벤트 정리
        Close();
        Destroy(gameObject);
    }

    private void OnClickStart()
    {
        // (선택) 훈련 확정 이벤트가 필요하면 유지
        OnConfirm?.Invoke(_trainingKey);

        if (_studentSelectPopupPrefab == null)
        {
            Debug.LogError("[TrainingConfirmPopup] _studentSelectPopupPrefab이 null입니다!");
            return;
        }

        // 1) 학생 선택 팝업 띄우기
        // ConfirmPopup은 transform.parent에 생성되고 있으므로 동일한 루트에 생성
        Transform popupRoot = transform.parent != null ? transform.parent : transform.root;

        StudentSelectPopup studentPopup = Instantiate(_studentSelectPopupPrefab, popupRoot);
        studentPopup.Init();
        studentPopup.Open();

        // 2) 훈련 선택 팝업 닫기 (씬 배치 방식이므로 Close로 비활성화)
        if (_ownerSelectPopup != null)
        {
            _ownerSelectPopup.ForceCloseFromChild();
        }

        // 3) 훈련 확인 팝업 닫기 (현재 팝업)
        CloseAndDestroy();
    }
}