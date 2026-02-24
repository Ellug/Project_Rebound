using TMPro;
using UnityEngine;

// 학생 스탯 1행 표시용 UI 컴포넌트
public class SelectStudentStatRow : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtStatLabel;   // 스탯 이름
    [SerializeField] private TMP_Text _txtStatValue;   // 스탯 값

    // 스탯 데이터 설정
    public void Setup(string label, int value)
    {
        if (_txtStatLabel != null)
        {
            _txtStatLabel.text = label;
            _txtStatLabel.raycastTarget = false;      // 클릭 방해 방지
        }

        if (_txtStatValue != null)
        {
            _txtStatValue.text = value.ToString();
            _txtStatValue.raycastTarget = false;      // 클릭 방해 방지
        }
    }
}