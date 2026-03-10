using System;
using System.Collections.Generic;

// StatData를 감싸는 노드 객체
// 해금 상태와 트리 연결 정보만 관리
[Serializable]
public class HeadCoachNode
{
    public HeadCoachStatData stat;
    public bool isUnlocked = false;

    // 런타임 전용 트리 연결 (직렬화 제외)
    [NonSerialized] public HeadCoachNode parent;
    [NonSerialized] public List<HeadCoachNode> children = new();
}