using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//훈련 리스트 내 개별 버튼 아이템 (프리팹으로 동적 생성)
public class TrainingButtonItem : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtStat;

    private Action _onClick;

    //버튼 데이터 세팅 및 클릭 콜백 등록
    public void Setup(string trainingName, string statText, Action onClick)
    {
        if (_txtName != null)
        {
            _txtName.text = trainingName;
        }

        //스탯 텍스트가 비어있으면 숨김 처리
        if (_txtStat != null)
        {
            bool hasStat = !string.IsNullOrEmpty(statText);
            _txtStat.gameObject.SetActive(hasStat);

            if (hasStat)
            {
                _txtStat.text = statText;
            }
        }

        _onClick = onClick;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => _onClick?.Invoke());
    }
}