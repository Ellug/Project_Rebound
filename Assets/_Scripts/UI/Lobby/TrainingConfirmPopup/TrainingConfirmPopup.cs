using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 훈련 확인 팝업 (범용 팝업으로 확장)
// Setup(TrainingButtonData)        → 훈련 모드 : 훈련시작 / 취소 버튼
// SetupAsConfirm(ConfirmPopupData) → 범용 확인 모드 : 상황별 버튼 라벨 및 콜백 주입
// SetupAsEvent(EventPopupData)     → 이벤트 팝업 모드 : 학생 이미지 + 메시지 + 포기/확인
public class TrainingConfirmPopup : UIPopup
{
    [Header("Preview")]
    [SerializeField] private Image _imgPreview;
    [SerializeField] private Sprite _defaultPreview;

    [Header("Texts")]
    [SerializeField] private TMP_Text _txtName2;
    [SerializeField] private TMP_Text _txtConditionModifier;
    [SerializeField] private TMP_Text _txtDesc;

    [Header("Buttons")]
    [SerializeField] private Button _btnCancel;
    [SerializeField] private TMP_Text _txtCancel;  // 취소 버튼 라벨 (CenceleText)
    [SerializeField] private Button _btnStart;
    [SerializeField] private TMP_Text _txtStart;   // 시작/확인 버튼 라벨 (StartText)

    [Header("Student Select")]
    [SerializeField] private StudentSelectPopup _studentSelectPrefab;

    private TrainingButtonData _trainingData;

    // 훈련 모드: (trainingKey, 선택된 학생 목록) 전달
    public event Action<string, List<Student>> OnTrainingConfirmed;
    // 범용/이벤트 모드: 확인 버튼 콜백
    public event Action OnConfirmed;
    // 범용/이벤트 모드: 취소/포기 버튼 콜백
    public event Action OnCancelled;

    public override void Init()
    {
        base.Init();

        if (_btnCancel != null)
        {
            _btnCancel.onClick.RemoveAllListeners();
            _btnCancel.onClick.AddListener(HandleCancelButton);
        }

        if (_btnStart != null)
        {
            _btnStart.onClick.RemoveAllListeners();
            _btnStart.onClick.AddListener(HandleStartButton);
        }
    }

    // 훈련 모드 세팅

    public void Setup(TrainingButtonData data)
    {
        _trainingData = data;

        SetText(_txtCancel, "취소");
        SetText(_txtStart, "훈련 시작");

        if (_txtName2 != null)
            _txtName2.text = data.trainingName ?? "";

        if (_txtConditionModifier != null)
        {
            if (data.conditionDelta == 0)
            {
                _txtConditionModifier.gameObject.SetActive(false);
            }
            else
            {
                _txtConditionModifier.gameObject.SetActive(true);
                string sign = data.conditionDelta > 0 ? $"+{data.conditionDelta}" : data.conditionDelta.ToString();
                _txtConditionModifier.text = $"컨디션 {sign}";
            }
        }

        if (_txtDesc != null)
        {
            bool hasDesc = !string.IsNullOrEmpty(data.trainingDesc);
            _txtDesc.gameObject.SetActive(hasDesc);
            if (hasDesc) _txtDesc.text = data.trainingDesc;
        }

        if (_imgPreview != null)
        {
            Sprite sp = data.previewSprite != null ? data.previewSprite : _defaultPreview;
            _imgPreview.sprite = sp;
            _imgPreview.enabled = (sp != null);
        }
    }

    // 범용 확인 모드 세팅
    // cancelLabel이 null이면 취소 버튼 숨김

    public void SetupAsConfirm(ConfirmPopupData data)
    {
        _trainingData = null;

        SetText(_txtName2, data.Title);
        SetText(_txtStart, data.ConfirmLabel ?? "확인");

        bool hasCancelLabel = !string.IsNullOrEmpty(data.CancelLabel);
        if (_btnCancel != null)
            _btnCancel.gameObject.SetActive(hasCancelLabel);
        if (hasCancelLabel)
            SetText(_txtCancel, data.CancelLabel);

        if (_txtDesc != null)
        {
            bool hasDesc = !string.IsNullOrEmpty(data.Description);
            _txtDesc.gameObject.SetActive(hasDesc);
            if (hasDesc) _txtDesc.text = data.Description;
        }

        // 컨디션 수치 라벨은 범용 모드에서 불필요
        if (_txtConditionModifier != null)
            _txtConditionModifier.gameObject.SetActive(false);

        if (_imgPreview != null)
        {
            Sprite sp = data.PreviewSprite != null ? data.PreviewSprite : _defaultPreview;
            _imgPreview.sprite = sp;
            _imgPreview.enabled = (sp != null);
        }
    }

    // 이벤트 팝업 모드 세팅
    // 학생 영입 안내 등 이벤트성 팝업에서 사용
    // 이미지 + 메시지 + 포기/확인 버튼 구성

