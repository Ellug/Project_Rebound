using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public sealed class VNManager : MonoBehaviour
{
    private const string DefaultReturnSceneName = "Lobby";

    [SerializeField] private VNUI _vnUI;
    [SerializeField] private int _fallbackStoryId = 10001;
    [SerializeField] private float _inputLockDuration = 0.1f;
    [SerializeField] private float _typingCharactersPerSecond = 45f;
    [SerializeField] private Button _skipButton;

    private readonly List<StoryRow> _activeRows = new();

    private string _returnSceneName = DefaultReturnSceneName;
    private int _nextRowIndex;
    private bool _isWaitingForFinishTap;
    private bool _isCompleted;
    private bool _isTypingLine;
    private float _inputUnlockTime;
    private Coroutine _typingCoroutine;

    // 시나리오를 초기화하고 첫 줄 출력
    void Start()
    {
        InitializeScenario();
        ShowNextLineOrFinish();
    }

    // 입력 잠금 해제 이후 탭/클릭 입력으로 다음 줄 진행
    void Update()
    {
        if (_isCompleted) return;
        if (Time.unscaledTime < _inputUnlockTime) return;
        if (!IsAdvanceInputTriggered()) return;

        ShowNextLineOrFinish();
    }

    // 브릿지 요청과 테이블 데이터를 기반으로 현재 시나리오 구성
    private void InitializeScenario()
    {
        int storyId = _fallbackStoryId;
        _returnSceneName = DefaultReturnSceneName;

        if (VNBridge.TryConsumeRequest(out int requestedStoryId, out string returnSceneName))
        {
            storyId = requestedStoryId;
            if (!string.IsNullOrWhiteSpace(returnSceneName))
                _returnSceneName = returnSceneName.Trim();
        }

        if (storyId <= 0)
            storyId = _fallbackStoryId;

        if (string.IsNullOrWhiteSpace(_returnSceneName))
            _returnSceneName = DefaultReturnSceneName;

        if (SaveManager.Instance != null)
            SaveManager.Instance.MarkVnStoryPlayed(storyId);

        _activeRows.Clear();

        StoryTableSO table = CachedSOData.Get<StoryTableSO>();
        if (table == null || table.Rows == null)
        {
            FinishScenario();
            return;
        }

        for (int i = 0; i < table.Rows.Count; i++)
        {
            StoryRow row = table.Rows[i];
            if (row.id != storyId)
                continue;

            _activeRows.Add(row);
        }

        _activeRows.Sort((a, b) => a.line.CompareTo(b.line));
        _nextRowIndex = 0;
        _isWaitingForFinishTap = false;
        _isCompleted = false;
        _isTypingLine = false;
        StopTypingRoutine();
        _inputUnlockTime = Time.unscaledTime + _inputLockDuration;

        if (_skipButton != null)
            _skipButton.gameObject.SetActive(true);

        if (_activeRows.Count == 0)
            FinishScenario();
    }

    // 다음 대사를 보여주거나 시나리오 종료로 전환
    private void ShowNextLineOrFinish()
    {
        if (_isCompleted) return;
        if (_vnUI == null) return;

        if (_isTypingLine)
        {
            CompleteCurrentTyping();
            return;
        }

        if (_isWaitingForFinishTap)
        {
            FinishScenario();
            return;
        }

        if (_nextRowIndex >= _activeRows.Count)
        {
            FinishScenario();
            return;
        }

        StoryRow row = _activeRows[_nextRowIndex];
        _nextRowIndex++;

        int characterCount = _vnUI.RenderLineForTyping(row);
        HandleLineAudio(row);
        StartTyping(characterCount);

        if (_nextRowIndex >= _activeRows.Count)
            _isWaitingForFinishTap = true;
    }

    // 현재 대사의 오디오 재생 요청을 처리
    private void HandleLineAudio(StoryRow row)
    {
        if (row.bgmIndex > 0)
            PlayBgmByIndex(row.bgmIndex);

        if (!string.IsNullOrWhiteSpace(row.sfxName))
            PlaySfxByName(row.sfxName);
    }

    // TODO: 실제 오디오 매니저 연동 지점
    // BGM 인덱스를 실제 오디오 시스템으로 전달
    private void PlayBgmByIndex(int bgmIndex)
    {
    }

    // TODO: 실제 오디오 매니저 연동 지점
    // SFX 이름을 실제 오디오 시스템으로 전달
    private void PlaySfxByName(string sfxName)
    {
    }

    // 스킵 버튼 클릭 시 현재 VN을 완료 처리하고 복귀 씬으로 이동
    public void HandleSkipButtonClicked()
    {
        FinishScenario();
    }

    // 대사를 한 글자씩 노출하는 코루틴 시작
    private void StartTyping(int characterCount)
    {
        StopTypingRoutine();

        if (characterCount <= 0 || _typingCharactersPerSecond <= 0f)
        {
            _vnUI.RevealCurrentDialogueInstantly();
            _isTypingLine = false;
            return;
        }

        _typingCoroutine = StartCoroutine(TypeDialogueRoutine(characterCount));
    }

    // 클릭 시 현재 타이핑을 즉시 완료
    private void CompleteCurrentTyping()
    {
        if (!_isTypingLine) return;

        StopTypingRoutine();
        _vnUI.RevealCurrentDialogueInstantly();
        _isTypingLine = false;
    }

    // 실행 중인 타이핑 코루틴 정리
    private void StopTypingRoutine()
    {
        if (_typingCoroutine == null) return;

        StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
    }

    // 현재 대사의 글자를 시간에 따라 점진적으로 노출
    private IEnumerator TypeDialogueRoutine(int characterCount)
    {
        _isTypingLine = true;

        float visibleCharacters = 0f;
        float charsPerSecond = Mathf.Max(1f, _typingCharactersPerSecond);

        while (visibleCharacters < characterCount)
        {
            visibleCharacters += charsPerSecond * Time.unscaledDeltaTime;
            _vnUI.SetCurrentDialogueVisibleCharacters(Mathf.FloorToInt(visibleCharacters));
            yield return null;
        }

        _vnUI.RevealCurrentDialogueInstantly();
        _isTypingLine = false;
        _typingCoroutine = null;
    }

    // 현재 VN을 종료하고 복귀 씬으로 이동
    private void FinishScenario()
    {
        if (_isCompleted) return;

        StopTypingRoutine();
        _isTypingLine = false;
        _isCompleted = true;

        // SceneTransitionManager 경유 시 SceneRoot 스케일 전환 연출이 적용됨
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(_returnSceneName);
        else
            SceneManager.LoadScene(_returnSceneName);
    }

    // 다음 줄 진행용 입력(마우스/터치 시작)을 감지
    private static bool IsAdvanceInputTriggered()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null) return false;

        for (int i = 0; i < touchscreen.touches.Count; i++)
            if (touchscreen.touches[i].press.wasPressedThisFrame) return true;

        return false;
    }
}
