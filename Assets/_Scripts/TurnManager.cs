using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//한 턴(= 하루) 진행의 전체 라이프사이클 관리
//흐름: PreTurn -> Begin -> Execute -> End -> PostTurn -> 날짜 전진
public class TurnManager : MonoBehaviour
{
    [Header("시작 날짜 설정")]
    [SerializeField] private int _startYear = 2026;
    [SerializeField] private int _startMonth = 3;
    [SerializeField] private int _startDay = 1;

    private DateManager _dateManager;
    private List<ITurnModule> _modules = new List<ITurnModule>();
    private TurnContext _currentContext;
    private int _turnIndex;
    private bool _isTurnRunning;
    private TurnState _turnState = TurnState.WaitingForInput;
    private GamePhase _currentPhase = GamePhase.Init;

    public DateManager DateManager => _dateManager;
    public TurnContext CurrentContext => _currentContext;
    public int TurnIndex => _turnIndex;
    public TurnState State => _turnState;
    public bool IsTurnRunning => _isTurnRunning;
    public GamePhase CurrentPhase => _currentPhase;

    //외부 구독용 이벤트
    public event Action<TurnContext> OnTurnStarted;
    public event Action<TurnContext> OnTurnCompleted;
    public event Action<TurnState> OnTurnStateChanged;
    public event Action<GamePhase> OnPhaseChanged;

    void Awake()
    {
        _dateManager = new DateManager(new DateTime(_startYear, _startMonth, _startDay));
        _turnIndex = 0;
    }

    //ITurnModule 구현체 등록 (Priority 낮을수록 먼저 실행)
    public void RegisterModule(ITurnModule module)
    {
        if (module == null)
        {
            Debug.LogWarning("[TurnManager] null 모듈 등록 시도 무시");
            return;
        }

        if (_modules.Any(m => m.ModuleName == module.ModuleName))
        {
            Debug.LogWarning($"[TurnManager] 이미 등록된 모듈: {module.ModuleName}");
            return;
        }

        _modules.Add(module);
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        Debug.Log($"[TurnManager] 모듈 등록: {module.ModuleName} (Priority {module.Priority})");
    }

    public void UnregisterModule(string moduleName)
    {
        int removedCount = _modules.RemoveAll(m => m.ModuleName == moduleName);
        if (removedCount > 0)
        {
            Debug.Log($"[TurnManager] 모듈 해제: {moduleName}");
        }
    }

    public IReadOnlyList<ITurnModule> GetRegisteredModules()
    {
        return _modules.AsReadOnly();
    }

    //게임 페이즈 전환
    public void SetPhase(GamePhase newPhase)
    {
        if (_currentPhase == newPhase) return;

        Debug.Log($"[TurnManager] 페이즈 전환: {_currentPhase} → {newPhase}");
        _currentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);
    }

    //플레이어 액션 선택 후 호출 (턴 전체 파이프라인 실행)
    public void ExecuteTurn(TurnActionType action)
    {
        if (_isTurnRunning)
        {
            Debug.LogWarning("[TurnManager] 이미 턴 진행 중입니다.");
            return;
        }

        _isTurnRunning = true;

        try
        {
            SetState(TurnState.PreTurn);
            InitTurnContext(action);
            OnTurnStarted?.Invoke(_currentContext);

            SetState(TurnState.Begin);
            ExecuteModulePhase(m => m.OnTurnBegin(_currentContext), "OnTurnBegin");

            SetState(TurnState.Execute);
            ExecuteModulePhase(m => m.OnTurnExecute(_currentContext), "OnTurnExecute");

            SetState(TurnState.End);
            ExecuteModulePhase(m => m.OnTurnEnd(_currentContext), "OnTurnEnd");

            //모든 모듈 처리 완료 후 날짜 전진
            SetState(TurnState.PostTurn);
            _dateManager.AdvanceDay();
            _turnIndex++;

            SetState(TurnState.WaitingForInput);
            OnTurnCompleted?.Invoke(_currentContext);
        }
        finally
        {
            _isTurnRunning = false;
        }
    }

    private void InitTurnContext(TurnActionType action)
    {
        _currentContext = new TurnContext
        {
            TurnIndex = _turnIndex,
            CurrentDate = _dateManager.CurrentDate,
            CurrentSemester = _dateManager.CurrentSemester,
            CurrentPhase = _currentPhase,
            SelectedAction = action,
            IsMatchDay = (_currentPhase == GamePhase.MatchDay)
        };
    }

    //등록된 모듈을 순회하며 해당 페이즈 실행 (개별 모듈 오류가 전체를 멈추지 않도록 처리)
    private void ExecuteModulePhase(Action<ITurnModule> phaseAction, string phaseName)
    {
        foreach (var module in _modules)
        {
            try
            {
                phaseAction(module);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TurnManager] {module.ModuleName}.{phaseName} 오류: {exception}");
            }
        }
    }

    private void SetState(TurnState newState)
    {
        _turnState = newState;
        OnTurnStateChanged?.Invoke(newState);
    }

    #if UNITY_EDITOR
    [ContextMenu("Debug - Execute Rest Turn")]
    private void DebugExecuteRestTurn()
    {
        ExecuteTurn(TurnActionType.Rest);
    }

    [ContextMenu("Debug - Print Status")]
    private void DebugPrintStatus()
    {
        Debug.Log(
            $"[TurnManager] 턴: {_turnIndex} | {_dateManager.FormattedDate} | " +
            $"학기={_dateManager.CurrentSemester} | 페이즈={_currentPhase} | " +
            $"모듈={_modules.Count}개 | State={_turnState}"
        );
    }
    #endif
}