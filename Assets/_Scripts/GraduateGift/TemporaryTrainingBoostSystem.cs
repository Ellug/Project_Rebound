using System;
using UnityEngine;

// itemeffect_06: 30일 동안 랜덤 스탯 경험치 1.3배 상승 효과 관리
public class TemporaryTrainingBoostSystem
{
    private const float BoostMultiplier = 1.3f;

    private StudentCoreStat _boostStat;
    private DateTime _expireDate;
    private bool _hasBoost;

    public bool HasBoost => _hasBoost;
    public StudentCoreStat BoostStat => _boostStat;
    public DateTime ExpireDate => _expireDate;

    // itemeffect_06 적용
    public void Apply(StudentCoreStat stat, DateTime expireDate)
    {
        _boostStat = stat;
        _expireDate = expireDate;
        _hasBoost = true;
        Debug.Log($"[TemporaryTrainingBoostSystem] 적용: {stat} 경험치 {BoostMultiplier}배 | 만료일: {expireDate:yyyy-MM-dd}");
    }

    // 현재 날짜 기준으로 만료 여부 체크 및 해제
    // 턴 종료 시 호출
    public void Tick(DateTime currentDate)
    {
        if (!_hasBoost) return;
        if (currentDate < _expireDate) return;

        Debug.Log($"[TemporaryTrainingBoostSystem] 만료: {_boostStat}");
        Clear();
    }

    // 해당 스탯의 부스트 배율 반환
    // 부스트 대상 스탯이 아니거나 만료된 경우 1f 반환
    public float GetMultiplier(StudentCoreStat stat, DateTime currentDate)
    {
        if (!_hasBoost) return 1f;
        if (_boostStat != stat) return 1f;

        if (currentDate >= _expireDate)
        {
            Clear();
            return 1f;
        }

        return BoostMultiplier;
    }

    // 부스트 해제
    public void Clear()
    {
        _hasBoost = false;
        _boostStat = default;
        _expireDate = default;
    }

    // 세이브용 데이터 반환
    public (StudentCoreStat stat, DateTime expireDate, bool hasBoost) GetSaveData()
    {
        return (_boostStat, _expireDate, _hasBoost);
    }

    // 로드 후 복원
    // 이미 만료된 경우 자동 스킵
    public void Restore(StudentCoreStat stat, DateTime expireDate, DateTime currentDate)
    {
        if (expireDate == default || currentDate >= expireDate)
        {
            Clear();
            return;
        }

        _boostStat = stat;
        _expireDate = expireDate;
        _hasBoost = true;
        Debug.Log($"[TemporaryTrainingBoostSystem] 복원: {stat} | 만료일: {expireDate:yyyy-MM-dd}");
    }
}