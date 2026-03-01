// 확인/취소 + 선택적 학생 선택 기능을 가지는 공용 확인 팝업
// 이미지, 서브텍스트, 설명 텍스트, 버튼 최대 두 개 활용
// UIPopup.Setup(request, PopupType.Confirm) 호출로 동작
public class ConfirmPopup : UIPopup
{
    // 외부에서 팝업 설정 적용 (기존 시그니처 유지)
    public void Setup(ConfirmPopupRequest request)
    {
        base.Setup(request, PopupType.Confirm);
    }
}