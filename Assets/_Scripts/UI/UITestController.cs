using UnityEngine;

// UI 테스트 씬에서 입력을 받아 팝업을 띄우는 컨트롤러
public class UITestController : MonoBehaviour
{
    [SerializeField] private TestPopup _popupPrefab;

    private int _popupCount = 0;

    // 인스펙터 버튼 연결용
    public void OnClickOpenPopup()
    {
        _popupCount++;

        // UIManager를 통해 팝업 생성 및 스택 추가
        TestPopup popup = UIManager.Instance.Show(_popupPrefab);

        // 팝업 내용 설정
        popup.SetTitle("Test Popup " + _popupCount);
    }

    public void OnClickCloseTop()
    {
        UIManager.Instance.CloseTop();
    }
}