    public void SetupAsEvent(EventPopupData data)
    {
        _trainingData = null;

        // 제목 표시
        if (_txtName2 != null)
        {
            bool hasTitle = !string.IsNullOrEmpty(data.Title);
            _txtName2.gameObject.SetActive(hasTitle);
            if (hasTitle) _txtName2.text = data.Title;
        }

        // 컨디션 수치 라벨 숨김
        if (_txtConditionModifier != null)
            _txtConditionModifier.gameObject.SetActive(false);

        // 메시지
        if (_txtDesc != null)
        {
            bool hasDesc = !string.IsNullOrEmpty(data.Message);
            _txtDesc.gameObject.SetActive(hasDesc);
            if (hasDesc) _txtDesc.text = data.Message;
        }

        // 버튼 라벨
        SetText(_txtCancel, data.CancelLabel ?? "포기");
        SetText(_txtStart, data.ConfirmLabel ?? "확인");

        if (_btnCancel != null)
            _btnCancel.gameObject.SetActive(true);

        // 이미지
        if (_imgPreview != null)
        {
            bool hasSprite = data.StudentSprite != null || _defaultPreview != null;
            Sprite sp = data.StudentSprite != null ? data.StudentSprite : _defaultPreview;
            _imgPreview.sprite = sp;
            _imgPreview.enabled = hasSprite;
        }
    }

    // 버튼 핸들러

    private void HandleCancelButton()
    {
        if (_trainingData != null)
        {
            // 훈련 모드: 그냥 닫기
            CloseAndDestroy();
        }
        else
        {
            // 범용/이벤트 모드: OnCancelled 발행 후 닫기
            OnCancelled?.Invoke();
            CloseAndDestroy();
        }
    }

    private void HandleStartButton()
    {
        if (_trainingData != null)
        {
            // 훈련 모드
            if (_trainingData.requiresStudentSelection)
                OpenStudentSelect();
            else
                ConfirmWithStudents(new List<Student>(StudentManager.Instance.Students));
        }
        else
        {
            // 범용/이벤트 모드: OnConfirmed 발행 후 닫기
            OnConfirmed?.Invoke();
            CloseAndDestroy();
        }
    }

    // 훈련 모드 내부 로직 (기존 코드 유지)

    private void OpenStudentSelect()
    {
        if (_studentSelectPrefab == null)
        {
            ConfirmWithStudents(new List<Student>(StudentManager.Instance.Students));
            return;
        }

        Close();

        StudentSelectPopup popup = Instantiate(_studentSelectPrefab, transform.parent);
        popup.SetMaxSelectCount(_trainingData != null ? _trainingData.maxSelectCount : 0);
        popup.Init();
        popup.Open();

        popup.OnSelectionConfirmed += (students) => ConfirmWithStudents(students);
        popup.OnCancelled += () => Open();
    }

    // 학생 확정 → 이벤트 발행 → 자기 파괴
    private void ConfirmWithStudents(List<Student> students)
    {
        string key = _trainingData != null ? _trainingData.trainingKey : "";
        OnTrainingConfirmed?.Invoke(key, students);
        CloseAndDestroy();
    }

    protected override void OnCloseButtonClicked()
    {
        CloseAndDestroy();
    }

    private void CloseAndDestroy()
    {
        OnTrainingConfirmed = null;
        OnConfirmed = null;
        OnCancelled = null;
        Close();
        Destroy(gameObject);
    }

    private static void SetText(TMP_Text target, string text)
    {
        if (target != null) target.text = text;
    }
}

// 범용 확인 팝업 데이터
public class ConfirmPopupData
{
    public string Title;
    public string Description;
    public string ConfirmLabel;
    public string CancelLabel;
    public Sprite PreviewSprite;

    public ConfirmPopupData(string title, string description,
        string confirmLabel = "확인", string cancelLabel = "포기",
        Sprite previewSprite = null)
    {
        Title = title;
        Description = description;
        ConfirmLabel = confirmLabel;
        CancelLabel = cancelLabel;
        PreviewSprite = previewSprite;
    }
}

// 이벤트 팝업 데이터 (TrainingConfirmPopup.SetupAsEvent 용)
// 학생 영입 안내 등 이벤트성 팝업에서 사용
// 이벤트 팝업 데이터 (TrainingConfirmPopup.SetupAsEvent 용)
// 학생 영입 안내 등 이벤트성 팝업에서 사용
public class EventPopupData
{
    public string Title;          // 제목 (null이면 숨김)
    public string Message;        // 본문 메시지
    public string ConfirmLabel;   // 확인 버튼 라벨 (기본 "확인")
    public string CancelLabel;    // 포기 버튼 라벨 (기본 "포기")
    public Sprite StudentSprite;  // 학생 이미지 (null이면 기본 이미지)

    public EventPopupData(string message, string title = null,
        string confirmLabel = "확인", string cancelLabel = "포기",
        Sprite studentSprite = null)
    {
        Title = title;
        Message = message;
        ConfirmLabel = confirmLabel;
        CancelLabel = cancelLabel;
        StudentSprite = studentSprite;
    }
}