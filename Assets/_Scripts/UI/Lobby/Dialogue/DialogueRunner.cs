using UnityEngine;
using System.Collections;
using System.Linq;

public class DialogueRunner : Singleton<DialogueRunner>
{
    [Header("Settings")]
    [SerializeField] private float _typingDelay = 1.0f; // 타자 치는 딜레이 시간

    // 외부에서 특정 대화를 시작할 때 호출
    public void PlayDialogue(string roomId, string roomName, string diagId, string startNodeId = "index_000")
    {
        StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, startNodeId));
    }

    private IEnumerator ProcessNodeRoutine(string roomId, string roomName, string diagId, string nodeId)
    {
        // 1. 대화 종료 조건
        if (string.IsNullOrEmpty(nodeId) || nodeId == "EOS" || nodeId == "END" || nodeId == "-")
            yield break;

        var flowTable = CachedSOData.Get<SuddenEventMsgTableSO>();
        var textTable = CachedSOData.Get<SuddenEventMsgTextTableSO>();

        if (flowTable == null || textTable == null)
        {
            Debug.LogError("[DialogueRunner] CachedSOData에서 다이얼로그 테이블을 찾을 수 없습니다.");
            yield break;
        }

        // 2. 현재 흐름 노드 찾기

        var row = flowTable.Rows.FirstOrDefault(r => r.iD == diagId && r.messageIndex == nodeId);

        if (row == null && nodeId == "index_000")
        {
            row = flowTable.Rows.FirstOrDefault(r => r.iD == diagId);
            if (row != null)
            {
                Debug.Log($"[DialogueRunner] {diagId}의 index_000이 없어 {row.messageIndex}부터 시작합니다");
            }
        }

        if (row == null)
        {
            Debug.LogWarning($"[DialogueRunner] 노드를 찾을 수 없습니다: {diagId} / {nodeId}");
            yield break;
        }

        // 3. 돌발 이벤트(스탯 변화) 연동
        if (!string.IsNullOrEmpty(row.suddenEvent) && row.suddenEvent != "-")
        {
            if (SuddenEventManager.Instance != null)
                SuddenEventManager.Instance.ExecuteEventById(row.suddenEvent);
        }

        // 4. 대사 및 화자 세팅
        string messageText = "";
        MessageSenderType senderType = MessageSenderType.Them;

        var textRow = textTable.Rows.FirstOrDefault(r => r.iD == row.messageDialogue);

        if (textRow == null)
        {
            string fallbackId = row.messageDialogue.Replace("text_", "text_diag_");
            textRow = textTable.Rows.FirstOrDefault(r => r.iD == fallbackId);
        }


        if (textRow != null)
        {
            messageText = textRow.dialogue;
            if (textRow.speaker.ToLower() == "player")
                senderType = MessageSenderType.Me; // 내가 보내는 메시지 (우측 정렬)
        }

        // 5. 선택지 분기 노드
        if (row.isChoice)
        {
            ChatMessage choiceMsg = new ChatMessage(MessageSenderType.Them, "", MessageEventType.Choice);

            void AddChoice(string choiceTextId, string choiceNextId)
            {
                if (!string.IsNullOrEmpty(choiceTextId) && choiceTextId != "-")
                {
                    string btnText = choiceTextId;
                    var choiceTextRow = textTable.Rows.FirstOrDefault(r => r.iD == choiceTextId);
                    if (choiceTextRow != null) btnText = choiceTextRow.dialogue;

                    choiceMsg.Choices.Add(new ChoiceOption
                    {
                        Text = btnText,
                        OnSelected = () => StartCoroutine(DelayAndPlayNext(roomId, roomName, diagId, choiceNextId))
                    });
                }
            }

            AddChoice(row.choice1Dialogue, row.choice1Next);
            AddChoice(row.choice2Dialogue, row.choice2Next);
            AddChoice(row.choice3Dialogue, row.choice3Next);

            MessengerManager.Instance.ReceiveMessage(roomId, roomName, choiceMsg);
        }
        // 6. 일반 대화 노드
        else
        {
            ChatMessage normalMsg = new ChatMessage(senderType, messageText, MessageEventType.NormalText);
            MessengerManager.Instance.ReceiveMessage(roomId, roomName, normalMsg);
            // 플레이어가 들어올 때까지 대기
            yield return new WaitUntil(() => MessengerManager.Instance.CurrentViewingRoomId == roomId);

            // 타이핑 딜레이
            if (senderType == MessageSenderType.Them) yield return new WaitForSeconds(_typingDelay);
            else yield return new WaitForSeconds(_typingDelay * 0.5f);

            StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, row.next));
        }
    }

    private IEnumerator DelayAndPlayNext(string roomId, string roomName, string diagId, string nextNodeId)
    {
        yield return new WaitForSeconds(_typingDelay);
        StartCoroutine(ProcessNodeRoutine(roomId, roomName, diagId, nextNodeId));
    }
}