using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 캘린더 셀 내부에 표시되는 소형 이벤트 배지
// EntryType에 따라 배경 색상이 결정
// 친선경기,토너먼트 → 보라색 / 공휴일 → 빨간색 / 시험,축제,방학 → 회색,녹색
public class CalendarBadge : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Image _background;

    [Header("배지 색상")]
    [SerializeField] private Color _colorHoliday = new Color(1f, 0.3f, 0.3f);         // 빨간
    [SerializeField] private Color _colorFriendlyMatch = new Color(0.6f, 0.2f, 1f);   // 보라
    [SerializeField] private Color _colorTournament = new Color(0.6f, 0.2f, 1f);      // 보라
    [SerializeField] private Color _colorAcademic = new Color(0.4f, 0.4f, 0.4f);      // 회색
    [SerializeField] private Color _colorVacation = new Color(0.2f, 0.7f, 0.4f);      // 녹색

    // CalendarCell에서 CalendarEntry를 받아 배지 텍스트와 색상 설정
    public void Render(CalendarEntry entry)
    {
        if (_label != null)
        {
            _label.text = entry.Label;
        }

        if (_background != null)
        {
            _background.color = entry.Type switch
            {
                CalendarEntry.EntryType.Holiday => _colorHoliday,
                CalendarEntry.EntryType.FriendlyMatch => _colorFriendlyMatch,
                CalendarEntry.EntryType.Tournament => _colorTournament,
                CalendarEntry.EntryType.AcademicExam => _colorAcademic,
                CalendarEntry.EntryType.AcademicFestival => _colorAcademic,
                CalendarEntry.EntryType.Vacation => _colorVacation,
                _ => _colorAcademic,
            };
        }
    }
}