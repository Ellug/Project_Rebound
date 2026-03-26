using UnityEngine;

public class EquipmentSystem : Singleton<EquipmentSystem>
{
    private const int MaxLevel = 14;

    private int _uniformLevel;
    private int _basketballLevel;
    private int _shoesLevel;

    // 농구화 점프력 누적 보너스 (레벨업 시점에 재학생 전체에 적용)
    private int _shoesAppliedJumpBonus;

    public int UniformLevel => _uniformLevel;
    public int BasketballLevel => _basketballLevel;
    public int ShoesLevel => _shoesLevel;

    protected override void OnSingletonAwake() { }

    // 강화

    // itemeffect_03 진입점: 3종 중 랜덤 1개를 1단계 강화
    public void UpgradeRandom()
    {
        string[] types = { "uniform", "basketball", "shoes" };
        string target = types[UnityEngine.Random.Range(0, types.Length)];
        Upgrade(target);
    }

    public void Upgrade(string equipType)
    {
        switch (equipType)
        {
            case "uniform": UpgradeUniform(); break;
            case "basketball": UpgradeBasketball(); break;
            case "shoes": UpgradeShoes(); break;
            default:
                Debug.LogWarning($"[EquipmentSystem] 알 수 없는 장비 타입: {equipType}");
                break;
        }
    }

    private void UpgradeUniform()
    {
        if (_uniformLevel >= MaxLevel)
        {
            // 최대 강화 → 지원금 +10% 전환
            GameManager.Instance?.AddSubsidyPermBonus(0.1f);
            Debug.Log("[EquipmentSystem] 유니폼 최대 강화 → 지원금 +10% 적용");
            return;
        }

        _uniformLevel++;
        var row = GetRow(_uniformLevel);
        if (row == null) return;

        // 명성치 상승은 1회성 즉시 적용
        if (row.amount1 > 0 && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddReputation((int)row.amount1);
            Debug.Log($"[EquipmentSystem] 유니폼 {_uniformLevel}단계 → 명성치 +{row.amount1}");
        }
    }

    private void UpgradeBasketball()
    {
        if (_basketballLevel >= MaxLevel)
        {
            GameManager.Instance?.AddSubsidyPermBonus(0.1f);
            Debug.Log("[EquipmentSystem] 농구공 최대 강화 → 지원금 +10% 적용");
            return;
        }

        _basketballLevel++;
        Debug.Log($"[EquipmentSystem] 농구공 {_basketballLevel}단계 → 훈련 경험치 효율 {GetBasketballBonusRate() * 100f}%");
    }

    private void UpgradeShoes()
    {
        if (_shoesLevel >= MaxLevel)
        {
            GameManager.Instance?.AddSubsidyPermBonus(0.1f);
            Debug.Log("[EquipmentSystem] 농구화 최대 강화 → 지원금 +10% 적용");
            return;
        }

        _shoesLevel++;
        var row = GetShoesRow(_shoesLevel);
        if (row == null) return;

        // 점프력 보너스 증분을 재학생 전체에 즉시 적용
        int prevJump = _shoesAppliedJumpBonus;
        int newJump = (int)row.amount;
        int jumpDelta = newJump - prevJump;

        if (jumpDelta > 0)
        {
            ApplyJumpBonusToAllStudents(jumpDelta);
            _shoesAppliedJumpBonus = newJump;
        }

        Debug.Log($"[EquipmentSystem] 농구화 {_shoesLevel}단계 → 컨디션 감쇄 {GetShoesConditionDecayRate() * 100f}%, 점프력 누적 +{_shoesAppliedJumpBonus}");
    }

    // 수치 조회 (외부 시스템에서 참조)

    // 농구공: 훈련 경험치 효율 배율 (1.05 = 5% 효율 증가)
    public float GetBasketballBonusRate()
    {
        if (_basketballLevel <= 0) return 1f;
        var row = GetBasketballRow(_basketballLevel);
        return row != null ? row.amount1 : 1f;
    }

    // 농구화: 컨디션 소모량 감쇄 배율 (1.05 = 5% 효율 증가)
    public float GetShoesConditionDecayRate()
    {
        if (_shoesLevel <= 0) return 1f;
        var row = GetShoesRow(_shoesLevel);
        return row != null ? row.amount1 : 1f;
    }

    // 세이브 / 로드

    public EquipmentSaveData CollectSaveData()
    {
        return new EquipmentSaveData
        {
            uniformLevel = _uniformLevel,
            basketballLevel = _basketballLevel,
            shoesLevel = _shoesLevel,
            shoesJumpBonus = _shoesAppliedJumpBonus,
        };
    }

    public void RestoreFromSave(EquipmentSaveData data)
    {
        if (data == null) return;

        _uniformLevel = data.uniformLevel;
        _basketballLevel = data.basketballLevel;
        _shoesLevel = data.shoesLevel;
        _shoesAppliedJumpBonus = data.shoesJumpBonus;

        Debug.Log($"[EquipmentSystem] 복원 완료 | 유니폼={_uniformLevel}, 농구공={_basketballLevel}, 농구화={_shoesLevel}");
    }

    public void ResetToDefault()
    {
        _uniformLevel = 0;
        _basketballLevel = 0;
        _shoesLevel = 0;
        _shoesAppliedJumpBonus = 0;
    }

    // 내부 헬퍼

    private EquipmentUpgradeRow GetRow(int level)
    {
        var table = CachedSOData.Get<EquipmentUpgradeTableSO>();
        if (table == null) return null;
        foreach (var r in table.Rows)
            if (r.presentCategory == "category_001" && r.rank == level) return r;
        return null;
    }

    private EquipmentUpgradeRow GetBasketballRow(int level)
    {
        var table = CachedSOData.Get<EquipmentUpgradeTableSO>();
        if (table == null) return null;
        foreach (var r in table.Rows)
            if (r.presentCategory == "category_002" && r.rank == level) return r;
        return null;
    }

    private EquipmentUpgradeRow GetShoesRow(int level)
    {
        var table = CachedSOData.Get<EquipmentUpgradeTableSO>();
        if (table == null) return null;
        foreach (var r in table.Rows)
            if (r.presentCategory == "category_003" && r.rank == level) return r;
        return null;
    }

    private void ApplyJumpBonusToAllStudents(int delta)
    {
        if (StudentManager.Instance == null) return;

        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student == null) continue;
            student.jump = Mathf.Clamp(student.jump + delta, 1, int.MaxValue);
            StudentManager.Instance.NotifyStudentModified(student);
        }
    }

    // 신규 영입 학생에게 현재 농구화 점프력 누적 보너스 소급 적용
    public void ApplyShoesJumpBonusToStudent(Student student)
    {
        if (student == null || _shoesAppliedJumpBonus <= 0) return;
        student.jump = Mathf.Clamp(student.jump + _shoesAppliedJumpBonus, 1, int.MaxValue);
    }
}