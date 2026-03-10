using System.Collections.Generic;
using UnityEngine;

// 상시 이벤트 효과 적용 / 해제 / 매 턴 틱 처리
// 외부 연동 포인트
// AlwaysEventManager: 활성화 시 ApplyEffect(row), 만료 시 RevertEffect(row)
// TurnModule.OnTurnEnd(): 매 턴 TickCondition() 호출
public static class AlwaysEffectApplier
{
    // 이벤트 활성화 시 효과 적용 — 팝업 확인 콜백에서 호출
    public static void ApplyEffect(AlwaysEventRow eventRow)
    {
        if (!TryGetValidEffect(eventRow, out AlwaysEffectRow effect)) return;

        ApplyEffectToTargets(eventRow, effect, isRevert: false);
        Debug.Log($"[AlwaysEffectApplier] 효과 적용: {effect.id} | range={eventRow.range}");
    }

    // 이벤트 만료 시 효과 해제 — AlwaysEventManager 만료 블록에서 호출
    public static void RevertEffect(AlwaysEventRow eventRow)
    {
        if (!TryGetValidEffect(eventRow, out AlwaysEffectRow effect)) return;

        ApplyEffectToTargets(eventRow, effect, isRevert: true);
        Debug.Log($"[AlwaysEffectApplier] 효과 해제: {effect.id} | range={eventRow.range}");
    }

    // 매 턴 종료 시 condition 지속 증감 처리 — TurnModule.OnTurnEnd()에서 호출
    public static void TickCondition()
    {
        if (StudentManager.Instance == null) return;

        AlwaysEffectTableSO effectTable = CachedSOData.Get<AlwaysEffectTableSO>();
        if (effectTable == null) return;

        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student.activeEffectIds == null || student.activeEffectIds.Count == 0) continue;

            foreach (string effectId in student.activeEffectIds)
            {
                if (!effectTable.TryGet(effectId, out AlwaysEffectRow effect)) continue;

                student.condition += effect.conditionIncrease;
                student.condition -= effect.conditionDecline;
            }

            student.condition = Student.ClampCondition(student.condition);
            StudentManager.Instance.NotifyStudentModified(student);
        }
    }

    // eventRow 유효성 검사 및 effectTable 조회
    private static bool TryGetValidEffect(AlwaysEventRow eventRow, out AlwaysEffectRow effect)
    {
        effect = null;

        if (eventRow == null) return false;
        if (string.IsNullOrEmpty(eventRow.effectId)) return false;
        if (eventRow.type == "roster") return false; // 로스터 타입은 RecruitmentManager가 처리

        AlwaysEffectTableSO effectTable = CachedSOData.Get<AlwaysEffectTableSO>();
        if (effectTable == null)
        {
            Debug.LogWarning("[AlwaysEffectApplier] AlwaysEffectTable이 없습니다.");
            return false;
        }

        if (!effectTable.TryGet(eventRow.effectId, out effect))
        {
            Debug.LogWarning($"[AlwaysEffectApplier] effectId '{eventRow.effectId}'를 찾을 수 없습니다.");
            return false;
        }

        return true;
    }

    // range == 0: 팀 전체 적용 / range > 0: 특정 슬롯 학생 적용
    private static void ApplyEffectToTargets(AlwaysEventRow eventRow, AlwaysEffectRow effect, bool isRevert)
    {
        if (StudentManager.Instance == null) return;

        if (eventRow.range == 0)
        {
            foreach (Student student in StudentManager.Instance.Students)
            {
                ApplyEffectToStudent(effect, student, isRevert);
            }
        }
        else
        {
            Student student = StudentManager.Instance.GetAssignedStudent(eventRow.range);
            if (student == null)
            {
                Debug.LogWarning($"[AlwaysEffectApplier] 슬롯 {eventRow.range}에 배치된 학생이 없습니다.");
                return;
            }

            ApplyEffectToStudent(effect, student, isRevert);
        }
    }

    // 학생 1명에게 효과 적용 또는 해제
    private static void ApplyEffectToStudent(AlwaysEffectRow effect, Student student, bool isRevert)
    {
        if (student == null) return;

        int multiplier = isRevert ? -1 : 1;

        // condition은 TickCondition()에서 매 턴 처리 → activeEffectIds로 관리
        if (student.activeEffectIds == null)
        {
            student.activeEffectIds = new List<string>();
        }

        if (isRevert)
        {
            student.activeEffectIds.Remove(effect.id);
        }
        else if (!student.activeEffectIds.Contains(effect.id))
        {
            student.activeEffectIds.Add(effect.id);
        }

        // 기간 시작/종료 시 1회 적용되는 보너스/패널티
        student.conditionRecoveryBonus += effect.conditionRecoveryUp * multiplier;
        student.conditionRecoveryBonus -= effect.conditionRecoveryDown * multiplier;
        student.trainingEfficiencyBonus += effect.trainingIncrease * multiplier;
        student.trainingEfficiencyBonus -= effect.trainingDecline * multiplier;

        // 훈련 불가 상태 — 만료 시 무조건 해제
        student.isTrainingBlocked = isRevert ? false : !effect.trainingState;

        StudentManager.Instance.NotifyStudentModified(student);
    }
}
