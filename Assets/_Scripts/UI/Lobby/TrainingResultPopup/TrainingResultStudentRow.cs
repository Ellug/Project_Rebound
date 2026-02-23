using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainingResultStudentRow : MonoBehaviour
{
    [Header("이름 + 컨디션 행")]
    [SerializeField] private TMP_Text _txtName;

    [SerializeField] private Image _conditionGaugeFill; // 회색(기존치)
    [SerializeField] private Image _deltaFill;          // 증감치(빨강/파랑)
    [SerializeField] private TMP_Text _txtConditionDelta;

    [Header("스탯 변화 행")]
    [SerializeField] private Transform _statRowContainer;
    [SerializeField] private StatChangeRow _statRowPrefab;

    [Header("폴백 텍스트")]
    [SerializeField] private TMP_Text _txtFallback;

    private readonly List<StatChangeRow> _spawnedRows = new();

    public void Setup(Student before, Student after)
    {
        SetupNameAndCondition(before, after);
        SetupStatChangeRows(before, after);
    }

    private void SetupNameAndCondition(Student before, Student after)
    {
        if (_txtName != null)
            _txtName.text = after.studentName;

        RefreshConditionBar3Layer(before, after);
        RefreshConditionDelta(before, after);
    }

    private void RefreshConditionBar3Layer(Student before, Student after)
    {
        if (_conditionGaugeFill == null || _deltaFill == null)
            return;

        int beforeValue = Mathf.Max(0, before.condition);
        int afterValue = Mathf.Max(0, after.condition);

        int condMax = GetConditionMax(beforeValue, afterValue);

        float before01 = Mathf.Clamp01((float)beforeValue / condMax);
        float after01 = Mathf.Clamp01((float)afterValue / condMax);

        int delta = afterValue - beforeValue;

        if (delta == 0)
        {
            _deltaFill.gameObject.SetActive(false);

            _conditionGaugeFill.fillAmount = after01;
            _conditionGaugeFill.color = Color.gray;
            return;
        }

        _deltaFill.gameObject.SetActive(true);

        // 1. 감소 (훈련): 깎인 만큼 빨간색 꼬리 남기기
        if (delta < 0)
        {
            // 밑장(_deltaFill)에 깎이기 전 원래 길이를 빨간색으로 채움
            _deltaFill.color = new Color(0.90f, 0.25f, 0.25f); // 빨강
            _deltaFill.fillAmount = before01;

            // 윗장(_conditionGaugeFill)으로 깎인 후 길이만큼 회색으로 덮음
            _conditionGaugeFill.color = Color.gray;
            _conditionGaugeFill.fillAmount = after01;
        }
        // 2. 증가 (휴식): 늘어난 만큼 파란색 꼬리 보여주기
        else
        {
            // 밑장(_deltaFill)에 회복된 후의 최종 길이를 파란색으로 채움
            _deltaFill.color = new Color(0.25f, 0.55f, 1.00f); // 파랑
            _deltaFill.fillAmount = after01;

            // 윗장(_conditionGaugeFill)으로 회복 전 길이만큼 회색으로 덮음
            _conditionGaugeFill.color = Color.gray;
            _conditionGaugeFill.fillAmount = before01;
        }
    }

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

        // 텍스트 색상도 감소(-)일 때 빨강, 증가(+)일 때 파랑으로 맞춤
        _txtConditionDelta.color = delta < 0
            ? new Color(0.90f, 0.25f, 0.25f)   // 빨강
            : new Color(0.25f, 0.55f, 1.00f);  // 파랑
    }


    private static int GetConditionMax(int beforeValue, int afterValue)
    {
        int v = Mathf.Max(beforeValue, afterValue, 100);
        int rounded = ((v + 9) / 10) * 10;   // 10단위 올림
        return Mathf.Max(rounded, 1);
    }

  
    private void SetupStatChangeRows(Student before, Student after)
    {
        ClearStatRows();

        var changes = CollectStatChanges(before, after);

        if (changes.Count == 0)
        {
            ShowFallback("변화 없음");
            return;
        }

        if (_statRowPrefab != null && _statRowContainer != null)
        {
            if (_txtFallback != null) _txtFallback.gameObject.SetActive(false);

            foreach (var (statName, original, changed) in changes)
            {
                StatChangeRow row = Instantiate(_statRowPrefab, _statRowContainer);
                row.Setup(statName, original, changed);
                row.gameObject.SetActive(true);
                _spawnedRows.Add(row);
            }
        }
        else
        {
            ShowFallback(BuildFallbackText(changes));
        }
    }

    private static List<(string name, int original, int changed)> CollectStatChanges(Student before, Student after)
    {
        var list = new List<(string, int, int)>(5);

        if (after.mental != before.mental) list.Add(("멘탈", before.mental, after.mental));
        if (after.shoot != before.shoot) list.Add(("슈팅", before.shoot, after.shoot));
        if (after.speed != before.speed) list.Add(("속도", before.speed, after.speed));
        if (after.jump != before.jump) list.Add(("점프력", before.jump, after.jump));
        if (after.stamina != before.stamina) list.Add(("지구력", before.stamina, after.stamina));

        return list;
    }

    private void ShowFallback(string message)
    {
        if (_txtFallback == null) return;
        _txtFallback.gameObject.SetActive(true);
        _txtFallback.text = message;
    }

    private static string BuildFallbackText(List<(string name, int original, int changed)> changes)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (name, original, changed) in changes)
        {
            int d = changed - original;
            string sign = d > 0 ? "+" : "";
            sb.Append($"{name} {original}→{changed}({sign}{d})  ");
        }
        return sb.ToString().TrimEnd();
    }

    private void ClearStatRows()
    {
        foreach (var row in _spawnedRows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        _spawnedRows.Clear();
    }
}