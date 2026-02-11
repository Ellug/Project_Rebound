using UnityEngine;

// 모든 UI 팝업 및 윈도우의 최상위 부모 클래스
public abstract class UIBase : MonoBehaviour
{
    // 이 UI가 열려있을 때 뒤로가기 키로 닫을 수 있는지 여부
    [SerializeField] private bool _isModal = true;

    // UI 초기화 (최초 1회만 호출 필요 시 사용)
    public virtual void Init()
    {
        // 필요 시 오버라이드
    }

    // UI가 열릴 때 호출
    public virtual void Open()
    {
        this.gameObject.SetActive(true);
        // 등장 애니메이션 등을 여기서 재생 가능
    }

    // UI가 닫힐 때 호출
    public virtual void Close()
    {
        this.gameObject.SetActive(false);
        // 퇴장 애니메이션 처리 후 비활성화 가능
    }

    // 안드로이드 뒤로가기(ESC) 키 입력 시 호출되는 메서드
    public virtual void OnBackKey()
    {
        if (_isModal)
        {
            // 모달이면 매니저를 통해 자신을 닫음
            UIManager.Instance.CloseTop();
        }
    }
}