using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 훈련 UI 흐름 관리 (게이지 연출 → 스탯 적용 → 결과 표시)
// 씬에 항상 활성화 상태로 배치 (코루틴 실행 주체)
public class TrainingFlowController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TrainingProgressUI _progressUI;
    [SerializeField] private TrainingResultPopup _resultPopup;

    [Header("Canvas")]
    [SerializeField] private Transform _popupParent;

    [Header("Progress Settings")]
    [SerializeField] private float _fillDuration = 2.0f;
    [SerializeField] private float _holdDuration = 0.5f;

    public event Action OnFlowComplete;

    private Coroutine _running;

    public void Execute(
        string trainingKey,
        string trainingName,
        List<Student> students,
        Action<string, List<Student>> applyEffect = null,
        Sprite backgroundSprite = null)
    {
        if (students == null || students.Count == 0)
        {
            Debug.LogWarning("[TrainingFlowController] 학생이 없습니다.");
            OnFlowComplete?.Invoke();
            return;
        }

        // before 스냅샷
        List<TrainingResult> results = new List<TrainingResult>(students.Count);
        foreach (Student student in students)
        {
            results.Add(new TrainingResult
            {
                before = SnapshotStudent(student),
                after = student
            });
        }

        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        if (_progressUI != null)
            _progressUI.Show(backgroundSprite);

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

                StartCoroutine(HoldAndFinish(trainingKey, trainingName, students, results, applyEffect));
            }
        ));
    }

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

    private IEnumerator HoldAndFinish(
        string trainingKey,
        string trainingName,
        List<Student> students,
        List<TrainingResult> results,
        Action<string, List<Student>> applyEffect)
    {
        yield return new WaitForSeconds(_holdDuration);

        if (_progressUI != null)
            _progressUI.Hide();

        // 스탯 적용
        if (applyEffect != null)
            applyEffect.Invoke(trainingKey, students);
        else
            ApplyTempEffect(trainingKey, students);

        // 결과 표시
        ShowResultPopup(trainingName, results);

        _running = null;
    }

    // TrainingFlowController.cs
    private void ShowResultPopup(string trainingName, List<TrainingResult> results)
    {
        if (_resultPopup == null)
        {
            Debug.LogError("TrainingResultPopup 참조가 없습니다.");
            OnFlowComplete?.Invoke();
            return;
        }

        _resultPopup.Init();
        _resultPopup.Setup(trainingName, results);
        _resultPopup.Open();

        // 누적 방지
        _resultPopup.OnConfirm -= HandlePopupConfirm;
        _resultPopup.OnConfirm += HandlePopupConfirm;
    }

    private void HandlePopupConfirm()
    {
        _resultPopup.OnConfirm -= HandlePopupConfirm;
        OnFlowComplete?.Invoke();
    }


    private void ApplyTempEffect(string trainingKey, List<Student> students)
    {
        foreach (Student student in students)
        {
            switch (trainingKey)
            {
                case "group_weight": student.jump += 2; break;
                case "group_shuttle": student.speed += 2; break;
                case "group_tactics": student.jump += 1; break;
                case "personal_shooting": student.shoot += 1; break;
                case "personal_postup": student.shoot += 1; student.jump += 1; break;
                case "personal_defense": student.jump += 1; break;
                case "personal_handling": student.speed += 1; student.jump += 1; break;
            }

            if (StudentManager.Instance != null)
                StudentManager.Instance.NotifyStudentModified(student);
        }
    }

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
            trust = original.trust
        };
    }
}