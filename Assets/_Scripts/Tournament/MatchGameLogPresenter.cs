using System;
using System.Collections.Generic;
using UnityEngine;

// 매치 로그 기록/포맷/타이핑 큐 흐름을 관리
public sealed class MatchGameLogPresenter
{
    private const string MyTeamLogColorHex = "#1E90FF";
    private const string OpponentTeamLogColorHex = "#E80000";
    private const string SystemLogColorHex = "#A3A3A3";
    private const string HighSchoolSuffix = "고등학교";

    private readonly struct PendingUiLogLine
    {
        public readonly string message;
        public readonly MatchLogStyle style;
        public readonly MatchLogCue cue;

        public PendingUiLogLine(string message, MatchLogStyle style, MatchLogCue cue)
        {
            this.message = message;
            this.style = style;
            this.cue = cue;
        }
    }

    private readonly MonoBehaviour _coroutineHost;
    private readonly MatchGameUI _matchGameUi;
    private readonly float _logTypingCharactersPerSecond;
    private readonly string _systemLogPrefix;
    private readonly Action _onQuarterEndCue;
    private readonly List<string> _logs = new(64);
    private readonly Queue<PendingUiLogLine> _pendingUiLogQueue = new();
    private readonly Queue<Action> _pendingFlowActionsAfterLogs = new();

    private TextTypewriter _logTypewriter;
    private string _mySchoolName;
    private string _opponentTeamName;

    // 저장/결과 전달용 로그 리스트 조회
    public IReadOnlyList<string> Logs => _logs;

    // 로그 프레젠터 의존성 주입
    public MatchGameLogPresenter(
        MonoBehaviour coroutineHost,
        MatchGameUI matchGameUi,
        float logTypingCharactersPerSecond,
        string systemLogPrefix,
        Action onQuarterEndCue)
    {
        _coroutineHost = coroutineHost;
        _matchGameUi = matchGameUi;
        _logTypingCharactersPerSecond = Mathf.Max(0f, logTypingCharactersPerSecond);
        _systemLogPrefix = string.IsNullOrWhiteSpace(systemLogPrefix) ? "[SYSTEM]" : systemLogPrefix;
        _onQuarterEndCue = onQuarterEndCue;
        _logTypewriter = new TextTypewriter(_coroutineHost);
    }

    // 팀명 토큰 치환 기준 문자열 설정
    public void SetTeamNames(string mySchoolName, string opponentTeamName)
    {
        _mySchoolName = mySchoolName;
        _opponentTeamName = opponentTeamName;
    }

    // 로그/타이핑 대기 상태 초기화
    public void ClearAll()
    {
        _logs.Clear();
        ResetUiLogTypingState();
    }

    // 세이브 로그 복원 후 UI에 즉시 반영
    public void RestoreLogsAndRender(IReadOnlyList<string> savedLogs)
    {
        _logs.Clear();

        if (savedLogs != null)
            _logs.AddRange(savedLogs);

        ResetUiLogTypingState();

        if (_matchGameUi == null) return;

        for (int i = 0; i < _logs.Count; i++)
        {
            string log = _logs[i];
            MatchLogStyle style = ResolveStyleForRestoredLog(log);
            AppendLogToUi(log, IsSystemLog(log), style, instant: true);
        }
    }

    // 쿼터 로그 목록을 순서대로 기록
    public void WriteQuarterLogs(IReadOnlyList<QuarterLogEntry> logs)
    {
        if (logs == null) return;

        for (int i = 0; i < logs.Count; i++)
        {
            QuarterLogEntry logEntry = logs[i];
            if (logEntry.isSystem)
                WriteSystemLog(logEntry.message);
            else
                WriteLog(logEntry.message, logEntry.style, logEntry.cue);
        }
    }

    // 일반 로그 기록 + UI 출력
    public void WriteLog(string message, MatchLogStyle style = MatchLogStyle.Normal, MatchLogCue cue = MatchLogCue.None)
    {
        string messageForRecord = message;
        if (style == MatchLogStyle.Announcement && !HasBoldMarkup(messageForRecord))
            messageForRecord = WrapBold(messageForRecord);

        _logs.Add(messageForRecord);
        Debug.Log(messageForRecord);
        AppendLogToUi(messageForRecord, isSystem: false, style, instant: false, cue);
    }

