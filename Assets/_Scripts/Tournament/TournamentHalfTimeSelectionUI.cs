using System;
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


    // 선택지 데이터 구조체
    private struct HalfTimeOption
    {
        public string Name;        // 선택지 이름 (팝업 타이틀)
        public string Description; // 선택지 설명 (팝업 본문)
        public string LogText;     // 선택 확정 시 로그에 남길 텍스트

        // TODO: 데이터 테이블 연동 시 아래 필드 활성화
        // public int EffectId;    // 효과 테이블 ID (어떤 효과를 적용할지 테이블에서 조회)
        // public string MethodId; // 실행할 효과 메서드 식별자 (예: "BuffAttack", "DebuffEnemy")
    }

    // 선택지 목록 (추후 데이터 테이블 연동 시 교체)
    private static readonly HalfTimeOption[] Options = new[]
    {
        new HalfTimeOption
        {
            Name        = "1. 삭발 어택",
            Description = "상대 팀의 머리털을 다 밀어버린다.",
            LogText     = "상대 팀의 머리털을 다 밀어버렸다."
        },
        new HalfTimeOption
        {
            Name        = "2. 스테로이드 주입",
            Description = "빠르게 스테로이드를 주입해서 모두 슈퍼솔져가 된다.",
            LogText     = "빠르게 스테로이드를 주입해서 모두 슈퍼솔져가 됐다."
        },
        new HalfTimeOption
        {
            Name        = "3. 심판 매수",
            Description = "심판을 5000원 주고 매수한다.",
            LogText     = "심판을 5000원 주고 매수했다."
        },
    };

    // 버튼 선택 시 선택 텍스트를 전달. MatchGameManager가 구독해 로그 출력 후 경기 재개
    public event Action<string> OnSelectionMade;

    void Awake()
    {
        BindButton(_button1, 0);
        BindButton(_button2, 1);
        BindButton(_button3, 2);
    }

    private void BindButton(Button button, int optionIndex)
    {
        if (button == null) return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = Options[optionIndex].Name;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnClickOption(optionIndex));
    }

    public void Open()
    {
        _selectionPanel.SetActive(true);
    }

    private void Close()
    {
        _selectionPanel.SetActive(false);
    }

    // Inspector의 버튼 OnClick에서 인덱스(0, 1, 2)로 연결
    private void OnClickOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= Options.Length)
        {
            Debug.LogWarning($"[TournamentHalfTimeSelectionUI] 유효하지 않은 선택지 인덱스: {optionIndex}");
            return;
        }

        HalfTimeOption selected = Options[optionIndex];

        // TODO: 선택지를 랜덤 또는 조건에 따라 여러 개 제공하도록 변경 예정

        if (UIManager.Instance == null) return; // 싱글톤이라 없으면 안됨. 없으면 뭔가 잘못된 거임 혼남.

        UIPopupRequest request = UIPopupRequest.Default(
            title: selected.Name,
            message: selected.Description,
            subMessage: null,
            previewSprite: null,
            onPrimary: () => ConfirmSelection(selected),
            onCancel: null
        );

        request.ShowCancel = true;
        request.AutoCloseOnPrimary = true;
        request.AutoCloseOnCancel = true;
        request.PrimaryInteractable = true;
        request.PrimaryKind = UIPopupRequest.PrimaryButtonKind.Confirm;

        UIManager.Instance.ShowPopup(request);
    }

    private void ConfirmSelection(HalfTimeOption option)
    {
        // TODO: option.EffectId 또는 option.MethodId를 이용해 효과 테이블 조회 후 실행
        // HalfTimeEffectTable.Execute(option.EffectId, _context);
        // HalfTimeEffectDispatcher.Dispatch(option.MethodId);

        Close();
        OnSelectionMade?.Invoke(option.LogText);
    }
}