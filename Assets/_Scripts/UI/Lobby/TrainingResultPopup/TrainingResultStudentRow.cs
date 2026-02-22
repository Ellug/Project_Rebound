using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrainingResultStudentRow : MonoBehaviour
{
    [Header("이름 + 컨디션 행")]
    [SerializeField] private TMP_Text _txtName;
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


    private void RefreshConditionBar(Student after)
    {
        if (_conditionGaugeFill == null) return;

        int condMax = Mathf.Max(after.mental + 20, after.condition, 1);
        _conditionGaugeFill.fillAmount = (float)after.condition / condMax;
    }

    // 컨디션 가감치 텍스트 표시 (+N은 빨강, -N은 파랑, 0이면 숨김)
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

        List<StatChange> changes = CollectStatChanges(before, after);

        if (changes.Count == 0)
        {
            ShowFallback("변화 없음");
            return;
        }

        if (_statRowPrefab != null && _statRowContainer != null)
        {
            SafeSetActive(_txtFallback?.gameObject, false);

            foreach (StatChange c in changes)
            {
                StatChangeRow row = Instantiate(_statRowPrefab, _statRowContainer);

                if (c.IsFloat)
                    row.Setup(c.Name, c.OriginalF, c.ChangedF, c.Decimals);
                else
                    row.Setup(c.Name, c.OriginalI, c.ChangedI);

                row.gameObject.SetActive(true);
                _spawnedRows.Add(row);
            }
        }
        else
        {
            ShowFallback(BuildFallbackText(changes));
        }
    }


    private struct StatChange
    {
        public string Name;

        public bool IsFloat;
        public int Decimals;

        public int OriginalI;
        public int ChangedI;

        public float OriginalF;
        public float ChangedF;
    }

    private static List<StatChange> CollectStatChanges(Student before, Student after)
    {
        const float EPS = 0.0001f;

        var list = new List<StatChange>(8);

        // 컨디션도 변화 행에 포함 (휴식/단체에서 체감됨)
        if (after.condition != before.condition)
        {
            list.Add(new StatChange
            {
                Name = "컨디션",
                IsFloat = false,
                OriginalI = before.condition,
                ChangedI = after.condition
            });
        }

        // mental은 int로 가정
        if (after.mental != before.mental)
        {
            list.Add(new StatChange
            {
                Name = "멘탈",
                IsFloat = false,
                OriginalI = before.mental,
                ChangedI = after.mental
            });
        }

        // 아래는 Student의 타입에 따라 수정해야 하는데,
        // "프로젝트에서 shoot/speed/jump/stamina가 int인 경우" 그대로 동작하고,
        // "float인 경우"에도 동작하도록 float 경로로 작성.

        AddFloatOrInt(list, "슈팅", before.shoot, after.shoot, EPS);
        AddFloatOrInt(list, "속도", before.speed, after.speed, EPS);
        AddFloatOrInt(list, "점프력", before.jump, after.jump, EPS);
        AddFloatOrInt(list, "지구력", before.stamina, after.stamina, EPS);

        return list;
    }

    // Student.shoot 등이 int면 자동으로 int 연산 경로로 컴파일되고,
    // float면 float 경로로 컴파일되도록 오버로드 제공.
    private static void AddFloatOrInt(List<StatChange> list, string name, int before, int after, float eps)
    {
        if (after == before) return;

        list.Add(new StatChange
        {
            Name = name,
            IsFloat = false,
            OriginalI = before,
            ChangedI = after
        });
    }

    private static void AddFloatOrInt(List<StatChange> list, string name, float before, float after, float eps)
    {
        if (Mathf.Abs(after - before) <= eps) return;

        list.Add(new StatChange
        {
            Name = name,
            IsFloat = true,
            Decimals = 0,
            OriginalF = before,
            ChangedF = after
        });
    }

    private void ShowFallback(string message)
    {
        if (_txtFallback == null) return;
        _txtFallback.gameObject.SetActive(true);
        _txtFallback.text = message;
    }

    private static string BuildFallbackText(List<StatChange> changes)
    {
        var sb = new System.Text.StringBuilder();

        foreach (StatChange c in changes)
        {
            if (c.IsFloat)
            {
                float delta = c.ChangedF - c.OriginalF;
                string sign = delta > 0 ? "+" : "";
                sb.Append($"{c.Name} {Mathf.RoundToInt(c.OriginalF)}→{Mathf.RoundToInt(c.ChangedF)}({sign}{Mathf.RoundToInt(delta)})  ");
            }
            else
            {
                int delta = c.ChangedI - c.OriginalI;
                string sign = delta > 0 ? "+" : "";
                sb.Append($"{c.Name} {c.OriginalI}→{c.ChangedI}({sign}{delta})  ");
            }
        }

        return sb.ToString().TrimEnd();
    }

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