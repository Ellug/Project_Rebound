using UnityEngine;
using TMPro;

// 캘린더 셀 내부에 표시되는 소형 이벤트 배지
// EntryType에 따라 라벨 색상이 결정
public class CalendarBadge : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;

    [Header("배지 색상")]
    [SerializeField] private Color _colorHoliday = new Color(1f, 0.3f, 0.3f);         // 빨간
    [SerializeField] private Color _colorFriendlyMatch = new Color(1f, 0.765f, 0f, 1f); //노랑
    [SerializeField] private Color _colorTournament = new Color(0.6f, 0.2f, 1f);      // 보라
    [SerializeField] private Color _colorAcademic = new Color(0.7f, 0.7f, 0.7f);      // 회색
    [SerializeField] private Color _colorVacation = new Color(0.2f, 0.7f, 0.4f);      // 녹색


    private void Awake()
    {
        if (_label == null)
            _label = GetComponent<TMP_Text>();

        if (_label == null)
            Debug.LogError($"[CalendarBadge] _label 없음: {gameObject.name}", gameObject);
    }

    // CalendarCell에서 CalendarEntry 데이터를 받아 텍스트와 색상을 갱신
    public void Render(CalendarEntry entry)
    {
        if (_label == null) return;

        if (entry == null || string.IsNullOrWhiteSpace(entry.Label))
        {
            _label.text = string.Empty;
            return;
        }

        _label.text = entry.Label;
        _label.color = entry.Type switch
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