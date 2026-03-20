using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionUI : MonoBehaviour
{
    [SerializeField] private GameObject _optinonPanel;

    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _effectSlider;

    [SerializeField] private TMP_Text _bgmValueText;
    [SerializeField] private TMP_Text _effectValueText;

    [SerializeField] private Image _bgmIcon;
    [SerializeField] private Image _effectIcon;

    [SerializeField] private Button _bgmIconButton;
    [SerializeField] private Button _effectIconButton;

    [SerializeField] private Sprite _muteSprite;
    [SerializeField] private Sprite _volume1Sprite;
    [SerializeField] private Sprite _volume2Sprite;

    private bool _bgmMuted;
    private bool _effectMuted;

    void Start()
    {
        InitUI();
        BindEvents();
    }

    public void OptinonPanelOpen()
    {
        InitUI();
        SoundManager.Instance.PlayEffect(202);
        _optinonPanel.SetActive(true);
    }

    public void OptinonPanelClose()
    {
        SoundManager.Instance.PlayEffect(203);
        _optinonPanel.SetActive(false);
    }
    private void InitUI()
    {
        float bgm = PlayerPrefs.GetFloat("BGM_VOL", 1f);
        float effect = PlayerPrefs.GetFloat("EFFECT_VOL", 1f);

        _bgmSlider.value = bgm;
        _effectSlider.value = effect;

        _bgmMuted = PlayerPrefs.GetInt("BGM_MUTE", 0) == 1;
        _effectMuted = PlayerPrefs.GetInt("EFFECT_MUTE", 0) == 1;

        _bgmSlider.interactable = !_bgmMuted;
        _effectSlider.interactable = !_effectMuted;

        UpdateVolumeText();
        UpdateVolumeIcon(_bgmSlider.value, _bgmIcon, _bgmMuted);
        UpdateVolumeIcon(_effectSlider.value, _effectIcon, _effectMuted);
    }

    private void BindEvents()
    {
        _bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        _effectSlider.onValueChanged.AddListener(OnEffectVolumeChanged);

        _bgmIconButton.onClick.AddListener(OnBgmIconClicked);
        _effectIconButton.onClick.AddListener(OnEffectIconClicked);
    }

    private void OnBgmVolumeChanged(float value)
    {
        SoundManager.Instance.SetVolume(SoundType.BGM, value);

        UpdateVolumeText();
        UpdateVolumeIcon(value, _bgmIcon, _bgmMuted);
    }

    private void OnEffectVolumeChanged(float value)
    {
        SoundManager.Instance.SetVolume(SoundType.EFFECT, value);

        UpdateVolumeText();
        UpdateVolumeIcon(value, _effectIcon, _effectMuted);
    }

    private void OnBgmIconClicked()
    {
        _bgmMuted = !_bgmMuted;

        SoundManager.Instance.SetBGMMute(_bgmMuted);
        _bgmSlider.interactable = !_bgmMuted;

        if (_bgmMuted)
            _bgmValueText.text = "0";
        else
            UpdateVolumeText();

        UpdateVolumeIcon(_bgmSlider.value, _bgmIcon, _bgmMuted);
    }


    private void OnEffectIconClicked()
    {
        _effectMuted = !_effectMuted;

        SoundManager.Instance.SetEffectMute(_effectMuted);
        _effectSlider.interactable = !_effectMuted;

        if (_effectMuted)
            _effectValueText.text = "0";
        else
            UpdateVolumeText();

        UpdateVolumeIcon(_effectSlider.value, _effectIcon, _effectMuted);
    }

    private void UpdateVolumeIcon(float value, Image icon, bool isMuted)
    {
        if (isMuted || value == 0)
        {
            icon.sprite = _muteSprite;
            return;
        }

        int volume = Mathf.RoundToInt(value * 100f);

        if (volume <= 50)
            icon.sprite = _volume1Sprite;
        else
            icon.sprite = _volume2Sprite;
    }

    // 볼륨 텍스트 업데이트
    private void UpdateVolumeText()
    {
        int bgm = Mathf.RoundToInt(_bgmSlider.value * 100f);
        int effect = Mathf.RoundToInt(_effectSlider.value * 100f);

        _bgmValueText.text = bgm.ToString();
        _effectValueText.text = effect.ToString();
    }

}
