using System.Collections.Generic;
using UnityEngine;

// 엔딩 크레딧 전체 데이터 ScriptableObject
// Assets 우클릭 → Game/Data/Ending Credit Data 로 생성
[CreateAssetMenu(menuName = "Game/Data/Ending Credit Data", fileName = "SO_EndingCreditData")]
public class EndingCreditDataSO : ScriptableObject
{
    [Header("스크롤 설정")]
    [Tooltip("초당 스크롤 속도 (px/s). 기획서: 1초에 2.5~3줄 → 약 75~90px/s 권장")]
    [SerializeField] private float _scrollSpeed = 70f;

    [Tooltip("최대 재생 시간 (초). 기획서: 45~50초")]
    [SerializeField] private float _totalDuration = 100f;

    [Header("오디오")]
    [Tooltip("엔딩 BGM ID. 기획서: bgm_start_lp.mp3")]
    [SerializeField] private int _bgmId = 101;

    [Header("하단 로고 이미지")]
    [SerializeField] private Sprite _logoB3;
    [SerializeField] private Vector2 _logoB3Size = new Vector2(200f, 200f);

    [SerializeField] private Sprite _logoKyungil;
    [SerializeField] private Vector2 _logoKyungilSize = new Vector2(400f, 100f);

    [SerializeField] private Sprite _thankYouImage;
    [SerializeField] private Vector2 _thankYouSize = new Vector2(600f, 120f);

    public float ScrollSpeed => _scrollSpeed;
    public float TotalDuration => _totalDuration;
    public int BgmId => _bgmId;

