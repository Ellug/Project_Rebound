#if UNITY_EDITOR
// 구글 시트 동기화 파이프라인의 CCD 업로드 동작 설정
public static class GoogleSheetCloudUploadConfig
{
    // UGS CLI 실행 파일. PowerShell 정책 이슈를 피하려면 Windows에서는 ugs.cmd 권장.
    public static readonly string UgsExecutable = "ugs.cmd";

    // CLI 단일 명령 타임아웃(초)
    public static readonly int UgsCommandTimeoutSeconds = 600;

    // 로컬에 없는 원격 엔트리를 삭제할지 여부
    public static readonly bool DeleteMissingEntries = true;

    // sync 완료 후 릴리즈 자동 생성 여부
    public static readonly bool CreateReleaseOnSync = true;

    // 세만틱 버전(vMajor.Minor.Patch) 자동 증가를 사용할지 여부
    public static readonly bool UseAutoSemanticVersion = true;

    // 앞의 두 자리(Major/Minor)는 수동 지정
    public static readonly int VersionMajor = 0;
    public static readonly int VersionMinor = 1;

    // Patch 번호 저장 위치(프로젝트 루트 기준 상대 경로 또는 절대 경로)
    public static readonly string VersionStateFilePath = "ProjectSettings/GoogleSheetCloudVersionState.json";

    // 자동 세만틱 버전 사용 시: release-notes = vMajor.Minor.Patch
    // 자동 세만틱 버전을 끄면 아래 접두어 + timestamp 형식 사용
    public static readonly string ReleaseNotesPrefix = "google_sheet_sync";
}
#endif
