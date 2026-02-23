using TMPro;
using UnityEngine;

public class SelectStudentStatRow : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtStatLabel;
    [SerializeField] private TMP_Text _txtStatValue;

    public void Setup(string label, int value)
    {
        if (_txtStatLabel != null)
        {
            _txtStatLabel.text = label;
            _txtStatLabel.raycastTarget = false;
        }

        if (_txtStatValue != null)
        {
            _txtStatValue.text = value.ToString();
            _txtStatValue.raycastTarget = false;
        }
    }
}