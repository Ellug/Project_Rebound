using UnityEngine;

// 매 턴 종료 시 상시 이벤트 효과(condition 지속 증감)를 처리하는 턴 모듈
// TurnManager에 RegisterModule()로 등록하여 사용
public class AlwaysEffectTickModule : MonoBehaviour, ITurnModule
{
    public string ModuleName => "AlwaysEffectTick";
    public int Priority => 90; // 대부분의 턴 처리 이후, 정산 직전

    public void OnTurnBegin(TurnContext context) { }

    public void OnTurnExecute(TurnContext context) { }

    // 턴 종료 시 activeEffectIds 기준으로 condition 지속 증감 적용
    public void OnTurnEnd(TurnContext context)
    {
        AlwaysEffectApplier.TickCondition();
    }
}