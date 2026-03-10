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
}

[Serializable]
public class HolidayEntry
{
    public int date;
    public string name;
}