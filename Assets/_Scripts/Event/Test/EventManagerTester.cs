using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EventManagerTester : MonoBehaviour
{
    [SerializeField] private EventManager _eventManager;

    private GameState _gameState;   //  반드시 클래스 필드로 선언

    private void Start()
    {
        _gameState = new GameState(DateTime.Today);
        _eventManager.Initialize(_gameState);

        Debug.Log($"[Init] Turn={_gameState.CurrentTurn}, Date={_gameState.CurrentDate:yyyy-MM-dd}");
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            

            _gameState.AdvanceTurn();
            _eventManager.CheckEvents();

            Debug.Log($"Turn={_gameState.CurrentTurn}, Date={_gameState.CurrentDate:yyyy-MM-dd}");
        }
    }
}