using UnityEngine;

//저장할 데이터들 나중에 추가 예정
[System.Serializable] public class PlayData
{
    public int slotIndex;       // 저장 슬롯
    public string school;       // 학교 이름
    public string playTime;     // 인게임 날짜
    public string saveTime;     // 저장시 현실 시간

    public int gold;            // 재화
    public int reputation;      // 명성치
}