using System;
using System.Collections.Generic;

// 학생 데이터 클래스
[Serializable]
public class Student
{
    public const int ConditionMin = 0;
    public const int ConditionMax = 120;

    // 기본 정보
    public int id;
    public string studentName;
    public string positionId;
    public string positionName;
    public int grade; // 학년 (1~3)

    // 이미지 배정
    public CharacterColor portraitColor;    // 색상
    public int portraitIndex;   // 이미지 번호

    // 신체 정보
    public int height;
    public int weight;

    // 기본 스탯
    public int mental;
    public int shoot;
    public int speed;
    public int jump;
    public int stamina;
    public int shootExp;
    public int speedExp;
    public int jumpExp;
    public int staminaExp;
    public int mentalExp;

    // 잠재 능력
    public int potential_tier;
    public string potential;

    // 컨디션 및 상태
    public int condition;
    // public int trust;

    // 이벤트 효과 추적용
    public List<string> activeEffectIds = new();
    public int conditionRecoveryBonus;
    public float trainingEfficiencyBonus;
    public bool isTrainingBlocked;

    // 현재 적용된 노드 보너스
    public int appliedShootBonus;     // 슈팅
    public int appliedJumpBonus;      // 점프력
    public int appliedAllStatBonus;   // 올스텟

    public AbnormalType abnormalState = AbnormalType.None;  // 상태이상 종류
    public int abnormalRemainTurn = 0;                      // 상태이상 남은턴
    public string abnormalReasonTextId;                     // 상세 사유

    public static int ClampCondition(int value)
    {
        if (value < ConditionMin) return ConditionMin;
        if (value > ConditionMax) return ConditionMax;
        return value;
    }
    public enum AbnormalType
    {
        None = 0,
        Disease = 1,
        Injury = 2
    }
}
