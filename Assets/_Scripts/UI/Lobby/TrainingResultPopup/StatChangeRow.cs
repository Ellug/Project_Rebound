using TMPro;
using UnityEngine;

// 훈련 결과 팝업 내 스탯 변화 1행 (프리팹 컴포넌트)
// 요구사항: "원래 -> 변화" 라벨/필드 없이, 수치 -> 수치만 표시
public class StatChangeRow : MonoBehaviour
{
    [Header("스탯명")]
    [SerializeField] private TMP_Text _txtStatName;

    [Header("원래 수치")]
    [SerializeField] private TMP_Text _txtOriginal;

    [Header("변화 수치")]
    [SerializeField] private TMP_Text _txtChanged;

    // 공개 API
    public void Setup(string statName, int original, int changed)
    {
        if (_txtStatName != null) _txtStatName.text = statName;
        if (_txtOriginal != null) _txtOriginal.text = original.ToString();

        SetupChangedText(original, changed);
    }

    // 변화값 텍스트 및 색상 설정 (증가: 빨강 / 감소: 파랑 / 동일: 회색)
    private void SetupChangedText(int original, int changed)
    {
        if (_txtChanged == null) return;

        _txtChanged.text = changed.ToString();

        if (changed > original)
        {
            _txtChanged.color = new Color(0.90f, 0.25f, 0.25f); // 빨강
        }
        else if (changed < original)
        {
            _txtChanged.color = new Color(0.25f, 0.55f, 1.00f); // 파랑
        }
        else
        {
            _txtChanged.color = Color.gray;
        }
    }
}