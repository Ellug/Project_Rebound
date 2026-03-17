using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class VNManager : MonoBehaviour
{
    private const string DefaultReturnSceneName = "Lobby";

    [SerializeField] private VNUI _vnUI;
    [SerializeField] private int _fallbackStoryId = 10001;
    [SerializeField] private float _inputLockDuration = 0.1f;

    private readonly List<StoryRow> _activeRows = new();

    private string _returnSceneName = DefaultReturnSceneName;
    private int _nextRowIndex;
    private bool _isWaitingForFinishTap;
    private bool _isCompleted;
    private float _inputUnlockTime;

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
        _inputUnlockTime = Time.unscaledTime + _inputLockDuration;

        if (_activeRows.Count == 0)
            FinishScenario();
    }

    // 다음 대사를 보여주거나 시나리오 종료로 전환
    private void ShowNextLineOrFinish()
    {
        if (_isCompleted) return;

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

        _vnUI.RenderLine(row);

        HandleLineAudio(row);

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

    // 현재 VN을 종료하고 복귀 씬으로 이동
    private void FinishScenario()
    {
        if (_isCompleted) return;

        _isCompleted = true;
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


