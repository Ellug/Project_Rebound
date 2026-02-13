using System.Collections.Generic;
using UnityEngine;

public static class StudentFactory
{
    private static int _nextStudentId = 1; // 학생 ID 인데 이거 캐싱 따로 시키거나 세이브 로드 대응 전략 도입 후 개선 필요
    private static HashSet<string> _usedNames = new();
    private static System.Random _random = new();

    // 새로운 학생 생성
    public static Student CreateStudent(int grade = 0)
    {
        string studentName = SelectUniqueName(); // 이름 선택

        // 학년 설정 (입력이 1, 2, 3 아니면 랜덤)
        if (grade <= 0 || grade > 3)
            grade = _random.Next(1, 4);

        var position = SelectRandomPosition(); // 포지션 결정
        var bodyInfo = GenerateBodyInfo(position.id); // 포지션 기반 신체 정보 생성

        // 학생 생성
        Student student = new()
        {
            id = _nextStudentId++,
            studentName = studentName,
            positionName = position.positionName,
            grade = grade,
            height = bodyInfo.height,
            weight = bodyInfo.weight,
            potential = "",
            potential_tier = 0,
            condition = 0,
            trust = 0,
        };

        GenerateStats(student, grade); // 학년 기반으로 기본 스탯 생성 및 할당
        GeneratePotential(student, position.id); // 포지션 기반 잠재력 생성

        student.condition = student.mental + 20;

        return student;
    }

    // 중복되지 않는 이름 선택
    private static string SelectUniqueName()
    {
        var nameTable = CachedSOData.StudentNameTable;

        // 사용 가능한 이름 필터링
        var availableNames = new List<StudentNameRow>();
        foreach (var row in nameTable.Rows)
        {
            // Student Manager 같은거 도입되면 거기서 현재 학생 이름 탐색하도록 변경
            if (!_usedNames.Contains(row.name))
                availableNames.Add(row);
        }

        // 사용 가능한 이름 없으면 클리어해서 중복 허용시켜
        if (availableNames.Count == 0)
        {
            Debug.LogWarning("[StudentFactory] All names are used. Resetting used names.");
            ResetUsedNames();
            availableNames = new List<StudentNameRow>(nameTable.Rows);
        }

        // 랜덤 선택
        var selectedName = availableNames[_random.Next(availableNames.Count)].name;
        _usedNames.Add(selectedName);

        return selectedName;
    }

    // 사용된 이름 초기화
    public static void ResetUsedNames()
    {
        _usedNames.Clear();
    }

    // 학생 ID 카운터 초기화
    public static void ResetStudentIdCounter()
    {
        _nextStudentId = 1;
    }

    // 포지션 선택 : 가중치 기반 랜덤 선택
    private static StudentPositionRow SelectRandomPosition()
    {
        var positionTable = CachedSOData.StudentPositionTable;

        // 총 확률 계산
        int totalWeight = 0;
        foreach (var pos in positionTable.Rows)
            totalWeight += pos.spawnRate;

        int randomValue = _random.Next(0, totalWeight);
        int currentWeight = 0;

        foreach (var pos in positionTable.Rows)
        {
            currentWeight += pos.spawnRate;
            if (randomValue < currentWeight)
                return pos;
        }

        return positionTable.Rows[0];
    }

    // 포지션 기반 신체 정보 생성
    private static (int height, int weight) GenerateBodyInfo(int positionId)
    {
        var bodyTable = CachedSOData.StudentBodyTable;
        var bodyData = bodyTable.GetOrNull(positionId);

        int height = _random.Next(bodyData.minHeight, bodyData.maxHeight + 1);
        int weight = _random.Next(bodyData.minWeight, bodyData.maxWeight + 1);

        return (height, weight);
    }

    // 학년 기반 기본 스탯 생성 및 할당
    private static void GenerateStats(Student student, int grade)
    {
        var startStatTable = CachedSOData.StudentStartStatTable;

        // stat_id 기반 스탯 생성 및 직접 할당
        // stat_id: 1=멘탈, 2=슛, 3=스피드, 4=점프력, 5=스태미너
        for (int statId = 1; statId <= 5; statId++)
        {
            var startStatData = startStatTable.GetOrNull(statId, grade);

            // stat_min ~ stat_max 범위에서 랜덤 선택
            int statValue = _random.Next(startStatData.statMin, startStatData.statMax + 1);

            switch (statId)
            {
                case 1: student.mental = statValue; break;
                case 2: student.shoot = statValue; break;
                case 3: student.speed = statValue; break;
                case 4: student.jump = statValue; break;
                case 5: student.stamina = statValue; break;
                default:
                    Debug.LogWarning($"[StudentFactory] Unknown stat_id: {statId}");
                    break;
            }
        }
    }

    // 포지션 기반으로 잠재능력 할당 : 가중치 기반 랜덤 선택
    private static void GeneratePotential(Student student, int positionId)
    {
        var potentialTable = CachedSOData.StudentPotentialTable;
        var potentialData = potentialTable.GetOrNull(positionId);

        // 총 확률 계산
        int totalWeight = potentialData.tier1Prob + potentialData.tier2Prob + potentialData.tier3Prob;
        int randomValue = _random.Next(0, totalWeight);
        int currentWeight = 0;

        // 티어별 확률 체크 및 티어별 정의
        var tiers = new[]
        {
            (tier: 1, prob: potentialData.tier1Prob, stat: potentialData.tier1Stat),
            (tier: 2, prob: potentialData.tier2Prob, stat: potentialData.tier2Stat),
            (tier: 3, prob: potentialData.tier3Prob, stat: potentialData.tier3Stat)
        };

        foreach (var t in tiers)
        {
            currentWeight += t.prob;
            if (randomValue < currentWeight)
            {
                student.potential_tier = t.tier;
                student.potential = t.stat;
                return;
            }
        }

        student.potential_tier = 3;
        student.potential = potentialData.tier3Stat;
    }
}
