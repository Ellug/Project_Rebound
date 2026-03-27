using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalendarDayPopup : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private TMP_Text _txtFriendly;
    [SerializeField] private TMP_Text _txtSchedule;

    [Header("버튼")]
    [SerializeField] private Button _btnClose;

    [Header("연출")]
    [SerializeField] private PopupAnimator _popupAnimator;

    private void Awake()
    {
        _btnClose.onClick.AddListener(Close);
    }

    public void Show(CalendarDayData data)
    {
        _txtTitle.text = BuildTitle(data.Date);
        _txtFriendly.text = BuildFriendlyLine(data.Entries);
        _txtSchedule.text = BuildScheduleLine(data.Entries);

        gameObject.SetActive(true);
        _popupAnimator.PlayIn();
    }

    private void Close()
    {
        _popupAnimator.PlayOut(() => gameObject.SetActive(false));
    }

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
        return $"{date.Month}월 {date.Day:D2}일 ({dayKor})";
    }

    private static string BuildFriendlyLine(IReadOnlyList<CalendarEntry> entries)
    {
        foreach (var e in entries)
            if (e.Type == CalendarEntry.EntryType.FriendlyMatch)
                return e.Label;

        return "-";
    }

    private static string BuildScheduleLine(IReadOnlyList<CalendarEntry> entries)
    {
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

        return sb.Length > 0 ? sb.ToString() : "-";
    }

    private static string FormatDetail(string detail)
    {
        try
        {
            string[] halves = detail.Split('~');
            if (halves.Length != 2) return detail;
            return $"{ParseDatePart(halves[0])} ~ {ParseDatePart(halves[1])}";
        }
        catch { return detail; }
    }

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