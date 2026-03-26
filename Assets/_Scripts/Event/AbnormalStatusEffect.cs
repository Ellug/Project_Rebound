public class AbnormalStatusEffect
{
    // 질병은 모든 훈련이 불가, 부상은 단체 훈련만 제외
    public bool IsTrainingBlocked(Student student, bool isIndividualTraining)
    {
        if (student == null)
        {
            return true;
        }

        if (student.isTrainingBlocked)
        {
            return true;
        }

        if (student.abnormalState == Student.AbnormalType.Disease)
        {
            return true;
        }

        return student.abnormalState == Student.AbnormalType.Injury && !isIndividualTraining;
    }

    // 상태이상 학생은 경기에 출전할 수 없음
    public bool IsMatchBlocked(Student student)
    {
        return student != null && student.abnormalState != Student.AbnormalType.None;
    }

    // 부상 학생은 개인 훈련 경험치를 절반만 획득
    public float GetTrainingExpMultiplier(Student student)
    {
        if (student == null)
        {
            return 1f;
        }

        return student.abnormalState == Student.AbnormalType.Injury ? 0.5f : 1f;
    }
}
