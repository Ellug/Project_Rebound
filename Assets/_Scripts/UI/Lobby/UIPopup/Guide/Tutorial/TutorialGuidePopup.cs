// 튜토리얼 가이드 팝업 (페이지형 UI)
// SO 테이블 데이터를 GuidePage 리스트로 변환 후 SetupGuide에 전달
using System.Collections.Generic;

public class TutorialGuidePopup : UIPopup
{
    private bool _isInited;

    private void OnEnable()
    {
        // 최초 1회 Init, 이후엔 Refresh만 수행
        if (!_isInited) Init();
    }

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        // 테이블 기반 페이지 생성 후 Guide 타입으로 설정
        SetupGuide(BuildPagesFromTable());
    }

    // SO 테이블 데이터를 GuidePage 리스트로 변환
    private List<GuidePage> BuildPagesFromTable()
    {
        List<GuidePage> pages = new List<GuidePage>();

        TutorialGuideTableSO table = CachedSOData.TutorialGuideTable;
        if (table == null || table.Rows == null || table.Rows.Count == 0)
            return pages;

        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            if (row == null) continue;

            pages.Add(new GuidePage
            {
                Title = row.titleText,
                Content = row.desc,
                Image = null // 프로토타입 단계: 이미지 미사용
            });
        }

        return pages;
    }
}