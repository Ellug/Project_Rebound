//게임 전체 진행 단계
//게임시작 -> 선수모집 -> 육성사이클 <-> 정규경기 -> 새학기판정 -> 반복
public enum GamePhase
{
    Init,              //게임 초기화
    Recruitment,       //선수 모집
    DailyTraining,     //육성 사이클 (N일차 반복)
    MatchDay,          //경기 D-day (정규 경기 진입)
    MatchInProgress,   //경기 진행 중 (토너먼트)
    MatchResult,       //경기 결과 처리
    SemesterEnd,       //학기 종료 판정
    Graduation,        //졸업 + 입학 + 선수 모집
    GameOver           //패배 게임 오버
}