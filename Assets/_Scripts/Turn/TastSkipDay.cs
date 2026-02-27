using TMPro;
using UnityEngine;

public class TastSkipDay : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TurnManager turnManager;

    public void OpenDaySkipPanel()
    {
        panel.SetActive(true);
        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void CloseDaySkipPanel()
    {
        panel.SetActive(false);
    }

    public void Confirm()
    {
        if (int.TryParse(inputField.text, out int days))
        {
            turnManager.SkipDays(days);
        }

        CloseDaySkipPanel();
    }
}