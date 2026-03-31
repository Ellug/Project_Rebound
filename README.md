# Project Rebound

> 농구부 운영 UI 시뮬레이션 게임 — 기업협약 기획/개발 협업 프로젝트  
> Unity 6 · Android · 개발팀 5인 / 기획팀 7인 · 2026.02 ~ 2026.04

---

## 소개

학교 농구부 감독이 되어 학생을 육성하고 토너먼트를 완주하는 턴 기반 시뮬레이션 게임.  
랜덤 생성되는 학생, 돌발 이벤트, 감독 노드·시설 업그레이드를 통해 매 회차마다 다른 플레이를 제공한다.

- Flip · Fold · 태블릿 등 다양한 디스플레이 환경을 지원하는 반응형 UGUI 구성
- 비주얼 노벨 형식의 스토리 연출
- 데이터 테이블 기반으로 동작하는 내러티브 · 이벤트 · 경기 시스템

---

## 역할 및 기여 (팀장)

- 전체 아키텍처 설계 및 GitHub 형상 관리 총괄
- Google Sheet → SO → Addressable → UGS 자동화 파이프라인 구축
- Addressable 리소스 업데이트 및 캐싱 전략 설계
- 경기 시뮬레이션 OOD 및 핵심 로직 구현
- 멀티 디바이스(Flip, Fold, 태블릿) 디스플레이 최적화
- CSV 기반 내러티브 실행 시스템 구축
- 커스텀 외곽선 컴포넌트 구현
- 학생 데이터 및 팩토리 패턴 기반 생성 엔진 구현

---

## 기술 스택

| 분류 | 내용 |
|---|---|
| 엔진 | Unity 6 `6000.2.10f1` · URP Universal 2D |
| 언어 | C# |
| 리소스 | Addressables · Unity Cloud Content Delivery (CCD) |
| 데이터 | Google Sheets · CSV · ScriptableObject |
| 배포 자동화 | UGS CLI |
| UI | UGUI · TextMeshPro |
| 애니메이션 | DOTween |
| 빌드 대상 | Android (Min SDK 24) |

---

## 핵심 기능

### 1. Google Sheet → Addressable → Unity Cloud 자동화 파이프라인

약 50종의 데이터 테이블을 Google Sheets로 관리하는 상황에서, 매번 수동으로 CSV를 다운받고 적용하는 방식의 비효율과 휴먼 에러 위험을 해결하기 위해 에디터 툴 기반 자동화 파이프라인을 구축했다.

**파이프라인 흐름**

```
Google Sheet → Local CSV → SO Script 생성 → Scriptable Object → Addressable → Unity Cloud (UGS CLI)
```

**구현 과정 및 개선 이력**

| 단계 | 내용 |
|---|---|
| 초기 | CSV 하나마다 SO 스크립트와 Importer 개별 작성 → 컬럼 변경 시 양쪽 모두 수정 필요 |
| 1차 개선 | 기획팀과 데이터 테이블 작성 규칙(1행: 컬럼명 / 2행: 타입 / 3행~: 데이터) 확립, Generic Importer로 통합 → 신규 테이블 추가 시 SO.cs 하나만 작성 |
| 최종 | StringBuilder로 SO.cs 자동 생성 + UGS CLI 연동으로 CCD 업로드·릴리즈·버전 관리까지 자동화 |

결과: 초기 대비 데이터 동기화에서 배포까지 **전체 작업 시간 90% 이상 단축 (약 10분 → 1분 내외)**

**관련 소스**
- [`GoogleSheetSyncer.cs`](Assets/_Scripts/CSVParser/GoogleSync/GoogleSheetSyncer.cs) — 파이프라인 진입점 (에디터 메뉴: `Tools/Data/Sync from Google Sheets`)
- [`CsvSoAutoCreator.cs`](Assets/_Scripts/CSVParser/CsvSoAutoCreator.cs) — CSV 헤더 기반 SO.cs 자동 생성
- [`CsvBatchImporter.cs`](Assets/_Scripts/CSVParser/CsvBatchImporter.cs) — 전체 테이블 일괄 임포트
- [`GoogleSheetCloudUploader.cs`](Assets/_Scripts/CSVParser/GoogleSync/GoogleSheetCloudUploader.cs) — CCD 업로드 및 UGS CLI 연동
- [`TableLoadConfigAutoSync.cs`](Assets/_Scripts/CSVParser/TableLoadConfigAutoSync.cs) — 임포트 후 테이블 로드 설정 자동 갱신

---

### 2. Addressable 리소스 업데이트 및 캐싱 전략

