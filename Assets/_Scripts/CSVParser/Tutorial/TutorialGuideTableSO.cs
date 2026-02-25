using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// 튜토리얼 가이드 한 줄(Row) 데이터 정의
public sealed class TutorialGuideRow
{
    public int index; // 고유 식별 인덱스

    // 이미지 식별자 (Addressables key / Resources path / Sprite name 등)
    // 프로토타입이면 "-" 또는 빈값 허용
    public string img;

    public string titleText; // 가이드 제목 텍스트

    [TextArea(3, 10)]
    public string desc; // 가이드 설명 텍스트
}

[CreateAssetMenu(menuName = "Game/Data/Tutorial Guide Table", fileName = "SO_TutorialGuideTable")]
// 튜토리얼 가이드 테이블을 관리하는 ScriptableObject
public sealed class TutorialGuideTableSO : ScriptableObject
{
    [SerializeField] private List<TutorialGuideRow> _rows = new(); // 전체 Row 목록

    // index 기반 빠른 조회를 위한 캐시 딕셔너리
    private Dictionary<int, TutorialGuideRow> _byIndex;

    // 외부 읽기 전용 접근
    public IReadOnlyList<TutorialGuideRow> Rows => _rows;

    // SO 활성화 시 캐시 구성
    private void OnEnable()
    {
        BuildCache();
    }

    // _rows 리스트를 기반으로 index 캐시 재구성
    public void BuildCache()
    {
        _byIndex = new Dictionary<int, TutorialGuideRow>(_rows.Count);
        foreach (var r in _rows)
        {
            if (r == null) continue;
            _byIndex[r.index] = r; // 중복 index는 마지막 값으로 덮어씀
        }
    }

    // index로 Row 조회 시도
    public bool TryGet(int index, out TutorialGuideRow row)
    {
        row = null;

        if (_byIndex == null)
            return false;

        return _byIndex.TryGetValue(index, out row);
    }

#if UNITY_EDITOR
    // CSV 임포트 등에서 전체 데이터 교체용 (에디터 전용)
    public void ReplaceAll(List<TutorialGuideRow> newRows)
    {
        _rows = newRows ?? new List<TutorialGuideRow>();
        BuildCache();
    }
#endif
}