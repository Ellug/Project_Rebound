using System;
using UnityEngine;

// 훈련 버튼 1개의 데이터
[Serializable]
public class TrainingButtonData
{
    public string trainingName;                     // 버튼에 표시될 훈련 이름
    public string trainingDesc;                     // 훈련 설명
    public string statModifierText;                 // 스탯 가감치 텍스트 (비어있으면 숨김)
    public int conditionDelta;                      // 컨디션 가감치
    public int navigateToPageIndex = -1;            // 이동할 페이지 인덱스 (-1이면 바로 실행)
    public string trainingKey;                      // 훈련 고유 식별 키
    public Sprite previewSprite;                    // 팝업창에 표시할 이미지
    public bool requiresStudentSelection = true;    // 학생 선택이 필요한 훈련인지 (기본: true)
    public int maxSelectCount = 0;                  // 학생 선택 최대 인원 (0이면 무제한)
    public int requiredFacilityLv;                  // 필요 시설레벨

    // CSV 기반 실제 적용 수치
    public float shootDelta;
    public float speedDelta;
    public float jumpDelta;
    public float staminaDelta;
    public int mentalDelta;
}