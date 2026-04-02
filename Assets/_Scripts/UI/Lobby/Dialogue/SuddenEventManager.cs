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
    private int _lastRolledTermDays = 0;

    private HashSet<int> _pickedStudentsThisTurn = new HashSet<int>();
    private Dictionary<string, int> _triggeredEventIdsWithTurn = new Dictionary<string, int>();

    void Update()
    {
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        GameManager gm = FindFirstObjectByType<GameManager>();

        bool isMatchOrVacation = (tm != null && (tm.CurrentPhase == GamePhase.MatchDay || tm.CurrentPhase == GamePhase.MatchInProgress)) ||
                                 (gm != null && (gm.HasPendingFriendlyMatch || gm.IsLeagueOpened));
        if (isMatchOrVacation)
        {
            if (_popupQueue.Count > 0)
            {
                _popupQueue.Clear();
                _isPopupShowing = false;
            }
        }
    }

    

    // ==========================================
    // 이벤트 통합 평가 (훈련 부상 포함)
    // ==========================================
    public void EvaluateEvents(SuddenEventConditionFlags condition, SuddenEventContextFlags context)
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (gm != null && tm != null && tm.DateManager != null)
        {
            DateTime today = tm.DateManager.CurrentDate.Date;
            DateTime tomorrow = today.AddDays(1);

            
            if (gm.IsLeagueOpened) return;

            int dDay = gm.GetTournamentDday();
            if (dDay == 0 || dDay == 1) return;

            if (gm.IsFriendlyMatchConfirmed)
            {
                if (gm.FriendlyMatchDate.Date == today || gm.FriendlyMatchDate.Date == tomorrow)
                    return;
            }
        }

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


        // 하루 3개 제한 확인
        if (_dailyEventCount >= MAX_EVENTS_PER_TURN)
            return;

        var table = CachedSOData.Get<SuddenEventTableSO>();
        if (table == null) return;

        // 조건에 맞는 모든 이벤트 추출
        List<SuddenEventRow> triggeredEvents = table.Rows.Where(row =>
            !row.condition.ToString().Contains("School") &&
            (row.condition & condition) != 0 &&
            (row.context & context) != 0).ToList();

        if (triggeredEvents.Count > 0)
        {
            // 랜덤성 부여를 위한 셔플 후 실행
            foreach (var evt in triggeredEvents.OrderBy(x => UnityEngine.Random.value))
            {
                if (_dailyEventCount >= MAX_EVENTS_PER_TURN) break;

                // 확률 검사 (엑셀 확률이 1보다 크면 %로 간주하여 보정)
                float prob = evt.probability > 1f ? evt.probability / 100f : evt.probability;
                if (!evt.isProbable || UnityEngine.Random.value <= prob)
                {
                    ExecuteEvent(evt);
                }
            }
        }
    }

    public void ExecuteEventById(string eventId, string specificTargetName = "", bool fromDialogue = false, Dictionary<string, string> passedVars = null, string originRoomId = "")
    {
        fromDialogue = true;
        var table = CachedSOData.Get<SuddenEventTableSO>();
        if (table != null && table.TryGet(eventId, out var row))
            ExecuteEvent(row, specificTargetName, fromDialogue, passedVars, originRoomId);
    }


    // ==========================================
    // 이벤트 실행 및 상태이상 판별 (이름 기준)
    // ==========================================
    private bool ExecuteEvent(SuddenEventRow row, string specificTargetName = "", bool fromDialogue = false, Dictionary<string, string> passedVars = null, string originRoomId = "")
    {
        // 대화 종료 후 파생 이벤트가 아닐 때만 카운트 체크 및 증가
        if (!fromDialogue && _dailyEventCount >= MAX_EVENTS_PER_TURN) return false;

        TurnManager tm = FindFirstObjectByType<TurnManager>();
        int currentTurn = tm != null ? tm.TurnIndex : -1;

        if (!fromDialogue)
        {
            if (_triggeredEventIdsWithTurn.TryGetValue(row.id, out int recordedTurn))
            {
                if (Mathf.Abs(currentTurn - recordedTurn) <= 1) return false;
            }
            _triggeredEventIdsWithTurn[row.id] = currentTurn;
        }

        List<Student> targets = new List<Student>();

        if (passedVars != null && passedVars.TryGetValue("{target1.name}", out string tName))
        {
            var student = StudentManager.Instance?.Students.FirstOrDefault(s => s.studentName == tName);
            if (student != null) targets.Add(student);
        }
        // 특정 타겟 이름이 넘어왔을 때
        else if (!string.IsNullOrEmpty(specificTargetName) && !specificTargetName.Contains("[공지]") && specificTargetName != "sys_notice")
        {
            string parsedName = specificTargetName.Replace("student_", "");
            var student = StudentManager.Instance?.Students.FirstOrDefault(s => s.studentName == parsedName);
            if (student != null) targets.Add(student);
        }

        //자동 타겟 탐색 
        if (targets.Count == 0 && ((int)row.scope >= 2 && (int)row.scope <= 4))
        {
            targets = PickTargets(row);
            if (targets.Count == 0 && row.targetMin > 0) return false;
        }

        if (!fromDialogue) _dailyEventCount++;

        // 지속 기간 설정
        int termDays = 0;
        // 1. 대화창 변수에서 가져오기 시도
        if (passedVars != null && passedVars.TryGetValue("{term}", out string passedTerm))
            int.TryParse(passedTerm, out termDays);

        // 2. 현재 CSV에서 가져오기 (첫 공지 이벤트일 경우)
        if (termDays <= 0 && row.termMax > 0)
            termDays = UnityEngine.Random.Range(row.termMin, row.termMax + 1);

        // 3. 2번째 파생 이벤트인데 다이얼로그가 기간 변수를 잃어버렸다면? -> 아까 쥐고 있던 기간 사용!
        if (termDays <= 0 && _lastRolledTermDays > 0)
            termDays = _lastRolledTermDays;

        // 4. 다음 파생 이벤트를 위해 무조건 백업
        if (termDays > 0) _lastRolledTermDays = termDays;

        // 파생된 시스템 이벤트 이름으로 질병/부상 구분
        bool isDisease = row.name.Contains("질병");
        bool isInjury = row.name.Contains("부상");

        if ((isDisease || isInjury) && termDays <= 0) termDays = 3;

        Dictionary<string, string> textVars = new Dictionary<string, string>();
        for (int i = 0; i < targets.Count; i++)
        {
            textVars[$"{{target{i + 1}.name}}"] = targets[i].studentName;
            textVars[$"{{target{i + 1}.grade}}"] = targets[i].grade.ToString() + "학년";
        }
        textVars["{term}"] = termDays.ToString();

        void ProcessEffect(string effectId, int effectIndex)
        {
            if (string.IsNullOrEmpty(effectId) || effectId == "-" || effectId == "none") return;

            var effectTable = CachedSOData.Get<SuddenEventEffectTableSO>();
            if (effectTable != null && effectTable.TryGet(effectId.Trim(), out var effectRow))
            {
                int amount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);
                PlayerStat targetStatId = (PlayerStat)effectRow.targetMin;

                // 질병/부상일 경우 무조건 적용되도록 보정 
                if (isDisease)
                {
                    targetStatId = (PlayerStat)21;
                    if (amount <= 0) amount = 1; // 강제 적용
                }
                else if (isInjury)
                {
                    targetStatId = (PlayerStat)22;
                    if (amount <= 0) amount = 1; // 강제 적용
                }

                ApplyEffectWithCalculatedAmount(targetStatId, targets, amount, ((int)effectRow.type == 2), termDays);

                string statName = GetStatNameKorean(targetStatId);
                textVars[$"{{effect{effectIndex}.target_name}}"] = statName;
                textVars[$"{{effect{effectIndex}.amount}}"] = ((int)effectRow.type == 2) ? $"{Mathf.Abs(amount)}%" : Mathf.Abs(amount).ToString();
            }
        }

        // 이름이 부상/질병이면 강제로 적용
        bool hasEffect = !string.IsNullOrEmpty(row.effect1) && row.effect1 != "-";
        if (!hasEffect && (isDisease || isInjury))
        {
            PlayerStat forceStat = isDisease ? (PlayerStat)21 : (PlayerStat)22;
            ApplyEffectWithCalculatedAmount(forceStat, targets, 1, false, termDays);
        }
        else
        {
            ProcessEffect(row.effect1, 1);
            ProcessEffect(row.effect2, 2);
            ProcessEffect(row.effect3, 3);
        }

        ShowEventTextOrDialogue(row, targets, textVars, fromDialogue, originRoomId, targets.Count > 0);
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

        pool = pool.Where(s => !_pickedStudentsThisTurn.Contains(s.id)).ToList();
        bool isBadEvent = row.name.Contains("질병") || row.name.Contains("부상") || row.name.Contains("입원");
        if (isBadEvent)
        {
            pool = pool.Where(s => s.abnormalState == Student.AbnormalType.None).ToList();
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
        pool = pool.OrderBy(x => UnityEngine.Random.value).ToList();
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
        if (targetStat == PlayerStat.Money || targetStat == PlayerStat.Fame)
        {
            ApplyPlayerStat(targetStat, amount);
            return;
        }

        bool statIncreased = false;
        foreach (var student in targets)
        {
            int finalAmount = amount;

            // 퍼센트 계산 로직
            if (isPercentage)
            {
                int currentStat = GetStudentStatValue(student, targetStat);
                finalAmount = Mathf.RoundToInt(currentStat * (amount / 100f));
            }

            statIncreased |= ApplyStudentStat(student, targetStat, finalAmount, termDays);

            // 상태이상이 아닌 일반 버프/디버프 스탯일 때만 리스트에 보관
            if (termDays > 0 && (int)targetStat < 21)
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
        if (statIncreased) SoundManager.Instance?.PlayStatUpSfx();
    }

    private void ApplyPlayerStat(PlayerStat statType, int amount)
    {
        if (MoneyManager.Instance == null) return;
        if (statType == PlayerStat.Money) { if (amount > 0) MoneyManager.Instance.AddGold(amount); else MoneyManager.Instance.TrySpendGold(-amount); }
        else if (statType == PlayerStat.Fame) { if (amount > 0) MoneyManager.Instance.AddReputation(amount); else MoneyManager.Instance.TrySpendReputation(-amount); }
    }

    private bool ApplyStudentStat(Student student, PlayerStat statType, int amount, int termDays = 0)
    {
        int statInt = (int)statType;
        bool changed = false;

        // 상태이상(21, 22) 처리
        if (statInt == 21 || statInt == 22)
        {
            if (amount > 0)
            {
                if (statInt == 21)
                {
                    student.isTrainingBlocked = true;
                    student.abnormalState = Student.AbnormalType.Disease;
                }
                else if (statInt == 22)
                {
                    student.isTrainingBlocked = false;
                    student.abnormalState = Student.AbnormalType.Injury;
                }

                int finalTerm = termDays > 0 ? termDays : (amount > 1 ? amount : 3);
                student.abnormalRemainTurn = finalTerm;

                Debug.Log($"[상태이상 정상 적용] {student.studentName} : {student.abnormalState} ({finalTerm}일 지속)");
            }
            else // 완치
            {
                student.isTrainingBlocked = false;
                student.abnormalState = Student.AbnormalType.None;
                student.abnormalRemainTurn = 0;
            }
            changed = true;
        }
        else
        {
            // 일반 스탯 증감 (기존 로직 유지)
            switch (statType)
            {
                case PlayerStat.Condition: student.condition = Student.ClampCondition(student.condition + amount);
                    changed = true;
                    break;
                case PlayerStat.Mental: student.mental += amount;
                    changed = true;
                    break;
                case PlayerStat.Shoot: student.shoot += amount; 
                    changed = true; 
                    break;
                case PlayerStat.Speed: student.speed += amount; 
                    changed = true; 
                    break;
                case PlayerStat.Jump: student.jump += amount; 
                    changed = true; 
                    break;
                case PlayerStat.Stamina: student.stamina += amount; 
                    changed = true; 
                    break;
            }
        }

        if (changed && StudentManager.Instance != null)
            StudentManager.Instance.NotifyStudentModified(student);

        return changed;
    }


    // ==========================================
    // 턴 매니저 호출용: 기간제 효과 타이머 감소 및 원상복구 로직
    // ==========================================
    public void TickTermEffects()
    {
        int daysToSubtract = 0;
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
        if (daysToSubtract <= 0) return;
        // =======================================================
        // 1. 일반 기간제 스탯 (버프/디버프) 차감 
        // =======================================================
        for (int i = _activeTermEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeTermEffects[i];
            effect.RemainingDays -= daysToSubtract;

            if (effect.RemainingDays <= 0)
            {
                var student = StudentManager.Instance?.Students.FirstOrDefault(s => s.studentName == effect.StudentName);
                if (student != null && (int)effect.StatType < 21)
                {
                    ApplyStudentStat(student, effect.StatType, -effect.Amount);
                    StudentManager.Instance.NotifyStudentModified(student);
                }
                _activeTermEffects.RemoveAt(i);
            }
        }

        // =======================================================
        // 2. 상태이상 (입원/부상) 차감 (학생 데이터 직접 사용)
        // =======================================================
        if (StudentManager.Instance != null)
        {
            foreach (var student in StudentManager.Instance.Students)
            {
                if (student.abnormalState == Student.AbnormalType.None && student.isTrainingBlocked)
                {
                    student.isTrainingBlocked = false;
                    StudentManager.Instance.NotifyStudentModified(student);
                    Debug.Log($"[훈련 차단 해제] {student.studentName} 학생의 잘못된 훈련 차단 상태를 초기화했습니다.");
                }
                if (student.abnormalState != Student.AbnormalType.None)
                {
                    student.abnormalRemainTurn -= daysToSubtract;

                    if (student.abnormalRemainTurn <= 0)
                    {
                        student.abnormalState = Student.AbnormalType.None;
                        student.isTrainingBlocked = false;
                        student.abnormalRemainTurn = 0;

                        StudentManager.Instance.NotifyStudentModified(student);
                        Debug.Log($"[상태이상 완치] {student.studentName} 학생이 다 나았습니다");
                    }
                }
            }
        }
    }

    public void ResetDailyEventCount()
    {
        _dailyEventCount = 0;
        _pickedStudentsThisTurn.Clear();
        _lastRolledTermDays = 0;

        TurnManager tm = FindFirstObjectByType<TurnManager>();
        int currentTurn = tm != null ? tm.TurnIndex : -1;

        var keysToRemove = _triggeredEventIdsWithTurn.Where(kvp => currentTurn - kvp.Value > 2).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _triggeredEventIdsWithTurn.Remove(key);
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
        foreach (var item in _popupQueue)
        {
            if (item.roomId == roomId && item.previewText == preview)
            {
                Debug.Log($"[중복 팝업 방어] 완전히 동일한 팝업이 대기 중이므로 무시합니다: {preview}");
                return;
            }
        }

        _popupQueue.Enqueue(new EventPopupData { title = title, roomId = roomId, roomName = roomName, previewText = preview });

        if (!_isPopupShowing)
        {
            ProcessNextPopup();
        }
    }

    public void ProcessNextPopup()
    {
        _isPopupShowing = false;

        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (tm != null && (tm.CurrentPhase == GamePhase.MatchDay || tm.CurrentPhase == GamePhase.MatchInProgress))
        {
            return;
        }

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
}