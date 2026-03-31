using UnityEngine;

// 졸업 선물 관련 영구 보너스 상태 관리
// itemeffect_07: 훈련 효율 영구 보너스
// 4강 이상 진출 횟수 (등급 판정 기준)
public class GraduateGiftBonusTracker
{
    private float _trainingEfficiencyPermBonusRate;
    private int _semiFinalReachedCount;

    // 4강 이상 진출 횟수
    public int SemiFinalReachedCount => _semiFinalReachedCount;

    public void AddSemiFinalReachedCount(int amount)
    {
        if (amount <= 0) return;
        _semiFinalReachedCount += amount;
    }

    // itemeffect_07: 배율 조회
    public float GetTrainingEfficiencyPermBonusRate() => _trainingEfficiencyPermBonusRate;

    // itemeffect_07: 배율 누적 및 재학생 전체 적용
    public void AddTrainingEfficiencyPermBonus(float rate)
    {
        if (rate <= 0f) return;

        _trainingEfficiencyPermBonusRate += rate;
        Debug.Log($"[GraduateGiftBonusTracker] 전역 훈련 효율 영구 보너스 누적: {_trainingEfficiencyPermBonusRate * 100f}%");

        if (StudentManager.Instance == null) return;

        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student == null) continue;
            student.trainingEfficiencyBonus += rate;
            StudentManager.Instance.NotifyStudentModified(student);
        }
    }

    // itemeffect_07: 신규 영입 학생에게 누적 배율 적용
    public void ApplyToStudent(Student student)
    {
        if (student == null || _trainingEfficiencyPermBonusRate <= 0f) return;
        student.trainingEfficiencyBonus += _trainingEfficiencyPermBonusRate;
    }

    // 세이브 데이터에서 복원
    public void RestoreFromSave(int semiFinalCount, float trainingEfficiencyRate)
    {
        _semiFinalReachedCount = semiFinalCount;
        _trainingEfficiencyPermBonusRate = trainingEfficiencyRate;
    }
}