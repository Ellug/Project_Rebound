using System.Collections.Generic;
using UnityEngine;

// 훈련 선택 팝업의 전체 페이지 구성 데이터
[CreateAssetMenu(fileName = "TrainingPagesConfig", menuName = "Game/Training Pages Config")]
public class TrainingPageData : ScriptableObject
{
    // 페이지 배열 (0: 훈련 선택, 1: 단체 훈련, 2: 개인 훈련 …)
    public List<TrainingPageInfo> pages = new List<TrainingPageInfo>();
}