using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LoadPrefab : MonoBehaviour
{
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private TMP_Text _fileNumText;
    [SerializeField] private TMP_Text _playTimeText;
    [SerializeField] private TMP_Text _schoolText;
    [SerializeField] private TMP_Text _saveTimeText;

    private int _slotIndex;
    private LoadUI _parent;
    private bool _bound;

    public void Initialize(TestSaveSlotViewData data, LoadUI parent)
    {
        _slotIndex = data.slotIndex;
        _parent = parent;

        _fileNumText.text = $"FILE {data.slotIndex}";
        _playTimeText.text = data.playTime;
        _schoolText.text = data.school;
        _saveTimeText.text = data.saveTime;

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
        _parent?.OpenConfirmPanel(_slotIndex);
    }

    private void OnDelete()
    {
        _parent?.OnClickDelete(_slotIndex);
    }
}
