using System.Collections.Generic;
using UnityEngine;

public static class StudentFactory
{
    private static int _nextStudentId = 1; // 학생 ID 인데 이거 캐싱 따로 시키거나 세이브 로드 대응 전략 도입 후 개선 필요
    private static HashSet<string> _usedNames = new();
    private static HashSet<(CharacterColor color, int index)> _usedPortraits = new();
    private static System.Random _random = new();

    private static CharacterColor _fixedColor;
    private static bool _isColorInitialized = false;    // 나중에 회차 초기화시 색 변경에 필요


    // 새로운 학생 생성
    public static Student CreateStudent(int grade = 0)
    {
        string studentName = SelectUniqueName(); // 이름 선택

        // 학년 설정 (입력이 1, 2, 3 아니면 랜덤)
        if (grade <= 0 || grade > 3)
            grade = _random.Next(1, 4);

        var position = SelectRandomPosition(); // 포지션 결정
        var bodyInfo = GenerateBodyInfo(position.id); // 포지션 기반 신체 정보 생성
        var (color, portraitIndex) = SelectUniquePortrait();   // 이미지 결정

        // 학생 생성
        Student student = new()
        {
            id = _nextStudentId++,
            studentName = studentName,
            positionId = position.id,
            positionName = position.positionName,
            grade = grade,
            height = bodyInfo.height,
            weight = bodyInfo.weight,
            potential = "",
            potential_tier = 0,
            condition = 0,
            // trust = 0,
            portraitColor = color,
            portraitIndex = portraitIndex,
        };

        GenerateStats(student, grade); // 학년 기반으로 기본 스탯 생성 및 할당
        GeneratePotential(student, position.id); // 포지션 기반 잠재력 생성

        student.condition = Student.ClampCondition(student.mental + 20);

        return student;
    }

