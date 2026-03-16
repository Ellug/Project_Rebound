using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 하프타임 작전타임 선택 UI
// 선택지 패널에서 버튼을 클릭하면 확인/취소 팝업이 뜨고, 확인 시 선택 확정
public class TournamentHalfTimeSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject _selectionPanel;
    [SerializeField] private Button _button1;
    [SerializeField] private Button _button2;
    [SerializeField] private Button _button3;

    // CachedSOData에서 로드한 선택지 row 목록 (최대 3개)
    private readonly List<HalftimeSelectRow> _selectRows = new(3);

    // 버튼 선택 시 선택 텍스트를 전달. MatchGameManager가 구독해 로그 출력 후 경기 재개
    public event Action<string> OnSelectionMade;

    void Awake()
    {
        BindButton(_button1, 0);
        BindButton(_button2, 1);
        BindButton(_button3, 2);
    }

    public void Open()
    {
        LoadSelectRows();
        BindButton(_button1, 0);
        BindButton(_button2, 1);
        BindButton(_button3, 2);
        _selectionPanel.SetActive(true);
    }

    private void Close()
    {
        _selectionPanel.SetActive(false);
    }

    // CachedSOData에서 선택지 row를 랜덤으로 로드
    private void LoadSelectRows()
    {
        _selectRows.Clear();

        if (!CachedSOData.TryGet<HalftimeSelectTableSO>(out var table))
        {
            Debug.LogWarning("[TournamentHalfTimeSelectionUI] HalftimeSelectTableSO가 CachedSOData에 등록되지 않았습니다.");
            return;
        }

        // TODO: 선택지를 랜덤 또는 조건에 따라 여러 개 제공하도록 변경 예정
        List<HalftimeSelectRow> shuffled = new(table.Rows);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
        }

        int count = Mathf.Min(shuffled.Count, 3);
        for (int i = 0; i < count; i++)
            _selectRows.Add(shuffled[i]);
    }

    private void BindButton(Button button, int optionIndex)
    {
        if (button == null) return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = optionIndex < _selectRows.Count
                ? ResolveText(_selectRows[optionIndex].choiceName)
                : string.Empty;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnClickOption(optionIndex));
    }

    // Inspector의 버튼 OnClick에서 인덱스(0, 1, 2)로 연결
    private void OnClickOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= _selectRows.Count)
        {
            Debug.LogWarning($"[TournamentHalfTimeSelectionUI] 유효하지 않은 선택지 인덱스: {optionIndex}");
            return;
        }

        HalftimeSelectRow row = _selectRows[optionIndex];

        if (UIManager.Instance == null) return; // 싱글톤이라 없으면 안됨. 없으면 뭔가 잘못된 거임 혼남.

        var req = UIPopupRequest.Default(
            title: ResolveText(row.choiceName),
            message: ResolveText(row.description),
            onPrimary: () => ConfirmSelection(row),
            onCancel: () => { },
            subMessage: ResolveText(row.effectDescription),
            previewImageId: "EventGame00_img_halftime",
            showCancel: true,
            primaryKind: UIPopupRequest.PrimaryButtonKind.Confirm
        );

        UIManager.Instance.ShowPopup(req);
    }

    private void ConfirmSelection(HalftimeSelectRow row)
    {
        // effect1 / effect2 / effect3 기반 효과 실행
        ApplyHalftimeEffect(row.effect1);
        ApplyHalftimeEffect(row.effect2);
        ApplyHalftimeEffect(row.effect3);

        Close();
        OnSelectionMade?.Invoke(ResolveText(row.description));

        // 하프타임 선택지 선택 후 세이브
        if (SaveManager.Instance != null)
            SaveManager.Instance.AutoSaveByBranch("선택지 선택");
    }

    // SuddenEventEffectTableSO에서 effectId로 효과를 조회해 적용
    private static void ApplyHalftimeEffect(string effectId)
    {
        if (string.IsNullOrEmpty(effectId) || effectId == "-") return;

        // randomPlayerStat의 경우 테이블 조회 없이 직접 파싱해서 처리
        if (effectId.IndexOf("randomPlayerStat", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            int amount = ParseAmountFromEffectId(effectId);
            PlayerStat stat = PickRandomStudentStat();
            Debug.Log($"[TournamentHalfTimeSelectionUI] 랜덤 스탯 효과 적용 — effectId: {effectId}, stat: {stat}, amount: {amount}");
            ApplyPlayerOrStudentStat(stat, amount);
            return;
        }

        if (!CachedSOData.TryGet<SuddenEventEffectTableSO>(out var effectTable))
        {
            Debug.LogWarning("[TournamentHalfTimeSelectionUI] SuddenEventEffectTableSO가 CachedSOData에 등록되지 않았습니다.");
            return;
        }

        if (!effectTable.TryGet(effectId.Trim(), out var effectRow)) return;

        int parsedAmount = UnityEngine.Random.Range(effectRow.amountMin, effectRow.amountMax + 1);
        PlayerStat parsedStat = effectRow.targetMin == PlayerStat.None ? PickRandomStudentStat() : effectRow.targetMin;

        Debug.Log($"[TournamentHalfTimeSelectionUI] 효과 적용 — effectId: {effectId}, stat: {parsedStat}, amount: {parsedAmount}");
        ApplyPlayerOrStudentStat(parsedStat, parsedAmount);
    }

    // effectId에서 수치를 파싱 (예: event_effect_2_randomPlayerStat_p10 → 10, m5 → -5)
    private static int ParseAmountFromEffectId(string effectId)
    {
        int lastUnderscoreIndex = effectId.LastIndexOf('_');
        if (lastUnderscoreIndex < 0 || lastUnderscoreIndex >= effectId.Length - 1) return 0;

        string token = effectId.Substring(lastUnderscoreIndex + 1);
        if (token.Length < 2) return 0;

        char sign = token[0];
        if (!int.TryParse(token.Substring(1), out int value)) return 0;
        return sign == 'm' ? -value : value;
    }

    // PlayerStat 기준으로 플레이어 재화 또는 학생 스탯에 수치 적용
    private static void ApplyPlayerOrStudentStat(PlayerStat stat, int amount)
    {
        bool isPlayerStat = stat == PlayerStat.Money || stat == PlayerStat.Fame;

        if (isPlayerStat)
        {
            ApplyPlayerStat(stat, amount);
        }
        else
        {
            if (StudentManager.Instance == null)
            {
                Debug.LogWarning("[TournamentHalfTimeSelectionUI] StudentManager가 없습니다.");
                return;
            }

            foreach (Student student in StudentManager.Instance.Students)
            {
                ApplyStudentStat(student, stat, amount);
                Debug.Log($"[TournamentHalfTimeSelectionUI] 학생 스탯 적용 — 학생: {student.studentName}, stat: {stat}, amount: {amount}");
            }

            if (StudentManager.Instance.Students.Count > 0)
            {
                StudentManager.Instance.NotifyStudentModified(StudentManager.Instance.Students[0]);
            }
        }
    }

    private static void ApplyPlayerStat(PlayerStat stat, int amount)
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("[TournamentHalfTimeSelectionUI] MoneyManager가 없습니다.");
            return;
        }

        if (stat == PlayerStat.Money)
        {
            if (amount > 0)
            {
                MoneyManager.Instance.AddGold(amount);
            }
            else
            {
                MoneyManager.Instance.TrySpendGold(-amount);
            }    
        }
        else if (stat == PlayerStat.Fame)
        {
            if (amount > 0)
            {
                MoneyManager.Instance.AddReputation(amount);
            }
            else
            {
                MoneyManager.Instance.TrySpendReputation(-amount);
            }
        }
        Debug.Log($"[TournamentHalfTimeSelectionUI] 플레이어 스탯 적용 — stat: {stat}, amount: {amount}");
    }

    private static void ApplyStudentStat(Student student, PlayerStat stat, int amount)
    {
        switch (stat)
        {
            case PlayerStat.Condition: student.condition = Student.ClampCondition(student.condition + amount); break;
            case PlayerStat.Mental: student.mental += amount; break;
            case PlayerStat.Shoot: student.shoot += amount; break;
            case PlayerStat.Speed: student.speed += amount; break;
            case PlayerStat.Jump: student.jump += amount; break;
            case PlayerStat.Stamina: student.stamina += amount; break;
        }
    }

    // 텍스트 테이블 id로 실제 문자열 조회. 조회 실패 시 id 원문 반환
    private static string ResolveText(string textId)
    {
        if (string.IsNullOrEmpty(textId) || textId == "-") return string.Empty;
        if (!CachedSOData.TryGet<HalftimeSelectTextTableSO>(out var textTable)) return textId;

        foreach (var row in textTable.Rows)
        {
            if (row.id == textId)
            {
                return row.description;
            }
        }

        return textId;
    }

    // Shoot / Speed / Jump / Condition / Stamina 중 랜덤으로 하나를 반환
    private static PlayerStat PickRandomStudentStat()
    {
        PlayerStat[] statPool =
        {
            PlayerStat.Shoot,
            PlayerStat.Speed,
            PlayerStat.Jump,
            PlayerStat.Condition,
            PlayerStat.Stamina
        };
        return statPool[UnityEngine.Random.Range(0, statPool.Length)];
    }
}