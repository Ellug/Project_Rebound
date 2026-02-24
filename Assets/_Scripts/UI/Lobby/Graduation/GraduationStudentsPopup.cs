using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// 졸업생 목록 표시 전용 팝업
// - 3학년 학생 카드 나열
// - 하단 확인 버튼 → 외부 콜백 실행
public class GraduationStudentsPopup : UIBase
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect _scrollRect;

    [Header("Header")]
    [SerializeField] private TMP_Text _txtTitle;

    [Header("Card")]
    [SerializeField] private Transform _cardRoot;
    [SerializeField] private GameObject _cardPrefab;

    [Header("Buttons")]
    [SerializeField] private Button _btnConfirm;
    [SerializeField] private TMP_Text _txtConfirm;

    // 생성된 카드 캐싱
    private readonly List<GameObject> _spawnedCards = new();

    // 외부에서 전달받은 졸업생 목록
    private List<Student> _graduates = new();

    // 확인 버튼 클릭 시 실행될 콜백
    private Action _onConfirmed;

    private bool _isInited;

    public override void Init()
    {
        if (_isInited)
            return;

        _isInited = true;

        base.Init();

        // 확인 버튼 바인딩
        if (_btnConfirm != null)
        {
            _btnConfirm.onClick.RemoveAllListeners();
            _btnConfirm.onClick.AddListener(HandleConfirm);
        }

        if (_txtConfirm != null)
            _txtConfirm.text = "확인";
    }

    // 졸업생 목록 설정
    public void Setup(List<Student> graduates, Action onConfirmed)
    {
        _graduates = graduates ?? new List<Student>();
        _onConfirmed = onConfirmed;

        if (_txtTitle != null)
            _txtTitle.text = "졸업생 목록";

        BuildCards();
    }

    public override void Open()
    {
        base.Open();
        StartCoroutine(ForceScrollTopRoutineSafe()); // 스크롤 최상단 고정
    }

    public override void Close()
    {
        base.Close();

        if (_btnConfirm != null)
            _btnConfirm.interactable = true;

        ClearCards();
        _graduates.Clear();
        _onConfirmed = null;
    }

    // 확인 버튼 클릭
    private void HandleConfirm()
    {
        if (_btnConfirm != null)
            _btnConfirm.interactable = false; // 중복 클릭 방지

        _onConfirmed?.Invoke();
    }

    // 졸업생 카드 생성
    private void BuildCards()
    {
        ClearCards();

        if (_cardPrefab == null || _cardRoot == null)
        {
            Debug.LogWarning("[GraduationStudentsPopup] 카드 프리팹/루트가 없습니다.");
            return;
        }

        if (_graduates == null || _graduates.Count == 0)
        {
            Debug.Log("[GraduationStudentsPopup] 졸업생이 없습니다.");
            return;
        }

        for (int i = 0; i < _graduates.Count; i++)
        {
            Student student = _graduates[i];
            if (student == null)
                continue;

            GameObject obj = Instantiate(_cardPrefab, _cardRoot);
            StudentCard card = obj.GetComponent<StudentCard>();

            if (card != null)
            {
                card.SetStudentData(student);
                card.SetViewState(StudentCard.CardViewState.Normal);
            }

            // 카드 내부 버튼 비활성화 (읽기 전용)
            Button[] buttons = obj.GetComponentsInChildren<Button>(true);
            for (int b = 0; b < buttons.Length; b++)
                buttons[b].interactable = false;

            obj.SetActive(true);
            _spawnedCards.Add(obj);
        }
    }

    // 생성된 카드 정리
    private void ClearCards()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            if (_spawnedCards[i] != null)
                Destroy(_spawnedCards[i]);
        }

        _spawnedCards.Clear();
    }

    // UI 레이아웃 갱신 후 스크롤 위치 보정
    private IEnumerator ForceScrollTopRoutineSafe()
    {
        yield return null;

        if (!isActiveAndEnabled)
            yield break;

        Canvas.ForceUpdateCanvases();
        ForceScrollTop();

        yield return null;

        if (!isActiveAndEnabled)
            yield break;

        Canvas.ForceUpdateCanvases();
        ForceScrollTop();
    }

    private void ForceScrollTop()
    {
        if (_scrollRect == null)
            return;

        _scrollRect.StopMovement();
        _scrollRect.verticalNormalizedPosition = 1f;
        _scrollRect.velocity = Vector2.zero;
    }
}