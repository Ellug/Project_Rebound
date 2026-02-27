using TMPro;
using UnityEngine;



public class LobbyMessageController : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private TextTyperTMP typer;

    public void Show(string message, bool animate = true)
    {
        
        if (animate && typer != null)
        {
            typer.Play(message);
        }
        else if (tmpText != null)
        {
            tmpText.text = message;
        }
    }
}