using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentStatusPopup : UIBase
{
    [Header("유니폼")]
    [SerializeField] private Image _uniformImage;
    [SerializeField] private Sprite[] _uniformSprites;          // 레벨별 아이템 이미지
    [SerializeField] private TMP_Text _uniformLevelText;        // +강화 수치
    [SerializeField] private TMP_Text _uniformEffectText;       // 효과 설명
    [SerializeField] private Slider _uniformProgressBar;        // 진행 게이지
    [SerializeField] private Image _uniformGaugeBackBg;         // 등급별로 바뀌는 게이지 백 배경

    [Header("농구공")]
    [SerializeField] private Image _basketballImage;
    [SerializeField] private Sprite[] _basketballSprites;
    [SerializeField] private TMP_Text _basketballLevelText;
    [SerializeField] private TMP_Text _basketballEffectText;
    [SerializeField] private Slider _basketballProgressBar;
    [SerializeField] private Image _basketballGaugeBackBg;

    [Header("농구화")]
    [SerializeField] private Image _shoesImage;
    [SerializeField] private Sprite[] _shoesSprites;
    [SerializeField] private TMP_Text _shoesLevelText;
    [SerializeField] private TMP_Text _shoesEffectText;
    [SerializeField] private Slider _shoesProgressBar;
    [SerializeField] private Image _shoesGaugeBackBg;

    [Header("닫기")]
    [SerializeField] private Button _btnClose;

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

    [Header("등급별 게이지 백 배경")]
    [SerializeField] private Sprite[] _gaugeBackTierSprites;    // 0=미강화, 1=노멀, 2=레어, 3=에픽, 4=유니크, 5=레전드, 6=얼티밋

    private const int MaxLevel = 14;
    private bool _isInited;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(Close);
        }

        if (_animator == null)
            _animator = GetComponent<PopupAnimator>();

        if (_animator != null)
            _animator.Initialize();
    }

    public override void Open()
    {
        Init();

        if (_animator != null)
            _animator.Initialize();

        base.Open();
        Refresh();

        if (_animator != null)
            _animator.PlayIn();
    }

    public override void Close()
    {
        if (_animator != null)
        {
            _animator.PlayOut(() => base.Close());
        }
        else
        {
            base.Close();
        }
    }

    public void Refresh()
    {
        var eq = EquipmentSystem.Instance;
        if (eq == null) return;

        RefreshCard(
            eq.UniformLevel,
            "category_001",
            _uniformImage,
            _uniformSprites,
            _uniformLevelText,
            _uniformEffectText,
            _uniformProgressBar,
            _uniformGaugeBackBg,
            GetUniformEffectText);

        RefreshCard(
            eq.BasketballLevel,
            "category_002",
            _basketballImage,
            _basketballSprites,
            _basketballLevelText,
            _basketballEffectText,
            _basketballProgressBar,
            _basketballGaugeBackBg,
            GetBasketballEffectText);

        RefreshCard(
            eq.ShoesLevel,
            "category_003",
            _shoesImage,
            _shoesSprites,
            _shoesLevelText,
            _shoesEffectText,
            _shoesProgressBar,
            _shoesGaugeBackBg,
            GetShoesEffectText);
    }

    private void RefreshCard(
        int level,
        string category,
        Image image,
        Sprite[] sprites,
        TMP_Text levelText,
        TMP_Text effectText,
        Slider progressBar,
        Image gaugeBackBg,
        System.Func<int, string> effectTextGetter)
    {
        // 아이템 썸네일: 레벨에 맞는 스프라이트 적용
        if (image != null && sprites != null && level >= 0 && level < sprites.Length)
        {
            var sprite = sprites[level];
            if (sprite != null)
                image.sprite = sprite;
        }

        // 강화 수치 텍스트
        if (levelText != null)
            levelText.text = $"+{level}";

        // 효과 설명
        if (effectText != null)
            effectText.text = effectTextGetter(level);

        // 진행 게이지
        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = MaxLevel;
            progressBar.value = Mathf.Clamp(level, 0, MaxLevel);
        }



        if (gaugeBackBg != null)
            gaugeBackBg.sprite = GetGaugeBackSpriteByLevel(level);
    }

    private string GetUniformEffectText(int level)
    {
        if (level <= 0) return "강화 시 명성치 상승";

        var row = GetRow("category_001", level);
        return row != null
            ? $"강화 시 명성치 상승 +{(int)row.amount1}"
            : "강화 시 명성치 상승";
    }

    private string GetBasketballEffectText(int level)
    {
        if (level <= 0) return "훈련 경험치 효율 +0%";

        var rate = EquipmentSystem.Instance.GetBasketballBonusRate();
        return $"훈련 경험치 효율 +{(rate - 1f) * 100f:0}%";
    }

    private string GetShoesEffectText(int level)
    {
        if (level <= 0) return "훈련 시 컨디션 소모량 감소 0%, 점프력 +0";

        var row = GetRow("category_003", level);
        float decay = row != null ? row.amount1 : 1f;
        int jump = row != null ? (int)row.amount : 0;

        return $"훈련 시 컨디션 소모량 감소 {(1f - decay) * 100f:0}%, 점프력 +{jump}";
    }

    private EquipmentUpgradeRow GetRow(string category, int level)
    {
        var table = CachedSOData.Get<EquipmentUpgradeTableSO>();
        if (table == null) return null;

        foreach (var r in table.Rows)
        {
            if (r.presentCategory == category && r.rank == level)
                return r;
        }

        return null;
    }
    private Sprite GetGaugeBackSpriteByLevel(int level)
    {
        if (_gaugeBackTierSprites == null || _gaugeBackTierSprites.Length < 8)
            return null;

        // 0=회색(미강화), 1=흰색(+1~+2), 2=녹색(+3~+4), 3=파란색(+5~+6),
        // 4=보라색(+7~+8), 5=주황색(+9~+10), 6=빨간색(+11~+12), 7=무지개/황금색(+13 이상)
        if (level <= 0) return _gaugeBackTierSprites[0];
        if (level <= 2) return _gaugeBackTierSprites[1];
        if (level <= 4) return _gaugeBackTierSprites[2];
        if (level <= 6) return _gaugeBackTierSprites[3];
        if (level <= 8) return _gaugeBackTierSprites[4];
        if (level <= 10) return _gaugeBackTierSprites[5];
        if (level <= 12) return _gaugeBackTierSprites[6];
        return _gaugeBackTierSprites[7];
    }
}