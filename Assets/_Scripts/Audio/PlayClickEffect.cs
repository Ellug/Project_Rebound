using UnityEngine;

public class PlayClickEffect : MonoBehaviour
{
    public string soundName;

    public void PlayClickSound()
    {
        SoundManager.Instance.PlayEffect(soundName);
    }
}
