using System;
using System.Collections.Generic;

// 페이지 1장의 구성 데이터
// commandIndices : 이 페이지에 표시할 GrowthCommandRow.index 목록
// 비어있으면 Builder가 parent_index 기준으로 자동 수집
[Serializable]
public class TrainingPageInfo
{
    public string pageTitle;

    // SO에서 직접 지정하고 싶은 경우 사용 (비워두면 Builder가 자동으로 채운다)
    public List<int> commandIndices = new List<int>();

    // 버튼 인덱스 순서대로 이미지 ID 입력 (비워두면 이미지 없음)
    public List<string> buttonPreviewImageIds = new List<string>();

    // ProgressUI 배경 이미지
    public List<string> buttonBackgroundImageIds = new List<string>();

    // 런타임 전용 — Builder가 채워주는 버튼 데이터 (SO에 저장되지 않음)
    [NonSerialized]
    public List<TrainingButtonData> buttons = new List<TrainingButtonData>();
}