using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueRunner : Singleton<DialogueRunner>
{
    [Header("Settings")]
    [SerializeField] private float _typingDelay = 1.0f;

    private HashSet<string> _activeRoutines = new HashSet<string>();

    private HashSet<string> _skippedRooms = new HashSet<string>();

    public void SkipRoom(string roomId)
    {
        if (!_skippedRooms.Contains(roomId))
        {
            _skippedRooms.Add(roomId);
        }
    }

    public void PlayDialogue(string roomId, string roomName, string diagId, string startNodeId = "index_000", Dictionary<string, string> textVars = null, string systemMsgContent = "")
    {
        if (_activeRoutines.Contains(roomId)) return;
        _activeRoutines.Add(roomId);

        StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, startNodeId, textVars, systemMsgContent));
    }

    private IEnumerator ProcessNodeRoutine(string roomId, string roomName, string diagId, string nodeId, Dictionary<string, string> textVars, string systemMsgContent)
    {
        // 1. 대화 종료 조건
        if (string.IsNullOrEmpty(nodeId) || nodeId == "EOS" || nodeId == "END" || nodeId == "-")
        {
            _activeRoutines.Remove(roomId);

            if (_skippedRooms.Contains(roomId))
            {
                _skippedRooms.Remove(roomId);
            }
            if (!string.IsNullOrEmpty(systemMsgContent) && MessengerManager.Instance != null)
            {
                ChatMessage sysMsg = new ChatMessage(MessageSenderType.Them, systemMsgContent, MessageEventType.System);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, sysMsg);
            }

            if (_skippedRooms.Contains(roomId)) _skippedRooms.Remove(roomId);
            yield break;
        }

        var flowTable = CachedSOData.Get<SuddenEventMsgTableSO>();
        var textTable = CachedSOData.Get<SuddenEventMsgTextTableSO>();
        if (flowTable == null || textTable == null) yield break;

        var row = flowTable.Rows.FirstOrDefault(r => r.iD == diagId && r.messageIndex == nodeId);
        if (row == null && nodeId == "index_000") row = flowTable.Rows.FirstOrDefault(r => r.iD == diagId);
        if (row == null) yield break;

        if (!string.IsNullOrEmpty(row.suddenEvent) && row.suddenEvent != "-")
        {
            if (SuddenEventManager.Instance != null) SuddenEventManager.Instance.ExecuteEventById(row.suddenEvent);
            SuddenEventManager.Instance.ExecuteEventById(row.suddenEvent, roomName, true);
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
            if (isSkipping)
            {
                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.choice1Next, textVars, systemMsgContent));
                yield break;
            }

            bool choiceMade = false;
            string nextNodeToPlay = "";

            ChatMessage choiceMsg = new ChatMessage(MessageSenderType.Them, "", MessageEventType.Choice);
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
                        // 유저가 직접 눌렀을 때의 신호
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

            // 메신저에 일단 선택지 띄우기
            MessengerManager.Instance.ReceiveMessage(roomId, roomName, choiceMsg);

            // 유저가 선택지를 누르거나, 취소를 눌러 스킵될 때까지 여기서 대기
            yield return new WaitUntil(() => choiceMade || _skippedRooms.Contains(roomId));

            // 만약 대기하다가 유저가 팝업에서 취소 눌러 스킵 시
            if (_skippedRooms.Contains(roomId) && !choiceMade)
            {
                // 1) 톡방 빨간점 지우기
                if (MessengerManager.Instance != null)
                {
                    var room = MessengerManager.Instance.GetRoom(roomId);
                    if (room != null) room.HasUnread = false;
                }

                // 2) 내가 1번 선택지를 누른 것처럼 대화 내역에 자연스럽게 남겨주기
                choiceMsg.SelectedChoiceIndex = 0;
                string autoReplyText = choiceMsg.Choices.Count > 0 ? choiceMsg.Choices[0].Text : "선택";
                ChatMessage autoReply = new ChatMessage(MessageSenderType.Me, autoReplyText);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, autoReply);

                // 3) 1번 선택지의 다음 노드로 즉시 스킵
                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.choice1Next, textVars, systemMsgContent));
            }
            else
            {
                // 유저가 정상적으로 눌렀을 때
                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, nextNodeToPlay, textVars, systemMsgContent));
            }
        }
        // ==========================================
        // 3. 일반 대화 노드 처리
        // ==========================================
        else
        {
            ChatMessage normalMsg = new ChatMessage(senderType, messageText, MessageEventType.NormalText);
            MessengerManager.Instance.ReceiveMessage(roomId, roomName, normalMsg);

            yield return new WaitUntil(() =>
                (MessengerManager.Instance != null && MessengerManager.Instance.CurrentViewingRoomId == roomId) ||
                _skippedRooms.Contains(roomId)
            );

            isSkipping = _skippedRooms.Contains(roomId);

            if (isSkipping && MessengerManager.Instance != null)
            {
                var room = MessengerManager.Instance.GetRoom(roomId);
                if (room != null) room.HasUnread = false;
            }

            if (!isSkipping)
            {
                if (senderType == MessageSenderType.Them) yield return new WaitForSeconds(_typingDelay);
                else yield return new WaitForSeconds(_typingDelay * 0.5f);
            }

            StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.next, textVars, systemMsgContent));
        }
    }
}