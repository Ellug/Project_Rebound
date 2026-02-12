using UnityEngine;
using UnityEngine.InputSystem;

public class EventTester : MonoBehaviour
{
    public GameEvent gameEvent;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("스페이스 입력 감지");
            gameEvent.Raise();
        }
    }
}