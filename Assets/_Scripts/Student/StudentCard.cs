using UnityEngine;
using TMPro;

// UI 상 학생 카드 표시 및 드래그앤드롭
public class StudentCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _positionText;
    [SerializeField] private TMP_Text _mentalText;
    [SerializeField] private TMP_Text _shootText;
    [SerializeField] private TMP_Text _speedText;
    [SerializeField] private TMP_Text _jumpText;
    [SerializeField] private TMP_Text _staminaText;


    // 참조하는 학생 데이터
    private Student _studentData;

    public Student StudentData => _studentData;


    // 학생 데이터 설정 및 UI 갱신
    public void SetStudentData(Student student)
    {
        _studentData = student;
        UpdateUI();
    }

    // UI 갱신
    private void UpdateUI()
    {
        if (_studentData == null) return;

        _nameText.text = _studentData.studentName;
        _gradeText.text = $"{_studentData.grade}학년";
        _positionText.text = _studentData.positionName;
        _mentalText.text = $"멘탈: {_studentData.mental}";
        _shootText.text = $"슛: {_studentData.shoot}";
        _speedText.text = $"속도: {_studentData.speed}";
        _jumpText.text = $"점프: {_studentData.jump}";
        _staminaText.text = $"스태: {_studentData.stamina}";
    }

    // 외부에서 Student 데이터 변경 후 호출
    public void RefreshUI()
    {
        UpdateUI();
    }

    // 인스펙터에서 연결해도 되고 이벤트 시스템 이용해도 되고 일단 예시 코드
    public void OnClickStudentCard()
    {
        // TODO: 학생 상세 정보 UI 열기 등
        ShowStudentDetail();
    }

    private void ShowStudentDetail()
    {
        // 학생 상세 정보 표시 일단 디버그 로그만 (호출 방식 참고)
        Debug.Log($"=== {_studentData.studentName} 상세 정보 ===");
        Debug.Log($"학년: {_studentData.grade}, 포지션: {_studentData.positionName}");
        Debug.Log($"신체: 키 {_studentData.height}cm, 몸무게 {_studentData.weight}kg");
        Debug.Log($"스탯: 멘탈 {_studentData.mental}, 슛 {_studentData.shoot}, " +
                  $"속도 {_studentData.speed}, 점프 {_studentData.jump}, 스태미너 {_studentData.stamina}");
        Debug.Log($"잠재력: Tier {_studentData.potential_tier} - {_studentData.potential}");
        Debug.Log($"컨디션: {_studentData.condition}, 신뢰도: {_studentData.trust}");
    }
}
