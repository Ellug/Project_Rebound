using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuddenEventManager : Singleton<SuddenEventManager>
{
    private int _dailyEventCount = 0;
    private int _lastTurnIndex = -1;
    private const int MAX_EVENTS_PER_TURN = 3;

    public void EvaluateEvents(SuddenEventConditionFlags condition, SuddenEventContextFlags context)
    {
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        int currentTurn = tm != null ? tm.TurnIndex : -1;

        if (tm != null && tm.DateManager != null)
        {
            DayOfWeek currentDay = tm.DateManager.CurrentDate.DayOfWeek;

            if (currentDay == DayOfWeek.Friday)
            {
                string condStr = condition.ToString().ToLower();

                if (!condStr.Contains("start") && !condStr.Contains("begin") && !condStr.Contains("morning") && !condStr.Contains("enter") && !condStr.Contains("init"))
                {
                    return;
                }
            }
        }

        if (_lastTurnIndex != currentTurn)
        {
            _lastTurnIndex = currentTurn;
            _dailyEventCount = 0;
        }

        if (_dailyEventCount >= MAX_EVENTS_PER_TURN) return;

        var table = CachedSOData.Get<SuddenEventTableSO>();
        if (table == null) return;

        List<SuddenEventRow> triggeredEvents = new List<SuddenEventRow>();

        foreach (var row in table.Rows)
        {
            if (row.condition.ToString().Contains("School")) continue;
            if ((row.condition & condition) == 0) continue;
            if ((row.context & context) == 0) continue;
            if (row.isProbable && UnityEngine.Random.value > row.probability) continue;

            triggeredEvents.Add(row);
        }

        if (triggeredEvents.Count > 0)
        {
            for (int i = 0; i < triggeredEvents.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(0, triggeredEvents.Count);
                var temp = triggeredEvents[i];
                triggeredEvents[i] = triggeredEvents[rnd];
                triggeredEvents[rnd] = temp;
            }

            foreach (var evt in triggeredEvents)
            {
                if (_dailyEventCount >= MAX_EVENTS_PER_TURN) break;
                ExecuteEvent(evt);
                _dailyEventCount++;
            }
        }
    }

    public void ExecuteEventById(string eventId, string specificTargetName = "", bool fromDialogue = false, Dictionary<string, string> passedVars = null, string originRoomId = "")
    {
        var table = CachedSOData.Get<SuddenEventTableSO>();
        if (table != null && table.TryGet(eventId, out var row))
        {
            ExecuteEvent(row, specificTargetName, fromDialogue, passedVars, originRoomId);
        }
    }

    private void ExecuteEvent(SuddenEventRow row, string specificTargetName = "", bool fromDialogue = false, Dictionary<string, string> passedVars = null, string originRoomId = "")
    {
        List<Student> targets = new List<Student>();

        if (fromDialogue && passedVars != null && passedVars.TryGetValue("{target1.name}", out string tName))
        {
            if (StudentManager.Instance != null)
            {
                var student = StudentManager.Instance.Students.FirstOrDefault(s => s.studentName == tName);
                if (student != null) targets.Add(student);
            }
        }
        else if (fromDialogue && !string.IsNullOrEmpty(specificTargetName) && !specificTargetName.Contains("[공지]"))
        {
            if (StudentManager.Instance != null)
            {
                string parsedName = specificTargetName.Replace("student_", "");
                var student = StudentManager.Instance.Students.FirstOrDefault(s => s.studentName == parsedName);
                if (student != null) targets.Add(student);
            }
        }

        if (targets.Count == 0 && !fromDialogue)
        {
            targets = PickTargets(row.scope, row.targetMin, row.targetMax);
        }

        Dictionary<string, string> textVars = new Dictionary<string, string>();

        if (targets.Count > 0)
        {
            textVars["{target1.name}"] = targets[0].studentName;
            textVars["{target1.grade}"] = targets[0].grade.ToString() + "학년";
        }
        if (targets.Count > 1)
        {
            textVars["{target2.name}"] = targets[1].studentName;
            textVars["{target2.grade}"] = targets[1].grade.ToString() + "학년";
        }
        if (targets.Count > 2)
        {
            textVars["{target3.name}"] = targets[2].studentName;
            textVars["{target3.grade}"] = targets[2].grade.ToString() + "학년";
        }
        if (targets.Count > 3)
        {
            textVars["{target4.name}"] = targets[3].studentName;
            textVars["{target4.grade}"] = targets[3].grade.ToString() + "학년";
        }

        void ProcessEffect(string effectId, int effectIndex)
        {
            if (string.IsNullOrEmpty(effectId) || effectId == "-" || effectId == "none") return;

            var effectTable = CachedSOData.Get<SuddenEventEffectTableSO>();
            if (effectTable != null && effectTable.TryGet(effectId.Trim(), out var effectRow))
            {
                int amount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);
                ApplyEffectWithCalculatedAmount(effectRow.targetMin, targets, amount);

                string statName = GetStatNameKorean(effectRow.targetMin);

                textVars[$"{{effect{effectIndex}.target_name}}"] = statName;
                textVars[$"{{effect{effectIndex}.amount}}"] = Mathf.Abs(amount).ToString();

                if (effectIndex == 1)
                {
                    textVars["{event_effect.target_name}"] = statName;
                    textVars["{event_effect.amount}"] = Mathf.Abs(amount).ToString();
                }
            }
        }
        ProcessEffect(row.effect1, 1);
        ProcessEffect(row.effect2, 2);
        ProcessEffect(row.effect3, 3);

        ShowEventTextOrDialogue(row, targets, textVars, fromDialogue, originRoomId);
    }

    private List<Student> PickTargets(SuddenEventScope scope, int min, int max)
    {
        List<Student> pool = new List<Student>();

        if (StudentManager.Instance != null && StudentManager.Instance.Students != null)
        {
            if (scope == SuddenEventScope.TeamKeyMember)
            {
                foreach (var pair in StudentManager.Instance.SlotAssignments)
                {
                    if (pair.Value != null) pool.Add(pair.Value);
                }
            }
            else
            {
                pool.AddRange(StudentManager.Instance.Students);
            }
        }

        if (pool.Count == 0) return pool;

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
        if (isPlayerStat) ApplyPlayerStat(targetStat, amount);
        else
        {
            bool statIncreased = false;
            foreach (var student in targets)
                statIncreased |= ApplyStudentStat(student, targetStat, amount);

            if (targets.Count > 0 && StudentManager.Instance != null) StudentManager.Instance.NotifyStudentModified(targets[0]);
            if (statIncreased)
                SoundManager.Instance?.PlayStatUpSfx();
        }
    }

    private void ApplyPlayerStat(PlayerStat statType, int amount)
    {
        if (MoneyManager.Instance == null) return;
        if (statType == PlayerStat.Money) { if (amount > 0) MoneyManager.Instance.AddGold(amount); else MoneyManager.Instance.TrySpendGold(-amount); }
        else if (statType == PlayerStat.Fame) { if (amount > 0) MoneyManager.Instance.AddReputation(amount); else MoneyManager.Instance.TrySpendReputation(-amount); }
    }

    private bool ApplyStudentStat(Student student, PlayerStat statType, int amount)
    {
        switch (statType)
        {
            case PlayerStat.Condition:
            {
                int before = student.condition;
                student.condition = Student.ClampCondition(student.condition + amount);
                return student.condition > before;
            }
            case PlayerStat.Mental:
            {
                int before = student.mental;
                student.mental += amount;
                return student.mental > before;
            }
            case PlayerStat.Shoot:
            {
                int before = student.shoot;
                student.shoot += amount;
                return student.shoot > before;
            }
            case PlayerStat.Speed:
            {
                int before = student.speed;
                student.speed += amount;
                return student.speed > before;
            }
            case PlayerStat.Jump:
            {
                int before = student.jump;
                student.jump += amount;
                return student.jump > before;
            }
            case PlayerStat.Stamina:
            {
                int before = student.stamina;
                student.stamina += amount;
                return student.stamina > before;
            }
        }

        return false;
    }

    private void ShowEventTextOrDialogue(SuddenEventRow eventRow, List<Student> targets, Dictionary<string, string> textVars, bool fromDialogue, string originRoomId)
    {
        string desc = eventRow.description?.Trim();
        if (string.IsNullOrEmpty(desc) || desc == "-") return;

        if (desc.StartsWith("text_diag_"))
        {
            desc = desc.Replace("text_diag_", "diag_");
            int lastIdx = desc.LastIndexOf('_');
            if (lastIdx > 0 && desc.Length - lastIdx == 4) desc = desc.Substring(0, lastIdx);
        }

        string sysRoomId = "sys_notice";
        string sysRoomName = "[공지] 한울 고등학교";
        string roomId = sysRoomId;
        string roomName = sysRoomName;
        bool isNotice = eventRow.name.Contains("[공지]") || eventRow.name.Contains("[시스템]");

        if (!string.IsNullOrEmpty(originRoomId))
        {
            roomId = originRoomId;
            if (roomId == "sys_notice")
            {
                isNotice = true;
                roomName = sysRoomName;
            }
            else
            {
                if (targets != null && targets.Count > 0) roomName = targets[0].studentName;
            }
        }
        else if (!isNotice && targets != null && targets.Count > 0)
        {
            roomId = $"student_{targets[0].studentName}";
            roomName = targets[0].studentName;
        }

        string previewText = "";
        string systemMsgContent = "";
        var textTable = CachedSOData.Get<SuddenEventTextTableSO>();
        string textSearchId = desc.StartsWith("diag_") ? "text_" + eventRow.id.Replace("event_", "SuddenEvent_") : desc;

        if (textTable != null && textTable.TryGet(textSearchId, targets?.Count ?? 1, out var textRow))
        {
            systemMsgContent = textRow.description.Replace("$", "").Replace("\"", "").Replace("\\n", "\n");

            foreach (var kvp in textVars)
            {
                systemMsgContent = System.Text.RegularExpressions.Regex.Replace(
                    systemMsgContent,
                    System.Text.RegularExpressions.Regex.Escape(kvp.Key),
                    kvp.Value,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            systemMsgContent = System.Text.RegularExpressions.Regex.Replace(systemMsgContent, @"\{effect\d+\.[^}]+\}", "");
            systemMsgContent = System.Text.RegularExpressions.Regex.Replace(systemMsgContent, @"\{target\d+\.[^}]+\}", "");
        }

        if (roomId == "sys_notice" && targets != null && targets.Count == 1 && !string.IsNullOrEmpty(systemMsgContent))
        {
            string tName = targets[0].studentName;
            if (!systemMsgContent.Contains(tName))
            {
                systemMsgContent = $"{tName} {systemMsgContent}";
            }
        }

        if (desc.StartsWith("diag_"))
        {
            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.PlayDialogue(roomId, roomName, desc, "index_000", textVars, systemMsgContent);
                previewText = $"{roomName}의 새로운 메시지가 도착했습니다.";
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(systemMsgContent) && MessengerManager.Instance != null)
            {
                ChatMessage msg = new ChatMessage(MessageSenderType.Them, systemMsgContent, MessageEventType.System);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, msg);
                previewText = systemMsgContent;
            }
        }
        if (!fromDialogue && !string.IsNullOrEmpty(previewText))
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

    // ==========================================
    // 팝업 스케줄링 관리 로직
    // ==========================================
    public struct EventPopupData
    {
        public string title;
        public string roomId;
        public string roomName;
        public string previewText;
    }

    private Queue<EventPopupData> _popupQueue = new Queue<EventPopupData>();
    public event System.Action<EventPopupData> OnPopupRequested;

    private bool _isPopupShowing = false;

    private void EnqueueEventPopup(string title, string roomId, string roomName, string preview)
    {
        _popupQueue.Enqueue(new EventPopupData { title = title, roomId = roomId, roomName = roomName, previewText = preview });

        // 현재 떠 있는 팝업이 없다면 즉시 실행
        if (!_isPopupShowing)
        {
            ProcessNextPopup();
        }
    }

    public void ProcessNextPopup()
    {
        _isPopupShowing = false; // 이전 팝업이 닫혔음을 선언

        if (_popupQueue.Count > 0)
        {
            var data = _popupQueue.Peek();

            // 이미 읽은 메시지라면 큐에서 빼고 바로 다음 것 검사
            if (MessengerManager.Instance != null)
            {
                var room = MessengerManager.Instance.GetRoom(data.roomId);
                if (room != null && !room.HasUnread)
                {
                    _popupQueue.Dequeue();
                    ProcessNextPopup();
                    return;
                }
            }

            // 조건 통과 시 즉시 팝업 실앵
            _isPopupShowing = true;
            var actualData = _popupQueue.Dequeue();
            OnPopupRequested?.Invoke(actualData);
        }
    }
}
