using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class FriendlyMatchRunner : Singleton<FriendlyMatchRunner>
{
    [SerializeField] private float _typingDelay = 1.0f;
    private HashSet<string> _skippedRooms = new HashSet<string>();

    public void SkipRoom(string roomId)
    {
        if (!_skippedRooms.Contains(roomId)) _skippedRooms.Add(roomId);
    }

    public void PlayDialogue(string roomId, string roomName, string diagId, string startNodeId = "index_001", Dictionary<string, string> textVars = null, int msgStartIndex = 0)
    {
        _skippedRooms.Remove(roomId);
        StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, startNodeId, textVars, msgStartIndex));
    }
    private IEnumerator WaitWithSkip(float delay, string roomId)
    {
        float timer = 0f;
        while (timer < delay && !_skippedRooms.Contains(roomId))
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ProcessNodeRoutine(string roomId, string roomName, string diagId, string nodeId, Dictionary<string, string> textVars, int msgStartIndex)
    {
        // =================================================================================
        // 1. 대화 종료 및 시스템 메시지 출력 로직
        // =================================================================================
        if (string.IsNullOrEmpty(nodeId) || nodeId == "EOS" || nodeId == "END" || nodeId == "-")
        {
            if (_skippedRooms.Contains(roomId)) _skippedRooms.Remove(roomId);

            // 상대방이 수락했는지 여부와 선택한 날짜 가져오기
            bool isAccepted = textVars != null && textVars.ContainsKey("{is_accepted}") && textVars["{is_accepted}"] == "true";
            string dateChoice = (textVars != null && textVars.ContainsKey("{date_choice}")) ? textVars["{date_choice}"] : "";

            //  시스템 메시지 띄우고 GameManager에 실제 예약 진행
            if (isAccepted && !string.IsNullOrEmpty(dateChoice))
            {
                string sysText = $"{dateChoice}에 친선전 일정이 잡혔습니다.";
                ChatMessage sysMsg1 = new ChatMessage(MessageSenderType.Them, sysText, MessageEventType.System);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, sysMsg1);

                // GameManager 연동: "3월 14일" 같은 텍스트를 파싱해서 DateTime으로 전달
                if (GameManager.Instance != null)
                {
                    TurnManager tm = FindFirstObjectByType<TurnManager>();
                    if (tm != null && tm.DateManager != null)
                    {
                        try
                        {
                            int currentYear = tm.DateManager.CurrentDate.Year;
                            string[] parts = dateChoice.Split(new char[] { '월', '일' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                int m = int.Parse(parts[0].Trim());
                                int d = int.Parse(parts[1].Trim());
                                DateTime matchDate = new DateTime(currentYear, m, d);
                                GameManager.Instance.ScheduleFriendlyMatch(matchDate, roomName);
                                SaveManager.Instance.AutoSaveByBranch("친선전 일정 설정 완료");
                            }
                        }
                        catch { Debug.LogWarning("[FriendlyMatchRunner] 날짜 파싱 실패. 형식을 확인해주세요."); }
                    }
                }
            }

            // 횟수 차감 시스템 메시지 출력
            ChatMessage sysMsg2 = new ChatMessage(MessageSenderType.Them, "친선전 신청 횟수 -1", MessageEventType.System);
            MessengerManager.Instance.ReceiveMessage(roomId, roomName, sysMsg2);

            // 로비 UI 숫자 갱신 (선택 창은 이미 닫혔지만, 메인 로비나 인박스 등에서 새로고침 필요시)
            var inbox = FindFirstObjectByType<MessengerInboxPopup>();
            if (inbox != null) inbox.RefreshFriendlyMatchUI();

            yield break;
        }

        // =================================================================================
        // 2. 대화 노드 파싱
        // =================================================================================
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
        if (textRow != null)
        {
            messageText = textRow.dialogue;
            string speaker = textRow.speaker.ToLower();

            if (speaker == "right" || speaker == "player") senderType = MessageSenderType.Me;
            else if (speaker == "center")
            {
                senderType = MessageSenderType.Them;
                eventType = MessageEventType.System;
            }
            else senderType = MessageSenderType.Them;

            if (textVars != null)
                foreach (var kvp in textVars) messageText = messageText.Replace(kvp.Key, kvp.Value);
        }

        bool isSkipping = _skippedRooms.Contains(roomId);

        bool hasChoices = !string.IsNullOrEmpty(row.choice1Dialogue) && row.choice1Dialogue != "-";
        if (branchType == 3 && !hasChoices)
        {
            branchType = 0;
        }

        // =================================================================================
        // 분기 3: 선택지
        // =================================================================================
        if (branchType == 3)
        {
            if (!string.IsNullOrEmpty(messageText) && messageText != "-")
            {
                ChatMessage preNormalMsg = new ChatMessage(senderType, messageText, eventType);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, preNormalMsg);
                yield return WaitWithSkip(_typingDelay, roomId);
            }

            if (_skippedRooms.Contains(roomId))
            {
                AbortFriendlyMatch(roomId, msgStartIndex);
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
                            // 내가 고른 날짜를 {date_choice}에 저장
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
                AbortFriendlyMatch(roomId, msgStartIndex);
                yield break;
            }
            else
            { 
                StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, nextNodeToPlay, textVars, msgStartIndex));
            }
        }
        // =================================================================================
        // 분기 1: 확률 결과 판정 (수락/거절)
        // =================================================================================
        else if (branchType == 1)
        {
            // 1. 현재 노드의 대사 먼저 출력
            if (!string.IsNullOrEmpty(messageText) && messageText != "-")
            {
                ChatMessage normalMsg = new ChatMessage(senderType, messageText, eventType);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, normalMsg);

                yield return WaitWithSkip(_typingDelay, roomId);
            }

            // 2. 확률 판정 후 수락/거절 텍스트 무작위 가져오기
            bool isAccepted = Random.value > 0.5f; // 임시로 50% 수락 확률
            if (textVars != null) textVars["{is_accepted}"] = isAccepted ? "true" : "false";

            string typeToFetch = isAccepted ? "accept" : "decline";
            var availableRows = textTable.Rows.Where(r => r.type == typeToFetch).ToList();

            // 3. 수락/거절 대사 출력
            if (availableRows.Count > 0)
            {
                var randomTextRow = availableRows[Random.Range(0, availableRows.Count)];
                string replyText = randomTextRow.dialogue;
                if (textVars != null) foreach (var kvp in textVars) replyText = replyText.Replace(kvp.Key, kvp.Value);

                ChatMessage replyMsg = new ChatMessage(MessageSenderType.Them, replyText, MessageEventType.NormalText);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, replyMsg);

                yield return WaitWithSkip(_typingDelay, roomId);
            }

            // 4. 다음 노드로 넘어가기
            string nextNodeToPlay = isAccepted ? row.choice1Next : row.choice2Next;
            StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, nextNodeToPlay, textVars, msgStartIndex));
        }
        // =================================================================================
        // 분기 0 또는 2: 일반 대화
        // =================================================================================
        else
        {
            if (!string.IsNullOrEmpty(messageText) && messageText != "-")
            {
                ChatMessage normalMsg = new ChatMessage(senderType, messageText, eventType);
                MessengerManager.Instance.ReceiveMessage(roomId, roomName, normalMsg);

                float delay = senderType == MessageSenderType.Them ? _typingDelay : _typingDelay * 0.5f;
                yield return WaitWithSkip(delay, roomId);
            }

            StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.next, textVars, msgStartIndex));
        }
    }
    private void AbortFriendlyMatch(string roomId, int msgStartIndex)
    {
        if (FriendlyMatchManager.Instance != null)
        {
            FriendlyMatchManager.Instance.RollbackApplyCount();
        }

        if (MessengerManager.Instance != null)
        {
            var room = MessengerManager.Instance.GetRoom(roomId);
            if (room != null && room.Messages.Count > msgStartIndex)
            {
                
                // 방의 전체 메시지 개수에서 msgStartIndex 빼고 지움
                int countToRemove = room.Messages.Count - msgStartIndex;
                room.Messages.RemoveRange(msgStartIndex, countToRemove);

                // UI 갱신을 위해 안읽음 강제 트리거
                room.HasUnread = true;
                MessengerManager.Instance.MarkAsRead(roomId);
            }
        }

        var selectPopup = UnityEngine.Object.FindFirstObjectByType<FriendlyMatchSelectPopup>(FindObjectsInactive.Exclude);
        if (selectPopup != null)
        {
            selectPopup.UpdateMatchCountUI();
        }

        if (_skippedRooms.Contains(roomId))
        {
            _skippedRooms.Remove(roomId);
        }

        Debug.Log("[FriendlyMatchRunner] 친선전 신청이 도중 취소되어 이번 달 채팅 로그만 롤백되었습니다.");
    }
}