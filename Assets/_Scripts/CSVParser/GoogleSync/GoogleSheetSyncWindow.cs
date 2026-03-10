#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 시트 동기화 → 전체 임포트 → 등록 동기화를 실행하는 단순 창
public class GoogleSheetSyncWindow : EditorWindow
{
    public static void OpenWindow()
    {
        var window = GetWindow<GoogleSheetSyncWindow>("Google Sheets Sync");
        window.minSize = new Vector2(420, 180);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Google Sheets CSV Sync", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox("Pipeline: Sheet Sync -> Import All CSV Tables -> TableLoadConfig/Addressables Sync", MessageType.Info);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("Run Data Pipeline", GUILayout.Height(34)))
            GoogleSheetSyncer.SyncAll();
    }
}
#endif
