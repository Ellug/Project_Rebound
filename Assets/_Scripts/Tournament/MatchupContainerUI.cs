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
    [SerializeField] private Color _winnerFontColor = new Color32(0x1E, 0x90, 0xFF, 0xFF);
    [SerializeField] private Color _loserFontColor = new Color32(0xFF, 0x45, 0x00, 0xFF);

    public void SetData(string upTeamName, string downTeamName, bool isHighlighted)
    {
        SetData(upTeamName, downTeamName, isHighlighted, isResolved: false, isUpTeamWinner: false);
    }

    public void SetData(string upTeamName, string downTeamName, bool isHighlighted, bool isResolved, bool isUpTeamWinner)
    {
        _upTeamText.text = upTeamName;
        _downTeamText.text = downTeamName;

        if (isResolved)
        {
            // 완료된 매치는 결과 색(승: 파랑, 패: 빨강)으로 고정해 기록 남김
            _backgroundImage.sprite = _otherMatchupSprite;
            _upTeamText.color = isUpTeamWinner ? _winnerFontColor : _loserFontColor;
            _downTeamText.color = isUpTeamWinner ? _loserFontColor : _winnerFontColor;
            return;
        }

        _backgroundImage.sprite = isHighlighted ? _myMatchupSprite : _otherMatchupSprite;

        Color fontColor = isHighlighted ? Color.black : Color.white;
        _upTeamText.color = fontColor;
        _downTeamText.color = fontColor;
    }
}
