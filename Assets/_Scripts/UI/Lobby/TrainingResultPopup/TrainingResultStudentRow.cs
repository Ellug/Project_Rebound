using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 훈련 결과 팝업 내 학생 1줄 (이름 + 스탯 변화)
// 프리팹은 학생 카드 프리팹 완성 후 제작 예정
// 현재는 인터페이스만 정의
public class TrainingResultStudentRow : MonoBehaviour
{
    [Header("Student Info")]
    [SerializeField] private TMP_Text _txtName;          // 학생 이름

    [Header("Stat Row (프리팹화 예정)")]
    [SerializeField] private TMP_Text _txtStatName;      // 스탯 이름 ("슈팅")
    [SerializeField] private TMP_Text _txtOriginal;      // 원래 스탯 ("50")
    [SerializeField] private TMP_Text _txtChanged;       // 변화 스탯 ("52")
    [SerializeField] private Slider _statBar;            // 스탯 바 (선택사항)

    public void Setup(Student before, Student after)
    {
        if (_txtName != null)
            _txtName.text = after.studentName;
    }
}