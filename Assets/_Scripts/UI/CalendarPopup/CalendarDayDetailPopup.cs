using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 날짜 셀 클릭 시 일정 확인 팝업 출력
public static class CalendarDayDetailPopup
{
    // CalendarDayData의 날짜와 일정을 받아 팝업을 띄움
    public static void Show(CalendarDayData data)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[CalendarDayDetailPopup] UIManager가 없어 팝업을 표시할 수 없습니다.");
            return;
        }

        UIManager.Instance.ShowPopup(
            UIPopupRequest.Simple(
                title: BuildTitle(data.Date),
                message: BuildMessage(data.Entries),
                onPrimary: null,
                onCancel: null,
                showCancel: false,
                autoCloseOnPrimary: true,
                autoCloseOnCancel: true,
                primaryInteractable: true
            )
        );
    }

    // "3월 1일 (월)" 형식으로 제목 생성
    private static string BuildTitle(DateTime date)
    {
        string dayKor = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "월",
            DayOfWeek.Tuesday => "화",
            DayOfWeek.Wednesday => "수",
            DayOfWeek.Thursday => "목",
            DayOfWeek.Friday => "금",
            DayOfWeek.Saturday => "토",
            DayOfWeek.Sunday => "일",
            _ => "",
        };
        return $"{date.Month}월 {date.Day}일 ({dayKor})";
    }

    // 메시지 빌드: 1행 친선경기 여부, 2행 공휴일·학사일정 (친선경기 제외, 복수 시 줄바꿈)
    private static string BuildMessage(IReadOnlyList<CalendarEntry> entries)
    {
        // 1행: 친선경기 유무
        string friendlyLine = "-";
        foreach (var e in entries)
        {
            if (e.Type == CalendarEntry.EntryType.FriendlyMatch)
            {
                friendlyLine = "친선경기";
                break;
            }
        }

        // 2행: 공휴일·학사일정 (친선경기 제외, 복수이면 줄바꿈)
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            if (e.Type == CalendarEntry.EntryType.FriendlyMatch) continue;

            string line = string.IsNullOrEmpty(e.Detail)
                ? e.Label
                : $"{e.Label} ({FormatDetail(e.Detail)})";

            if (sb.Length > 0) sb.AppendLine();
            sb.Append(line);
        }

        string scheduleLine = sb.Length > 0 ? sb.ToString() : "-";
        return $"{friendlyLine}\n{scheduleLine}";
    }

    // "3월 1일 ~ 3월 2일" → "03. 01 ~ 03. 02" 형식으로 변환, 실패 시 원본 반환
    private static string FormatDetail(string detail)
    {
        try
        {
            string[] halves = detail.Split('~');
            if (halves.Length != 2) return detail;
            return $"{ParseDatePart(halves[0])} ~ {ParseDatePart(halves[1])}";
        }
        catch
        {
            return detail;
        }
    }

    // "3월 1일" → "03. 01" 형식으로 변환, 실패 시 원본 반환
    private static string ParseDatePart(string part)
    {
        part = part.Replace("월", " ").Replace("일", "").Trim();
        string[] tokens = part.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2) return part;

        return int.TryParse(tokens[0], out int m) && int.TryParse(tokens[1], out int d)
            ? $"{m:D2}. {d:D2}"
            : part;
    }
}