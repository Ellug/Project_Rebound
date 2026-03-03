using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 훈련 결과 데이터 (학생 1명분)
[Serializable]
public class TrainingResult
{
    public Student before;  // 훈련 전 스냅샷
    public Student after;   // 훈련 후 (현재 상태)
}

// 훈련 결과 팝업
// Row 프리팹은 학생 카드 프리팹 완성 후 제작 예정
// 프리팹이 없으면 콘솔에 결과 출력 후 확인 버튼만 표시
public class TrainingResultPopup : UIPopup
{
    [Header("Content")]
    [SerializeField] private TMP_Text _txtTrainingName;                 // 훈련 이름 표시
    [SerializeField] private Transform _rowContainer;                   // 학생 행 부모 (Vertical Layout Group)
    [SerializeField] private TrainingResultStudentRow _rowPrefab;       // 학생 행 프리팹 (없어도 동작)

    [Header("Buttons")]
    [SerializeField] private Button _btnConfirm;                        // 확인 버튼

    private readonly List<TrainingResultStudentRow> _spawnedRows = new List<TrainingResultStudentRow>(); // 생성된 Row 목록

    public event Action OnConfirm; // 확인 버튼 클릭 이벤트

    public override void Init()
    {
        base.Init();

        // 확인 버튼 이벤트 바인딩
        if (_btnConfirm != null)
        {
            _btnConfirm.onClick.RemoveAllListeners();
            _btnConfirm.onClick.AddListener(HandleConfirm);
        }
    }

    // 외부에서 결과 데이터 세팅
    public void Setup(string trainingName, List<TrainingResult> results)
    {
        if (_txtTrainingName != null)
            _txtTrainingName.text = trainingName;

        ClearRows();

        // Row 프리팹이 있으면 UI 생성
        if (_rowPrefab != null && _rowContainer != null)
        {
            foreach (TrainingResult result in results)
            {
                TrainingResultStudentRow row = Instantiate(_rowPrefab, _rowContainer);
                row.Setup(result.before, result.after);
                row.gameObject.SetActive(true);
                _spawnedRows.Add(row);
            }
        }

        // 프리팹 유무와 관계없이 콘솔에 결과 로그 출력
        foreach (TrainingResult result in results)
        {
            LogResult(result);
        }
    }

    // 콘솔에 결과 출력 (디버그용)
    private void LogResult(TrainingResult result)
    {
        Student b = result.before;
        Student a = result.after;

        string log = $"[TrainingResult] {a.studentName}: ";

        if (a.mental != b.mental) log += $"멘탈 {b.mental}→{a.mental} ";
        if (a.shoot != b.shoot) log += $"슈팅 {b.shoot}→{a.shoot} ";
        if (a.speed != b.speed) log += $"속도 {b.speed}→{a.speed} ";
        if (a.jump != b.jump) log += $"점프 {b.jump}→{a.jump} ";
        if (a.stamina != b.stamina) log += $"스태미너 {b.stamina}→{a.stamina} ";

        Debug.Log(log);
    }

    // 확인 버튼 처리
    private void HandleConfirm()
    {
        OnConfirm?.Invoke();   // 외부에 흐름 완료 알림
        CloseAndCleanup();
    }

    protected override void OnCloseButtonClicked()
    {
        CloseAndCleanup();
    }

    // 팝업 종료 및 내부 정리
    private void CloseAndCleanup()
    {
        OnConfirm = null;   // 이벤트 초기화
        ClearRows();
        Close();
    }

    // 생성된 Row 제거
    private void ClearRows()
    {
        foreach (TrainingResultStudentRow row in _spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        _spawnedRows.Clear();
    }
}