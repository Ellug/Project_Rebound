using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 졸업생 목록 팝업 (3학년 카드 이미지 목록)
// - RecruitmentPopup 패턴 재활용: 스크롤/카드 생성/ForceScrollTop
// - 선택/영입 로직은 제거 (표시 전용)
// - 하단 확인 버튼 -> 외부 콜백 실행
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

    private readonly List<GameObject> _spawnedCards = new();

    private List<Student> _graduates = new();
    private Action _onConfirmed;

    private bool _isInited;

    public override void Init()
    {
        if (_isInited)
            return;

        _isInited = true;

        base.Init();

        if (_btnConfirm != null)
        {
            _btnConfirm.onClick.RemoveAllListeners();
            _btnConfirm.onClick.AddListener(HandleConfirm);
        }

        if (_txtConfirm != null)
        {
            _txtConfirm.text = "확인";
        }
    }

    // 외부에서 졸업생 리스트 전달
    public void Setup(List<Student> graduates, Action onConfirmed)
    {
        _graduates = graduates ?? new List<Student>();
        _onConfirmed = onConfirmed;

        if (_txtTitle != null)
        {
            _txtTitle.text = "졸업생 목록";
        }

        BuildCards();
    }

    public override void Open()
    {
        base.Open();
        StartCoroutine(ForceScrollTopRoutineSafe());
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

    private void HandleConfirm()
    {
        if (_btnConfirm != null)
            _btnConfirm.interactable = false;   // 중복 클릭 방지

        _onConfirmed?.Invoke();
    }

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

            Button[] buttons = obj.GetComponentsInChildren<Button>(true);
            for (int b = 0; b < buttons.Length; b++)
            {
                buttons[b].interactable = false;
            }

            obj.SetActive(true);
            _spawnedCards.Add(obj);
        }
    }

    private void ClearCards()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            GameObject obj = _spawnedCards[i];
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        _spawnedCards.Clear();
    }

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