이미지·오디오 등 무거운 리소스를 Addressable로 분리해 **빌드 파일 용량 80% 이상 감소**, 데이터 수정 시 재빌드 없이 클라우드 갱신만으로 반영 가능하도록 설계했다.

**데이터 크기와 사용 빈도에 따라 전략을 분리**

| 전략 | 대상 | 이유 |
|---|---|---|
| Static 전역 캐싱 | CSV 텍스트 데이터 (총 10MB 미만) | 매번 할당·해제 시 메모리 파편화 및 GC 부하 발생 |
| 전략적 프리웜 | 게임 전반에 걸쳐 사용 빈도 높은 이미지·주요 오디오 | 런타임 로딩 지연 방지 |
| 요청 시 로드 | 씬·상황이 고정된 나머지 리소스 | 불필요한 메모리 점유 방지 |

**Library SO 기반 리소스 접근 구조**

기획자가 작성한 데이터 테이블의 파일명(string) 또는 id(int)를 기준으로 리소스를 로드할 수 있도록 Library 역할의 SO를 설계해, 팀 전체가 일관된 방식으로 리소스에 접근할 수 있도록 했다.

- 이미지: 파일명(string) ↔ AssetReferenceSprite 매핑
- 오디오: id(int) ↔ AssetReferenceAudioClip 매핑

**관련 소스**
- [`AddressableImageManager.cs`](Assets/_Scripts/AddressableManager/AddressableImageManager.cs) — 이미지 비동기 로드, 프리웜, 캐싱
- [`AddressableAudioManager.cs`](Assets/_Scripts/AddressableManager/AddressableAudioManager.cs) — 오디오 클립 로드
- [`AddressableImageLibrarySO.cs`](Assets/_Scripts/AddressableManager/AddressableImageLibrarySO.cs) — 이미지 라이브러리 SO
- [`AddressableAudioLibrarySO.cs`](Assets/_Scripts/AddressableManager/AddressableAudioLibrarySO.cs) — 오디오 라이브러리 SO

---

### 3. 경기 시뮬레이션

MVP 패턴의 책임 분리 전략을 응용한 하이브리드 객체지향 설계로, **공방 → 쿼터 → 경기 → 토너먼트** 계층 트리 구조를 구현했다.

**씬 및 레이어 구성**

```
Lobby 씬
  └─ TournamentManager     (Bridge 역할: 대진 생성 → 라운드 진행 → 결과 판정 → 로비 복귀)
       └─ MatchGameManager  (단일 경기 흐름: 쿼터 → 공방 → 하프타임 → 종료)
            ├─ 시뮬레이션 레이어: 쿼터/공방 로직, 경기 데이터 Model, 로그 Presenter
            └─ UI 레이어:    대진표 렌더링, 경기 진행 View 클래스들
```

선수 Stat과 적 선수의 Stat 데이터 테이블을 기반으로 공방 알고리즘을 구현. 경기 종료 후 결과를 가지고 Lobby 씬으로 복귀해 보상·이벤트를 처리한다.

**관련 소스**
- [`TournamentManager.cs`](Assets/_Scripts/Tournament/TournamentManager.cs) — 토너먼트 전체 흐름 총괄
- [`MatchGameManager.cs`](Assets/_Scripts/Tournament/MatchGameManager.cs) — 단일 경기 흐름 제어
- [`MatchGamePlayTurnSimulation.cs`](Assets/_Scripts/Tournament/MatchGamePlayTurnSimulation.cs) — 공방 1회 시뮬레이션
- [`MatchGameQuarterSimulation.cs`](Assets/_Scripts/Tournament/MatchGameQuarterSimulation.cs) — 쿼터 시뮬레이션
- [`MatchGameModels.cs`](Assets/_Scripts/Tournament/MatchGameModels.cs) — 경기 데이터 모델
- [`MatchGameLogPresenter.cs`](Assets/_Scripts/Tournament/MatchGameLogPresenter.cs) — 경기 로그 Presenter

---

### 4. 멀티 디바이스 대응

적절한 Anchor 설계와 레터박스 활용으로 일반적인 갤럭시·아이폰 규격 이외에도 Flip, Fold, 태블릿 등 다양한 디스플레이에서 정상적으로 플레이할 수 있도록 UGUI를 구성했다.

---

### 5. CSV 기반 내러티브 실행 시스템

데이터 테이블 기반으로 내러티브를 실행하는 시스템. VN(비주얼 노벨) 씬과 Lobby 내 메신저 대화 두 가지 경로를 별도로 구현했다.

**스토리 테이블 컬럼 구조**

