//턴 진행 상태
public enum TurnState
{
    WaitingForInput,  //플레이어 입력 대기
    PreTurn,          //컨텍스트 생성
    Begin,            //모듈 OnTurnBegin 실행 중
    Execute,          //모듈 OnTurnExecute 실행 중
    End,              //모듈 OnTurnEnd 실행 중
    PostTurn          //날짜 전진 및 정리
}