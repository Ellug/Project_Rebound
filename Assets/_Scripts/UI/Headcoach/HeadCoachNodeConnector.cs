using UnityEngine;
using UnityEngine.UI;

// 두 노드 슬롯 사이를 잇는 세로 연결선 UI
// Inspector에서 fromNodeId(선행 노드)와 toNodeId(현재 노드)를 지정
// RebuildLane()에서 해당 노드 쌍의 해금 상태에 따라 색상만 갱신
public class HeadCoachNodeConnector : MonoBehaviour
{
    [Header("연결 노드 설정")]
    public int fromNodeId;  // 선행 노드 id
    public int toNodeId;    // 현재 노드 id

    [Header("연결선 색상")]
    [SerializeField] private Color _connectedColor = new(0.8f, 0.8f, 0.8f, 1f);    // 두 노드 모두 해금 시
    [SerializeField] private Color _disconnectedColor = new(0.4f, 0.4f, 0.4f, 1f); // 미해금 시

    private Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
        if (_image == null)
            _image = GetComponentInChildren<Image>();
    }

    // 해금 상태에 따라 연결선 색상만 갱신
    public void Refresh(bool isConnected)
    {
        if (_image == null) return;
        _image.color = isConnected ? _connectedColor : _disconnectedColor;
    }
}