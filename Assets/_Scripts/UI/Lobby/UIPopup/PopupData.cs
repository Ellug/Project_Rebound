using System;
using System.Collections.Generic;
using UnityEngine;

// 팝업 버튼 하나에 대한 정보
public class PopupButtonInfo
{
    public Action OnClick;
    public bool AutoClose;

    // 액션 기반
    public PopupButtonInfo(Action onClick = null, bool autoClose = true)
    {
        OnClick = onClick;
        AutoClose = autoClose;
    }

    // 기존 호환용
    public PopupButtonInfo(string text, Action onClick = null, bool autoClose = true)
    {
        OnClick = onClick;
        AutoClose = autoClose;
    }
}

// 팝업 데이터 (이미지, 서브텍스트 추가)
public class PopupData
{
    public string Title;           // 제목
    public string Content;         // 본문
    public string SubContent;      // 부가 설명
    public Sprite Image;           // 상단 이미지
    public List<PopupButtonInfo> Buttons;

    // 필요한 것만 넣을 수 있도록 기본값 설정
    public PopupData(string title, string content, string subContent = null, Sprite image = null, List<PopupButtonInfo> buttons = null)
    {
        this.Title = title;
        this.Content = content;
        this.SubContent = subContent;
        this.Image = image;
        this.Buttons = buttons ?? new List<PopupButtonInfo>();
    }
}