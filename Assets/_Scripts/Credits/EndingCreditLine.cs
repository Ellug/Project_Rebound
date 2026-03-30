using System;

// 엔딩 크레딧 한 줄을 표현하는 클래스
[Serializable]
public class EndingCreditLine
{
    public enum LineType
    {
        Section,            // 섹션 헤더 (DIRECTOR, DESIGN, PROGRAMMING 등)
        Name,               // 이름 (한글 + 영문 로마자)
        Role,               // 역할 설명
        Empty,              // 빈 줄 (간격용)
        Special             // SPECIAL THANKS 본문
    }

    public LineType Type;   // 라인 타입
    public string Text;     // 라인 텍스트 (빈 줄인 경우 빈 문자열)

    public EndingCreditLine(LineType type, string text)
    {
        Type = type;
        Text = text ?? string.Empty;
    }

    public static EndingCreditLine Section(string text) => new(LineType.Section, text); // 섹션 헤더
    public static EndingCreditLine Name(string text) => new(LineType.Name, text);       // 이름 (한글 + 영문 로마자)
    public static EndingCreditLine Role(string text) => new(LineType.Role, text);       // 역할 설명
    public static EndingCreditLine Empty() => new(LineType.Empty, string.Empty);        // 빈 줄 (간격용)
    public static EndingCreditLine Special(string text) => new(LineType.Special, text); // SPECIAL THANKS 본문
}