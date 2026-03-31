using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadPrefab : MonoBehaviour
{
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private TMP_Text _fileNumText;
    [SerializeField] private TMP_Text _saveTimeText;

    [Header("엔딩 완료 표시")]
    [SerializeField] private TMP_Text _endingBadgeText;

    private const string EndingLabel = "[엔딩] 끝이 아닌 시작";

    private int _slotIndex;
    private LoadUI _parent;
    private bool _bound;

    public void Initialize(PlayData data, LoadUI parent)
    {
        _slotIndex = data.slotIndex;
        _parent = parent;

        _fileNumText.text = $"FILE {data.slotIndex}: {data.playTime}";

        if (data.isEndingReached)
        {
            // 세이브 파일에 [엔딩] 끝이 아닌 시작 문구 추가 표시
            if (_endingBadgeText != null)
            {
                _endingBadgeText.text = EndingLabel;
                _endingBadgeText.gameObject.SetActive(true);
                _saveTimeText.text = data.saveTime;
            }
            else
            {
                // 별도 뱃지 TMP가 없으면 saveTime 줄에 인라인 표시
                _saveTimeText.text = $"{data.saveTime}　　{EndingLabel}";
            }

            // 기획서: 엔딩 완료 슬롯은 로드(클릭) 불가 — 비활성화
            if (_selectButton != null)
                _selectButton.interactable = false;
        }
        else
        {
            if (_endingBadgeText != null)
                _endingBadgeText.gameObject.SetActive(false);

            _saveTimeText.text = data.saveTime;

            if (_selectButton != null)
                _selectButton.interactable = true;
        }

        Bind();
    }

    private void Bind()
    {
        if (_bound) Unbind();

        _deleteButton.onClick.AddListener(OnDelete);
        _selectButton.onClick.AddListener(OnSelect);

        _bound = true;
    }

    private void Unbind()
    {
        _deleteButton.onClick.RemoveListener(OnDelete);
        _selectButton.onClick.RemoveListener(OnSelect);
        _bound = false;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void OnSelect()
    {
        if (SaveManager.Instance != null)
        {
            PlayData data = SaveSystem.Instance?.Load(_slotIndex);
            if (data != null && data.isEndingReached) return;
        }

        _parent?.OpenConfirmPanel(_slotIndex);
    }

    private void OnDelete()
    {
        _parent?.OpenDeletePanel(_slotIndex);
    }
}