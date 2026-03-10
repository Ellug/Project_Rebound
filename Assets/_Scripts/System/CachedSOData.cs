using System;
using System.Collections.Generic;
using UnityEngine;

public static class CachedSOData
{
    // 로드된 테이블을 타입 키로 보관
    private static readonly Dictionary<Type, ScriptableObject> _tables = new();

    // 타입으로 캐시된 테이블을 조회
    public static T Get<T>() where T : ScriptableObject
        => _tables.TryGetValue(typeof(T), out var table) ? table as T : null;

    // 타입 조회 결과를 bool과 out으로 반환
    public static bool TryGet<T>(out T table) where T : ScriptableObject
    {
        if (_tables.TryGetValue(typeof(T), out var value) && value is T typed)
        {
            table = typed;
            return true;
        }

        table = null;
        return false;
    }

    // 단일 테이블을 타입 키로 등록/갱신
    public static void RegisterTable(ScriptableObject table)
    {
        if (table == null) return;
        _tables[table.GetType()] = table;
    }

    // 여러 테이블을 순회 등록
    public static void RegisterTables(IEnumerable<ScriptableObject> tables)
    {
        if (tables == null) return;

        foreach (var table in tables)
            RegisterTable(table);
    }

    // 가변 인자 등록 오버로드
    public static void RegisterTables(params ScriptableObject[] tables)
        => RegisterTables((IEnumerable<ScriptableObject>)tables);

    // 캐시된 테이블 전체를 비움
    public static void Clear()
    {
        _tables.Clear();
    }
}
