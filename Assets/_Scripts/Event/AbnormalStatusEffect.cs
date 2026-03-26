public class AbnormalStatusEffect
{
    // 질병 걸릴시 훈련 막기
    public bool IsTrainingBlocked(Student student)
    {
        return student != null && (student.isTrainingBlocked || student.abnormalState == Student.AbnormalType.Disease);
    }

    // 경기 출전 막기
    public bool IsMatchBlocked(Student student)
    {
        return student != null && student.abnormalState != Student.AbnormalType.None;
    }

    // 훈련 경험치 감소
    public float GetTrainingExpMultiplier(Student student)
    {
        if (student == null)
        {
            return 1f;
        }

        return student.abnormalState == Student.AbnormalType.Injury ? 0.5f : 1f;
    }
}
