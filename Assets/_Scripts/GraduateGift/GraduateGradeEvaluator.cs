using UnityEngine;

// 졸업생 등급 판정 결과
public class GraduateEvaluationResult
{
    public Student Student;
    public int GradeIndex;      // 1~4
    public string GradeLabel;   // "1등급" 등
}

// 졸업생 등급 판정
public static class GraduateGradeEvaluator
{
    public static GraduateEvaluationResult Evaluate(Student student, int semiFinalCount)
    {
        var table = CachedSOData.Get<GraduateGradeTableSO>();
        if (table == null || table.Rows == null || table.Rows.Count == 0)
        {
            Debug.LogError("[GraduateGradeEvaluator] GraduateGradeTableSO를 찾을 수 없습니다.");
            return MakeFallbackResult(student);
        }

        if (student == null)
        {
            Debug.LogError("[GraduateGradeEvaluator] student가 null입니다.");
            return MakeFallbackResult(null);
        }

        int statAll = student.mental + student.shoot + student.speed + student.jump + student.stamina;

        // student.potential 이 string이면 숫자 파싱
        int potentialValue = ParsePotentialValue(student.potential);

        foreach (var row in table.Rows)
        {
            bool statOk = statAll >= row.statAll;
            bool potentialOk = potentialValue >= row.potentialMin;
            bool semiFinalOk = semiFinalCount >= row.semiFinalPlus;

            // trustLevel은 현재 Student에 trust 필드가 없으므로 일단 제외
            if (statOk && potentialOk && semiFinalOk)
            {
                return new GraduateEvaluationResult
                {
                    Student = student,
                    GradeIndex = ParseGradeIndex(row.grade),
                    GradeLabel = row.grade
                };
            }
        }

        var lastRow = table.Rows[table.Rows.Count - 1];
        return new GraduateEvaluationResult
        {
            Student = student,
            GradeIndex = ParseGradeIndex(lastRow.grade),
            GradeLabel = lastRow.grade
        };
    }

    private static int ParsePotentialValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return int.TryParse(value, out int result) ? result : 0;
    }

    private static int ParseGradeIndex(string gradeLabel)
    {
        if (string.IsNullOrEmpty(gradeLabel))
            return 4;

        if (int.TryParse(gradeLabel.Replace("등급", "").Trim(), out int result))
            return Mathf.Clamp(result, 1, 4);

        return 4;
    }

    private static GraduateEvaluationResult MakeFallbackResult(Student student)
    {
        return new GraduateEvaluationResult
        {
            Student = student,
            GradeIndex = 4,
            GradeLabel = "4등급"
        };
    }
}