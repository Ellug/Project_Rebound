using UnityEngine;
using UnityEngine.UI;

public class LobbyRandomBackground : MonoBehaviour
{
    [Header("Background Target")]
    [SerializeField] private Image _backgroundImage;

    [Header("Random Background IDs")]
    [SerializeField]
    private string[] _backgroundFileNames =
    {
        "Lobby00_bg_counseling",
        "Lobby00_bg_gym",
        "Lobby00_bg_restaurant",
        "Lobby00_bg_school"
    };

    private string _currentBackgroundId;

    private void Start()
    {
        ApplyRandomBackground();
    }

    public void ApplyRandomBackground()
    {
        if (_backgroundImage == null)
        {
            Debug.LogWarning("[LobbyRandomBackground] Background Image is not assigned.");
            return;
        }

        if (_backgroundFileNames == null || _backgroundFileNames.Length == 0)
        {
            Debug.LogWarning("[LobbyRandomBackground] Background file name list is empty.");
            return;
        }

        int randomIndex = Random.Range(0, _backgroundFileNames.Length);
        _currentBackgroundId = _backgroundFileNames[randomIndex];

        if (AddressableImageManager.Instance == null)
        {
            Debug.LogWarning("[LobbyRandomBackground] AddressableImageManager is missing.");
            return;
        }

        AddressableImageManager.Instance.LoadSprite(_currentBackgroundId, OnBackgroundLoaded);
    }

    private void OnBackgroundLoaded(Sprite loadedSprite)
    {
        if (loadedSprite == null)
        {
            Debug.LogWarning($"[LobbyRandomBackground] Failed to load background: {_currentBackgroundId}");
            return;
        }

        _backgroundImage.sprite = loadedSprite;
        _backgroundImage.preserveAspect = true;
    }
}