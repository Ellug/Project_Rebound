using System;

// 학생 데이터 클래스
[Serializable]
public class Student
{
    // 기본 정보
    public int id;
    public string studentName;
    public string positionName;
    public int grade; // 학년 (1~3)

    // 신체 정보
    public int height;
    public int weight;

    // 기본 스탯
    public int mental;
    public int shoot;
    public int speed;
    public int jump;
    public int stamina;

    // 잠재 능력 (미구현)
    public int potential_tier;
    public string potential;

    // 컨디션 및 상태
    public int condition;
    public int trust;
}
