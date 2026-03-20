using System;

[Serializable]
public struct StudentCardPreviewDelta
{
    public int condition;
    public bool treatStatFieldsAsExp;
    public int mental;
    public int shoot;
    public int speed;
    public int jump;
    public int stamina;

    public bool HasAnyDelta =>
        condition != 0 ||
        mental != 0 ||
        shoot != 0 ||
        speed != 0 ||
        jump != 0 ||
        stamina != 0;
}
