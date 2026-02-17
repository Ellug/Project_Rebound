using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MatchupContainerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text _upTeamText;
    [SerializeField] private TMP_Text _downTeamText;
    [SerializeField] private Image _backgroundImage;

    [Header("Style")]
    [SerializeField] private Color _myMatchupColor = Color.white;
    [SerializeField] private Color _otherMatchupColor = new(0.8f, 0.8f, 0.8f, 1f);

    public void SetData(string upTeamName, string downTeamName, bool isHighlighted)
    {
        _upTeamText.text = upTeamName;
        _downTeamText.text = downTeamName;
        _backgroundImage.color = isHighlighted ? _myMatchupColor : _otherMatchupColor;
    }
}