    // 시스템 로그 기록 + UI 출력
    public void WriteSystemLog(string message)
    {
        string formatted = $"{_systemLogPrefix} {message}";
        _logs.Add(formatted);
        Debug.Log(formatted);
        AppendLogToUi(formatted, isSystem: true, MatchLogStyle.Normal);
    }

    // 진행 버튼 입력 시 타이핑 완료 처리
    public bool TryCompletePendingUiLogs()
    {
        bool hasPendingLogs = (_logTypewriter != null && _logTypewriter.IsTyping) || _pendingUiLogQueue.Count > 0;
        if (!hasPendingLogs) return false;

        _logTypewriter?.StopTyping();
        _matchGameUi?.RevealMatchLogsInstantly();
        FlushPendingUiLogsInstantly();
        TryRunPendingFlowActionsAfterUiLogs();
        return true;
    }

    // 로그 완료 시점에 실행할 후속 액션 적재
    public void EnqueueFlowActionAfterUiLogs(Action action)
    {
        if (action == null) return;

        _pendingFlowActionsAfterLogs.Enqueue(action);
        TryRunPendingFlowActionsAfterUiLogs();
    }

    // 로그 큐가 비었으면 후속 액션 실행
    public bool TryRunPendingFlowActionsAfterUiLogs()
    {
        if ((_logTypewriter != null && _logTypewriter.IsTyping) || _pendingUiLogQueue.Count > 0)
            return false;

        bool hasExecuted = false;

        while (_pendingFlowActionsAfterLogs.Count > 0)
        {
            Action action = _pendingFlowActionsAfterLogs.Dequeue();
            action?.Invoke();
            hasExecuted = true;

            if ((_logTypewriter != null && _logTypewriter.IsTyping) || _pendingUiLogQueue.Count > 0)
                return true;
        }

        return hasExecuted;
    }

    // 매치 종료/복원 시 타이핑 상태 초기화
    public void ResetUiLogTypingState()
    {
        _pendingUiLogQueue.Clear();
        _pendingFlowActionsAfterLogs.Clear();
        _logTypewriter?.StopTyping();
    }

    // 로그 포맷 후 즉시 출력 또는 타이핑 큐 분기
    private void AppendLogToUi(string rawMessage, bool isSystem, MatchLogStyle style, bool instant = false, MatchLogCue cue = MatchLogCue.None)
    {
        if (_matchGameUi == null) return;

        string formattedMessage = FormatLogForUi(rawMessage, isSystem, style);

        if (instant)
        {
            if (cue == MatchLogCue.QuarterEndScoreLine)
                PlayQuarterEndCue();

            _matchGameUi.AppendMatchLog(formattedMessage);
            return;
        }

        QueueUiLogForTyping(formattedMessage, style, cue);
    }

    // 로그 타이핑 큐 적재 진입점
    private void QueueUiLogForTyping(string formattedMessage, MatchLogStyle style, MatchLogCue cue)
    {
        if (_logTypingCharactersPerSecond <= 0f)
        {
            if (cue == MatchLogCue.QuarterEndScoreLine)
                PlayQuarterEndCue();

            _matchGameUi.AppendMatchLog(formattedMessage);
            return;
        }

        _pendingUiLogQueue.Enqueue(new PendingUiLogLine(formattedMessage, style, cue));
        ProcessPendingUiLogs();
    }

    // 큐 로그 순차 타이핑 처리
    private void ProcessPendingUiLogs()
    {
        if (_logTypewriter == null)
            _logTypewriter = new TextTypewriter(_coroutineHost);

        if (_logTypewriter.IsTyping || _pendingUiLogQueue.Count == 0)
            return;

        while (_pendingUiLogQueue.Count > 0)
        {
            PendingUiLogLine queuedLog = _pendingUiLogQueue.Dequeue();
            string logLine = queuedLog.message;

            if (queuedLog.cue == MatchLogCue.QuarterEndScoreLine)
                PlayQuarterEndCue();

            // 구분선 타이핑 예외
            if (IsTypingExcludedLogLine(queuedLog.style, logLine))
            {
                _matchGameUi.AppendMatchLog(logLine);
                continue;
            }

            int targetCharacters = _matchGameUi.AppendMatchLogForTyping(logLine, out int startCharacters);
            if (targetCharacters <= startCharacters)
            {
                _matchGameUi.RevealMatchLogsInstantly();
                continue;
            }

            _logTypewriter.StartTyping(
                startCharacters,
                targetCharacters,
                _logTypingCharactersPerSecond,
                _matchGameUi.SetMatchLogVisibleCharacters,
                _matchGameUi.RevealMatchLogsInstantly,
                ProcessPendingUiLogs);
            return;
        }

        TryRunPendingFlowActionsAfterUiLogs();
    }