| 컬럼 | 타입 | 설명 |
|---|---|---|
| `id` | int | 내러티브 섹션 묶음 기준 |
| `line` | int | 섹션 내 출력 순서 |
| `name` | string | 화자 이름 |
| `context` | string | 대사 내용 |
| `bg_img` | string | 배경 이미지 파일명 (ImageLibrary에서 Load) |
| `img_left` / `img_right` | string | 좌·우측 캐릭터 이미지 |
| `bgm_index` | int | 해당 라인에서 재생 시작할 BGM id |
| `sfx_name` | int | 해당 라인에서 재생할 SFX id |

**관련 소스**
- [`VNManager.cs`](Assets/_Scripts/_VN/VNManager.cs) — VN 시나리오 실행 엔진
- [`VNBridge.cs`](Assets/_Scripts/_VN/VNBridge.cs) — VN 씬 진입 요청 관리
- [`DialogueRunner.cs`](Assets/_Scripts/UI/Lobby/Dialogue/DialogueRunner.cs) — Lobby 메신저 대화 실행 엔진

---

### 6. 커스텀 외곽선 컴포넌트

Unity 셰이더 기반 외곽선은 텍스트 테두리 자체가 두꺼워져 디자인 의도와 다른 결과가 나왔다.  
"텍스트를 여러 레이어로 겹쳐서 외곽선처럼 보이게 만들 수 있지 않을까?"라는 아이디어를 기반으로, 원본 TMP를 기준으로 외곽 레이어를 자동 생성·동기화하는 커스텀 컴포넌트를 구현했다.

- 두께와 레이어 수를 분리해 품질·성능 조정 가능
- 원본 텍스트 변경 시 내용·폰트·정렬·크기 자동 동기화
- 생성 레이어는 Layout ignore, Raycast off로 동작 간섭 방지
- 렌더 순서 정렬: 외곽 레이어가 뒤, 원본 텍스트가 앞

**관련 소스**
- [`TMPExternalStroke.cs`](Assets/_Scripts/Util/TMPExternalStroke.cs)

---

### 7. 학생 생성 (팩토리 패턴)

CSV 데이터 테이블에 정의된 학생 생성 규칙을 기반으로 랜덤 학생 인스턴스를 생성하는 팩토리 엔진을 구현했다.

**관련 소스**
- [`StudentFactory.cs`](Assets/_Scripts/Student/StudentFactory.cs) — 학생 생성 팩토리
- [`Student.cs`](Assets/_Scripts/Student/Student.cs) — 학생 데이터 모델

---

## 아키텍처 개요

```
Start 씬
  └─ StartManager: 카탈로그 업데이트 → 리소스 다운로드 → SO 로딩(CachedSOData) → 이미지·오디오 프리웜

Lobby 씬
  ├─ GameManager (씬 전환 상태 허브)
  ├─ TurnManager (1턴 = 1일 파이프라인, ITurnModule 순회 실행)
  ├─ AlwaysEventManager / SuddenEventManager (이벤트 처리)
  ├─ UIManager (팝업 스택, ESC 처리)
  └─ TournamentManager → MatchGameManager (대진·경기·결과)

데이터 흐름
  Google Sheets → CSV (Assets/CSV/) → SO (SO_*.asset) → CachedSOData.Get<T>() → 런타임
```

---

## 폴더 구조

```
Assets/
├─ _Scripts/
│   ├─ System/          # GameManager, TurnManager, DateManager 등 코어
│   ├─ Turn/            # ITurnModule 인터페이스, TurnContext
│   ├─ Tournament/      # 경기 시뮬레이션
│   ├─ Event/           # AlwaysEvent, SuddenEvent
│   ├─ Student/         # 학생 데이터, 팩토리
│   ├─ CSVParser/       # CSV 파싱·임포터·Google Sheets 동기화 (Editor)
│   ├─ AddressableManager/ # 이미지·오디오 Addressable 로더
│   ├─ Save/            # 슬롯 기반 세이브/로드
│   ├─ _VN/             # 비주얼 노벨 엔진
│   ├─ UI/              # 로비·팝업·선택지 UI
│   └─ Util/            # TMPExternalStroke 등 유틸
├─ CSV/                 # 기획 데이터 원본 (~50종)
└─ AddressableAssetsData/ # Addressable 그룹·설정
```

---

## 실행 방법

1. Unity Hub에서 `6000.2.10f1` 버전으로 프로젝트 오픈
2. `Assets/_Scenes/Start.unity` 실행
3. Start 로딩 완료 후 Title에서 새 게임/이어하기 진행

### UGS CLI 사전 준비 (CCD 업로드 시)

```bash
ugs --version          # CLI 설치 확인
ugs login
ugs config get project-id
```

---

## 빌드 정보

- Package: `com.ThreeBGames.ProjectRebound`
- Version: `1.0.6`
- Min SDK: Android 24
