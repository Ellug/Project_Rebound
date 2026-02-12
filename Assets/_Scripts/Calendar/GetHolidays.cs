using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Globalization;

// 2050년 까지만 생성 가능
public class GetHoliday
{
    static KoreanLunisolarCalendar lunar = new KoreanLunisolarCalendar();

    [MenuItem("Tools/GetHoliday")]
    public static void Generate()
    {
        int startYear = 2026;
        int endYear = 2050;

        List<HolidayEntry> list = new();

        for (int year = startYear; year <= endYear; year++)
        {
            AddYear(year, list);
        }

        list.Sort((a, b) => a.date.CompareTo(b.date));
        Save(list);

        Debug.Log("공휴일 생성 완료");
    }

    static void AddYear(int year, List<HolidayEntry> list)
    {
        AddHoliday(list, new DateTime(year, 1, 1), "새해");
        AddHoliday(list, new DateTime(year, 3, 1), "삼일절");
        AddHoliday(list, new DateTime(year, 5, 5), "어린이날");
        AddHoliday(list, new DateTime(year, 6, 6), "현충일");
        AddHoliday(list, new DateTime(year, 7, 17), "제헌절");
        AddHoliday(list, new DateTime(year, 8, 15), "광복절");
        AddHoliday(list, new DateTime(year, 10, 3), "개천절");
        AddHoliday(list, new DateTime(year, 10, 9), "한글날");
        AddHoliday(list, new DateTime(year, 12, 25), "성탄절");

        AddSeollal(year, list);

        AddChuseok(year, list);

        DateTime buddha = LunarToSolar(year, 4, 8);
        AddHoliday(list, buddha, "석가탄신일");
    }

    static void AddSeollal(int year, List<HolidayEntry> list)
    {
        DateTime seollal = LunarToSolar(year, 1, 1);

        DateTime d1 = seollal.AddDays(-1);
        DateTime d2 = seollal;
        DateTime d3 = seollal.AddDays(1);

        List<DateTime> days = new() { d1, d2, d3 };

        bool hasWeekend = false;

        foreach (var d in days)
        {
            if (IsWeekend(d))
                hasWeekend = true;

            if (!IsWeekend(d))
            {
                AddRaw(list, d, d == d2 ? "설날" : "설날 연휴");
            }
        }

        if (hasWeekend)
        {
            DateTime sub = d3.AddDays(1);
            while (IsWeekend(sub))
                sub = sub.AddDays(1);

            AddRaw(list, sub, "대체공휴일(설날)");
        }
    }

    static void AddChuseok(int year, List<HolidayEntry> list)
    {
        DateTime chuseok = LunarToSolar(year, 8, 15);

        DateTime d1 = chuseok.AddDays(-1);
        DateTime d2 = chuseok;
        DateTime d3 = chuseok.AddDays(1);

        List<DateTime> days = new() { d1, d2, d3 };

        bool hasWeekend = false;

        foreach (var d in days)
        {
            if (IsWeekend(d))
                hasWeekend = true;

            if (!IsWeekend(d))
            {
                AddRaw(list, d, d == d2 ? "추석" : "추석 연휴");
            }
        }

        if (hasWeekend)
        {
            DateTime sub = d3.AddDays(1);
            while (IsWeekend(sub))
                sub = sub.AddDays(1);

            AddRaw(list, sub, "대체공휴일(추석)");
        }
    }

    static void AddHoliday(List<HolidayEntry> list, DateTime dt, string name)
    {
        if (IsWeekend(dt))
        {
            DateTime sub = dt;
            while (IsWeekend(sub))
                sub = sub.AddDays(1);

            AddRaw(list, sub, $"대체공휴일({name})");
            return;
        }

        AddRaw(list, dt, name);
    }

    static DateTime LunarToSolar(int year, int lunarMonth, int lunarDay)
    {
        int leapMonth = lunar.GetLeapMonth(year);
        int month = lunarMonth;

        if (leapMonth > 0 && lunarMonth >= leapMonth)
            month++;

        return lunar.ToDateTime(year, month, lunarDay, 0, 0, 0, 0);
    }

    static void AddRaw(List<HolidayEntry> list, DateTime dt, string name)
    {
        int key = dt.Year * 10000 + dt.Month * 100 + dt.Day;

        if (!list.Exists(x => x.date == key))
        {
            list.Add(new HolidayEntry
            {
                date = key,
                name = name
            });
        }
    }

    static bool IsWeekend(DateTime dt)
    {
        return dt.DayOfWeek == DayOfWeek.Saturday ||
               dt.DayOfWeek == DayOfWeek.Sunday;
    }

    static void Save(List<HolidayEntry> list)
    {
        string path = "Assets/HolidayDatabase.asset";

        AssetDatabase.DeleteAsset(path);

        var asset = ScriptableObject.CreateInstance<HolidayDatabase>();
        asset.entries = list;

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}