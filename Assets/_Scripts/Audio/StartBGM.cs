using UnityEngine;

public class StartBGM : MonoBehaviour
{
    [SerializeField] private string _bgmName;

    void Start()
    {
        if (string.IsNullOrEmpty(_bgmName))
        {
            return;
        }

        SoundManager.Instance.PlayBGM(_bgmName);
    }
}