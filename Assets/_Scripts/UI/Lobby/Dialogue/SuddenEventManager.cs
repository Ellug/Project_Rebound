using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuddenEventManager : Singleton<SuddenEventManager>
{
    // 특정 상황에 맞는 이벤트를 1개만 골라서 발생시킴
    public void EvaluateEvents(SuddenEventConditionFlags condition, SuddenEventContextFlags context)
    {
        var table = CachedSOData.Get<SuddenEventTableSO>();
        if (table == null) return;

        List<SuddenEventRow> triggeredEvents = new List<SuddenEventRow>();

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

            // 조건을 모두 통과한 이벤트를 후보 목록에 추가
            triggeredEvents.Add(row);
        }

        // 후보 중 하나만 랜덤으로 뽑아서 실행 
        if (triggeredEvents.Count > 0)
        {
            var selectedEvent = triggeredEvents[UnityEngine.Random.Range(0, triggeredEvents.Count)];
            ExecuteEvent(selectedEvent);
        }
    }

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

        // 2. 첫 번째 효과 적용 및 정보 추출 
        int primaryAmount = 0;
        string primaryStatName = "";

        if (!string.IsNullOrEmpty(row.effect1) && row.effect1 != "-")
        {
            var effectTable = CachedSOData.Get<SuddenEventEffectTableSO>();
            // Trim()으로 엑셀에 숨어있는 공백 제거
            if (effectTable != null && effectTable.TryGet(row.effect1.Trim(), out var effectRow))
            {
                primaryAmount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);
                primaryStatName = GetStatNameKorean(effectRow.targetMin); // 스탯 이름을 한글로 변환

                ApplyEffectWithCalculatedAmount(effectRow.targetMin, targets, primaryAmount);
            }
            else
            {
                Debug.LogWarning($"[SuddenEventManager] 효과를 찾을 수 없습니다: {row.effect1}");
            }
        }

        // 3. 나머지 효과 적용
        ApplyEffect(row.effect2, targets);
        ApplyEffect(row.effect3, targets);

        // 4. 텍스트 출력
        ShowEventText(row, targets, primaryStatName, primaryAmount);
    }

    private List<Student> PickTargets(SuddenEventScope scope, int min, int max)
    {
        List<Student> pool = new List<Student>();

        if (StudentManager.Instance != null)
        {
            if (scope == SuddenEventScope.Member || scope == SuddenEventScope.TeamMember)
            {
                pool.AddRange(StudentManager.Instance.Students);
            }
            else if (scope == SuddenEventScope.TeamKeyMember)
            {
                foreach (var pair in StudentManager.Instance.SlotAssignments)
                {
                    if (pair.Value != null) pool.Add(pair.Value);
                }
            }
        }

        if (pool.Count == 0) return pool;

        // 리스트 섞기
        for (int i = 0; i < pool.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(0, pool.Count);
            var temp = pool[i];
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        int count = UnityEngine.Random.Range(min, max + 1);
        count = Mathf.Clamp(count, 0, pool.Count);

        return pool.Take(count).ToList();
    }

    private void ApplyEffect(string effectId, List<Student> targets)
    {
        if (string.IsNullOrEmpty(effectId) || effectId == "-" || effectId == "none") return;

        var effectTable = CachedSOData.Get<SuddenEventEffectTableSO>();
        if (effectTable == null || !effectTable.TryGet(effectId.Trim(), out var effectRow)) return;

        int amount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);

        ApplyEffectWithCalculatedAmount(effectRow.targetMin, targets, amount);
    }

    private void ApplyEffectWithCalculatedAmount(PlayerStat targetStat, List<Student> targets, int amount)
    {
        bool isPlayerStat = targetStat == PlayerStat.Money || targetStat == PlayerStat.Fame;

        if (isPlayerStat)
        {
            ApplyPlayerStat(targetStat, amount);
        }
        else
        {
            foreach (var student in targets)
            {
                ApplyStudentStat(student, targetStat, amount);
            }
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

    private void ShowEventText(SuddenEventRow eventRow, List<Student> targets, string statName, int amount)
    {
        if (string.IsNullOrEmpty(eventRow.description) || eventRow.description == "-") return;

        var textTable = CachedSOData.Get<SuddenEventTextTableSO>();
        if (textTable != null && textTable.TryGet(eventRow.description, targets.Count, out var textRow))
        {
            string finalMsg = textRow.description;

            // 엑셀에 작성된 불필요한 기호 제거
            finalMsg = finalMsg.Replace("$\"", "").Replace("\"", "");

            // 타겟 이름 치환
            if (targets.Count > 0) finalMsg = finalMsg.Replace("{target1.name}", targets[0].studentName);
            if (targets.Count > 1) finalMsg = finalMsg.Replace("{target2.name}", targets[1].studentName);
            if (targets.Count > 2) finalMsg = finalMsg.Replace("{target3.name}", targets[2].studentName);

            // 효과 내용 치환
            int displayAmount = Mathf.Abs(amount);
            finalMsg = finalMsg.Replace("{event_effect.target_name}", statName);
            finalMsg = finalMsg.Replace("{event_effect.amount}", displayAmount.ToString());
            finalMsg = finalMsg.Replace("{event_event_effect.amount}", displayAmount.ToString());

            // 출력 분기
            if ((eventRow.condition & SuddenEventConditionFlags.Daily) != 0)
            {
                if (MessengerManager.Instance != null)
                {
                    ChatMessage msg = new ChatMessage(MessageSenderType.Them, finalMsg);
                    MessengerManager.Instance.ReceiveMessage("sys_sudden_event", "알림 센터", msg);
                }
            }
            else if ((eventRow.condition & SuddenEventConditionFlags.Match) != 0)
            {
                Debug.Log($"[경기장 돌발 이벤트] {finalMsg}");
            }
            else
            {
                Debug.Log($"[기타 돌발 이벤트] {finalMsg}");
            }
        }
    }

    private string GetStatNameKorean(PlayerStat stat)
    {
        return stat switch
        {
            PlayerStat.Mental => "멘탈",
            PlayerStat.Shoot => "슈팅",
            PlayerStat.Speed => "속도",
            PlayerStat.Jump => "점프력",
            PlayerStat.Stamina => "지구력",
            PlayerStat.Condition => "컨디션",
            PlayerStat.Money => "자금",
            PlayerStat.Fame => "명성치",
            _ => stat.ToString()
        };
    }

#if UNITY_EDITOR
    [ContextMenu("Test Random Daily Event")]
    private void DebugTestDailyEvent()
    {
        EvaluateEvents(SuddenEventConditionFlags.Daily, SuddenEventContextFlags.PreProcess);
    }
#endif
}