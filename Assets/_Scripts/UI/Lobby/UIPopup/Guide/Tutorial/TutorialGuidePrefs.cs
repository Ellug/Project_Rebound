using UnityEngine;

// 튜토리얼 가이드 표시 여부를 PlayerPrefs로 저장/조회하는 유틸 클래스
public static class TutorialGuidePrefs
{
    // PlayerPrefs에 저장될 키 값
    private const string KEY_DISMISSED = "TutorialGuide_Dismissed";

    // 튜토리얼을 이미 닫았는지 여부 (1 = true, 0 = false)
    public static bool IsDismissed
    {
        get => PlayerPrefs.GetInt(KEY_DISMISSED, 0) == 1;
    }

    // 튜토리얼 닫힘 여부 저장
    public static void SetDismissed(bool dismissed)
    {
        PlayerPrefs.SetInt(KEY_DISMISSED, dismissed ? 1 : 0);
        PlayerPrefs.Save(); // 즉시 디스크에 반영
    }

    // 튜토리얼 닫힘 상태 초기화 (다시 보이도록 설정)
    public static void ResetDismissed()
    {
        SetDismissed(false);
    }
}