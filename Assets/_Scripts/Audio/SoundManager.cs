using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SoundManager : Singleton<SoundManager>
{
    private const int UiTouchSfxId = 201;
    private const int StatUpSfxId = 206;
    private const int StatExpUpSfxId = 207;
    private const float UiTouchSuppressDuration = 0.05f;

    private static readonly int[] DefaultPreloadAudioIds =
    {
        101, 102, 103, 104, 105, 106,
        201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213,
        301, 302, 303, 304, 305
    };

    // 아래 SFX가 재생되는 클릭에서는 201(터치음)을 겹치지 않게 막는다.
    private static readonly HashSet<int> TouchSfxNoOverlapIds = new()
    {
        202, 203, 204, 205, 209, 210, 211, 213, 304
    };

    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioClip[] _preloadClips;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _effectSource;

    private Dictionary<string, AudioClip> _clipCache;
    private Coroutine _bgmFadeRoutine;
    private int _bgmRequestToken;
    private bool _preloadRequested;
    private int _pendingUiTouchFrame = -1;
    private int _touchSuppressFrame = -1;
    private int _statUpSfxFrame = -1;
    private int _statExpUpSfxFrame = -1;
    private float _touchSuppressUntilTime = -1f;

    private float lastEffectVolume = 1f;
    private float lastBGMVolume = 1f;


    // BGM은 반복 재생, 효과음은 한 번만
    protected override void Awake()
    {
        base.Awake();

        _clipCache = new Dictionary<string, AudioClip>();

        foreach (var clip in _preloadClips)
        {
            if (!_clipCache.ContainsKey(clip.name))
                _clipCache.Add(clip.name, clip);
        }
    }

    // 저장된 볼륨과 mute 상태 확인
    void Start()
    {
        float bgm = PlayerPrefs.GetFloat("BGM_VOL", 1f);
        float effect = PlayerPrefs.GetFloat("EFFECT_VOL", 1f);

        bool bgmMute = PlayerPrefs.GetInt("BGM_MUTE", 0) == 1;
        bool effectMute = PlayerPrefs.GetInt("EFFECT_MUTE", 0) == 1;

        ApplyVolume(SoundType.BGM, bgmMute ? 0f : bgm);
        ApplyVolume(SoundType.EFFECT, effectMute ? 0f : effect);

        TryPreloadAddressableAudio();
    }

    void Update()
    {
        if (IsUiPointerReleasedThisFrame())
            _pendingUiTouchFrame = Time.frameCount;
    }

    void LateUpdate()
    {
        if (_pendingUiTouchFrame != Time.frameCount)
            return;

        _pendingUiTouchFrame = -1;
        PlayEffect(UiTouchSfxId);
    }

    private float NormalizedToDB(float value)
    {
        if (value <= 0.0001f)
            return -80f;
            
            return Mathf.Clamp(Mathf.Log10(value) * 20f, -80f, 0f);
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
            return clip;

        Debug.LogWarning($"클립 없음: {clipName}");
        return null;
    }

    public void PlayEffect(int clipId)
    {
        if (clipId <= 0) return;

        if (ShouldSuppressTouchSfx(clipId))
            return;

        MarkTouchSuppressionIfNeeded(clipId);

        if (AddressableAudioManager.Instance.TryGetCachedClip(clipId, out AudioClip cached) && cached != null)
        {
            _effectSource.PlayOneShot(cached);
            return;
        }

        AddressableAudioManager.Instance.LoadClip(clipId, loaded =>
        {
            if (loaded == null) return;

            _effectSource.PlayOneShot(loaded);
        });
    }

    public void PlayEffect(string clipName)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null) return;

        _effectSource.PlayOneShot(clip);
    }

    public void PlayStatUpSfx()
    {
        PlayDedupedSfx(StatUpSfxId, ref _statUpSfxFrame);
    }

    public void PlayStatExpUpSfx()
    {
        PlayDedupedSfx(StatExpUpSfxId, ref _statExpUpSfxFrame);
    }

    public void PlayBGM(int clipId)
    {
        if (clipId <= 0) return;

        _bgmRequestToken++;
        int requestToken = _bgmRequestToken;

        if (AddressableAudioManager.Instance.TryGetCachedClip(clipId, out AudioClip cached) && cached != null)
        {
            ApplyBgmClip(cached);
            return;
        }

        AddressableAudioManager.Instance.LoadClip(clipId, loaded =>
        {
            if (requestToken != _bgmRequestToken) return;
            if (loaded == null) return;

            ApplyBgmClip(loaded);
        });
    }

    public void PlayBGM(string clipName)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null) return;

        ApplyBgmClip(clip);
    }

    public void StopBGM()
    {
        if (_bgmFadeRoutine != null)
        {
            StopCoroutine(_bgmFadeRoutine);
            _bgmFadeRoutine = null;
        }

        _bgmSource.Stop();
        _bgmSource.clip = null;
    }

    // 현재 재생 중인 효과음을 정지
    public void StopEffect()
    {
        if (_effectSource == null) return;

        _effectSource.Stop();
        _effectSource.clip = null;
    }

    public void FadeOutBGM(float duration = 0.2f)
    {
        if (!_bgmSource.isPlaying) return;

        if (duration <= 0f)
        {
            StopBGM();
            return;
        }

        if (_bgmFadeRoutine != null)
            StopCoroutine(_bgmFadeRoutine);

        _bgmFadeRoutine = StartCoroutine(FadeOutBgmRoutine(duration));
    }

    private void ApplyBgmClip(AudioClip clip)
    {
        if (clip == null) return;

        if (_bgmFadeRoutine != null)
        {
            StopCoroutine(_bgmFadeRoutine);
            _bgmFadeRoutine = null;
        }

        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
            return;

        _bgmSource.volume = 1f;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    private IEnumerator FadeOutBgmRoutine(float duration)
    {
        float startVolume = _bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.clip = null;
        _bgmSource.volume = startVolume;
        _bgmFadeRoutine = null;
    }

    private void TryPreloadAddressableAudio()
    {
        if (_preloadRequested) return;

        _preloadRequested = true;

        AddressableAudioManager.Instance.PreloadClips(DefaultPreloadAudioIds);
    }

    private bool ShouldSuppressTouchSfx(int clipId)
    {
        if (clipId != UiTouchSfxId) return false;
        if (_touchSuppressFrame == Time.frameCount) return true;

        return _touchSuppressUntilTime > Time.unscaledTime;
    }

    private void MarkTouchSuppressionIfNeeded(int clipId)
    {
        if (!TouchSfxNoOverlapIds.Contains(clipId)) return;

        _touchSuppressFrame = Time.frameCount;
        _touchSuppressUntilTime = Time.unscaledTime + UiTouchSuppressDuration;
    }

    private void PlayDedupedSfx(int clipId, ref int lastPlayedFrame)
    {
        if (lastPlayedFrame == Time.frameCount)
            return;

        lastPlayedFrame = Time.frameCount;
        PlayEffect(clipId);
    }

    private static bool IsUiPointerReleasedThisFrame()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasReleasedThisFrame && eventSystem.IsPointerOverGameObject())
            return true;

        Touchscreen touch = Touchscreen.current;
        if (touch == null)
            return false;

        for (int i = 0; i < touch.touches.Count; i++)
        {
            var touchControl = touch.touches[i];
            if (!touchControl.press.wasReleasedThisFrame)
                continue;

            int touchId = touchControl.touchId.ReadValue();
            if (eventSystem.IsPointerOverGameObject(touchId))
                return true;
        }

        return false;
    }
}
