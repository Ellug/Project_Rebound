using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

public class GetHolidays
{
    const string API_KEY = "AIzaSyB4IDC28U5Eqx50bIvNCGvyAuVei1pc1Qc";
    const string CALENDAR_ID = "ko.south_korea.official%23holiday%40group.v.calendar.google.com";

    [MenuItem("Tools/GetHolidays")]
    public static void Fetch()
    {
        int addYear = 10;
        int startYear = DateTime.Now.Year;
        int endYear = startYear + addYear;

        List<int> allDates = new List<int>();

        for (int year = startYear; year <= endYear; year++)
        {
            Debug.Log($"{year}");

            var dates = FetchYear(year);
            allDates.AddRange(dates);
        }

        HashSet<int> unique = new HashSet<int>(allDates);
        List<int> finalList = new List<int>(unique);
        finalList.Sort();

        SaveAsset(finalList);

        Debug.Log("공휴일 저장 완료");
    }

    static List<int> FetchYear(int year)
    {
        DateTime start = new DateTime(year, 1, 1);
        DateTime end = new DateTime(year, 12, 31);

        string url =
            $"https://www.googleapis.com/calendar/v3/calendars/{CALENDAR_ID}/events" +
            $"?key={API_KEY}" +
            $"&timeMin={start:yyyy-MM-dd}T00:00:00Z" +
            $"&timeMax={end:yyyy-MM-dd}T23:59:59Z" +
            $"&singleEvents=true" +
            $"&orderBy=startTime";

        var req = UnityWebRequest.Get(url);
        var op = req.SendWebRequest();

        while (!op.isDone) { }

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            return new List<int>();
        }

        return Parse(req.downloadHandler.text);
    }

    static List<int> Parse(string json)
    {
        var root = JsonUtility.FromJson<Root>(json);
        List<int> result = new List<int>();

        foreach (var e in root.items)
        {
            if (string.IsNullOrEmpty(e.start.date))
                continue;

            DateTime date = DateTime.Parse(e.start.date);

            int ymd = date.Year * 10000 + date.Month * 100 + date.Day;
            result.Add(ymd);
        }

        return result;
    }

    static void SaveAsset(List<int> dates)
    {
        string path = "Assets/AddressableAssetsData/HolidayDatabase.asset";

        var asset = ScriptableObject.CreateInstance<HolidayDatabase>();
        asset.dates = dates;

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("HolidayDatabase.asset 생성 완료");
    }

    [Serializable] class Root
    {
        public List<EventItem> items;
    }

    [Serializable] class EventItem
    {
        public string summary;
        public Start start;
    }

    [Serializable] class Start
    {
        public string date;
    }
}
