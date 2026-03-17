using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class VNUI : MonoBehaviour
{
    [SerializeField] private GameObject _leftTextContainer;
    [SerializeField] private GameObject _rightTextContainer;
    [SerializeField] private GameObject _narTextContainer;

    [SerializeField] private TMP_Text _leftName;
    [SerializeField] private TMP_Text _rightName;

    [SerializeField] private TMP_Text _leftText;
    [SerializeField] private TMP_Text _rightText;
    [SerializeField] private TMP_Text _narText;

    [SerializeField] private Image _bg;
    [SerializeField] private Image _leftImage;
    [SerializeField] private Image _rightImage;
    private int _bgImageRequestToken;
    private int _leftImageRequestToken;
    private int _rightImageRequestToken;

    private enum CharacterImageSlot
    {
        Left,
        Right,
        Background
    }

    // UI 참조를 검증하고 초기 표시 상태를 리셋
    void Awake()
    {
        SetContainerStates(false, false, false);
        ClearDialogueText();
        ClearCharacterImage(_bg);
        ClearCharacterImage(_leftImage);
        ClearCharacterImage(_rightImage);
    }

    // 전달받은 대사 행을 화자 타입에 맞게 화면에 렌더링
    public void RenderLine(StoryRow row)
    {
        string speaker = NormalizeSpeaker(row.name);
        string dialogue = row.context ?? string.Empty;
        bool hasLeftImage = HasImageKey(row.imgLeft);
        bool hasRightImage = HasImageKey(row.imgRight);

        if (hasLeftImage)           ShowLeftDialogue(speaker, dialogue);
        else if (hasRightImage)     ShowRightDialogue(speaker, dialogue);
        else                        ShowNarration(dialogue);

        ApplyBackgroundImage(row.bgImg);
        ApplyLeftCharacterImage(row.imgLeft);
        ApplyRightCharacterImage(row.imgRight);
    }

    // 나레이션 UI만 표시하고 관련 텍스트 갱신
    private void ShowNarration(string dialogue)
    {
        SetContainerStates(false, false, true);

        SetText(_leftName, string.Empty);
        SetText(_rightName, string.Empty);
        SetText(_leftText, string.Empty);
        SetText(_rightText, string.Empty);
        SetText(_narText, dialogue);
    }

    // 좌측 화자 UI를 표시하고 텍스트 갱신
    private void ShowLeftDialogue(string speaker, string dialogue)
    {
        SetContainerStates(true, false, false);

        SetText(_leftName, speaker);
        SetText(_leftText, dialogue);

        SetText(_rightName, string.Empty);
        SetText(_rightText, string.Empty);
        SetText(_narText, string.Empty);
    }

    // 우측 화자 UI를 표시하고 텍스트 갱신
    private void ShowRightDialogue(string speaker, string dialogue)
    {
        SetContainerStates(false, true, false);

        SetText(_rightName, speaker);
        SetText(_rightText, dialogue);

        SetText(_leftName, string.Empty);
        SetText(_leftText, string.Empty);
        SetText(_narText, string.Empty);
    }

    // 좌측 캐릭터 이미지 요청 토큰을 갱신하고 로드
    private void ApplyLeftCharacterImage(string fileName)
    {
        _leftImageRequestToken++;
        ApplyCharacterImage(_leftImage, fileName, _leftImageRequestToken, CharacterImageSlot.Left);
    }

    // 우측 캐릭터 이미지 요청 토큰을 갱신하고 로드
    private void ApplyRightCharacterImage(string fileName)
    {
        _rightImageRequestToken++;
        ApplyCharacterImage(_rightImage, fileName, _rightImageRequestToken, CharacterImageSlot.Right);
    }

    // 배경 이미지 요청 토큰을 갱신하고 로드
    private void ApplyBackgroundImage(string fileName)
    {
        _bgImageRequestToken++;
        ApplyCharacterImage(_bg, fileName, _bgImageRequestToken, CharacterImageSlot.Background);
    }

    // 캐릭터 이미지 적용
    private void ApplyCharacterImage(Image target, string fileName, int requestToken, CharacterImageSlot slot)
    {
        string normalized = NormalizeValue(fileName);
        if (string.IsNullOrEmpty(normalized) || normalized.ToLowerInvariant() == "none")
        {
            ClearCharacterImage(target);
            return;
        }

        AddressableImageManager imageManager = AddressableImageManager.Instance;
        if (imageManager.TryGetCachedSprite(normalized, out Sprite cachedSprite) && cachedSprite != null)
        {
            SetCharacterImage(target, cachedSprite);
            return;
        }

        imageManager.LoadSprite(normalized, sprite =>
        {
            if (IsExpiredRequest(slot, requestToken)) return;

            if (sprite == null)
            {
                ClearCharacterImage(target);
                return;
            }

            SetCharacterImage(target, sprite);
        });
    }

    // 비동기 콜백이 최신 요청인지 확인
    private bool IsExpiredRequest(CharacterImageSlot slot, int token)
    {
        return slot switch
        {
            CharacterImageSlot.Left => token != _leftImageRequestToken,
            CharacterImageSlot.Right => token != _rightImageRequestToken,
            _ => token != _bgImageRequestToken
        };
    }

    // 대상 이미지와 오브젝트를 활성 상태로 갱신
    private static void SetCharacterImage(Image target, Sprite sprite)
    {
        target.sprite = sprite;
        if (!target.gameObject.activeSelf)
            target.gameObject.SetActive(true);
    }

    // 대상 이미지를 비우고 오브젝트를 비활성화
    private static void ClearCharacterImage(Image target)
    {
        target.sprite = null;
        if (target.gameObject.activeSelf)
            target.gameObject.SetActive(false);
    }

    // 좌/우/나레이션 컨테이너의 표시 상태를 일괄 적용
    private void SetContainerStates(bool showLeft, bool showRight, bool showNarration)
    {
        SetActive(_leftTextContainer, showLeft);
        SetActive(_rightTextContainer, showRight);
        SetActive(_narTextContainer, showNarration);
    }

    // 모든 대사/이름 텍스트를 빈 문자열로 초기화
    private void ClearDialogueText()
    {
        SetText(_leftName, string.Empty);
        SetText(_rightName, string.Empty);
        SetText(_leftText, string.Empty);
        SetText(_rightText, string.Empty);
        SetText(_narText, string.Empty);
    }

    // TMP 텍스트 값을 null 안전하게 갱신
    private static void SetText(TMP_Text textComponent, string value)
    {
        textComponent.text = value ?? string.Empty;
    }

    // 게임오브젝트 활성 상태를 변경이 필요할 때만 적용
    private static void SetActive(GameObject target, bool isActive)
    {
        if (target.activeSelf == isActive) return;

        target.SetActive(isActive);
    }

    // 화자 이름 문자열의 앞뒤 공백을 정리
    private static string NormalizeSpeaker(string speaker)
    {
        return string.IsNullOrWhiteSpace(speaker) ? string.Empty : speaker.Trim();
    }

    // 일반 문자열의 앞뒤 공백을 정리
    private static string NormalizeValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    // 이미지 키가 실제 표시 대상인지 판단
    private static bool HasImageKey(string value)
    {
        string normalized = NormalizeValue(value);
        return !string.IsNullOrEmpty(normalized) && normalized.ToLowerInvariant() != "none";
    }
}

