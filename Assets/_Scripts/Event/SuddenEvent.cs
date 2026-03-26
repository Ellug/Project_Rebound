using UnityEngine;

public class SuddenEvent
{   
    // 임시 나중에 참조 변경 예정
    private static readonly string[] DISEASE_REASON_TEXT_IDS =
    {
        "text_diag_100002_000",
        "text_diag_100003_000",
        "text_diag_100004_000"
    };

    private static readonly string[] INJURY_REASON_TEXT_IDS =
    {
        "text_diag_110001_000",
        "text_diag_110002_000",
        "text_diag_110003_000",
        "text_diag_110004_000"
    };

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
        // 날짜 시작 질병은 즉시  반영
        SetAbnormal(
            student,
            Student.AbnormalType.Disease,
            Random.Range(durationRange.min, durationRange.max + 1),
            PickReasonTextId(DISEASE_REASON_TEXT_IDS)
        );
        return true;
    }

    public bool TryApplyTrainingInjury(Student student)
    {
        if (!TryRollInjury(student, MatchInjuryTrigger.Training, out int duration, out string reasonTextId))
            return false;

        // 훈련 부상은 훈련 직후 바로 적용
        ApplyAbnormal(student, Student.AbnormalType.Injury, duration, reasonTextId);
        return true;
    }

    public bool TryRollQuarterStartInjury(Student student, out int duration, out string reasonTextId)
    {
        return TryRollInjury(student, MatchInjuryTrigger.QuarterStart, out duration, out reasonTextId);
    }

    public bool TryRollFriendlyStartInjury(Student student, out int duration, out string reasonTextId)
    {
        return TryRollInjury(student, MatchInjuryTrigger.FriendlyStart, out duration, out reasonTextId);
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

    public void ApplyAbnormal(Student student, Student.AbnormalType type, int duration, string reasonTextId = null)
    {
        SetAbnormal(student, type, duration, reasonTextId);
    }

    private bool TryRollInjury(Student student, MatchInjuryTrigger trigger, out int duration, out string reasonTextId)
    {
        duration = 0;
        reasonTextId = null;

        if (!CanRollAbnormal(student))
            return false;

        StudentStatusProbRow probabilityRow = GetProbabilityRow(student);
        if (probabilityRow == null || probabilityRow.probInjury <= 0)
            return false;

        if (!RollPercent(probabilityRow.probInjury))
            return false;

        DurationRange durationRange = GetInjuryDurationRange(student.condition, trigger);
        duration = Random.Range(durationRange.min, durationRange.max + 1);
        reasonTextId = PickReasonTextId(INJURY_REASON_TEXT_IDS);
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


    // 그냥 하드 코딩
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

    private void SetAbnormal(Student student, Student.AbnormalType type, int duration, string reasonTextId)
    {
        if (student == null)
            return;

        if (type == Student.AbnormalType.None || duration <= 0)
        {
            ClearAbnormal(student);
            return;
        }

        // 전부 갱신
        student.abnormalState = type;
        student.abnormalRemainTurn = duration;
        student.abnormalReasonTextId = reasonTextId;
    }

    private void ClearAbnormal(Student student)
    {
        if (student == null)
            return;

        student.abnormalState = Student.AbnormalType.None;
        student.abnormalRemainTurn = 0;
        student.abnormalReasonTextId = null;
    }

    private string PickReasonTextId(string[] candidateIds)
    {
        if (candidateIds == null || candidateIds.Length == 0)
            return null;

        // 사유 랜덤으로
        return candidateIds[Random.Range(0, candidateIds.Length)];
    }

    private enum MatchInjuryTrigger
    {
        Training = 0,
        QuarterStart = 1,
        FriendlyStart = 2
    }
}
