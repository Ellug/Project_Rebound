using UnityEngine;

public class SuddenEvent
{
    // 상태이상에 최소, 최대 턴
    private struct DurationRange
    {
        public int min;
        public int max;

        public DurationRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
    }

    // 훈련 부상 판정
    public bool TryApplyTrainingInjury(Student student)
    {
        if (!TryRollInjury(student, MatchInjuryTrigger.Training, out int duration))
            return false;

        ApplyAbnormal(student, Student.AbnormalType.Injury, duration);
        return true;
    }

    // 쿼터 부상 판정
    public bool TryRollQuarterStartInjury(Student student, out int duration)
    {
        return TryRollInjury(student, MatchInjuryTrigger.QuarterStart, out duration);
    }

    // 친선전 부상 판정
    public bool TryRollFriendlyStartInjury(Student student, out int duration)
    {
        return TryRollInjury(student, MatchInjuryTrigger.FriendlyStart, out duration);
    }

    // 날짜가 지나갈 때 상태이상의 남은 턴을 감소
    public void TickAbnormal(Student student)
    {
        if (student == null)
            return;

        if (!HasAbnormal(student))
            return;

        student.abnormalRemainTurn--;

        if (student.abnormalRemainTurn <= 0)
            ClearAbnormal(student);
    }

    public void ApplyAbnormal(Student student, Student.AbnormalType type, int duration)
    {
        SetAbnormal(student, type, duration);
    }

    // 이미 상태이상이 있는 학생은 다시 부상 판정 X
    private bool TryRollInjury(Student student, MatchInjuryTrigger trigger, out int duration)
    {
        duration = 0;

        if (!CanRollAbnormal(student))
            return false;

        StudentStatusProbRow probabilityRow = GetProbabilityRow(student);
        if (probabilityRow == null || probabilityRow.probInjury <= 0)
            return false;

        if (!RollPercent(probabilityRow.probInjury))
            return false;

        DurationRange durationRange = GetInjuryDurationRange(student.condition, trigger);
        duration = Random.Range(durationRange.min, durationRange.max + 1);
        return true;
    }

    private bool CanRollAbnormal(Student student)
    {
        return student != null && !HasAbnormal(student);
    }

    // 컨디션 구간에 맞는 부상 확률
    private StudentStatusProbRow GetProbabilityRow(Student student)
    {
        StudentStatusProbTableSO table = CachedSOData.Get<StudentStatusProbTableSO>();
        if (table == null)
        {
            Debug.LogWarning("[SuddenEvent] StudentStatusProbTableSO not found.");
            return null;
        }

        return table.GetByCondition(isInsane: false, conditionValue: student.condition);
    }

    private DurationRange GetInjuryDurationRange(int condition, MatchInjuryTrigger trigger)
    {
        switch (trigger)
        {
            case MatchInjuryTrigger.Training:
                if (condition < 10) return new DurationRange(7, 28);
                if (condition < 30) return new DurationRange(3, 7);
                if (condition < 50) return new DurationRange(1, 7);
                return new DurationRange(1, 3);
            case MatchInjuryTrigger.QuarterStart:
            case MatchInjuryTrigger.FriendlyStart:
                if (condition < 20) return new DurationRange(7, 28);
                if (condition < 40) return new DurationRange(3, 7);
                if (condition < 60) return new DurationRange(1, 7);
                return new DurationRange(1, 3);
            default:
                return new DurationRange(1, 3);
        }
    }

    private bool RollPercent(int probabilityPercent)
    {
        return Random.Range(0, 100) < probabilityPercent;
    }

    // 학생이 현재 상태이상 중인지 확인
    private bool HasAbnormal(Student student)
    {
        return student != null && student.abnormalState != Student.AbnormalType.None;
    }

    // 실제 학생 데이터에 상태이상 종류와 남은 턴을 기록
    private void SetAbnormal(Student student, Student.AbnormalType type, int duration)
    {
        if (student == null)
            return;

        if (type == Student.AbnormalType.None || duration <= 0)
        {
            ClearAbnormal(student);
            return;
        }

        student.abnormalState = type;
        student.abnormalRemainTurn = duration;
    }

    // 상태이상 해제 시 초기화
    private void ClearAbnormal(Student student)
    {
        if (student == null)
            return;

        student.abnormalState = Student.AbnormalType.None;
        student.abnormalRemainTurn = 0;
    }

    private enum MatchInjuryTrigger
    {
        Training = 0,
        QuarterStart = 1,
        FriendlyStart = 2
    }
}
