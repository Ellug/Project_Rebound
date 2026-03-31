using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuddenEventManager : Singleton<SuddenEventManager>
{
    private int _dailyEventCount = 0;
    private const int MAX_EVENTS_PER_TURN = 3;

    // 지속 기간이 있는 상태 이상/버프를 추적하기 위한 데이터
    [Serializable]
    public class ActiveTermEffect
    {
        public string StudentName;
        public PlayerStat StatType;
        public int Amount;
        public int RemainingDays;
    }

    // 현재 적용 중인 기간제 효과 목록
    [SerializeField] private List<ActiveTermEffect> _activeTermEffects = new List<ActiveTermEffect>();


    private DateTime _lastTickDate;
    private bool _isLastTickDateSet = false;
    public void EvaluateEvents(SuddenEventConditionFlags condition, SuddenEventContextFlags context)
    {
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        int currentTurn = tm != null ? tm.TurnIndex : -1;

        if (tm != null)
        {
            // 시합 당일 차단
            if (tm.CurrentPhase == GamePhase.MatchDay) return;

            if (tm.DateManager != null)
            {
                DayOfWeek currentDay = tm.DateManager.CurrentDate.DayOfWeek;
                // 주말(토, 일) 차단
                if (currentDay == DayOfWeek.Saturday || currentDay == DayOfWeek.Sunday) return;
                // 금요일 제약 조건
                if (currentDay == DayOfWeek.Friday)
                {
                    string condStr = condition.ToString().ToLower();
                    if (!condStr.Contains("start") && !condStr.Contains("begin") && !condStr.Contains("morning") && !condStr.Contains("enter") && !condStr.Contains("init"))
                        return;
                }
            }
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


            // isProbable 플래그 확인 및 발동 확률 연산
            if (row.isProbable && UnityEngine.Random.value > row.probability) continue;

            triggeredEvents.Add(row);
        }

        if (triggeredEvents.Count > 0)
        {
            // 셔플
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

                bool executed = ExecuteEvent(evt);
                if (executed) _dailyEventCount++;
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

    private bool ExecuteEvent(SuddenEventRow row, string specificTargetName = "", bool fromDialogue = false, Dictionary<string, string> passedVars = null, string originRoomId = "")
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

        bool isStudentScope = ((int)row.scope == 2 || (int)row.scope == 3 || (int)row.scope == 4);

        if (targets.Count == 0 && isStudentScope)
        {
            targets = PickTargets(row);


            // 학생을 뽑아야 하는데 못 뽑았으면 이벤트 취소
            if (targets.Count == 0 && row.targetMin > 0) return false;
        }

        Dictionary<string, string> textVars = new Dictionary<string, string>();


        // 학생이 뽑혔을 때만 치환 변수에 이름 할당
        for (int i = 0; i < targets.Count; i++)
        {
            textVars[$"{{target{i + 1}.name}}"] = targets[i].studentName;
            textVars[$"{{target{i + 1}.grade}}"] = targets[i].grade.ToString() + "학년";
        }

        //기간제 연산
        int termDays = 0;
        if (row.termMax > 0)
        {
            termDays = UnityEngine.Random.Range(row.termMin, row.termMax + 1);
        }

        textVars["{term}"] = termDays.ToString();

        void ProcessEffect(string effectId, int effectIndex)
        {
            if (string.IsNullOrEmpty(effectId) || effectId == "-" || effectId == "none") return;

            var effectTable = CachedSOData.Get<SuddenEventEffectTableSO>();
            if (effectTable != null && effectTable.TryGet(effectId.Trim(), out var effectRow))
            {
                int amount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);

                bool isPercentage = ((int)effectRow.type == 2);

                ApplyEffectWithCalculatedAmount(effectRow.targetMin, targets, amount, isPercentage, termDays);

                string statName = GetStatNameKorean(effectRow.targetMin);

                textVars[$"{{effect{effectIndex}.target_name}}"] = statName;
                textVars[$"{{effect{effectIndex}.amount}}"] = isPercentage ? $"{Mathf.Abs(amount)}%" : Mathf.Abs(amount).ToString();

                if (effectIndex == 1)
                {
                    textVars["{event_effect.target_name}"] = statName;
                    textVars["{event_effect.amount}"] = isPercentage ? $"{Mathf.Abs(amount)}%" : Mathf.Abs(amount).ToString();
                }
            }
        }

        ProcessEffect(row.effect1, 1);
        ProcessEffect(row.effect2, 2);
        ProcessEffect(row.effect3, 3);

        ShowEventTextOrDialogue(row, targets, textVars, fromDialogue, originRoomId, isStudentScope);
        return true;
    }


    // ==========================================
    // Scope 및 Trigger 연산
    // ==========================================
    private List<Student> PickTargets(SuddenEventRow row)
    {
        List<Student> pool = new List<Student>();

        if (StudentManager.Instance != null && StudentManager.Instance.Students != null)
        {
            // Scope (enum int 변환 기준: 2=Member, 3=Team_Member, 4=Bench_Member)
            int scopeVal = (int)row.scope;

            if (scopeVal == 3) // Team_Member
            {
                foreach (var pair in StudentManager.Instance.SlotAssignments)
                    if (pair.Value != null) pool.Add(pair.Value);
            }
            else if (scopeVal == 4) // Bench_Member
            {
                var starters = StudentManager.Instance.SlotAssignments.Values.Where(s => s != null).ToList();
                pool.AddRange(StudentManager.Instance.Students.Where(s => !starters.Contains(s)));
            }
            else // Member 등 기본
            {
                pool.AddRange(StudentManager.Instance.Students);
            }
        }

        if (row.isTrigger && pool.Count > 0)
        {
            pool = pool.Where(student =>
                CheckTriggerCondition(student, (int)row.triggerStatus1, (int)row.triggerCondition1, row.triggerThreshold1) &&
                ((int)row.triggerCondition2 == 0 || CheckTriggerCondition(student, (int)row.triggerStatus2, (int)row.triggerCondition2, row.triggerThreshold2))
            ).ToList();
        }

        if (pool.Count == 0) return pool;
        // 셔플
        for (int i = 0; i < pool.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(0, pool.Count);
            var temp = pool[i];
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        int count = UnityEngine.Random.Range(row.targetMin, row.targetMax + 1);
        count = Mathf.Clamp(count, 0, pool.Count);

        return pool.Take(count).ToList();
    }
    // 트리거 충족 여부 연산
    private bool CheckTriggerCondition(Student student, int statusType, int conditionType, int threshold)
    {
        if (conditionType == 0) return true;

        int statValue = GetStudentStatValue(student, (PlayerStat)statusType);

        return conditionType switch
        {
            2 => statValue == threshold,
            3 => statValue != threshold,
            4 => statValue > threshold,
            5 => statValue <= threshold,
            6 => statValue >= threshold,
            7 => statValue < threshold,
            _ => true
        };
    }

    private int GetStudentStatValue(Student student, PlayerStat statType)
    {
        return statType switch
        {
            PlayerStat.Condition => student.condition,
            PlayerStat.Mental => student.mental,
            PlayerStat.Shoot => student.shoot,
            PlayerStat.Speed => student.speed,
            PlayerStat.Jump => student.jump,
            PlayerStat.Stamina => student.stamina,
            _ => 0
        };
    }


    // ==========================================
    // Effect Type(비율/고정) 및 Term(기간) 적용
    // ==========================================
    private void ApplyEffectWithCalculatedAmount(PlayerStat targetStat, List<Student> targets, int amount, bool isPercentage, int termDays)
    {
        bool isPlayerStat = targetStat == PlayerStat.Money || targetStat == PlayerStat.Fame;

        if (isPlayerStat)
        {
            ApplyPlayerStat(targetStat, amount);
            return;
        }

        bool statIncreased = false;
        foreach (var student in targets)
        {
            // 실제 반영할 수치 계산 (비율일 경우 현재 스탯 기반 퍼센트 연산)
            int finalAmount = amount;
            if (isPercentage)
            {
                int currentStat = GetStudentStatValue(student, targetStat);
                finalAmount = Mathf.RoundToInt(currentStat * (amount / 100f));
            }

            statIncreased |= ApplyStudentStat(student, targetStat, finalAmount);

            // 기간제 스탯 증감일 경우, 롤백을 위해 리스트에 등록
            if (termDays > 0)
            {
                _activeTermEffects.Add(new ActiveTermEffect
                {
                    StudentName = student.studentName,
                    StatType = targetStat,
                    Amount = finalAmount,
                    RemainingDays = termDays
                });
            }
        }

        if (targets.Count > 0 && StudentManager.Instance != null)
        {
            foreach (var s in targets)
            {
                StudentManager.Instance.NotifyStudentModified(s);
            }
        }

        if (statIncreased) SoundManager.Instance?.PlayStatUpSfx();
    }

    private void ApplyPlayerStat(PlayerStat statType, int amount)
    {
        if (MoneyManager.Instance == null) return;
        if (statType == PlayerStat.Money) { if (amount > 0) MoneyManager.Instance.AddGold(amount); else MoneyManager.Instance.TrySpendGold(-amount); }
        else if (statType == PlayerStat.Fame) { if (amount > 0) MoneyManager.Instance.AddReputation(amount); else MoneyManager.Instance.TrySpendReputation(-amount); }
    }

    private bool ApplyStudentStat(Student student, PlayerStat statType, int amount)
    {
        int statInt = (int)statType;
        if (statInt == 21 || statInt == 22 || statInt == 23)
        {
            Debug.Log($"[상태이상 발생!] {student.studentName} 학생에게 상태이상 번호 {statInt} 적용 (수치: {amount})");

            if (amount > 0)
            {
                if (statInt == 21 || statInt == 22) student.isTrainingBlocked = true;

                if (statInt == 21) student.abnormalState = Student.AbnormalType.Disease;
                else if (statInt == 22 || statInt == 23) student.abnormalState = Student.AbnormalType.Injury;

          
                if (StudentManager.Instance != null)
                {
                    StudentManager.Instance.RemoveStudentFromSlots(student);
                }
            }
            else // amount < 0 (회복 이벤트)
            {
                student.isTrainingBlocked = false;
                student.abnormalState = Student.AbnormalType.None;

                // 해당 학생이 가지고 있던 모든 기간제 질병 데이터를 삭제하여 더 이상 추적하지 않게 함
                _activeTermEffects.RemoveAll(e => e.StudentName == student.studentName && ((int)e.StatType == 21 || (int)e.StatType == 22 || (int)e.StatType == 23));
            }

            if (statInt == 21 || statInt == 22)
            {
                student.isTrainingBlocked = (amount > 0);
            }

            return true;
        }

        switch (statType)
        {
            case PlayerStat.Condition:
                int beforeC = student.condition;
                student.condition = Student.ClampCondition(student.condition + amount);
                return student.condition > beforeC;
            case PlayerStat.Mental:
                int beforeM = student.mental;
                student.mental += amount;
                return student.mental > beforeM;
            case PlayerStat.Shoot:
                int beforeSh = student.shoot;
                student.shoot += amount;
                return student.shoot > beforeSh;
            case PlayerStat.Speed:
                int beforeSp = student.speed;
                student.speed += amount;
                return student.speed > beforeSp;
            case PlayerStat.Jump:
                int beforeJ = student.jump;
                student.jump += amount;
                return student.jump > beforeJ;
            case PlayerStat.Stamina:
                int beforeSt = student.stamina;
                student.stamina += amount;
                return student.stamina > beforeSt;
        }
        return false;
    }


    // ==========================================
    // 턴 매니저 호출용: 기간제 효과 타이머 감소 및 원상복구 로직
    // ==========================================
    public void TickTermEffects()
    {
        int daysToSubtract = 1;
        TurnManager tm = FindFirstObjectByType<TurnManager>();

        if (tm != null && tm.DateManager != null)
        {
            DateTime currentDate = tm.DateManager.CurrentDate.Date;

            if (_isLastTickDateSet && currentDate > _lastTickDate)
            {
                daysToSubtract = (currentDate - _lastTickDate).Days;
            }

            _lastTickDate = currentDate;
            _isLastTickDateSet = true;
        }

        for (int i = _activeTermEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeTermEffects[i];

            // 스킵된 주말 날짜까지 한 번에 차감
            effect.RemainingDays -= daysToSubtract;

            if (effect.RemainingDays <= 0)
            {
                var student = StudentManager.Instance?.Students.FirstOrDefault(s => s.studentName == effect.StudentName);
                if (student != null)
                {
                    int statInt = (int)effect.StatType;
                    if (statInt == 21 || statInt == 22 || statInt == 23)
                    {
                        // 상태이상 완치
                        student.isTrainingBlocked = false;
                        student.abnormalState = Student.AbnormalType.None;
                    }
                    else
                    {
                        ApplyStudentStat(student, effect.StatType, -effect.Amount);
                    }
                    StudentManager.Instance.NotifyStudentModified(student);
                }
                _activeTermEffects.RemoveAt(i);
            }
        }
    }

    private void ShowEventTextOrDialogue(SuddenEventRow eventRow, List<Student> targets, Dictionary<string, string> textVars, bool fromDialogue, string originRoomId, bool isStudentScope)
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
        string textSearchId = desc.StartsWith("diag_") ? "" : desc;

        int targetCountForText = (targets != null && targets.Count > 0) ? targets.Count : 1;

        if (!string.IsNullOrEmpty(textSearchId) && textTable != null)
        {
            if (!textTable.TryGet(textSearchId, targetCountForText, out var textRow))
            {
                if (!textTable.TryGet(textSearchId, 20, out textRow))
                {
                    textTable.TryGet(textSearchId, 1, out textRow);
                }
            }

            if (textRow != null)
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
        }

        if (roomId == "sys_notice" && targets != null && targets.Count == 1 && !string.IsNullOrEmpty(systemMsgContent))
        {
            if (isStudentScope && !systemMsgContent.Contains("팀") && !systemMsgContent.Contains("전원"))
            {
                string tName = targets[0].studentName;
                if (!systemMsgContent.Contains(tName))
                {
                    systemMsgContent = $"{tName} {systemMsgContent}";
                }
            }
        }

        DateTime? firstMsgDate = null;
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (tm != null && tm.DateManager != null)
        {
            firstMsgDate = tm.DateManager.CurrentDate; 

            string contextStr = eventRow.context.ToString().ToLower();

            if (contextStr.Contains("post") && !fromDialogue)
            {
                firstMsgDate = tm.DateManager.CurrentDate.AddDays(1);
            }
        }

        if (desc.StartsWith("diag_"))
        {
            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.PlayDialogue(roomId, roomName, desc, "index_000", textVars, systemMsgContent, firstMsgDate);
                previewText = $"{roomName}의 새로운 메시지가 도착했습니다.";
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(systemMsgContent) && MessengerManager.Instance != null)
            {
                ChatMessage msg = new ChatMessage(MessageSenderType.Them, systemMsgContent, MessageEventType.System);
                if (firstMsgDate.HasValue) msg.Timestamp = firstMsgDate.Value;
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

        if (!_isPopupShowing)
        {
            ProcessNextPopup();
        }
    }

    public void ProcessNextPopup()
    {
        _isPopupShowing = false;

        if (_popupQueue.Count > 0)
        {
            var data = _popupQueue.Peek();

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

            _isPopupShowing = true;
            OnPopupRequested?.Invoke(_popupQueue.Dequeue());
        }
    }

    public void ResetDailyEventCount()
    {
        _dailyEventCount = 0;
    }
}