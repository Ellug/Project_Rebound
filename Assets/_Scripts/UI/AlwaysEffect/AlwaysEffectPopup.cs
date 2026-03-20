using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 현재 적용 중인 상시 이벤트 효과를 리스트로 표시하는 팝업
// LobbyUI._btnEffectIcon 클릭 → Open() 호출
public class AlwaysEffectPopup : UIBase
{
    [SerializeField] private Button _btnClose;
    [SerializeField] private Transform _content;         // ScrollView > Content
    [SerializeField] private GameObject _rowPrefab;      // AlwaysEffectRowUI Prefab

    public override void Init()
    {
        base.Init();
        _btnClose.onClick.AddListener(Close);
    }

    public override void Open()
    {
        base.Open();
        RefreshEffectList();
    }

    // 기존 Row 제거 후 현재 활성 효과 기준으로 재생성
    private void RefreshEffectList()
    {
        foreach (Transform child in _content)
            Destroy(child.gameObject);

        List<EffectEntry> entries = CollectActiveEffectEntries();

        foreach (EffectEntry entry in entries)
        {
            GameObject row = Instantiate(_rowPrefab, _content);
            row.GetComponent<AlwaysEffectRowUI>().Setup(entry.label, entry.valueText, entry.isNegative);
        }
    }

    // 모든 학생의 activeEffectIds 수집 → effectId 중복 제거 → 표시 항목 변환
    private List<EffectEntry> CollectActiveEffectEntries()
    {
        List<EffectEntry> result = new List<EffectEntry>();

        if (StudentManager.Instance == null)
        {
            Debug.LogWarning("[AlwaysEffectPopup] StudentManager.Instance == null");
            return result;
        }

        AlwaysEffectTableSO effectTable = CachedSOData.Get<AlwaysEffectTableSO>();
        if (effectTable == null)
        {
            Debug.LogWarning("[AlwaysEffectPopup] AlwaysEffectTableSO == null");
            return result;
        }

        Dictionary<string, AlwaysEffectRow> effectMap = new Dictionary<string, AlwaysEffectRow>();

        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student.activeEffectIds == null || student.activeEffectIds.Count == 0)
            {
                Debug.Log($"[AlwaysEffectPopup] {student.studentName} — activeEffectIds 없음");
                continue;
            }

            foreach (string effectId in student.activeEffectIds)
            {
                Debug.Log($"[AlwaysEffectPopup] {student.studentName} — effectId: {effectId}");

                if (effectMap.ContainsKey(effectId))
                    continue;

                if (effectTable.TryGet(effectId, out AlwaysEffectRow effect))
                    effectMap[effectId] = effect;
                else
                    Debug.LogWarning($"[AlwaysEffectPopup] effectTable에서 '{effectId}'를 찾지 못함");
            }
        }

        Debug.Log($"[AlwaysEffectPopup] 최종 effectMap 수: {effectMap.Count}");

        foreach (KeyValuePair<string, AlwaysEffectRow> kv in effectMap)
            result.AddRange(BuildEntries(kv.Value));

        Debug.Log($"[AlwaysEffectPopup] 최종 표시 항목 수: {result.Count}");

        return result;
    }
    // AlwaysEffectRow 한 행 → 값이 있는 항목만 EffectEntry 리스트로 분해
    private static List<EffectEntry> BuildEntries(AlwaysEffectRow effect)
    {
        List<EffectEntry> list = new List<EffectEntry>();

        TryAddEntry(list, "훈련 효과 상승", effect.trainingIncrease, isNegative: false);
        TryAddEntry(list, "훈련 효과 감소", effect.trainingDecline, isNegative: true);
        TryAddEntry(list, "컨디션 회복 상승", effect.conditionRecoveryUp, isNegative: false);
        TryAddEntry(list, "컨디션 회복 감소", effect.conditionRecoveryDown, isNegative: true);
        TryAddEntry(list, "컨디션 증가", effect.conditionIncrease, isNegative: false);
        TryAddEntry(list, "컨디션 감소", effect.conditionDecline, isNegative: true);

        if (!effect.trainingState)
            list.Add(new EffectEntry { label = "훈련 불가", valueText = "!", isNegative = true });

        return list;
    }

    private static void TryAddEntry(List<EffectEntry> list, string label, float value, bool isNegative)
    {
        if (value <= 0f)
            return;

        list.Add(new EffectEntry
        {
            label = label,
            valueText = isNegative ? $"-{value}" : $"+{value}",
            isNegative = isNegative
        });
    }

    private struct EffectEntry
    {
        public string label;
        public string valueText;
        public bool isNegative;
    }
}