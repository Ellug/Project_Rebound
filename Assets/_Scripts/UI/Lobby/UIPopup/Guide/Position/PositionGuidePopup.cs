// 포지션 안내 팝업 (페이지형 UI)
// Inspector에서 직접 페이지 데이터를 입력하여 사용
using System.Collections.Generic;
using UnityEngine;

public class PositionGuidePopup : UIPopup
{
    [Header("Pages")]
    [SerializeField] private List<GuidePage> _pages = new(); // Inspector에서 직접 입력

    private bool _isInited;

    private void OnEnable()
    {
        // 씬에 비활성으로 두었다가 켜도 항상 정상 갱신
        if (!_isInited)
            Init();
    }

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        // Guide 타입으로 설정 (페이지 기능 활성화)
        SetupGuide(_pages);
    }
}