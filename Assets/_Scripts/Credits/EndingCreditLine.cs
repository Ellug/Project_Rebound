using System;
using UnityEngine;

[Serializable]
public class EndingCreditLine
{
    public enum LineType
    {
        Section,  // 섹션 헤더 (DIRECTOR, DESIGN, PROGRAMMING 등)
        Name,     // 이름 (한글 + 영문 로마자)
        Role,     // 역할 설명
        Empty,    // 빈 줄 (간격용)
        Special,  // SPECIAL THANKS 본문
        Logo      // 이미지 (크레딧 하단 로고 등)
    }

    public LineType Type;
    public string Text;
    public Sprite Sprite;     // LineType.Logo 일 때 사용
    public Vector2 ImageSize;  // Logo 크기 (x=폭, y=높이). (0,0)이면 EndingCreditUI 기본값 사용

    public EndingCreditLine(LineType type, string text)
    {
        Type = type;
        Text = text ?? string.Empty;
    }

    public static EndingCreditLine Section(string text) => new(LineType.Section, text);
    public static EndingCreditLine Name(string text) => new(LineType.Name, text);
    public static EndingCreditLine Role(string text) => new(LineType.Role, text);
    public static EndingCreditLine Empty() => new(LineType.Empty, string.Empty);
    public static EndingCreditLine Special(string text) => new(LineType.Special, text);

    public static EndingCreditLine Logo(Sprite sprite, Vector2 size = default)
    {
        return new EndingCreditLine(LineType.Logo, string.Empty)
        {
            Sprite = sprite,
            ImageSize = size
        };
    }
}