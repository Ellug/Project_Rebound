using System;

[Serializable]
public class EquipmentSaveData
{
    public int uniformLevel;     // 유니폼 강화 단계
    public int basketballLevel;  // 농구공 강화 단계
    public int shoesLevel;       // 농구화 강화 단계
    public int shoesJumpBonus;   // 농구화 누적 점프력 보너스 (학생별 적용은 별도)
}