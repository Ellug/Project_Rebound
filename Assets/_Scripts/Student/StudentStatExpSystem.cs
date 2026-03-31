using System;
using UnityEngine;

public enum StudentCoreStat
{
    Mental = 1,
    Shoot = 2,
    Speed = 3,
    Jump = 4,
    Stamina = 5
}

public static class StudentStatExpSystem
{
    public static bool TryParseStatKey(string statKey, out StudentCoreStat stat)
    {
        stat = default;
        if (string.IsNullOrWhiteSpace(statKey)) return false;

        string normalized = statKey.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "mental":
                stat = StudentCoreStat.Mental;
                return true;
            case "shoot":
                stat = StudentCoreStat.Shoot;
                return true;
            case "speed":
                stat = StudentCoreStat.Speed;
                return true;
            case "jump":
                stat = StudentCoreStat.Jump;
                return true;
            case "stamina":
                stat = StudentCoreStat.Stamina;
                return true;
            default:
                return false;
        }
    }

    // exp = exp + base + base * (facilityBonus * (currentFacilityLv - requiredFacilityLv)) + (base * coachBonus)
    public static int AddTrainingExp(
        Student student,
        StudentCoreStat stat,
        float baseIncrease,
        float facilityBonusRate,
        int currentFacilityLevel,
        int requiredFacilityLevel,
        float coachBonusRate = 0f)
    {
        int diff = Mathf.Max(0, currentFacilityLevel - requiredFacilityLevel);
        float amount = baseIncrease;
        amount += baseIncrease * (facilityBonusRate * diff);
        amount += baseIncrease * coachBonusRate;

        // itemeffect_06 일시적 훈련 효과 배율 적용
        if (GameManager.Instance != null)
            amount *= GameManager.Instance.GetTrainingBoostMultiplier(stat);

        return AddRawExp(student, stat, Mathf.RoundToInt(amount));
    }

    // facilityBonusRate 계산을 외부에서 끝낸 경우 사용
    public static int AddTrainingExpWithRate(
        Student student,
        StudentCoreStat stat,
        float baseIncrease,
        float facilityBonusRate,
        float coachBonusRate = 0f)
    {
        float amount = baseIncrease;
        amount += baseIncrease * facilityBonusRate;
        amount += baseIncrease * coachBonusRate;

        // itemeffect_06 일시적 훈련 효과 배율 적용
        if (GameManager.Instance != null)
            amount *= GameManager.Instance.GetTrainingBoostMultiplier(stat);

        return AddRawExp(student, stat, Mathf.RoundToInt(amount));
    }

    public static int AddRawExp(Student student, StudentCoreStat stat, int expDelta)
    {
        if (student == null || expDelta == 0)
            return 0;

        StudentStatExpTableSO table = CachedSOData.Get<StudentStatExpTableSO>();
        if (table == null || table.Rows == null || table.Rows.Count == 0)
        {
            int beforeFallbackStat = GetStatValue(student, stat);
            ApplyFallbackStatDelta(student, stat, expDelta);
            int afterFallbackStat = GetStatValue(student, stat);
            if (afterFallbackStat > beforeFallbackStat)
                SoundManager.Instance.PlayStatUpSfx();
            return expDelta;
        }

        int maxLevel = GetMaxLevel(table);
        int level = Mathf.Clamp(GetStatValue(student, stat), 1, maxLevel);
        int exp = Mathf.Max(0, GetStatExp(student, stat));
        int beforeLevel = level;
        int beforeExp = exp;

        SimulateExpDelta(table, ref level, ref exp, expDelta, maxLevel);

        SetStatValue(student, stat, level);
        SetStatExp(student, stat, exp);

        bool statIncreased = level > beforeLevel;
        bool expIncreased = expDelta > 0 && (statIncreased || exp > beforeExp);

        if (expIncreased)
            SoundManager.Instance.PlayStatExpUpSfx();

        if (statIncreased)
            SoundManager.Instance.PlayStatUpSfx();

        return expDelta;
    }

    // 경험치 변화량이 실제 레벨(스탯)에 얼마나 반영되는지 계산
    public static int PredictStatLevelDelta(Student student, StudentCoreStat stat, int expDelta)
    {
        if (student == null || expDelta == 0)
            return 0;

        StudentStatExpTableSO table = CachedSOData.Get<StudentStatExpTableSO>();

        // 테이블 없으면 레벨 변화 예측 불가
        if (table == null || table.Rows == null || table.Rows.Count == 0) return 0;

        int maxLevel = GetMaxLevel(table);
        int level = Mathf.Clamp(GetStatValue(student, stat), 1, maxLevel);
        int exp = Mathf.Max(0, GetStatExp(student, stat));
        int beforeLevel = level;

        SimulateExpDelta(table, ref level, ref exp, expDelta, maxLevel);
        return level - beforeLevel;
    }

    // 매 훈련 실행 시, 포지션/티어 기반 랜덤 추가 경험치를 잠재력 스탯에 적용
    public static int ApplyPotentialTrainingBonusExp(Student student)
    {
        if (student == null) return 0;
        if (student.potential_tier <= 0) return 0;
        if (!TryParseStatKey(student.potential, out var potentialStat)) return 0;

        string positionId = ResolvePositionId(student);
        if (string.IsNullOrEmpty(positionId)) return 0;

        StudentPlusExpTableSO plusTable = CachedSOData.Get<StudentPlusExpTableSO>();
        if (plusTable == null) return 0;

        StudentPlusExpRow row = plusTable.GetOrNull(positionId, student.potential_tier);
        if (row == null) return 0;

        int min = Mathf.Min(row.minValue, row.maxValue);
        int max = Mathf.Max(row.minValue, row.maxValue);
        if (max <= 0) return 0;

        int bonusExp = UnityEngine.Random.Range(min, max + 1);
        if (bonusExp <= 0) return 0;

        AddRawExp(student, potentialStat, bonusExp);
        return bonusExp;
    }

    // 이번 훈련에서 잠재력 스탯 경험치가 실제로 증가한 경우에만 추가 경험치를 적용
    public static int ApplyPotentialTrainingBonusExpIfMatchingStatTrained(
        Student student,
        int mentalExpDelta,
        int shootExpDelta,
        int speedExpDelta,
        int jumpExpDelta,
        int staminaExpDelta)
    {
        if (student == null)
            return 0;

        if (!TryParseStatKey(student.potential, out StudentCoreStat potentialStat))
            return 0;

        if (!IsPotentialStatTrained(
                student,
                mentalExpDelta,
                shootExpDelta,
                speedExpDelta,
                jumpExpDelta,
                staminaExpDelta))
            return 0;

        int matchedDelta = GetMatchedPotentialExpDelta(
            potentialStat,
            mentalExpDelta,
            shootExpDelta,
            speedExpDelta,
            jumpExpDelta,
            staminaExpDelta);

#if UNITY_EDITOR
        Debug.Log(
            $"[PotentialBonus] MATCH student={student.studentName}(id={student.id}) " +
            $"tier={student.potential_tier} potential={potentialStat} matchedExpDelta={matchedDelta}");
#endif

        int bonusExp = ApplyPotentialTrainingBonusExp(student);

#if UNITY_EDITOR
        Debug.Log(
            $"[PotentialBonus] APPLIED student={student.studentName}(id={student.id}) " +
            $"potential={potentialStat} bonusExp={bonusExp}");
#endif

        return bonusExp;
    }

    private static bool IsPotentialStatTrained(
        Student student,
        int mentalExpDelta,
        int shootExpDelta,
        int speedExpDelta,
        int jumpExpDelta,
        int staminaExpDelta)
    {
        if (student == null)
            return false;

        if (!TryParseStatKey(student.potential, out StudentCoreStat potentialStat))
            return false;

        switch (potentialStat)
        {
            case StudentCoreStat.Mental:
                return mentalExpDelta > 0;
            case StudentCoreStat.Shoot:
                return shootExpDelta > 0;
            case StudentCoreStat.Speed:
                return speedExpDelta > 0;
            case StudentCoreStat.Jump:
                return jumpExpDelta > 0;
            case StudentCoreStat.Stamina:
                return staminaExpDelta > 0;
            default:
                return false;
        }
    }

    private static int GetMatchedPotentialExpDelta(
        StudentCoreStat potentialStat,
        int mentalExpDelta,
        int shootExpDelta,
        int speedExpDelta,
        int jumpExpDelta,
        int staminaExpDelta)
    {
        switch (potentialStat)
        {
            case StudentCoreStat.Mental:
                return mentalExpDelta;
            case StudentCoreStat.Shoot:
                return shootExpDelta;
            case StudentCoreStat.Speed:
                return speedExpDelta;
            case StudentCoreStat.Jump:
                return jumpExpDelta;
            case StudentCoreStat.Stamina:
                return staminaExpDelta;
            default:
                return 0;
        }
    }

    private static void ApplyFallbackStatDelta(Student student, StudentCoreStat stat, int delta)
    {
        int current = GetStatValue(student, stat);
        int next = Mathf.Max(1, current + delta);
        SetStatValue(student, stat, next);
        SetStatExp(student, stat, 0);
    }

    private static int GetMaxLevel(StudentStatExpTableSO table)
    {
        int max = 1;
        for (int i = 0; i < table.Rows.Count; i++)
        {
            StudentStatExpRow row = table.Rows[i];
            if (row == null) continue;
            max = Mathf.Max(max, row.level);
        }
        return max;
    }

    private static string ResolvePositionId(Student student)
    {
        if (!string.IsNullOrEmpty(student.positionId))
            return student.positionId;

        if (string.IsNullOrEmpty(student.positionName))
            return null;

        StudentPositionTableSO positionTable = CachedSOData.Get<StudentPositionTableSO>();
        if (positionTable == null || positionTable.Rows == null)
            return null;

        for (int i = 0; i < positionTable.Rows.Count; i++)
        {
            StudentPositionRow row = positionTable.Rows[i];
            if (row == null) continue;

            if (string.Equals(row.positionName, student.positionName, StringComparison.OrdinalIgnoreCase))
            {
                student.positionId = row.id;
                return row.id;
            }
        }

        return null;
    }

    private static int GetExpNext(StudentStatExpTableSO table, int level)
    {
        StudentStatExpRow row = table.GetOrNull(level);
        if (row == null) return 0;
        return Mathf.Max(0, row.expNext);
    }

    private static void SimulateExpDelta(StudentStatExpTableSO table, ref int level, ref int exp, int expDelta, int maxLevel)
    {
        if (expDelta > 0)
        {
            int remain = expDelta;

            while (remain > 0 && level < maxLevel)
            {
                int expNext = GetExpNext(table, level);
                if (expNext <= 0) break;

                int need = expNext - exp;
                if (need <= 0)
                {
                    level++;
                    exp = 0;
                    continue;
                }

                if (remain >= need)
                {
                    remain -= need;
                    level++;
                    exp = 0;
                }
                else
                {
                    exp += remain;
                    remain = 0;
                }
            }

            if (level >= maxLevel)
                exp = 0;

            return;
        }

        int remainDown = -expDelta;

        while (remainDown > 0)
        {
            if (exp >= remainDown)
            {
                exp -= remainDown;
                break;
            }

            remainDown -= exp;

            if (level <= 1)
            {
                level = 1;
                exp = 0;
                break;
            }

            level--;
            exp = Mathf.Max(0, GetExpNext(table, level));
        }
    }

    private static int GetStatValue(Student student, StudentCoreStat stat)
    {
        switch (stat)
        {
            case StudentCoreStat.Mental: return student.mental;
            case StudentCoreStat.Shoot: return student.shoot;
            case StudentCoreStat.Speed: return student.speed;
            case StudentCoreStat.Jump: return student.jump;
            case StudentCoreStat.Stamina: return student.stamina;
            default: return 1;
        }
    }

    private static void SetStatValue(Student student, StudentCoreStat stat, int value)
    {
        int clamped = Mathf.Max(1, value);
        switch (stat)
        {
            case StudentCoreStat.Mental:
                student.mental = clamped;
                break;
            case StudentCoreStat.Shoot:
                student.shoot = clamped;
                break;
            case StudentCoreStat.Speed:
                student.speed = clamped;
                break;
            case StudentCoreStat.Jump:
                student.jump = clamped;
                break;
            case StudentCoreStat.Stamina:
                student.stamina = clamped;
                break;
        }
    }

    private static int GetStatExp(Student student, StudentCoreStat stat)
    {
        switch (stat)
        {
            case StudentCoreStat.Mental: return student.mentalExp;
            case StudentCoreStat.Shoot: return student.shootExp;
            case StudentCoreStat.Speed: return student.speedExp;
            case StudentCoreStat.Jump: return student.jumpExp;
            case StudentCoreStat.Stamina: return student.staminaExp;
            default: return 0;
        }
    }

    private static void SetStatExp(Student student, StudentCoreStat stat, int value)
    {
        int clamped = Mathf.Max(0, value);
        switch (stat)
        {
            case StudentCoreStat.Mental:
                student.mentalExp = clamped;
                break;
            case StudentCoreStat.Shoot:
                student.shootExp = clamped;
                break;
            case StudentCoreStat.Speed:
                student.speedExp = clamped;
                break;
            case StudentCoreStat.Jump:
                student.jumpExp = clamped;
                break;
            case StudentCoreStat.Stamina:
                student.staminaExp = clamped;
                break;
        }
    }
}
