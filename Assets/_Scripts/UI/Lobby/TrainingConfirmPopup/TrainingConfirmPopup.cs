using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 훈련 확인 팝업
// requiresStudentSelection에 따라 학생 선택 팝업 열거나 전체 학생으로 바로 확정
// 결과: OnTrainingConfirmed(trainingKey, 학생 목록) 전달
public class TrainingConfirmPopup : UIPopup
{
    [Header("Preview")]
    [SerializeField] private Image _imgPreview;
    [SerializeField] private Sprite _defaultPreview;

    [Header("Texts")]
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtConditionModifier;
    [SerializeField] private TMP_Text _txtDesc;

    [Header("Buttons")]
    [SerializeField] private Button _btnCancel;
    [SerializeField] private Button _btnStart;

    [Header("Student Select")]
    [SerializeField] private StudentSelectPopup _studentSelectPrefab;

    private TrainingButtonData _trainingData;

    // (trainingKey, 선택된 학생 목록) 전달
    public event Action<string, List<Student>> OnTrainingConfirmed;

    public override void Init()
    {
        base.Init();

        if (_btnCancel != null)
        {
            _btnCancel.onClick.RemoveAllListeners();
            _btnCancel.onClick.AddListener(CloseAndDestroy);
        }

        if (_btnStart != null)
        {
            _btnStart.onClick.RemoveAllListeners();
            _btnStart.onClick.AddListener(HandleStartButton);
        }
    }

    public void Setup(TrainingButtonData data)
    {
        _trainingData = data;

        if (_txtName != null)
            _txtName.text = data.trainingName ?? "";

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

    private void HandleStartButton()
    {
        if (_trainingData == null) { CloseAndDestroy(); return; }

        if (_trainingData.requiresStudentSelection)
            OpenStudentSelect();
        else
            ConfirmWithStudents(new List<Student>(StudentManager.Instance.Students));
    }

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
        Close();
        Destroy(gameObject);
    }
}