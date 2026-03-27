using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Calendar/Holiday Database")]
public class HolidayDatabase : ScriptableObject
{
    public List<HolidayEntry> entries = new();

    public bool TryGetHoliday(DateTime date, out HolidayEntry entry)
    {
        int key = date.Year * 10000 + date.Month * 100 + date.Day;

        foreach (var e in entries)
        {
            if (e.date == key)
            {
                entry = e;
                return true;
            }
        }

        entry = null;
        return false;
    }

    // CalendarManager가 월 단위로 공휴일을 한 번에 읽을 때 사용
    // key가 yyyyMMdd이므로 연·월 범위로 필터링
    public List<HolidayEntry> GetHolidaysInMonth(int year, int month)
    {
        int min = year * 10000 + month * 100 + 1;
        int max = year * 10000 + month * 100 + 31;

        var result = new List<HolidayEntry>();
        foreach (var e in entries)
        {
            if (e.date >= min && e.date <= max)
                result.Add(e);
        }
        return result;
    }
}

[Serializable]
public class HolidayEntry
{
    public int date;
    public string name;
}