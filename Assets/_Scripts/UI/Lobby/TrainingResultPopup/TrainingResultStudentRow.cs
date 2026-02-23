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

    [Header("Condition Preview Bar")]
    [Tooltip("밑에 깔리는 색상 바 (감소 시 빨강, 증가 시 파랑)")]
    [SerializeField] private Image _statBarBottom;
    [Tooltip("위에 덮는 기본 회색 바")]
    [SerializeField] private Image _statBarTop;


    [Header("Stat Row (프리팹화 예정)")]
    [SerializeField] private TMP_Text _txtStatName;      // 스탯 이름 ("슈팅")
    [SerializeField] private TMP_Text _txtOriginal;      // 원래 스탯 ("50")
    [SerializeField] private TMP_Text _txtChanged;       // 변화 스탯 ("52")

    public void Setup(Student before, Student after)
    {
        if (_txtName != null)
            _txtName.text = after.studentName;

        // 컨디션 바 UI 갱신 로직 (증감에 따라 동적 처리)
        if (_statBarBottom != null && _statBarTop != null)
        {
            float maxCondition = 100f;

            float beforeRatio = Mathf.Clamp01(before.condition / maxCondition);
            float afterRatio = Mathf.Clamp01(after.condition / maxCondition);

            // 1. 컨디션이 깎였을 때
            if (afterRatio < beforeRatio)
            {
                // 밑장은 깎이기 전 원래 길이만큼 빨간색으로 채움
                _statBarBottom.fillAmount = beforeRatio;
                _statBarBottom.color = Color.red;

                // 윗장은 깎인 후 길이만큼 회색으로 덮어버림
                _statBarTop.fillAmount = afterRatio;
                _statBarTop.color = Color.gray;
            }
            // 2. 컨디션이 올랐을 때
            else
            {
                // 밑장은 회복된 후의 최종 길이만큼 파란색으로 채움
                _statBarBottom.fillAmount = afterRatio;
                _statBarBottom.color = Color.blue; 

                // 윗장은 회복 전 길이만큼 회색으로 덮어버림
                _statBarTop.fillAmount = beforeRatio;
                _statBarTop.color = Color.gray;
            }
        }

        // 텍스트 업데이트
        if (_txtStatName != null) 
            _txtStatName.text = "컨디션";
        if (_txtOriginal != null)
            _txtOriginal.text = before.condition.ToString();
        if (_txtChanged != null) 
            _txtChanged.text = after.condition.ToString();
    }
}
