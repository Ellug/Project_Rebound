using UnityEngine;
using UnityEngine.UI;

public class OptionUI : MonoBehaviour
{
    [SerializeField] private GameObject _optinonPanel;

    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _effectSlider;

    [SerializeField] private Toggle _bgmMuteToggle;
    [SerializeField] private Toggle _effectMuteToggle;

    void Start()
    {
        InitUI();
        BindEvents();
    }

    public void OptinonPanelOpen()
    {
        _optinonPanel.SetActive(true);
    }

    public void OptinonPanelClose()
    {
        _optinonPanel.SetActive(false);
    }
    private void InitUI()
    {
        float bgm = PlayerPrefs.GetFloat("BGM_VOL", 1f);
        float effect = PlayerPrefs.GetFloat("EFFECT_VOL", 1f);

        bool bgmMute = PlayerPrefs.GetInt("BGM_MUTE", 0) == 1;
        bool effectMute = PlayerPrefs.GetInt("EFFECT_MUTE", 0) == 1;

        _bgmSlider.value = bgm;
        _effectSlider.value = effect;

        _bgmMuteToggle.isOn = bgmMute;
        _effectMuteToggle.isOn = effectMute;

        // mute 상태면 슬라이더 비활성화
        _bgmSlider.interactable = !bgmMute;
        _effectSlider.interactable = !effectMute;
    }

    private void BindEvents()
    {
        _bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        _effectSlider.onValueChanged.AddListener(OnEffectVolumeChanged);

        _bgmMuteToggle.onValueChanged.AddListener(OnBgmMuteChanged);
        _effectMuteToggle.onValueChanged.AddListener(OnEffectMuteChanged);
    }

    private void OnBgmVolumeChanged(float value)
    {
        SoundManager.Instance.SetVolume(SoundType.BGM, value);
    }

    private void OnEffectVolumeChanged(float value)
    {
        SoundManager.Instance.SetVolume(SoundType.EFFECT, value);
    }

    private void OnBgmMuteChanged(bool isMute)
    {
        _bgmSlider.interactable = !isMute;
        SoundManager.Instance.SetBGMMute(isMute);
    }

    private void OnEffectMuteChanged(bool isMute)
    {
        _effectSlider.interactable = !isMute;
        SoundManager.Instance.SetEffectMute(isMute);
    }
}
