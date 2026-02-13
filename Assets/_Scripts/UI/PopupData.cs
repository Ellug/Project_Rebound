using System;
using System.Collections.Generic;

// 팝업 버튼 하나에 대한 정보
public class PopupButtonInfo
{
    public string Text;         // 버튼 텍스트
    public Action OnClick;      // 버튼 눌렀을 때 실행할 함수
    public bool AutoClose;      // 누르면 팝업 닫을지 여부

    public PopupButtonInfo(string text, Action onClick = null, bool autoClose = true)
    {
        this.Text = text;
        this.OnClick = onClick;
        this.AutoClose = autoClose;
    }
}

// 팝업 하나를 구성하는 전체 데이터
public class PopupData
{
    public string Title;                    // 제목
    public string Content;                  // 본문 내용
    public List<PopupButtonInfo> Buttons;   // 버튼 목록 (0개 ~ N개)

    public PopupData(string title, string content, List<PopupButtonInfo> buttons = null)
    {
        this.Title = title;
        this.Content = content;
        this.Buttons = buttons ?? new List<PopupButtonInfo>();
    }
}