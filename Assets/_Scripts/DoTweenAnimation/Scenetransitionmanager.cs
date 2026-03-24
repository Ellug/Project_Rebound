using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

// 씬 전환 전체 흐름 관리 싱글톤
// 각 씬의 Canvas 하위 SceneRoot(태그: SceneRoot)를 X Scale로 찌그러뜨렸다 펴는 연출
// 사용법: SceneTransitionManager.Instance.LoadScene("Lobby");
public class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    [Header("전환 설정")]
    [SerializeField] private float _outDuration = 0.3f;  // 현재 씬 퇴장 (찌그러짐)
    [SerializeField] private float _inDuration = 0.45f;  // 다음 씬 등장 (펴짐)
    [SerializeField] private Ease _outEase = Ease.InCubic;
    [SerializeField] private Ease _inEase = Ease.OutCubic;

    private bool _isTransitioning;

    // 씬에 배치 없이 첫 접근 시 Resources에서 자동 생성
    public static new SceneTransitionManager Instance
    {
        get
        {
            if (Singleton<SceneTransitionManager>.Instance == null)
                CreateFromResources();
            return Singleton<SceneTransitionManager>.Instance;
        }
    }

    private static bool _isCreating = false;

    private static void CreateFromResources()
    {
        // 중복 생성 방지 — Awake 실행 전 Instance에 재접근하는 경우 대응
        if (_isCreating) return;
        _isCreating = true;

        GameObject prefab = Resources.Load<GameObject>("SceneTransitionManager");
        if (prefab != null)
            Instantiate(prefab);
        else
        {
            GameObject go = new GameObject("SceneTransitionManager");
            go.AddComponent<SceneTransitionManager>();
        }

        _isCreating = false;
    }

    protected override void OnSingletonAwake() { }

    public void LoadScene(string sceneName, Action onMidpoint = null)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"[SceneTransitionManager] 이미 전환 중입니다. 중복 호출 무시: {sceneName}");
            return;
        }
        StartCoroutine(TransitionRoutine(sceneName, onMidpoint));
    }

    public void LoadScene(int sceneIndex, Action onMidpoint = null)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"[SceneTransitionManager] 이미 전환 중입니다. 중복 호출 무시: {sceneIndex}");
            return;
        }
        StartCoroutine(TransitionRoutine(sceneIndex, onMidpoint));
    }

    private IEnumerator TransitionRoutine(object sceneTarget, Action onMidpoint)
    {
        _isTransitioning = true;

        // 1. 현재 씬의 SceneRoot들을 X Scale 1→0 (찌그러짐)
        List<RectTransform> currentRoots = GetSceneRoots();
        yield return StartCoroutine(ScaleRoots(currentRoots, 1f, 0f, _outDuration, _outEase));

        onMidpoint?.Invoke();

        // 2. 씬 로드
        AsyncOperation op;
        if (sceneTarget is string sceneName)
            op = SceneManager.LoadSceneAsync(sceneName);
        else
            op = SceneManager.LoadSceneAsync((int)sceneTarget);

        yield return op;

        // 3. 새 씬 SceneRoot들을 X Scale 0→1 (펴짐)
        yield return null; // 새 씬 오브젝트 초기화 대기

        List<RectTransform> newRoots = GetSceneRoots();

        foreach (RectTransform root in newRoots)
            root.localScale = new Vector3(0f, 1f, 1f);

        yield return StartCoroutine(ScaleRoots(newRoots, 0f, 1f, _inDuration, _inEase));

        _isTransitioning = false;
    }

    // 태그 "SceneRoot"로 등록된 RectTransform 목록 수집
    // DontDestroyOnLoad 씬 오브젝트는 제외
    private List<RectTransform> GetSceneRoots()
    {
        List<RectTransform> result = new List<RectTransform>();

        GameObject[] roots = GameObject.FindGameObjectsWithTag("SceneRoot");
        foreach (GameObject go in roots)
        {
            if (go.scene.name == "DontDestroyOnLoad") continue;

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
                result.Add(rt);
        }

        return result;
    }

    private IEnumerator ScaleRoots(List<RectTransform> roots, float from, float to, float duration, Ease ease)
    {
        if (roots == null || roots.Count == 0)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        int completed = 0;
        int total = roots.Count;

        foreach (RectTransform root in roots)
        {
            if (root == null) { completed++; continue; }

            // 이전 씬 전환에서 남은 Tween 제거 후 새로 시작
            root.DOKill();
            root.localScale = new Vector3(from, 1f, 1f);
            RectTransform captured = root;
            captured.DOScaleX(to, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    // 완료 후 Scale을 정확히 목표값으로 리셋 (부동소수점 오차 방지)
                    captured.localScale = new Vector3(to, 1f, 1f);
                    completed++;
                });
        }

        yield return new WaitUntil(() => completed >= total);
    }
}