    // 큐 잔여 로그 즉시 출력
    private void FlushPendingUiLogsInstantly()
    {
        if (_matchGameUi == null)
        {
            _pendingUiLogQueue.Clear();
            return;
        }

        while (_pendingUiLogQueue.Count > 0)
        {
            PendingUiLogLine queuedLog = _pendingUiLogQueue.Dequeue();
            if (queuedLog.cue == MatchLogCue.QuarterEndScoreLine)
                PlayQuarterEndCue();

            _matchGameUi.AppendMatchLog(queuedLog.message);
        }
    }

    // 타이핑 예외 로그 판별
    private static bool IsTypingExcludedLogLine(MatchLogStyle style, string logLine)
    {
        if (style == MatchLogStyle.Divider)
            return true;

        if (string.IsNullOrWhiteSpace(logLine))
            return false;

        return string.Equals(logLine.Trim(), MatchGameLogTokens.Divider, StringComparison.Ordinal);
    }

    // 쿼터 종료 연출 콜백 실행
    private void PlayQuarterEndCue()
    {
        _onQuarterEndCue?.Invoke();
    }

    // 팀명 강조/시스템 컬러 적용
    private string FormatLogForUi(string rawMessage, bool isSystem, MatchLogStyle style)
    {
        if (string.IsNullOrEmpty(rawMessage))
            return rawMessage;

        string formatted = rawMessage;
        formatted = ReplaceTeamNameToken(formatted, _mySchoolName, isSystem ? null : MyTeamLogColorHex);
        formatted = ReplaceTeamNameToken(formatted, _opponentTeamName, isSystem ? null : OpponentTeamLogColorHex);

        if (isSystem)
            formatted = WrapColor(formatted, SystemLogColorHex);

        if (!isSystem && style == MatchLogStyle.Announcement && !HasBoldMarkup(formatted))
            formatted = WrapBold(formatted);

        return formatted;
    }

    private static MatchLogStyle ResolveStyleForRestoredLog(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
            return MatchLogStyle.Normal;

        string trimmed = rawMessage.Trim();
        if (string.Equals(trimmed, MatchGameLogTokens.Divider, StringComparison.Ordinal))
            return MatchLogStyle.Divider;

        if (HasBoldMarkup(trimmed))
            return MatchLogStyle.Announcement;

        return MatchLogStyle.Normal;
    }

    // 팀명 토큰 치환 + 색상 래핑
    private static string ReplaceTeamNameToken(string message, string fullTeamName, string colorHex)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(fullTeamName))
            return message;

        string shortTeamName = ShortenSchoolName(fullTeamName);
        string plainTag = $"[{shortTeamName}]";
        string replacement = string.IsNullOrEmpty(colorHex) ? plainTag : WrapColor(plainTag, colorHex);

        message = message.Replace($"[{fullTeamName}]", replacement);
        message = message.Replace(fullTeamName, replacement);

        if (!string.IsNullOrEmpty(colorHex))
            message = message.Replace(plainTag, replacement);

        return message;
    }

    // 학교명 접미사 제거 단축 표기
    private static string ShortenSchoolName(string schoolName)
    {
        if (string.IsNullOrWhiteSpace(schoolName))
            return string.Empty;

        string trimmed = schoolName.Trim();
        if (trimmed.EndsWith(HighSchoolSuffix, StringComparison.Ordinal))
        {
            string withoutSuffix = trimmed[..^HighSchoolSuffix.Length].Trim();
            if (!string.IsNullOrEmpty(withoutSuffix))
                return withoutSuffix;
        }

        return trimmed;
    }

    // TMP 컬러 태그 래핑
    private static string WrapColor(string message, string colorHex)
    {
        return $"<color={colorHex}>{message}</color>";
    }

    private static string WrapBold(string message)
    {
        return $"<b>{message}</b>";
    }

    private static bool HasBoldMarkup(string message)
    {
        return !string.IsNullOrEmpty(message) &&
               message.IndexOf("<b>", StringComparison.OrdinalIgnoreCase) >= 0 &&
               message.IndexOf("</b>", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // 시스템 로그 프리픽스 판별
    private bool IsSystemLog(string message)
    {
        return !string.IsNullOrEmpty(message) && message.StartsWith(_systemLogPrefix, StringComparison.Ordinal);
    }
}
