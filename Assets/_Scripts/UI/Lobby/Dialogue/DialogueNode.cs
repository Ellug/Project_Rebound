using System.Collections.Generic;

// 대화 노드 모델
public class DialogueNode
{
    public string NodeId;                 // 대화 인덱스 (예: index_000)
    public MessageSenderType SenderType;  // 화자 (나 or 상대방)
    public string DialogueText;           // 대사 내용

    public string NextNodeId;             // 다음으로 넘어갈 노드 ID (EOS면 종료)
    public List<DialogueChoice> Choices;  // 선택지가 있을 경우 사용

    public string TriggerSuddenEventId;   // 연결된 돌발 이벤트 ID (없으면 - 또는 null)
}

// 선택지 모델
public class DialogueChoice
{
    public string ChoiceText;             // 버튼에 표시될 텍스트
    public string NextNodeId;             // 해당 버튼 선택 시 넘어갈 노드 ID
}

// 가짜 데이터베이스
public static class DialogueDB
{
    private static Dictionary<string, DialogueNode> _nodes = new Dictionary<string, DialogueNode>();

    // 게임 시작 시 한 번만 호출
    public static void Init()
    {
        _nodes.Clear();

        // 샘플 대화 데이터 세팅
        AddNode(new DialogueNode
        {
            NodeId = "index_000",
            SenderType = MessageSenderType.Them,
            DialogueText = "쌤쌤",
            NextNodeId = "index_001"
        });

        AddNode(new DialogueNode
        {
            NodeId = "index_001",
            SenderType = MessageSenderType.Me,
            DialogueText = "?",
            NextNodeId = "index_002"
        });

        // 선택지 분기 노드
        AddNode(new DialogueNode
        {
            NodeId = "index_002",
            SenderType = MessageSenderType.Them,
            DialogueText = "오늘만 좀 봐주시면 안돼요?",
            Choices = new List<DialogueChoice>
            {
                new DialogueChoice { ChoiceText = "ㅇㅇ 봐줌", NextNodeId = "index_003_A" },
                new DialogueChoice { ChoiceText = "안돼 돌아가", NextNodeId = "index_003_B" }
            }
        });

        // A루트: 이벤트 발동 및 대화 종료
        AddNode(new DialogueNode
        {
            NodeId = "index_003_A",
            SenderType = MessageSenderType.Them,
            DialogueText = "감사합니다! 내일 뵐게요ㅋㅋ",
            NextNodeId = "EOS",
            TriggerSuddenEventId = "event_000015" // 이 시점에 스탯 감소 등 발생
        });

        // B루트: 대화 종료
        AddNode(new DialogueNode
        {
            NodeId = "index_003_B",
            SenderType = MessageSenderType.Them,
            DialogueText = "아... 넵 가겠습니다 ㅠㅠ",
            NextNodeId = "EOS",
            TriggerSuddenEventId = "-"
        });
    }

    private static void AddNode(DialogueNode node)
    {
        _nodes[node.NodeId] = node;
    }

    public static DialogueNode GetNode(string nodeId)
    {
        return _nodes.TryGetValue(nodeId, out var node) ? node : null;
    }
}