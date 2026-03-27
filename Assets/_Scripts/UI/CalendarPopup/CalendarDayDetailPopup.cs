public static class CalendarDayDetailPopup
{
    private static CalendarDayPopup _popup;

    public static void Show(CalendarDayData data)
    {
        if (_popup == null)
        {
            return;
        }

        _popup.Show(data);
    }

    // CalendarMonthView.Init()에서 호출해서 팝업 참조 연결
    public static void Bind(CalendarDayPopup popup)
    {
        _popup = popup;
    }
}