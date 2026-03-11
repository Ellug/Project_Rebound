using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuddenEventManager : Singleton<SuddenEventManager>
{
    // 특정 상황(Daily, Match 등)에 맞는 이벤트를 확률적으로 발생시킴
    public void EvaluateEvents(SuddenEventConditionFlags condition, SuddenEventContextFlags context)
    {
        var table = CachedSOData.Get<SuddenEventTableSO>();
        if (table == null) return;

        foreach (var row in table.Rows)
        {
            // 조건이나 시점이 맞지 않으면 패스
            if ((row.condition & condition) == 0) continue;
            if ((row.context & context) == 0) continue;

            // 확률 계산
            if (row.isProbable)
            {
                if (UnityEngine.Random.value > row.probability)
                    continue; // 확률 실패 시 패스
            }

            // 조건과 확률을 모두 통과하면 이벤트 실행!
            ExecuteEvent(row);
        }
    }

    // ID로 이벤트를 강제 실행할 때 사용
    public void ExecuteEventById(string eventId)
    {
        var table = CachedSOData.Get<SuddenEventTableSO>();
        if (table != null && table.TryGet(eventId, out var row))
        {
            ExecuteEvent(row);
        }
        else
        {
            Debug.LogWarning($"[SuddenEventManager] 이벤트를 찾을 수 없습니다: {eventId}");
        }
    }

    private void ExecuteEvent(SuddenEventRow row)
    {
        Debug.Log($"[SuddenEventManager] 돌발 이벤트 발생: {row.name} ({row.id})");

        // 1. 타겟 선정
        List<Student> targets = PickTargets(row.scope, row.targetMin, row.targetMax);

        // 2. 효과 적용
        ApplyEffect(row.effect1, targets);
        ApplyEffect(row.effect2, targets);
        ApplyEffect(row.effect3, targets);

        // 3. 텍스트 출력
        ShowEventText(row, targets);
    }

    private List<Student> PickTargets(SuddenEventScope scope, int min, int max)
    {
        List<Student> pool = new List<Student>();

        // 스코프에 따른 대상 풀 설정
        if (StudentManager.Instance != null)
        {
            if (scope == SuddenEventScope.Member || scope == SuddenEventScope.TeamMember)
            {
                pool.AddRange(StudentManager.Instance.Students);
            }
            else if (scope == SuddenEventScope.TeamKeyMember)
            {
                // 출전 슬롯에 배치된 학생들만
                foreach (var pair in StudentManager.Instance.SlotAssignments)
                {
                    if (pair.Value != null) pool.Add(pair.Value);
                }
            }
            // TODO: 밴치멤버 등 필요 시 추가
        }

        if (pool.Count == 0) return pool;

        // 무작위로 섞기
        for (int i = 0; i < pool.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(0, pool.Count);
            var temp = pool[i];
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        // min ~ max 사이의 랜덤 인원수 결정
        int count = UnityEngine.Random.Range(min, max + 1);
        count = Mathf.Clamp(count, 0, pool.Count);

        return pool.Take(count).ToList();
    }

    private void ApplyEffect(string effectId, List<Student> targets)
    {
        if (string.IsNullOrEmpty(effectId) || effectId == "-" || effectId == "none") return;

        var effectTable = CachedSOData.Get<SuddenEventEffectTableSO>();
        if (effectTable == null || !effectTable.TryGet(effectId, out var effectRow))
        {
            Debug.LogWarning($"[SuddenEventManager] 효과를 찾을 수 없습니다: {effectId}");
            return;
        }

        // 학생 스탯 변화인지, 재화 변화인지 판별하여 적용
        bool isPlayerStat = effectRow.targetMin == PlayerStat.Money || effectRow.targetMin == PlayerStat.Fame;

        if (isPlayerStat)
        {
            int amount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);
            ApplyPlayerStat(effectRow.targetMin, amount);
        }
        else
        {
            foreach (var student in targets)
            {
                int amount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);
                ApplyStudentStat(student, effectRow.targetMin, amount);
            }
            // 스탯 변경 사항 UI 갱신 알림
            if (targets.Count > 0 && StudentManager.Instance != null)
            {
                StudentManager.Instance.NotifyStudentModified(targets[0]);
            }
        }
    }

    private void ApplyPlayerStat(PlayerStat statType, int amount)
    {
        if (MoneyManager.Instance == null) return;

        if (statType == PlayerStat.Money)
        {
            if (amount > 0) MoneyManager.Instance.AddGold(amount);
            else MoneyManager.Instance.TrySpendGold(-amount);
        }
        else if (statType == PlayerStat.Fame)
        {
            if (amount > 0) MoneyManager.Instance.AddReputation(amount);
            else MoneyManager.Instance.TrySpendReputation(-amount);
        }
    }

    private void ApplyStudentStat(Student student, PlayerStat statType, int amount)
    {
        switch (statType)
        {
            case PlayerStat.Condition: student.condition = Student.ClampCondition(student.condition + amount); break;
            case PlayerStat.Mental: student.mental += amount; break;
            case PlayerStat.Shoot: student.shoot += amount; break;
            case PlayerStat.Speed: student.speed += amount; break;
            case PlayerStat.Jump: student.jump += amount; break;
            case PlayerStat.Stamina: student.stamina += amount; break;
        }
    }

    private void ShowEventText(SuddenEventRow eventRow, List<Student> targets)
    {
        if (string.IsNullOrEmpty(eventRow.description) || eventRow.description == "-") return;

        var textTable = CachedSOData.Get<SuddenEventTextTableSO>();
        if (textTable != null && textTable.TryGet(eventRow.description, targets.Count, out var textRow))
        {
            string finalMsg = textRow.description;

            // 문자열 치환 ({target1.name} 등)
            if (targets.Count > 0) finalMsg = finalMsg.Replace("{target1.name}", targets[0].studentName);
            if (targets.Count > 1) finalMsg = finalMsg.Replace("{target2.name}", targets[1].studentName);
            if (targets.Count > 2) finalMsg = finalMsg.Replace("{target3.name}", targets[2].studentName);

            //  이벤트 조건(Condition)에 따라 출력 위치 분기
            if ((eventRow.condition & SuddenEventConditionFlags.Daily) != 0)
            {
                // 1. Daily 이벤트 -> 메신저 알림으로 전송
                if (MessengerManager.Instance != null)
                {
                    ChatMessage msg = new ChatMessage(MessageSenderType.Them, finalMsg);
                    MessengerManager.Instance.ReceiveMessage("sys_sudden_event", "알림 센터", msg);
                }
            }
            else if ((eventRow.condition & SuddenEventConditionFlags.Match) != 0)
            {
                // 2. Match(경기) 이벤트 -> 메신저에 보내지 않음
                Debug.Log($"[경기장 돌발 이벤트 발생] {finalMsg}");
            }
            else
            {
                // School, Exercise 등 기타 조건 처리
                Debug.Log($"[기타 돌발 이벤트 발생] {finalMsg}");
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Random Daily Event")]
    private void DebugTestDailyEvent()
    {
        EvaluateEvents(SuddenEventConditionFlags.Daily, SuddenEventContextFlags.PreProcess);
    }
#endif
}