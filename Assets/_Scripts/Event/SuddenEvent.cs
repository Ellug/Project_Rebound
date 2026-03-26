using UnityEngine;

public class SuddenEvent
{
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

    public bool TryApplyDiseaseAtDayStart(Student student)
    {
        if (!CanRollAbnormal(student))
            return false;

        StudentStatusProbRow probabilityRow = GetProbabilityRow(student);
        if (probabilityRow == null || probabilityRow.probDisease <= 0)
            return false;

        if (!RollPercent(probabilityRow.probDisease))
            return false;

        DurationRange durationRange = GetDiseaseDurationRange(student.condition);
        SetAbnormal(
            student,
            Student.AbnormalType.Disease,
            Random.Range(durationRange.min, durationRange.max + 1)
        );
        return true;
    }

    public bool TryApplyTrainingInjury(Student student)
    {
        if (!TryRollInjury(student, MatchInjuryTrigger.Training, out int duration))
            return false;

        ApplyAbnormal(student, Student.AbnormalType.Injury, duration);
        return true;
    }

    public bool TryRollQuarterStartInjury(Student student, out int duration)
    {
        return TryRollInjury(student, MatchInjuryTrigger.QuarterStart, out duration);
    }

    public bool TryRollFriendlyStartInjury(Student student, out int duration)
    {
        return TryRollInjury(student, MatchInjuryTrigger.FriendlyStart, out duration);
    }

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

    private DurationRange GetDiseaseDurationRange(int condition)
    {
        if (condition < 20) return new DurationRange(7, 28);
        if (condition < 40) return new DurationRange(3, 7);
        return new DurationRange(1, 7);
    }

    private bool RollPercent(int probabilityPercent)
    {
        return Random.Range(0, 100) < probabilityPercent;
    }

    private bool HasAbnormal(Student student)
    {
        return student != null && student.abnormalState != Student.AbnormalType.None;
    }

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
