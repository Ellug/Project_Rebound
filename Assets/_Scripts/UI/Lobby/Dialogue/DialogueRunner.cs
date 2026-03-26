using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueRunner : Singleton<DialogueRunner>
{
    [Header("Settings")]
    [SerializeField] private float _typingDelay = 1.0f;

    private HashSet<string> _activeRoutines = new HashSet<string>();
    private HashSet<string> _skippedRooms = new HashSet<string>();

    private class PendingEventData
    {
        public string eventId;
        public Dictionary<string, string> textVars;
    }

    // 이벤트를 읽자마자 실행하지 않고, END가 뜰 때까지 잠시 품고 있을 변수
    private Dictionary<string, PendingEventData> _pendingEvents = new Dictionary<string, PendingEventData>();

    public void SkipRoom(string roomId)
    {
        if (!_skippedRooms.Contains(roomId))
        {
            _skippedRooms.Add(roomId);
        }
    }

    public void PlayDialogue(string roomId, string roomName, string diagId, string startNodeId = "index_000", Dictionary<string, string> textVars = null, string systemMsgContent = "", DateTime? firstMsgDate = null)
    {
        if (_activeRoutines.Contains(roomId)) return;
        _activeRoutines.Add(roomId);

        // 대화가 새로 시작될 때 스킵 상태 초기화
        _skippedRooms.Remove(roomId);

        StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, startNodeId, textVars, systemMsgContent, firstMsgDate));
    }

    private IEnumerator ProcessNodeRoutine(string roomId, string roomName, string diagId, string nodeId, Dictionary<string, string> textVars, string systemMsgContent, DateTime? firstMsgDate = null)
    {
        // ==========================================
        // 1. 대화 종료 조건
        // ==========================================
        if (string.IsNullOrEmpty(nodeId) || nodeId == "EOS" || nodeId == "END" || nodeId == "-")
        {
            _activeRoutines.Remove(roomId);

            bool wasSkipped = _skippedRooms.Contains(roomId);

            if (_skippedRooms.Contains(roomId))
            {
                _skippedRooms.Remove(roomId);
            }

            if (!string.IsNullOrEmpty(systemMsgContent) && MessengerManager.Instance != null)
            {
                string originalViewingId = MessengerManager.Instance.CurrentViewingRoomId;
                if (wasSkipped) MessengerManager.Instance.CurrentViewingRoomId = roomId;

                ChatMessage sysMsg = new ChatMessage(MessageSenderType.Them, systemMsgContent, MessageEventType.System);
                if (firstMsgDate.HasValue) sysMsg.Timestamp = firstMsgDate.Value;
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, sysMsg);
 
                if (wasSkipped) MessengerManager.Instance.CurrentViewingRoomId = originalViewingId;
            }

            // 대화가 완전히 끝나고, 대기 중이던 파생 이벤트 실행
            if (_pendingEvents.TryGetValue(roomId, out var pendingData))
            {
                if (SuddenEventManager.Instance != null)
                {
                    SuddenEventManager.Instance.ExecuteEventById(pendingData.eventId, roomName, true, pendingData.textVars, roomId);
                }
                _pendingEvents.Remove(roomId);
            }

            // 스킵으로 닫힌 경우, 매니저의 MarkAsRead를 호출해 UI 목록창 스프라이트 갱신
            if (wasSkipped)
            {
                _skippedRooms.Remove(roomId);
                if (MessengerManager.Instance != null)
                {
                    MessengerManager.Instance.MarkAsRead(roomId);
                }
            }
            else if (_skippedRooms.Contains(roomId))
            {
                _skippedRooms.Remove(roomId);
            }
            yield break;
        }

        var flowTable = CachedSOData.Get<SuddenEventMsgTableSO>();
        var textTable = CachedSOData.Get<SuddenEventMsgTextTableSO>();
        if (flowTable == null || textTable == null) yield break;

        var row = flowTable.Rows.FirstOrDefault(r => r.iD == diagId && r.messageIndex == nodeId);
        if (row == null && nodeId == "index_000") row = flowTable.Rows.FirstOrDefault(r => r.iD == diagId);
        if (row == null) yield break;

        // 이벤트를 여기서 바로 터뜨리지 않고, _pendingEvents에 잠시 보관
        if (!string.IsNullOrEmpty(row.suddenEvent) && row.suddenEvent != "-")
        {
            _pendingEvents[roomId] = new PendingEventData { eventId = row.suddenEvent, textVars = textVars };
        }

        string messageText = "";
        MessageSenderType senderType = MessageSenderType.Them;

        var textRow = textTable.Rows.FirstOrDefault(r => r.iD == row.messageDialogue);
        if (textRow == null) textRow = textTable.Rows.FirstOrDefault(r => r.iD == row.messageDialogue.Replace("text_", "text_diag_"));

        if (textRow != null)
        {
            messageText = textRow.dialogue;
            if (textRow.speaker.ToLower() == "player") senderType = MessageSenderType.Me;
            if (textVars != null) foreach (var kvp in textVars) messageText = messageText.Replace(kvp.Key, kvp.Value);
        }

        bool isSkipping = _skippedRooms.Contains(roomId);

        // ==========================================
        // 2. 선택지 노드 처리
        // ==========================================
        if (row.isChoice)
        {
            // 선택지가 나오기 직전의 대화 출력
            if (!string.IsNullOrEmpty(messageText) && messageText != "-")
            {
                ChatMessage preMsg = new ChatMessage(senderType, messageText, MessageEventType.NormalText);
                if (firstMsgDate.HasValue) preMsg.Timestamp = firstMsgDate.Value;
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, preMsg);
                if (!isSkipping) yield return new WaitForSeconds(_typingDelay);

                firstMsgDate = null;
            }

            bool choiceMade = false;
            string nextNodeToPlay = "";
            ChatMessage choiceMsg = new ChatMessage(MessageSenderType.Them, "", MessageEventType.Choice);
            if (firstMsgDate.HasValue) choiceMsg.Timestamp = firstMsgDate.Value;

            void AddChoice(string choiceTextId, string choiceNextId)
            {
                if (!string.IsNullOrEmpty(choiceTextId) && choiceTextId != "-")
                {
                    string btnText = choiceTextId.Trim();
                    var choiceTextRow = textTable.Rows.FirstOrDefault(r => r.iD == btnText);
                    if (choiceTextRow == null) choiceTextRow = textTable.Rows.FirstOrDefault(r => r.iD == btnText.Replace("text_", "text_diag_"));
                    if (choiceTextRow != null) btnText = choiceTextRow.dialogue;
                    if (textVars != null) foreach (var kvp in textVars) btnText = btnText.Replace(kvp.Key, kvp.Value);

                    choiceMsg.Choices.Add(new ChoiceOption
                    {
                        Text = btnText,
                        OnSelected = () => {
                            choiceMade = true;
                            nextNodeToPlay = choiceNextId;
                        }
                    });
                }
            }
            AddChoice(row.choice1Dialogue, row.choice1Next);
            AddChoice(row.choice2Dialogue, row.choice2Next);
            AddChoice(row.choice3Dialogue, row.choice3Next);

            // 이미 스킵된 상태라도, 선택지 박스를 채팅 내역에 남겨주고 1번을 강제 선택
            if (isSkipping)
            {
                choiceMsg.SelectedChoiceIndex = 0; // 1번 선택지를 고른 것으로 강제 처리
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, choiceMsg);
                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.choice1Next, textVars, systemMsgContent, null));
                yield break;
            }

            // 일반 진행 시 메신저에 띄우기
            MessengerManager.Instance.ReceiveMessage(roomId, roomName, choiceMsg);

            yield return new WaitUntil(() => choiceMade || _skippedRooms.Contains(roomId));

            // 기다리다가 취소를 눌렀을 때, 1번 선택지가 남도록 처리
            if (_skippedRooms.Contains(roomId) && !choiceMade)
            {
                if (MessengerManager.Instance != null)
                {
                    MessengerManager.Instance.MarkAsRead(roomId);
                }

                choiceMsg.SelectedChoiceIndex = 0; // 1번 선택지로 강제 세팅 
                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.choice1Next, textVars, systemMsgContent));
            }
            else
            {
                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, nextNodeToPlay, textVars, systemMsgContent));
            }
        }
        // ==========================================
        // 3. 일반 대화 노드 처리
        // ==========================================
        else
        {
            ChatMessage normalMsg = new ChatMessage(senderType, messageText, MessageEventType.NormalText);
            if (firstMsgDate.HasValue) normalMsg.Timestamp = firstMsgDate.Value;
            MessengerManager.Instance.ReceiveMessage(roomId, roomName, normalMsg);

            firstMsgDate = null;

            yield return new WaitUntil(() =>
                (MessengerManager.Instance != null && MessengerManager.Instance.CurrentViewingRoomId == roomId) ||
                _skippedRooms.Contains(roomId)
            );

            isSkipping = _skippedRooms.Contains(roomId);

            if (isSkipping && MessengerManager.Instance != null)
            {
                MessengerManager.Instance.MarkAsRead(roomId);
            }

            if (!isSkipping)
            {
                if (senderType == MessageSenderType.Them) yield return new WaitForSeconds(_typingDelay);
                else yield return new WaitForSeconds(_typingDelay * 0.5f);
            }

            StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.next, textVars, systemMsgContent, null));
        }
    }
}