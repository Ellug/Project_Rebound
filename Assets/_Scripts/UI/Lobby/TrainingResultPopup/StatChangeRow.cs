using TMPro;
using UnityEngine;


// 훈련 결과 팝업 내 스탯 변화 1행 (프리팹 컴포넌트)
public class StatChangeRow : MonoBehaviour
{
    [Header("스탯명")]
    [SerializeField] private TMP_Text _txtStatName;

    [Header("원래 스탯")]
    [SerializeField] private TMP_Text _txtOriginal;
    [SerializeField] private TMP_Text _lblOriginal;     // "원래 스탯" 고정 라벨

    [Header("변화 스탯")]
    [SerializeField] private TMP_Text _txtChanged;
    [SerializeField] private TMP_Text _lblChanged;      // "변화 스탯" 고정 라벨

    [Header("화살표 아이콘 (선택)")]
    [SerializeField] private GameObject _arrowIcon;

    // 공개 API

    /// <summary>
    /// 스탯 변화 행 데이터 세팅
    /// statName: "슈팅" / "속도" 등 (StudentStatTable.csv stat_name)
    /// </summary>
    public void Setup(string statName, int original, int changed)
    {
        if (_txtStatName != null) _txtStatName.text = statName;
        if (_txtOriginal != null) _txtOriginal.text = original.ToString();
        if (_lblOriginal != null) _lblOriginal.text = "원래 스탯";
        if (_lblChanged != null) _lblChanged.text = "변화 스탯";

        if (_arrowIcon != null) _arrowIcon.SetActive(true);

        SetupChangedText(original, changed);
    }

    // 변화값 텍스트 및 색상 설정 (증가: 파랑 / 감소: 빨강 / 동일: 흰색)
    private void SetupChangedText(int original, int changed)
    {
        if (_txtChanged == null) return;

        _txtChanged.text = changed.ToString();
        _txtChanged.color = changed > original
            ? new Color(0.25f, 0.55f, 1.00f)
            : changed < original
                ? new Color(0.90f, 0.25f, 0.25f)
                : Color.white;
    }
}