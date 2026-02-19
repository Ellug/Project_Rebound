using UnityEngine;
using UnityEngine.InputSystem;

//TurnManager 테스트용 스크립트
//키보드 입력으로 턴 실행, 콘솔 로그로 결과 확인
public class TurnManagerTest : MonoBehaviour
{
    [SerializeField] private TurnManager _turnManager;

    void Start()
    {
        if (_turnManager == null)
        {
            _turnManager = GetComponent<TurnManager>();
        }

        _turnManager.OnTurnStarted += HandleTurnStarted;
        _turnManager.OnTurnCompleted += HandleTurnCompleted;
        _turnManager.OnPhaseChanged += HandlePhaseChanged;
        _turnManager.DateManager.OnSemesterStarted += HandleSemesterStarted;
        _turnManager.DateManager.OnGraduationTriggered += HandleGraduation;
        _turnManager.DateManager.OnEnrollmentTriggered += HandleEnrollment;
        _turnManager.DateManager.OnYearChanged += HandleYearChanged;

        //테스트 시작 시 육성 사이클 페이즈로 설정
        _turnManager.SetPhase(GamePhase.DailyTraining);

        Debug.Log("===== TurnManager 테스트 시작 =====");
        Debug.Log($"시작 날짜: {_turnManager.DateManager.FormattedDate}");
        Debug.Log($"학기: {_turnManager.DateManager.CurrentSemester}");
        Debug.Log("");
        Debug.Log("[조작법]");
        Debug.Log("1 : 훈련 (단체)");
        Debug.Log("2 : 개인 훈련");
        Debug.Log("3 : 상담");
        Debug.Log("4 : 휴식");
        Debug.Log("Space : 휴식 10턴 연속 진행");
    }

    void Update()
    {
        if (_turnManager.IsTurnRunning) return;
        if (_turnManager.CurrentPhase != GamePhase.DailyTraining) return;

        Keyboard pressKey = Keyboard.current;
        if (pressKey == null) return;

        if (pressKey.digit1Key.wasPressedThisFrame)
        {
            _turnManager.ExecuteTurn(TurnActionType.Training);
        }
        else if (pressKey.digit2Key.wasPressedThisFrame)
        {
            _turnManager.ExecuteTurn(TurnActionType.PersonalTraining);
        }
        else if (pressKey.digit3Key.wasPressedThisFrame)
        {
            _turnManager.ExecuteTurn(TurnActionType.Counseling);
        }
        else if (pressKey.digit4Key.wasPressedThisFrame)
        {
            _turnManager.ExecuteTurn(TurnActionType.Rest);
        }
        else if (pressKey.spaceKey.wasPressedThisFrame)
        {
            for (int i = 0; i < 10; i++)
            {
                if (_turnManager.CurrentPhase != GamePhase.DailyTraining) break;
                _turnManager.ExecuteTurn(TurnActionType.Rest);
            }
        }
    }

    private void HandleTurnStarted(TurnContext ctx)
    {
        Debug.Log($"[턴 시작] Day {ctx.DayNumber} | {ctx.CurrentDate:yyyy.MM.dd} | 행동: {ctx.SelectedAction}");
    }

    private void HandleTurnCompleted(TurnContext ctx)
    {
        Debug.Log($"[턴 완료] 다음 날짜: {_turnManager.DateManager.FormattedDate} | 학기: {_turnManager.DateManager.CurrentSemester}");
        Debug.Log("--------------------");
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        Debug.Log($"[페이즈 전환] → {phase}");
    }

    private void HandleSemesterStarted(int semester)
    {
        Debug.Log($"{semester}학기 시작");
    }

    private void HandleGraduation()
    {
        Debug.Log("졸업 시점 도달");
    }

    private void HandleEnrollment()
    {
        Debug.Log("입학 시점 도달");
    }

    private void HandleYearChanged(int year)
    {
        Debug.Log($"{year}년 차 돌입");
    }
}