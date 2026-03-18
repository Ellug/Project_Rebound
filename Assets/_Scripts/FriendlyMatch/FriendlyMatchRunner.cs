using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FriendlyMatchRunner : Singleton<FriendlyMatchRunner>
{
    [SerializeField] private float _typingDelay = 1.0f;
    private HashSet<string> _skippedRooms = new HashSet<string>();

    public void SkipRoom(string roomId)
    {
        if (!_skippedRooms.Contains(roomId)) _skippedRooms.Add(roomId);
    }

    public void PlayDialogue(string roomId, string roomName, string diagId, string startNodeId = "index_001", Dictionary<string, string> textVars = null)
    {
        StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, startNodeId, textVars));
    }

    private IEnumerator ProcessNodeRoutine(string roomId, string roomName, string diagId, string nodeId, Dictionary<string, string> textVars)
    {
        if (string.IsNullOrEmpty(nodeId) || nodeId == "EOS" || nodeId == "END" || nodeId == "-")
        {
            if (_skippedRooms.Contains(roomId)) _skippedRooms.Remove(roomId);
            yield break;
        }

        var flowTable = CachedSOData.Get<FriendlyMatchScheduleMsgTableSO>();
        var textTable = CachedSOData.Get<FriendlyMatchScheduleMsgTextTableSO>();
        if (flowTable == null || textTable == null) yield break;

        var row = flowTable.Rows.FirstOrDefault(r => r.iD == diagId && r.messageIndex == nodeId);
        if (row == null) yield break;

        int branchType = row.branchType;
        string messageText = "";
        MessageSenderType senderType = MessageSenderType.Them;
        MessageEventType eventType = MessageEventType.NormalText;

        var textRow = textTable.Rows.FirstOrDefault(r => r.id == row.messageDialogue);

        // accept / decline 중 하나를 랜덤으로 뽑아서 출력
        if (row.messageDialogue == "text_diag_schedule_accept")
        {
            var acceptRows = textTable.Rows.Where(r => r.type == "accept").ToList();
            if (acceptRows.Count > 0) textRow = acceptRows[Random.Range(0, acceptRows.Count)];
        }
        else if (row.messageDialogue == "text_diag_schedule_decline")
        {
            var declineRows = textTable.Rows.Where(r => r.type == "decline").ToList();
            if (declineRows.Count > 0) textRow = declineRows[Random.Range(0, declineRows.Count)];
        }

        if (textRow != null)
        {
            messageText = textRow.dialogue;
            string speaker = textRow.speaker.ToLower();

            if (speaker == "right" || speaker == "player") senderType = MessageSenderType.Me;
            else if (speaker == "center")
            {
                senderType = MessageSenderType.Them;
                eventType = MessageEventType.System; // 중앙 정렬 시스템 메시지
            }
            else senderType = MessageSenderType.Them;

            if (textVars != null)
                foreach (var kvp in textVars) messageText = messageText.Replace(kvp.Key, kvp.Value);
        }

        bool isSkipping = _skippedRooms.Contains(roomId);

        // 분기 3: 선택지
        if (branchType == 3)
        {
            if (isSkipping)
            {
                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.choice1Next, textVars));
                yield break;
            }

            bool choiceMade = false;
            string nextNodeToPlay = "";
            ChatMessage choiceMsg = new ChatMessage(MessageSenderType.Them, "", MessageEventType.Choice);

            void AddChoice(string choiceTextId, string choiceNextId, int choiceIdx)
            {
                if (!string.IsNullOrEmpty(choiceTextId) && choiceTextId != "-")
                {
                    string btnText = choiceTextId.Trim();
                    var choiceTextRow = textTable.Rows.FirstOrDefault(r => r.id == btnText);
                    if (choiceTextRow != null) btnText = choiceTextRow.dialogue;
                    if (textVars != null) foreach (var kvp in textVars) btnText = btnText.Replace(kvp.Key, kvp.Value);

                    choiceMsg.Choices.Add(new ChoiceOption
                    {
                        Text = btnText,
                        OnSelected = () => {
                            choiceMade = true;
                            nextNodeToPlay = choiceNextId;
                            // 내가 고른 날짜를 {date_choice}에 저장하여 다음 대사에서 출력되게 함
                            if (textVars != null && textVars.ContainsKey($"{{date{choiceIdx}}}"))
                                textVars["{date_choice}"] = textVars[$"{{date{choiceIdx}}}"];
                        }
                    });
                }
            }

            AddChoice(row.choice1Dialogue, row.choice1Next, 1);
            AddChoice(row.choice2Dialogue, row.choice2Next, 2);
            AddChoice(row.choice3Dialogue, row.choice3Next, 3);

            MessengerManager.Instance.ReceiveMessage(roomId, roomName, choiceMsg);
            yield return new WaitUntil(() => choiceMade || _skippedRooms.Contains(roomId));

            if (_skippedRooms.Contains(roomId) && !choiceMade)
            {
                if (MessengerManager.Instance != null)
                {
                    var room = MessengerManager.Instance.GetRoom(roomId);
                    if (room != null) room.HasUnread = false;
                }
                if (textVars != null && textVars.ContainsKey("{date1}")) textVars["{date_choice}"] = textVars["{date1}"];

                choiceMsg.SelectedChoiceIndex = 0;
                string autoReplyText = choiceMsg.Choices.Count > 0 ? choiceMsg.Choices[0].Text : "선택";
                ChatMessage autoReply = new ChatMessage(MessageSenderType.Me, autoReplyText);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, autoReply);

                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.choice1Next, textVars));
            }
            else StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, nextNodeToPlay, textVars));
        }
        else // 일반 대화 및 확률 분기
        {
            ChatMessage normalMsg = new ChatMessage(senderType, messageText, eventType);
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

            string nextNodeToPlay = row.next;

            // 확률 판정을 하여 수락 / 거절 결정
            if (branchType == 1)
            {
                bool isAccepted = Random.value > 0.5f; // 50% 확률 (추후 명성치로 조절 가능)
                nextNodeToPlay = isAccepted ? row.choice1Next : row.choice2Next;
            }

            StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, nextNodeToPlay, textVars));
        }
    }
}