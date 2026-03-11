using UnityEngine;
using System.Collections;

// 대화 흐름을 제어하는 실행기
public class DialogueRunner : Singleton<DialogueRunner>
{
    [Header("Settings")]
    [SerializeField] private float _typingDelay = 1.0f; // 상대방이 타자 치는 딜레이 시간

    // 외부에서 영입이나 이벤트 시작 시 호출하는 진입점
    public void PlayDialogue(string roomId, string roomName, string startNodeId)
    {
        StartCoroutine(ProcessNodeRoutine(roomId, roomName, startNodeId));
    }

    private IEnumerator ProcessNodeRoutine(string roomId, string roomName, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || nodeId == "EOS")
            yield break; // 대화 완전 종료

        DialogueNode node = DialogueDB.GetNode(nodeId);
        if (node == null)
        {
            Debug.LogWarning($"[DialogueRunner] 노드를 찾을 수 없습니다: {nodeId}");
            yield break;
        }

        // 1. 돌발 이벤트 트리거 확인
        if (!string.IsNullOrEmpty(node.TriggerSuddenEventId) && node.TriggerSuddenEventId != "-")
        {
            Debug.Log($"[DialogueRunner] 돌발 이벤트 연계 발동 ID: {node.TriggerSuddenEventId}");

            // 향후 돌발 이벤트 매니저가 구현되면 아래 주석을 해제하고 연결
        }

        // 2. 선택지가 있는 노드인 경우
        if (node.Choices != null && node.Choices.Count > 0)
        {
            ChatMessage choiceMsg = new ChatMessage(node.SenderType, node.DialogueText, MessageEventType.Choice);

            foreach (var choice in node.Choices)
            {
                // 람다식 내에서 안전하게 값을 캡처하기 위해 지역 변수에 할당
                string nextNodeId = choice.NextNodeId;

                choiceMsg.Choices.Add(new ChoiceOption
                {
                    Text = choice.ChoiceText,
                    OnSelected = () =>
                    {
                        // 유저가 버튼을 누르면 상대방이 타이핑하는 딜레이를 살짝 준 뒤 다음 대화 진행
                        StartCoroutine(DelayAndPlayNext(roomId, roomName, nextNodeId));
                    }
                });
            }
            MessengerManager.Instance.ReceiveMessage(roomId, roomName, choiceMsg);
        }
        // 3. 일반 대화인 경우
        else
        {
            ChatMessage normalMsg = new ChatMessage(node.SenderType, node.DialogueText, MessageEventType.NormalText);
            MessengerManager.Instance.ReceiveMessage(roomId, roomName, normalMsg);

            // 다음 대사로 넘어가기 전에 딜레이 적용
            DialogueNode nextNode = DialogueDB.GetNode(node.NextNodeId);
            if (nextNode != null)
            {
                if (nextNode.SenderType == MessageSenderType.Them)
                {
                    // 상대방이 칠 때는 길게 대기
                    yield return new WaitForSeconds(_typingDelay);
                }
                else if (nextNode.SenderType == MessageSenderType.Me)
                {
                    // 내가 칠 때는 약간 빠르게 대기
                    yield return new WaitForSeconds(_typingDelay * 0.5f);
                }
            }

            StartCoroutine(ProcessNodeRoutine(roomId, roomName, node.NextNodeId));
        }
    }

    private IEnumerator DelayAndPlayNext(string roomId, string roomName, string nextNodeId)
    {
        yield return new WaitForSeconds(_typingDelay);
        StartCoroutine(ProcessNodeRoutine(roomId, roomName, nextNodeId));
    }
}