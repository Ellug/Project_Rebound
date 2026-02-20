using System.Collections.Generic;
using UnityEngine;

// GrowthCommandTableSO 데이터를 TrainingPageData(SO)의 런타임 버튼으로 변환
// SO는 페이지 타이틀과 commandIndices만 관리, 버튼 데이터는 CSV 기준으로 자동 생성
public static class TrainingPageBuilder
{
    // 공개 진입점
    // SO.pages가 비어있으면 CSV parent_index 구조 기준으로 전부 자동 생성
    // SO.pages가 정의돼 있으면 페이지 구조 유지, buttons(NonSerialized)만 채운다
    public static void Build(TrainingPageData pageData, GrowthCommandTableSO table)
    {
        if (pageData == null || table == null)
        {
            Debug.LogWarning("[TrainingPageBuilder] pageData 또는 table이 null입니다.");
            return;
        }

        if (pageData.pages == null || pageData.pages.Count == 0)
            BuildAutoPages(pageData, table);
        else
            FillButtonsIntoPages(pageData, table);
    }

    // SO가 비어있을 때 : CSV parent_index 구조 기준으로 자동 생성

    private static void BuildAutoPages(TrainingPageData pageData, GrowthCommandTableSO table)
    {
        pageData.pages.Clear();

        var rootPage = new TrainingPageInfo { pageTitle = "훈련 선택" };
        int nextPageIndex = 1;

        foreach (var row in table.GetChildren(0))
        {
            if (row.btnType == GrowthCommandBtnType.Category)
            {
                int childPageIndex = nextPageIndex++;
                rootPage.buttons.Add(BuildNavigateButton(row, childPageIndex));

                var childPage = new TrainingPageInfo { pageTitle = row.name };
                FillActionButtons(childPage, table.GetChildren(row.index));
                pageData.pages.Add(childPage);
            }
            else
            {
                rootPage.buttons.Add(BuildActionButton(row));
            }
        }

        pageData.pages.Insert(0, rootPage);
    }

    // SO에 페이지가 정의돼 있을 때 : buttons(NonSerialized)만 채운다
    //
    // SO 페이지 구성 규칙:
    //   0번 페이지 commandIndices → 최상위에 표시할 row index 목록
    //     - Category row : 해당 row의 자식들을 담는 하위 페이지를 자동 생성해 연결
    //     - Action row   : 즉시 실행 버튼
    //   1번~ 페이지 commandIndices → 해당 페이지에 직접 표시할 Action row index 목록
    //     (Category row를 넣으면 재귀적으로 하위 페이지 생성)

    private static void FillButtonsIntoPages(TrainingPageData pageData, GrowthCommandTableSO table)
    {
        // 런타임 버튼 초기화
        foreach (var page in pageData.pages)
            page.buttons = new List<TrainingButtonData>();

        // 0번 페이지부터 순서대로 처리
        // 처리 중 하위 페이지가 동적으로 추가될 수 있으므로 index 방식 순회
        for (int i = 0; i < pageData.pages.Count; i++)
        {
            var page = pageData.pages[i];
            IReadOnlyList<GrowthCommandRow> rows = ResolveRows(page, table, i);

            foreach (var row in rows)
            {
                if (row.btnType == GrowthCommandBtnType.Category)
                {
                    // 하위 페이지가 SO에 이미 정의돼 있는지 확인
                    // (다음 페이지의 commandIndices에 이 카테고리의 자식 index들이 있으면 해당 페이지로 연결)
                    int childPageIndex = FindLinkedChildPage(pageData, table, row.index, i + 1);

                    if (childPageIndex < 0)
                    {
                        // 없으면 런타임에 하위 페이지 자동 생성
                        childPageIndex = pageData.pages.Count;
                        var childPage = new TrainingPageInfo { pageTitle = row.name };
                        childPage.buttons = new List<TrainingButtonData>();
                        FillActionButtons(childPage, table.GetChildren(row.index));
                        pageData.pages.Add(childPage);
                    }

                    page.buttons.Add(BuildNavigateButton(row, childPageIndex));
                }
                else
                {
                    page.buttons.Add(BuildActionButton(row));
                }
            }
        }
    }

