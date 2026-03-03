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
    [SerializeField] private Sprite _myMatchupSprite;
    [SerializeField] private Sprite _otherMatchupSprite;

    public void SetData(string upTeamName, string downTeamName, bool isHighlighted)
    {
        _upTeamText.text = upTeamName;
        _downTeamText.text = downTeamName;

        _backgroundImage.sprite = isHighlighted ? _myMatchupSprite : _otherMatchupSprite;
        _backgroundImage.color = Color.white;

        Color fontColor = isHighlighted ? Color.black : Color.white;
        _upTeamText.color = fontColor;
        _downTeamText.color = fontColor;
    }
}
