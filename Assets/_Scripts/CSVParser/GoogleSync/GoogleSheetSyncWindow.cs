#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 테이블별 체크박스 선택 및 개별/일괄 동기화를 지원하는 구글 시트 동기화 에디터 창
public class GoogleSheetSyncWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private bool[] _selected;
    private bool _selectAll = true;

    [MenuItem("Tools/Data/Google Sheets Sync Window")]
    public static void OpenWindow()
    {
        var window = GetWindow<GoogleSheetSyncWindow>("Google Sheets Sync");
        window.minSize = new Vector2(480, 380);
        window.Show();
    }

    void OnEnable()
    {
        int count = GoogleSheetSyncConfig.Tables.Count;
        _selected = new bool[count];
        for (int i = 0; i < count; i++)
            _selected[i] = true;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Google Sheets CSV Sync", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        bool newSelectAll = EditorGUILayout.ToggleLeft("Select All", _selectAll, GUILayout.Width(100));
        if (newSelectAll != _selectAll)
        {
            _selectAll = newSelectAll;
            for (int i = 0; i < _selected.Length; i++)
                _selected[i] = _selectAll;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 컬럼 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(24);
        EditorGUILayout.LabelField("Display Name", EditorStyles.miniLabel, GUILayout.Width(160));
        EditorGUILayout.LabelField("CSV File",     EditorStyles.miniLabel, GUILayout.MinWidth(80));
        GUILayout.Space(58);
        EditorGUILayout.EndHorizontal();

        // 테이블 목록
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
        var tables = GoogleSheetSyncConfig.Tables;
        for (int i = 0; i < tables.Count; i++)
        {
            var entry = tables[i];
            EditorGUILayout.BeginHorizontal();

            _selected[i] = EditorGUILayout.Toggle(_selected[i], GUILayout.Width(20));
            EditorGUILayout.LabelField(entry.DisplayName, GUILayout.Width(160));
            EditorGUILayout.LabelField(entry.CsvFileName, EditorStyles.miniLabel, GUILayout.MinWidth(80));

            // 해당 테이블만 즉시 강제 동기화
            if (GUILayout.Button("Sync", GUILayout.Width(50)))
            {
                GoogleSheetSyncer.SyncTables(new List<SheetTableEntry> { entry }, forceAll: true);
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
        EditorGUILayout.Space(2);

        // 하단 일괄 동기화 버튼
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Sync Selected\n(Changed Only)", GUILayout.Height(36)))
        {
            var selected = GetSelectedEntries();
            if (selected.Count > 0)
            {
                GoogleSheetSyncer.SyncTables(selected, forceAll: false);
                Repaint();
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", "Please select at least one table.", "OK");
            }
        }

        if (GUILayout.Button("Force Sync Selected\n(Ignore Local)", GUILayout.Height(36)))
        {
            var selected = GetSelectedEntries();
            if (selected.Count > 0)
            {
                GoogleSheetSyncer.SyncTables(selected, forceAll: true);
                Repaint();
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", "Please select at least one table.", "OK");
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    // 체크박스가 선택된 테이블 항목만 모아서 반환
    private List<SheetTableEntry> GetSelectedEntries()
    {
        var result = new List<SheetTableEntry>();
        var tables = GoogleSheetSyncConfig.Tables;
        for (int i = 0; i < tables.Count; i++)
        {
            if (_selected[i])
                result.Add(tables[i]);
        }
        return result;
    }
}
#endif
