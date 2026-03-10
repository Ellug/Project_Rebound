//턴 파이프라인에 참여하는 모듈 인터페이스 (턴에 참여하는 시스템 단위)
public interface ITurnModule
{
    string ModuleName { get; }  //모듈 식별 이름
    int Priority { get; }       //실행 우선순위 (낮을수록 먼저 실행)

    //실행 순서: OnTurnBegin -> OnTurnExecute -> OnTurnEnd
    void OnTurnBegin(TurnContext context);    //턴 시작 준비
    void OnTurnExecute(TurnContext context);  //메인 로직 처리
    void OnTurnEnd(TurnContext context);      //턴 종료 정산
}