    public List<EndingCreditLine> GetCreditLines()
    {
        var lines = new List<EndingCreditLine>
        {
            // DIRECTOR
            EndingCreditLine.Section("DIRECTOR"),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("이수호 (Lee Soo-ho)"),
            EndingCreditLine.Role("Team Lead / Scenario & Direction"),
            EndingCreditLine.Role("Documentation Finalization"),
            EndingCreditLine.Role("Release Preparation"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            // DESIGN
            EndingCreditLine.Section("DESIGN"),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("성백규 (Sung Baek-gyu)"),
            EndingCreditLine.Role("Deputy Team Lead / Documentation"),
            EndingCreditLine.Role("System Design (Progression, Tutorial,"),
            EndingCreditLine.Role("Reward, Save/Load, Tournament)"),
            EndingCreditLine.Role("Data Table Setup & Management"),
            EndingCreditLine.Role("AI Image Resource (Gemini)"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("제진혁 (Je Jin-hyuk)"),
            EndingCreditLine.Role("Project Management"),
            EndingCreditLine.Role("Sound Design & Balancing"),
            EndingCreditLine.Role("Node System Design"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("황영재 (Hwang Young-jae)"),
            EndingCreditLine.Role("System Design (Calendar,"),
            EndingCreditLine.Role("Always Events, Roster Management)"),
            EndingCreditLine.Role("Ending Direction"),
            EndingCreditLine.Role("Promotion Video (PV) Production"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("박신형 (Park Shin-hyung)"),
            EndingCreditLine.Role("Game Simulation System Design"),
            EndingCreditLine.Role("UX Flow & Scene Structure Design"),
            EndingCreditLine.Role("UI Asset Production"),
            EndingCreditLine.Role("Image Resource Direction"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("김인엽 (Kim In-yeop)"),
            EndingCreditLine.Role("Data Table Management"),
            EndingCreditLine.Role("Event System Design (Random Events,"),
            EndingCreditLine.Role("Friendly Match Events)"),
            EndingCreditLine.Role("Match Halftime Choice System"),
            EndingCreditLine.Role("Graduation Reward Upgrade System"),
            EndingCreditLine.Role("TC List Management & Validation"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("최서윤 (Choi Seo-yoon)"),
            EndingCreditLine.Role("Data Design (Student Data, Equipment)"),
            EndingCreditLine.Role("System Design"),
            EndingCreditLine.Role("(Facility Upgrade, Graduation Gift)"),
            EndingCreditLine.Role("AI Resource Generation"),
            EndingCreditLine.Role("Marketing & Promotion"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            // PROGRAMMING
            EndingCreditLine.Section("PROGRAMMING"),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("이덕기 (Lee Duk-gi)"),
            EndingCreditLine.Role("Lead Programmer"),
            EndingCreditLine.Role("Architecture Design & System Framework"),
            EndingCreditLine.Role("Task & GitHub Version Control Management"),
            EndingCreditLine.Role("Data Pipeline Automation"),
            EndingCreditLine.Role("Addressable Deployment Automation"),
            EndingCreditLine.Role("Addressable Resource Patch"),
            EndingCreditLine.Role("Caching System"),
            EndingCreditLine.Role("Match Simulation System"),
            EndingCreditLine.Role("Multi-device Support"),
            EndingCreditLine.Role("Narrative System Implementation"),
            EndingCreditLine.Role("Custom Outline Component"),
            EndingCreditLine.Role("Event System Implementation"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("김진우 (Kim Jin-woo)"),
            EndingCreditLine.Role("Deputy Lead Programmer"),
            EndingCreditLine.Role("UI Manager & Base System"),
            EndingCreditLine.Role("Random Event System (Non-match)"),
            EndingCreditLine.Role("Dialogue System"),
            EndingCreditLine.Role("Friendly Match Application System"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("장우빈 (Jang Woo-bin)"),
            EndingCreditLine.Role("Director Node System"),
            EndingCreditLine.Role("Save System & Calendar System"),
            EndingCreditLine.Role("Student Recruitment System"),
            EndingCreditLine.Role("Funding, Training System"),
            EndingCreditLine.Role("Turn Manager System & Tutorial System"),
            EndingCreditLine.Role("Student Management System"),
            EndingCreditLine.Role("Graduate Gift System"),
            EndingCreditLine.Role("Common Event Popup System"),
            EndingCreditLine.Role("DoTween-based UI Popup &"),
            EndingCreditLine.Role("Scene Transition Animation"),
            EndingCreditLine.Role("Always Event Effects"),
            EndingCreditLine.Role("Lobby Image Randomization"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("노주형 (Noh Joo-hyung)"),
            EndingCreditLine.Role("Audio System"),
            EndingCreditLine.Role("Currency System"),
            EndingCreditLine.Role("Status Effect System"),
            EndingCreditLine.Role("Settings System"),
            EndingCreditLine.Role("Facility Management System"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            EndingCreditLine.Name("권재민 (Kwon Jae-min)"),
            EndingCreditLine.Role("Event System R&D"),
            EndingCreditLine.Role("Effect R&D"),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),

            // SPECIAL THANKS
            EndingCreditLine.Section("SPECIAL THANKS"),
            EndingCreditLine.Empty(),

            EndingCreditLine.Special("To Everyone"),
            EndingCreditLine.Special("Who Played This Game"),

            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),
            EndingCreditLine.Empty(),
        };

        // 하단 로고 이미지 3종 — Inspector에서 연결되지 않은 항목은 생략
        if (_logoB3 != null)
        {
            lines.Add(EndingCreditLine.Logo(_logoB3, _logoB3Size));
            lines.Add(EndingCreditLine.Empty());
        }

        if (_logoKyungil != null)
        {
            lines.Add(EndingCreditLine.Logo(_logoKyungil, _logoKyungilSize));
            lines.Add(EndingCreditLine.Empty());
            lines.Add(EndingCreditLine.Empty());
        }

        if (_thankYouImage != null)
        {
            lines.Add(EndingCreditLine.Logo(_thankYouImage, _thankYouSize));
        }

        lines.Add(EndingCreditLine.Empty());
        lines.Add(EndingCreditLine.Empty());
        lines.Add(EndingCreditLine.Empty());

        return lines;
    }
}