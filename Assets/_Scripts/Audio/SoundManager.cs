using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioClip[] _preloadClips;

    private Dictionary<string, AudioClip> _clipCache;

    private AudioSource _bgmSource;
    private AudioSource _effectSource;

    private float lastEffectVolume = 1f;
    private float lastBGMVolume = 1f;


    // BGM은 반복 재생, 효과음은 한 번만
    protected override void Awake()
    {
        base.Awake();

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("BGM")[0];

        _effectSource = gameObject.AddComponent<AudioSource>();
        _effectSource.loop = false;
        _effectSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("EFFECT")[0];

        _clipCache = new Dictionary<string, AudioClip>();

        foreach (var clip in _preloadClips)
        {
            if (!_clipCache.ContainsKey(clip.name))
            {
                _clipCache.Add(clip.name, clip);
            }
        }
    }

    // 저장된 볼륨과 mute 상태 확인
    private void Start()
    {
        float bgm = PlayerPrefs.GetFloat("BGM_VOL", 1f);
        float effect = PlayerPrefs.GetFloat("EFFECT_VOL", 1f);

        bool bgmMute = PlayerPrefs.GetInt("BGM_MUTE", 0) == 1;
        bool effectMute = PlayerPrefs.GetInt("EFFECT_MUTE", 0) == 1;

        ApplyVolume(SoundType.BGM, bgmMute ? 0f : bgm);
        ApplyVolume(SoundType.EFFECT, effectMute ? 0f : effect);
    }

    private float NormalizedToDB(float value)
    {
        if (value <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Lerp(-80f, 0f, value);
    }

    private void ApplyVolume(SoundType type, float normalized)
    {
        float db = NormalizedToDB(normalized);
        _audioMixer.SetFloat(type.ToString(), db);
    }

    public void SetVolume(SoundType type, float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        if (type == SoundType.BGM)
        {
            PlayerPrefs.SetFloat("BGM_VOL", normalized);
        }

        else
        {
            PlayerPrefs.SetFloat("EFFECT_VOL", normalized);
        }

        ApplyVolume(type, normalized);
    }

    // BGM 음소거
    public void SetBGMMute(bool isMute)
    {
        PlayerPrefs.SetInt("BGM_MUTE", isMute ? 1 : 0);

        if (isMute)
        {
            lastBGMVolume = PlayerPrefs.GetFloat("BGM_VOL", 1f);
            ApplyVolume(SoundType.BGM, 0f);
        }
        else
        {
            ApplyVolume(SoundType.BGM, lastBGMVolume);
        }
    }

    // 효과음 음소거
    public void SetEffectMute(bool isMute)
    {
        PlayerPrefs.SetInt("EFFECT_MUTE", isMute ? 1 : 0);

        if (isMute)
        {
            lastEffectVolume = PlayerPrefs.GetFloat("EFFECT_VOL", 1f);
            ApplyVolume(SoundType.EFFECT, 0f);
        }
        else
        {
            ApplyVolume(SoundType.EFFECT, lastEffectVolume);
        }
    }

    private AudioClip GetClip(string clipName)
    {
        if (_clipCache.TryGetValue(clipName, out var clip))
        {
            return clip;
        }

        clip = Resources.Load<AudioClip>($"Sound/BGM/{clipName}");

        if (clip == null)
        {
            clip = Resources.Load<AudioClip>($"Sound/SFX/{clipName}");
        }

        if (clip != null)
        {
            _clipCache.Add(clipName, clip);
            return clip;
        }

        Debug.LogWarning($"클립 없음: {clipName}");
        return null;
    }

    public void PlayEffect(string clipName)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null)
        {
            return;
        }

        _effectSource.PlayOneShot(clip);
    }

    public void PlayBGM(string clipName)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null)
        {
            return;
        }
        
        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
        {
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }
}