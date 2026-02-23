using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    public GameEvent gameEvent;
    public UnityEvent response;

    private void OnEnable()
    {
        if (gameEvent != null)
        {
            Debug.Log("리스너 등록");
            gameEvent.Register(this);
        }
    }

    private void OnDisable()
    {
        if (gameEvent != null)
        {
            Debug.Log("리스너 해제");
            gameEvent.Unregister(this);
        }
    }

    public void OnEventRaised()
    {
        Debug.Log("이벤트 받음");
        response?.Invoke();
    }
}