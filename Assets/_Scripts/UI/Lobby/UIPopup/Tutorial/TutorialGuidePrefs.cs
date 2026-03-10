using System.Collections.Generic;
using UnityEngine;


// TutorialGuidePrefs
// 튜토리얼 "다시 보지 않기" 저장/리셋 (PlayerPrefs)
// TutorialGuideTableSO -> UIPopupRequest.GuidePage 변환
public static class TutorialGuidePrefs
{
    // PlayerPrefs Key
    private const string PrefKey_Dismissed = "tutorial_guide_dismissed";

    // Prefs
    public static bool IsDismissed()
    {
        return PlayerPrefs.GetInt(PrefKey_Dismissed, 0) == 1;
    }

    public static void SetDismissed(bool dismissed)
    {
        PlayerPrefs.SetInt(PrefKey_Dismissed, dismissed ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ResetDismissed()
    {
        PlayerPrefs.DeleteKey(PrefKey_Dismissed);
        PlayerPrefs.Save();
    }

    // Pages Builder (기존 TutorialGuidePageAdapter 통합)
    // index 오름차순으로 정렬해서 GuidePage 리스트 생성
    public static List<UIPopupRequest.GuidePage> BuildPages(TutorialGuideTableSO table)
    {
        List<UIPopupRequest.GuidePage> pages = new();

        if (table == null || table.Rows == null || table.Rows.Count == 0)
            return pages;

        // Row 복사 후 index 정렬
        List<TutorialGuideRow> sorted = new List<TutorialGuideRow>(table.Rows.Count);
        for (int i = 0; i < table.Rows.Count; i++)
        {
            TutorialGuideRow r = table.Rows[i];
            if (r == null) continue;
            if (r.index <= 0) continue;
            sorted.Add(r);
        }

        sorted.Sort((a, b) => a.index.CompareTo(b.index));

        for (int i = 0; i < sorted.Count; i++)
        {
            TutorialGuideRow r = sorted[i];

            Sprite sprite = TryLoadSpriteByRow(r);

            UIPopupRequest.GuidePage page = new UIPopupRequest.GuidePage
            {
                Title = r.titleText,
                Message = r.desc,
                SubMessage = null,
                PreviewSprite = sprite
            };
            pages.Add(page);
        }

        return pages;
    }

    private static Sprite TryLoadSpriteByRow(TutorialGuideRow row)
    {
        if (row == null) return null;
        if (string.IsNullOrWhiteSpace(row.img)) return null;

        // Resources 경로 예: "Tutorial/guide_01"
        return Resources.Load<Sprite>(row.img.Trim());
    }
}