    // 중복되지 않는 이름 선택
    private static string SelectUniqueName()
    {
        var nameTable = CachedSOData.Get<StudentNameTableSO>();

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

    // 중복되지 않는 이미지 선택
    private static (CharacterColor, int) SelectUniquePortrait()
    {
        InitializeColorIfNeeded();

        int maxIndex = 32;

        // 사용 가능한 이미지 필터링
        var available = new List<(CharacterColor, int)>();
        for (int i = 1; i <= maxIndex; i++)
        {
            var key = (_fixedColor, i);

            if (!_usedPortraits.Contains(key))
                available.Add(key);
        }

        // 사용 가능한 이미지 없으면 클리어해서 중복 허용시켜
        if (available.Count == 0)
        {
            Debug.LogWarning("[StudentFactory] All portraits used. Resetting used portraits.");
            _usedPortraits.Clear();

            for (int i = 1; i <= maxIndex; i++)
            {
                available.Add((_fixedColor, i));
            }
        }

        var selected = available[_random.Next(available.Count)];
        _usedPortraits.Add(selected);

        return selected;
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

    // 세이브 로드 후 기존 id와 충돌하지 않도록 카운터 복원
    public static void RestoreStudentIdCounter(int nextId)
    {
        _nextStudentId = Mathf.Max(1, nextId);
    }

    // 로드 후 이름/초상화 중복 캐시 재구성
    public static void RebuildRuntimeCaches(IEnumerable<Student> students)
    {
        _usedNames.Clear();
        _usedPortraits.Clear();

        foreach (Student student in students)
        {
            if (student == null) continue;

            if (!string.IsNullOrEmpty(student.studentName))
                _usedNames.Add(student.studentName);

            _usedPortraits.Add((student.portraitColor, student.portraitIndex));
        }
    }

    // 포지션 선택 : 가중치 기반 랜덤 선택
    private static StudentPositionRow SelectRandomPosition()
    {
        var positionTable = CachedSOData.Get<StudentPositionTableSO>();

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
    private static (int height, int weight) GenerateBodyInfo(string id)
    {
        var bodyTable = CachedSOData.Get<StudentBodyTableSO>();
        var bodyData = bodyTable.GetOrNull(id);

        int height = _random.Next(bodyData.minHeight, bodyData.maxHeight + 1);
        int weight = _random.Next(bodyData.minWeight, bodyData.maxWeight + 1);

        return (height, weight);
    }

    // 학년 기반 기본 스탯 생성 및 할당
    private static void GenerateStats(Student student, int grade)
    {
        var startStatTable = CachedSOData.Get<StudentStartStatTableSO>();

        // stat_id 기반 스탯 생성 및 직접 할당
        // stat_id: 1=멘탈, 2=슛, 3=스피드, 4=점프력, 5=스태미너
        for (int statId = 1; statId <= 5; statId++)
        {
            var startStatData = startStatTable.GetOrNull(statId, grade);

            // 초기 스탯은 base_min/base_max에서 뽑고,
            // stat_min/stat_max는 해당 스탯의 절대 허용 범위로 사용한다.
            int statMin = Mathf.Min(startStatData.statMin, startStatData.statMax);
            int statMax = Mathf.Max(startStatData.statMin, startStatData.statMax);
            int baseMin = Mathf.Min(startStatData.baseMin, startStatData.baseMax);
            int baseMax = Mathf.Max(startStatData.baseMin, startStatData.baseMax);

            int initialMin = Mathf.Clamp(baseMin, statMin, statMax);
            int initialMax = Mathf.Clamp(baseMax, statMin, statMax);

            if (initialMin > initialMax)
            {
                Debug.LogWarning(
                    $"[StudentFactory] Invalid initial range after clamp: stat_id={statId}, grade={grade}, " +
                    $"stat=[{statMin},{statMax}], base=[{baseMin},{baseMax}]");
                initialMin = statMin;
                initialMax = statMax;
            }

            int statValue = _random.Next(initialMin, initialMax + 1);

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
    private static void GeneratePotential(Student student, string id)
    {
        // 잠재력 테이블 로드
        var potentialTable = CachedSOData.Get<StudentPotentialTableSO>();
        if (potentialTable == null)
        {
            Debug.LogError("[StudentFactory] StudentPotentialTableSO를 찾을 수 없습니다.");
            student.potential_tier = 3;
            student.potential = "";
            return;
        }

        // 포지션 ID로 잠재력 데이터 조회
        var potentialData = potentialTable.GetOrNull(id);
        if (potentialData == null)
        {
            Debug.LogError($"[StudentFactory] positionId='{id}'에 해당하는 잠재력 데이터가 없습니다.");
            student.potential_tier = 3;
            student.potential = "";
            return;
        }

        // 티어별 확률 및 스탯 수치 로컬 변수로 분리
        int tier1Prob = potentialData.tier1Prob;
        int tier2Prob = potentialData.tier2Prob;
        int tier3Prob = potentialData.tier3Prob;

        string tier1Stat = potentialData.tier1Stat;
        string tier2Stat = potentialData.tier2Stat;
        string tier3Stat = potentialData.tier3Stat;

        // 확률 합계 계산 및 유효성 검사
        int totalWeight = tier1Prob + tier2Prob + tier3Prob;
        if (totalWeight <= 0)
        {
            Debug.LogWarning($"[StudentFactory] positionId='{id}'의 잠재력 확률 합계가 0입니다. tier3로 강제 설정합니다.");
            student.potential_tier = 3;
            student.potential = tier3Stat;
            return;
        }

        // 가중치 기반 랜덤 티어 선택
        int randomValue = _random.Next(0, totalWeight);
        int currentWeight = 0;

        var tiers = new[]
        {
        (tier: 1, prob: tier1Prob, stat: tier1Stat),
        (tier: 2, prob: tier2Prob, stat: tier2Stat),
        (tier: 3, prob: tier3Prob, stat: tier3Stat)
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

        // foreach 루프를 통과한 경우 tier3로 fallback
        student.potential_tier = 3;
        student.potential = tier3Stat;
    }

    // 팀 색 결정 _isColorInitialized = false 로 바꾸면 새로운 회차 시작할 때 색 랜덤
    private static void InitializeColorIfNeeded()
    {
        if (_isColorInitialized) return;

        _fixedColor = _random.Next(0, 2) == 0
            ? CharacterColor.Red
            : CharacterColor.Green;

        _isColorInitialized = true;
    }
}
