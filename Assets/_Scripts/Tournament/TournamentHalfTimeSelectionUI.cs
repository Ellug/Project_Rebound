using System;
using UnityEngine;

public class TournamentHalfTimeSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject _selectionPanel;

    // 세부 기능 명세가 없으므로 일단 하드코딩
    // 각 버튼을 동적으로 생성하는 것 보단 .text 로 텍스트랑 효과만 바꾸는 게 더 효율적으로 보임
    // 데이터 테이블까지 나오면 세부 개발

    // 버튼 선택 시 선택 텍스트를 전달. MatchGameManager가 구독해 로그 출력 후 경기 재개
    public event Action<string> OnSelectionMade;

    public void Open()
    {
        _selectionPanel.SetActive(true);
    }

    private void Close()
    {
        _selectionPanel.SetActive(false);
    }

    public void OnClickedButton1()
    {
        OnSelectionMade?.Invoke("상대 팀의 머리털을 다 밀어버렸다.");
        Close();
    }

    public void OnClickedButton2()
    {
        OnSelectionMade?.Invoke("빠르게 스테로이드를 주입해서 모두 슈퍼솔져가 됐다.");
        Close();
    }

    public void OnClickedButton3()
    {
        OnSelectionMade?.Invoke("심판을 5000원 주고 매수했다.");
        Close();
    }
}
