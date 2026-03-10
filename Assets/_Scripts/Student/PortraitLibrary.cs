using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Characters")]
public class PortraitLibrary : ScriptableObject
{
    [Header("Red Characters")]
    public Sprite[] redPortraits;

    [Header("Green Characters")]
    public Sprite[] greenPortraits;

    public Sprite Get(CharacterColor color, int index)
    {
        index = Mathf.Clamp(index - 1, 0, 31);

        if (color == CharacterColor.Red)
        {
            if (redPortraits != null && redPortraits.Length > index)
                return redPortraits[index];
        }
        else
        {
            if (greenPortraits != null && greenPortraits.Length > index)
                return greenPortraits[index];
        }

        return null;
    }
}