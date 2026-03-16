using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuddenEventManager : Singleton<SuddenEventManager>
{
    // 턴 당 발생 횟수 추적용 변수
    private int _dailyEventCount = 0;
    private int _lastTurnIndex = -1;
    private const int MAX_EVENTS_PER_TURN = 3;
    public void EvaluateEvents(SuddenEventConditionFlags condition, SuddenEventContextFlags context)
    {
        // 1. 턴이 바뀌었는지 확인하고 카운트 초기화
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        int currentTurn = tm != null ? tm.TurnIndex : -1;

        if (_lastTurnIndex != currentTurn)
        {
            _lastTurnIndex = currentTurn;
            _dailyEventCount = 0;
        }

        // 2. 턴 당 최대 발생 횟수 제한 체크
        if (_dailyEventCount >= MAX_EVENTS_PER_TURN)
        {
            Debug.Log($"[SuddenEventManager] 턴 당 돌발 이벤트 제한({MAX_EVENTS_PER_TURN}회) 도달로 평가를 생략합니다.");
            return;
        }

        var table = CachedSOData.Get<SuddenEventTableSO>();
        if (table == null) return;

        List<SuddenEventRow> triggeredEvents = new List<SuddenEventRow>();

        foreach (var row in table.Rows)
        {
            if ((row.condition & condition) == 0) continue;
            if ((row.context & context) == 0) continue;

            if (row.isProbable)
            {
                if (UnityEngine.Random.value > row.probability)
                    continue;
            }

            triggeredEvents.Add(row);
        }

        if (triggeredEvents.Count > 0)
        {
            // 후보 중 하나를 랜덤으로 뽑아서 실행 후 카운트 증가
            var selectedEvent = triggeredEvents[UnityEngine.Random.Range(0, triggeredEvents.Count)];
            ExecuteEvent(selectedEvent);
            _dailyEventCount++;
        }
    }
    public void ExecuteEventById(string eventId)
    {
        // DialogueRunner 등에서 확정적으로 호출할 때는 카운트를 무시하고 무조건 실행
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

        // 2.Effect 1, 2, 3 처리 및 텍스트 치환용 데이터 수집
        Dictionary<string, string> textVars = new Dictionary<string, string>();

        // 타겟 이름 미리 캐싱
        if (targets.Count > 0) textVars["{target1.name}"] = targets[0].studentName;
        if (targets.Count > 1) textVars["{target2.name}"] = targets[1].studentName;
        if (targets.Count > 2) textVars["{target3.name}"] = targets[2].studentName;
        if (targets.Count > 3) textVars["{target4.name}"] = targets[3].studentName;

        void ProcessEffect(string effectId, int effectIndex)
        {
            if (string.IsNullOrEmpty(effectId) || effectId == "-" || effectId == "none") return;

            var effectTable = CachedSOData.Get<SuddenEventEffectTableSO>();
            if (effectTable != null && effectTable.TryGet(effectId.Trim(), out var effectRow))
            {
                int amount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);
                ApplyEffectWithCalculatedAmount(effectRow.targetMin, targets, amount);

                string statName = GetStatNameKorean(effectRow.targetMin);

                // 새로운 텍스트 포맷을 위한 치환 데이터 등록 ({effect1.target_name}, {effect1.amount} 등)
                textVars[$"{{effect{effectIndex}.target_name}}"] = statName;
                textVars[$"{{effect{effectIndex}.amount}}"] = Mathf.Abs(amount).ToString();


                if (effectIndex == 1)
                {
                    textVars["{event_effect.target_name}"] = statName;
                    textVars["{event_effect.amount}"] = Mathf.Abs(amount).ToString();
                }
            }
        }

        // 효과 3개를 모두 평가하고 적용
        ProcessEffect(row.effect1, 1);
        ProcessEffect(row.effect2, 2);
        ProcessEffect(row.effect3, 3);

        // 3. 텍스트 또는 다이얼로그 출력 분기
        ShowEventTextOrDialogue(row, targets, textVars);
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

    private void ShowEventTextOrDialogue(SuddenEventRow eventRow, List<Student> targets, Dictionary<string, string> textVars)
    {
        string desc = eventRow.description?.Trim();
        if (string.IsNullOrEmpty(desc) || desc == "-") return;

        bool isSystemNotice = eventRow.name.Contains("[공지]") || eventRow.name.Contains("[시스템]");

        string roomId = (targets.Count > 0 && !isSystemNotice) ? $"student_{targets[0].studentName}" : "sys_sudden_event";
        string roomName = (targets.Count > 0 && !isSystemNotice) ? targets[0].studentName : "알림";

        string previewText = ""; // 팝업에 띄울 미리보기 텍스트

        // 분기 1: 메신저 대화(Dialogue) 트리거
        if (desc.StartsWith("diag_"))
        {
            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.PlayDialogue(roomId, roomName, desc);
                previewText = $"{roomName}의 새로운 메시지가 도착했습니다.";
            }
        }
        // 분기 2: 단순 텍스트 메시지 출력
        else
        {
            var textTable = CachedSOData.Get<SuddenEventTextTableSO>();
            if (textTable != null && textTable.TryGet(desc, targets.Count, out var textRow))
            {
                string finalMsg = textRow.description.Replace("$", "").Replace("\"", "").Replace("\\n", "\n");
                foreach (var kvp in textVars) finalMsg = finalMsg.Replace(kvp.Key, kvp.Value);

                if ((eventRow.condition & SuddenEventConditionFlags.Daily) != 0 && MessengerManager.Instance != null)
                {
                    ChatMessage msg = new ChatMessage(MessageSenderType.Them, finalMsg, MessageEventType.System);
                    MessengerManager.Instance.ReceiveMessage(roomId, roomName, msg);
                    previewText = finalMsg; // 팝업에는 실제 내용을 띄움
                }
            }
        }

        if (!string.IsNullOrEmpty(previewText))
        {
            EnqueueEventPopup(eventRow.name, roomId, roomName, previewText);
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

    public struct EventPopupData
    {
        public string title;
        public string roomId;
        public string roomName;
        public string previewText;
    }
    private Queue<EventPopupData> _popupQueue = new Queue<EventPopupData>();
    public event System.Action<EventPopupData> OnPopupRequested; // 로비 UI로 신호를 보낼 이벤트

    private bool _isPopupShowing = false;

    private void EnqueueEventPopup(string title, string roomId, string roomName, string preview)
    {
        _popupQueue.Enqueue(new EventPopupData { title = title, roomId = roomId, roomName = roomName, previewText = preview });

        if (!_isPopupShowing)
        {
            ProcessNextPopup();
        }
    }

    public void ProcessNextPopup()
    {
        if (_popupQueue.Count > 0)
        {
            _isPopupShowing = true; // 팝업 노출 상태로 잠금
            var data = _popupQueue.Dequeue();
            OnPopupRequested?.Invoke(data);
        }
        else
        {
            _isPopupShowing = false; // 모든 팝업을 다 봤으면 잠금 해제
        }
    }
}
