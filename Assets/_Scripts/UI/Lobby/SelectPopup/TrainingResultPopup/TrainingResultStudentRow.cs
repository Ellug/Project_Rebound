using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 훈련 결과 학생 1명 표시용 Row
// 컨디션 변화 + 스탯 변화 표시
public class TrainingResultStudentRow : MonoBehaviour
{
    [Header("이름 + 컨디션 행")]
    [SerializeField] private TMP_Text _txtName;

    [SerializeField] private Image _conditionGaugeFill; // 검은색(기존치)
    [SerializeField] private Image _deltaFill;          // 증감치(빨강/파랑)
    [SerializeField] private TMP_Text _txtConditionDelta;

    [Header("스탯 변화 행")]
    [SerializeField] private Transform _statRowContainer;      // 스탯 변화 행 부모
    [SerializeField] private StatChangeRow _statRowPrefab;

    private readonly List<StatChangeRow> _spawnedRows = new(); // 동적 생성된 스탯 행

    // 외부에서 before/after 전달받아 전체 UI 구성
    public void Setup(Student before, Student after)
    {
        SetupNameAndCondition(before, after);
        SetupStatChangeRows(before, after);
    }

    // 이름 + 컨디션 영역 구성
    private void SetupNameAndCondition(Student before, Student after)
    {
        if (_txtName != null)
            _txtName.text = after.studentName;

        RefreshConditionBar3Layer(before, after);
        RefreshConditionDelta(before, after);
    }

    // 3레이어 방식 컨디션 게이지 표현
    private void RefreshConditionBar3Layer(Student before, Student after)
    {
        if (_conditionGaugeFill == null || _deltaFill == null)
            return;

        int beforeValue = Student.ClampCondition(before.condition);
        int afterValue = Student.ClampCondition(after.condition);
        int condMax = Student.ConditionMax;

        float before01 = Mathf.Clamp01((float)beforeValue / condMax);
        float after01 = Mathf.Clamp01((float)afterValue / condMax);

        int delta = afterValue - beforeValue;

        if (delta == 0)
        {
            _deltaFill.gameObject.SetActive(false);

            _conditionGaugeFill.fillAmount = after01;
            _conditionGaugeFill.color = Color.black;
            return;
        }

        _deltaFill.gameObject.SetActive(true);

        // 1. 감소 (훈련): 깎인 만큼 빨간색 꼬리 남기기
        if (delta < 0)
        {
            // 밑장(_deltaFill)에 깎이기 전 원래 길이를 빨간색으로 채움
            _deltaFill.color = new Color(0.90f, 0.25f, 0.25f); // 빨강
            _deltaFill.fillAmount = before01;

            // 윗장(_conditionGaugeFill)으로 깎인 후 길이만큼 검은색으로 덮음
            _conditionGaugeFill.color = Color.black;
            _conditionGaugeFill.fillAmount = after01;
        }
        // 2. 증가 (휴식): 늘어난 만큼 파란색 꼬리 보여주기
        else
        {
            // 밑장(_deltaFill)에 회복된 후의 최종 길이를 파란색으로 채움
            _deltaFill.color = new Color(0.25f, 0.55f, 1.00f); // 파랑
            _deltaFill.fillAmount = after01;

            // 윗장(_conditionGaugeFill)으로 회복 전 길이만큼 검은색으로 덮음
            _conditionGaugeFill.color = Color.black;
            _conditionGaugeFill.fillAmount = before01;
        }
    }

    // 컨디션 수치 증감 텍스트 표시
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

    // 스탯 변화 행 구성
    private void SetupStatChangeRows(Student before, Student after)
    {
        ClearStatRows();

        if (_statRowPrefab == null || _statRowContainer == null)
            return;

        var changes = CollectStatChanges(before, after);

        // 변경된 스탯만 동적 생성
        foreach (var (statName, original, changed) in changes)
        {
            StatChangeRow row = Instantiate(_statRowPrefab, _statRowContainer);
            row.Setup(statName, original, changed);
            row.gameObject.SetActive(true);
            _spawnedRows.Add(row);
        }
    }

    // 변경된 스탯 목록 수집
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

    // 생성된 스탯 행 정리
    private void ClearStatRows()
    {
        foreach (var row in _spawnedRows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        _spawnedRows.Clear();
    }
}
