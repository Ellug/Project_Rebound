using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


// 훈련 결과 팝업 내 학생 1명 행 (Image 3)
public class TrainingResultStudentRow : MonoBehaviour
{
    [Header("이름 + 컨디션 행")]
    [SerializeField] private TMP_Text _txtName;
    // Slider 대신 Image(Filled) 사용 — 클릭/드래그 입력 없음
    // ConditionGauge (부모) : 배경 이미지
    // Fill (자식)           : Filled 타입, fillAmount로 게이지 표시, 색상 제어
    [SerializeField] private Image _conditionGaugeBg;   // ConditionGauge 오브젝트
    [SerializeField] private Image _conditionGaugeFill; // Fill 오브젝트 — Image Type: Filled, Fill Method: Horizontal
    [SerializeField] private TMP_Text _txtConditionDelta;   // 우측 컨디션 가감치 (+N / -N)

    [Header("스탯 변화 행")]
    [SerializeField] private Transform _statRowContainer;   // Vertical Layout Group 부모
    [SerializeField] private StatChangeRow _statRowPrefab;  // 없으면 폴백 텍스트 사용

    [Header("폴백 텍스트 (프리팹 없을 때)")]
    [SerializeField] private TMP_Text _txtFallback;

    private readonly List<StatChangeRow> _spawnedRows = new List<StatChangeRow>();

    // 공개 API

    public void Setup(Student before, Student after)
    {
        SetupNameConditionRow(before, after);
        SetupStatChangeRows(before, after);
    }

    // 이름 + 컨디션 행

    private void SetupNameConditionRow(Student before, Student after)
    {
        if (_txtName != null)
            _txtName.text = after.studentName;

        RefreshConditionBar(after);
        RefreshConditionDelta(before, after);
    }

    // 컨디션 게이지 fillAmount와 색상 갱신
    // 컨디션 게이지 갱신
    // _conditionGaugeFill(자식 Fill)의 fillAmount와 색상만 제어
    // _conditionGaugeBg(부모)는 고정 배경이므로 별도 조작 불필요
    private void RefreshConditionBar(Student after)
    {
        if (_conditionGaugeFill == null) return;

        int condMax = Mathf.Max(after.mental + 20, after.condition, 1);
        _conditionGaugeFill.fillAmount = (float)after.condition / condMax;
    }

    // 컨디션 가감치 텍스트 표시 (+N은 파랑, -N은 빨강, 0이면 숨김)
    private void RefreshConditionDelta(Student before, Student after)
    {
        if (_txtConditionDelta == null) return;

        int delta = after.condition - before.condition;

        if (delta == 0)
        {
            _txtConditionDelta.gameObject.SetActive(false);
            return;
        }

        _txtConditionDelta.gameObject.SetActive(true);
        _txtConditionDelta.text = delta > 0 ? $"+{delta}" : delta.ToString();
        _txtConditionDelta.color = delta > 0
            ? new Color(0.25f, 0.55f, 1.00f)
            : new Color(0.90f, 0.25f, 0.25f);
    }

    // 스탯 변화 행
    // 변화한 스탯 목록을 수집하고 각 행을 동적으로 생성
    private void SetupStatChangeRows(Student before, Student after)
    {
        ClearStatRows();

        List<(string name, int original, int changed)> changes = CollectStatChanges(before, after);

        if (changes.Count == 0)
        {
            ShowFallback("변화 없음");
            return;
        }

        if (_statRowPrefab != null && _statRowContainer != null)
        {
            SafeSetActive(_txtFallback?.gameObject, false);
            SpawnStatRows(changes);
        }
        else
        {
            ShowFallback(BuildFallbackText(changes));
        }
    }

    // 변화한 스탯을 StatChangeRow 프리팹으로 하나씩 생성
    private void SpawnStatRows(List<(string name, int original, int changed)> changes)
    {
        foreach (var (statName, original, changed) in changes)
        {
            StatChangeRow row = Instantiate(_statRowPrefab, _statRowContainer);
            row.Setup(statName, original, changed);
            row.gameObject.SetActive(true);
            _spawnedRows.Add(row);
        }
    }

    // 스탯 변화 수집 (StudentStatTable.csv stat_id 01~05 순서)

    private static List<(string name, int original, int changed)> CollectStatChanges(
        Student before, Student after)
    {
        var list = new List<(string, int, int)>(5);

        if (after.mental != before.mental) list.Add(("멘탈", before.mental, after.mental));
        if (after.shoot != before.shoot) list.Add(("슈팅", before.shoot, after.shoot));
        if (after.speed != before.speed) list.Add(("속도", before.speed, after.speed));
        if (after.jump != before.jump) list.Add(("점프력", before.jump, after.jump));
        if (after.stamina != before.stamina) list.Add(("지구력", before.stamina, after.stamina));

        return list;
    }

    // 폴백

    private void ShowFallback(string message)
    {
        if (_txtFallback == null) return;
        _txtFallback.gameObject.SetActive(true);
        _txtFallback.text = message;
    }

    // 프리팹 없을 때 스탯 변화를 한 줄 텍스트로 요약
    private static string BuildFallbackText(List<(string name, int original, int changed)> changes)
    {
        var parts = new System.Text.StringBuilder();
        foreach (var (name, original, changed) in changes)
        {
            int delta = changed - original;
            string sign = delta > 0 ? "+" : "";
            parts.Append($"{name} {original}→{changed}({sign}{delta})  ");
        }
        return parts.ToString().TrimEnd();
    }

    // 정리

    private void ClearStatRows()
    {
        foreach (StatChangeRow row in _spawnedRows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        _spawnedRows.Clear();
    }

    private static void SafeSetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}