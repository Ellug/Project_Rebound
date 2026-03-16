using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 훈련 전체 흐름 관리
// 게이지 연출 → 스탯 적용 → 결과 팝업 표시
public class TrainingFlowController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TrainingProgressUI _progressUI;     // 진행 게이지 UI
    [SerializeField] private TrainingResultPopup _resultPopup;   // 결과 팝업

    [Header("Progress Settings")]
    [SerializeField] private float _fillDuration = 2.0f;         // 게이지 채우는 시간
    [SerializeField] private float _holdDuration = 0.5f;         // 완료 후 대기 시간

    [Header("Weekend Training Images")]
    [SerializeField] private string _weekendTrainingConfirmBgImageId;  // 주말 훈련 배경 이미지 ID
    [SerializeField] private string _weekendTrainingCancelBgImageId;   // 주말 휴식 배경 이미지 ID
    [SerializeField] private string _weekendTrainingConfirmResultImageId;  // 주말 훈련 결과 이미지 ID
    [SerializeField] private string _weekendTrainingCancelResultImageId;   // 주말 휴식 결과 이미지 ID


    public event Action OnFlowComplete;                          // 전체 흐름 종료 콜백
    private Coroutine _running;                                  // 실행중 코루틴

    // 훈련 실행 진입점
    public void Execute(
        string trainingKey,
        string trainingName,
        List<Student> students,
        Action<string, List<Student>> applyEffect = null,
        string backgroundImageId = null,
        string resultImageId = null)
    {
        if (students == null || students.Count == 0)
        {
            Debug.LogWarning("[TrainingFlowController] 학생이 없습니다.");
            OnFlowComplete?.Invoke();
            return;
        }

        // 훈련 전 상태 스냅샷 저장
        List<TrainingResult> results = new List<TrainingResult>(students.Count);
        foreach (Student student in students)
        {
            results.Add(new TrainingResult
            {
                before = SnapshotStudent(student),
                after = student
            });
        }

        // 기존 코루틴 중지
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        // 진행 UI 표시
        if (_progressUI != null)
            _progressUI.Show(backgroundImageId);

        // 게이지 연출 시작
        _running = StartCoroutine(ProgressRoutine(
            targetFill01: 1f,
            onTick: (fill01) =>
            {
                if (_progressUI != null && _progressUI.gameObject.activeInHierarchy)
                    _progressUI.SetProgress01(fill01);
            },
            onDone: () =>
            {
                if (_progressUI != null && _progressUI.gameObject.activeInHierarchy)
                {
                    _progressUI.SetProgress01(1f);
                    _progressUI.SetStatus("완료!");
                }

                StartCoroutine(HoldAndFinish(trainingKey, trainingName, students, results, applyEffect, resultImageId));
            }
        ));
    }

    // 게이지 채우기 코루틴
    private IEnumerator ProgressRoutine(float targetFill01, Action<float> onTick, Action onDone)
    {
        float elapsed = 0f;

        while (elapsed < _fillDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / _fillDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float fill = eased * Mathf.Clamp01(targetFill01);

            onTick?.Invoke(fill);
            yield return null;
        }

        onTick?.Invoke(Mathf.Clamp01(targetFill01));
        onDone?.Invoke();
    }

    // 완료 후 대기 → 효과 적용 → 결과 팝업
    private IEnumerator HoldAndFinish(
        string trainingKey,
        string trainingName,
        List<Student> students,
        List<TrainingResult> results,
        Action<string, List<Student>> applyEffect,
        string resultImageId = null)
    {
        yield return new WaitForSeconds(_holdDuration);

        if (_progressUI != null)
            _progressUI.Hide();

        // 스탯 적용
        if (applyEffect != null)
            applyEffect.Invoke(trainingKey, students);
        else
            Debug.LogWarning("[TrainingFlowController] applyEffect가 null이라 스탯 적용을 건너뜁니다.");

        ShowResultPopup(trainingName, results, resultImageId);

        _running = null;
    }

    // 결과 팝업 표시
    private void ShowResultPopup(string trainingName, List<TrainingResult> results, string resultImageId = null)
    {
        if (_resultPopup == null)
        {
            Debug.LogError("[TrainingFlowController] TrainingResultPopup 참조가 없습니다.");
            OnFlowComplete?.Invoke();
            return;
        }

        _resultPopup.Init();
        _resultPopup.Setup(trainingName, results, resultImageId);
        _resultPopup.Open();

        _resultPopup.OnConfirm -= HandlePopupConfirm;
        _resultPopup.OnConfirm += HandlePopupConfirm;
    }

    // 결과 팝업 확인 시 흐름 종료
    private void HandlePopupConfirm()
    {
        if (_resultPopup != null)
            _resultPopup.OnConfirm -= HandlePopupConfirm;

        OnFlowComplete?.Invoke();
    }

    // 학생 상태 복사 (훈련 전 상태 저장용)
    private Student SnapshotStudent(Student original)
    {
        return new Student
        {
            id = original.id,
            studentName = original.studentName,
            positionName = original.positionName,
            grade = original.grade,
            height = original.height,
            weight = original.weight,
            mental = original.mental,
            shoot = original.shoot,
            speed = original.speed,
            jump = original.jump,
            stamina = original.stamina,
            potential = original.potential,
            potential_tier = original.potential_tier,
            condition = original.condition,
            // trust = original.trust
        };
    }

    public string GetWeekendBgImageId(int rowIndex)
    {
        return rowIndex == 901
            ? _weekendTrainingConfirmBgImageId
            : _weekendTrainingCancelBgImageId;
    }

    public string GetWeekendResultImageId(int rowIndex)
    {
        return rowIndex == 901
            ? _weekendTrainingConfirmResultImageId
            : _weekendTrainingCancelResultImageId;
    }
}