    // commandIndices → row 목록 변환
    // 0번 페이지이고 commandIndices가 비어있으면 parent_index == 0 기준 자동 수집
    private static IReadOnlyList<GrowthCommandRow> ResolveRows(
        TrainingPageInfo page, GrowthCommandTableSO table, int pageIndex)
    {
        if (page.commandIndices != null && page.commandIndices.Count > 0)
        {
            var result = new List<GrowthCommandRow>(page.commandIndices.Count);
            foreach (int idx in page.commandIndices)
            {
                if (table.TryGet(idx, out var row))
                    result.Add(row);
                else
                    Debug.LogWarning($"[TrainingPageBuilder] index {idx} 를 테이블에서 찾을 수 없습니다.");
            }
            return result;
        }

        if (pageIndex == 0)
            return table.GetChildren(0);

        Debug.LogWarning($"[TrainingPageBuilder] '{page.pageTitle}' 페이지에 commandIndices가 없습니다.");
        return System.Array.Empty<GrowthCommandRow>();
    }

    // 카테고리 row의 자식 index들을 commandIndices로 포함하는 페이지를 찾는다
    // searchFromIndex 이후의 페이지만 탐색 (이미 처리된 페이지 제외)
    private static int FindLinkedChildPage(
        TrainingPageData pageData, GrowthCommandTableSO table, int categoryIndex, int searchFromIndex)
    {
        IReadOnlyList<GrowthCommandRow> children = table.GetChildren(categoryIndex);
        if (children.Count == 0) return -1;

        // 자식 index 집합
        var childIndexSet = new HashSet<int>();
        foreach (var child in children)
            childIndexSet.Add(child.index);

        for (int i = searchFromIndex; i < pageData.pages.Count; i++)
        {
            var page = pageData.pages[i];
            if (page.commandIndices == null || page.commandIndices.Count == 0) continue;

            // 페이지의 commandIndices가 자식 집합과 겹치면 해당 페이지로 연결
            foreach (int idx in page.commandIndices)
            {
                if (childIndexSet.Contains(idx))
                    return i;
            }
        }

        return -1;
    }

    // 액션 row 목록을 페이지 buttons에 추가
    private static void FillActionButtons(TrainingPageInfo page, IReadOnlyList<GrowthCommandRow> rows)
    {
        foreach (var row in rows)
            page.buttons.Add(BuildActionButton(row));
    }

    // 버튼 데이터 빌더

    private static TrainingButtonData BuildNavigateButton(GrowthCommandRow row, int targetPageIndex)
    {
        return new TrainingButtonData
        {
            trainingName = row.name,
            trainingDesc = "",
            statModifierText = "",
            conditionDelta = 0,
            navigateToPageIndex = targetPageIndex,
            trainingKey = $"category_{row.index}",
            previewSprite = null,
            requiresStudentSelection = false,
            maxSelectCount = 0
        };
    }

    private static TrainingButtonData BuildActionButton(GrowthCommandRow row)
    {
        return new TrainingButtonData
        {
            trainingName = row.name,
            trainingDesc = BuildDesc(row),
            statModifierText = BuildStatText(row),
            conditionDelta = row.hpCost,
            navigateToPageIndex = -1,
            trainingKey = $"cmd_{row.index}",
            previewSprite = null,
            requiresStudentSelection = row.target == GrowthCommandTarget.Individual,
            // Team 훈련은 전체 학생 대상이므로 무제한(0), Individual은 1명
            maxSelectCount = row.target == GrowthCommandTarget.Individual ? 1 : 0
        };
    }

    // 텍스트 빌더

    private static string BuildStatText(GrowthCommandRow row)
    {
        var parts = new List<string>(5);

        if (row.shoot != 0f) parts.Add(FormatStat("슈팅", row.shoot));
        if (row.speed != 0f) parts.Add(FormatStat("속도", row.speed));
        if (row.defense != 0f) parts.Add(FormatStat("수비", row.defense));
        if (row.stamina != 0f) parts.Add(FormatStat("스태미나", row.stamina));
        if (row.mental != 0) parts.Add(FormatStatInt("멘탈", row.mental));

        return parts.Count > 0 ? string.Join(" / ", parts) : "";
    }

    private static string FormatStat(string label, float value)
        => value > 0f ? $"{label} +{value}" : $"{label} {value}";

    private static string FormatStatInt(string label, int value)
        => value > 0 ? $"{label} +{value}" : $"{label} {value}";

    private static string BuildDesc(GrowthCommandRow row)
    {
        if (row.facilityReq == GrowthFacilityReq.None)
            return "";

        string facilityName = row.facilityReq switch
        {
            GrowthFacilityReq.School => "학교",
            GrowthFacilityReq.Gym => "체육관",
            GrowthFacilityReq.Cafeteria => "식당",
            GrowthFacilityReq.CounselingCenter => "상담실",
            _ => ""
        };

        return $"[{facilityName} Lv.{row.facilityLv} 이상 필요]";
    }
}