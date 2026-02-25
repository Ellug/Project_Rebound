using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 확인/취소 + 선택적 학생 선택 기능을 가지는 공용 확인 팝업
public class ConfirmPopup : UIPopup
{
    [Header("UI")]
    [SerializeField] private TMP_Text _txtName;       // 제목
    [SerializeField] private TMP_Text _txtSub;        // 서브 메시지
    [SerializeField] private TMP_Text _txtMessage;    // 본문 메시지
    [SerializeField] private Image _imgPreview;       // 미리보기 이미지

    [Header("Buttons")]
    [SerializeField] private Button _btnSecondary;    // 취소 버튼
    [SerializeField] private TMP_Text _txtSecondary;
    [SerializeField] private Button _btnPrimary;      // 확인 버튼
    [SerializeField] private TMP_Text _txtPrimary;

    [Header("Student Select")]
    [SerializeField] private StudentSelectPopup _studentSelectPrefab; // 학생 선택 팝업 프리팹

    private ConfirmPopupRequest _request; // 외부에서 전달받는 설정 데이터

    public override void Init()
    {
        base.Init();

        // 버튼 이벤트 바인딩
        if (_btnPrimary != null)
        {
            _btnPrimary.onClick.RemoveAllListeners();
            _btnPrimary.onClick.AddListener(HandlePrimaryClicked);
        }

        if (_btnSecondary != null)
        {
            _btnSecondary.onClick.RemoveAllListeners();
            _btnSecondary.onClick.AddListener(HandleSecondaryClicked);
        }
    }

    // 외부에서 팝업 설정 적용
    public void Setup(ConfirmPopupRequest request)
    {
        _request = request;

        ApplyTexts(request);
        ApplyPreview(request);
        ApplyButtons(request);
    }

    // 텍스트 영역 표시/숨김 처리
    private void ApplyTexts(ConfirmPopupRequest request)
    {
        if (_txtName != null)
        {
            bool hasTitle = !string.IsNullOrEmpty(request.Title);
            _txtName.gameObject.SetActive(hasTitle);
            if (hasTitle) _txtName.text = request.Title;
        }

        if (_txtSub != null)
        {
            bool hasSub = !string.IsNullOrEmpty(request.SubMessage);
            _txtSub.gameObject.SetActive(hasSub);
            if (hasSub) _txtSub.text = request.SubMessage;
        }

        if (_txtMessage != null)
        {
            bool hasMsg = !string.IsNullOrEmpty(request.Message);
            _txtMessage.gameObject.SetActive(hasMsg);
            if (hasMsg) _txtMessage.text = request.Message;
        }
    }

    // 미리보기 이미지 설정
    private void ApplyPreview(ConfirmPopupRequest request)
    {
        if (_imgPreview == null) return;

        bool hasSprite = request.PreviewSprite != null;
        _imgPreview.gameObject.SetActive(hasSprite);

        if (hasSprite)
        {
            _imgPreview.sprite = request.PreviewSprite;
            _imgPreview.preserveAspect = true;
        }
    }

    // 버튼 라벨 및 표시 여부 설정
    private void ApplyButtons(ConfirmPopupRequest request)
    {
        if (_txtPrimary != null)
            _txtPrimary.text = string.IsNullOrEmpty(request.PrimaryLabel)
                ? "확인"
                : request.PrimaryLabel;

        if (_btnPrimary != null)
            _btnPrimary.interactable = request.PrimaryInteractable;

        bool hasSecondary = !string.IsNullOrEmpty(request.SecondaryLabel);

        if (_btnSecondary != null)
            _btnSecondary.gameObject.SetActive(hasSecondary);

        if (hasSecondary && _txtSecondary != null)
            _txtSecondary.text = request.SecondaryLabel;
    }

    // 확인 버튼 클릭
    private void HandlePrimaryClicked()
    {
        if (_request == null)
        {
            CloseSelf();
            return;
        }

        // 학생 선택이 필요한 경우
        if (_request.RequiresStudentSelection)
        {
            OpenStudentSelect();
            return;
        }

        _request.PrimaryAction?.Invoke();

        if (_request.AutoCloseOnPrimary)
            CloseSelf();
    }

    // 취소 버튼 클릭
    private void HandleSecondaryClicked()
    {
        if (_request == null)
        {
            CloseSelf();
            return;
        }

        _request.SecondaryAction?.Invoke();

        if (_request.AutoCloseOnSecondary)
            CloseSelf();
    }

    // 학생 선택 팝업 열기
    private void OpenStudentSelect()
    {
        if (_studentSelectPrefab == null)
        {
            // 프리팹 없으면 전체 학생 반환
            List<Student> fallback = StudentManager.Instance != null
                ? new List<Student>(StudentManager.Instance.Students)
                : new List<Student>();

            _request.OnStudentsSelected?.Invoke(fallback);

            if (_request.AutoCloseOnPrimary)
                CloseSelf();

            return;
        }

        // 현재 팝업 닫고 학생 선택 팝업 생성
        Close();

        StudentSelectPopup popup = Instantiate(_studentSelectPrefab, transform.parent);
        popup.SetMaxSelectCount(_request.MaxSelectCount);
        popup.Init();
        popup.Open();

        popup.OnSelectionConfirmed += HandleStudentsSelected;
        popup.OnCancelled += HandleStudentSelectCancelled;
    }

    // 학생 선택 완료 콜백
    private void HandleStudentsSelected(List<Student> students)
    {
        _request.OnStudentsSelected?.Invoke(students);

        if (_request.AutoCloseOnPrimary)
            CloseSelf();
    }

    // 학생 선택 취소 시 다시 열기
    private void HandleStudentSelectCancelled()
    {
        Open();
    }

    protected override void OnCloseButtonClicked()
    {
        CloseSelf();
    }

    // 안전한 닫기 처리 (UIManager 우선)
    private void CloseSelf()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Close(this);
            return;
        }

        Close();
        Destroy(gameObject);
    }
}