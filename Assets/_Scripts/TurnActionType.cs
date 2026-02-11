//플레이어가 턴에서 선택 가능한 행동 유형
public enum TurnActionType
{
    Training,          //훈련 (단체)
    PersonalTraining,  //개인 훈련
    Counseling,        //상담
    Rest,              //휴식
    Match,             //경기 (시스템 자동 설정)
    Special            //특수 (이벤트 전용)
}