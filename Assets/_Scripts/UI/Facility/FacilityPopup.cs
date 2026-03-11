using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FacilityPopup : UIBase
{
    [Serializable]
    private class FacilityUpgradeRowUI
    {
        public TMP_Text facilityNameLV;           // 시설 이름과 레벨
        public TMP_Text upgradeCost;            // 업그레이드 비용
        public Button upgradeButton;            // 업그레이드 버튼
        public TMP_Text upgradeButtonText;      // 버튼 텍스트
    }

    [Header("시설 설명")]
    [SerializeField] private TMP_Text _txtDescription;

    [Header("시설 선택 버튼")]
    [SerializeField] private Button _btnSchool;
    [SerializeField] private Button _btnGym;
    [SerializeField] private Button _btnCafeteria;
    [SerializeField] private Button _btnCounseling;

    [Header("업그레이드 UI")]
    [SerializeField] private FacilityUpgradeRowUI _schoolRow;
    [SerializeField] private FacilityUpgradeRowUI _gymRow;
    [SerializeField] private FacilityUpgradeRowUI _cafeteriaRow;
    [SerializeField] private FacilityUpgradeRowUI _counselingRow;
    
    // 중복 방지
    private bool _inited;
    public override void Init()
    {
        if (_inited)
        {
            return;
        }
        _inited = true;

        base.Init();

        BindButtons();
        RefreshAll();
    }
    // 시설 클릭시 설명, 업그레이드 클릭 시 업그레이드
    private void BindButtons()
    {
        if (_btnSchool != null)
        {
            _btnSchool.onClick.AddListener(() => ShowDescription("school"));
        }

        if (_btnGym != null)
        {
            _btnGym.onClick.AddListener(() => ShowDescription("gym"));
        }

        if (_btnCafeteria != null)
        {
            _btnCafeteria.onClick.AddListener(() => ShowDescription("cafeteria"));
        }

        if (_btnCounseling != null)
        {
            _btnCounseling.onClick.AddListener(() => ShowDescription("counselingcenter"));
        }

        _schoolRow.upgradeButton.onClick.AddListener(() => TryUpgrade("school"));
        _gymRow.upgradeButton.onClick.AddListener(() => TryUpgrade("gym"));
        _cafeteriaRow.upgradeButton.onClick.AddListener(() => TryUpgrade("cafeteria"));
        _counselingRow.upgradeButton.onClick.AddListener(() => TryUpgrade("counselingcenter"));
    }

    private void TryUpgrade(string facility)
    {
        if (FacilitySystem.Instance.TryUpgrade(facility))
        {
            RefreshAll();
        }
    }
    // 갱신
    private void RefreshAll()
    {
        RefreshRow("school", _schoolRow);
        RefreshRow("gym", _gymRow);
        RefreshRow("cafeteria", _cafeteriaRow);
        RefreshRow("counselingcenter", _counselingRow);
    }

    private void RefreshRow(string facility, FacilityUpgradeRowUI row)
    {
        int level = FacilitySystem.Instance.GetLevel(facility);
        var current = FacilitySystem.Instance.GetCurrentData(facility);
        var next = FacilitySystem.Instance.GetNextData(facility);

        if (current == null)
        {
            return;
        }

        if (row.facilityNameLV != null)
        {
            row.facilityNameLV.text = $"{current.facilityName} Lv {level}";
        }

        if (next == null)
        {
            row.upgradeCost.text = "-";
            row.upgradeButtonText.text = "MAX";
            row.upgradeButton.interactable = false;
            return;
        }

        row.upgradeCost.text = next.upgradeCost.ToString();

        int money = MoneyManager.Instance.Gold;

        if (facility == "school")
        {
            if (!FacilitySystem.Instance.CanUpgradeSchool())
            {
                row.upgradeButtonText.text = "조건 부족";
                row.upgradeButton.interactable = false;
                return;
            }
        }

        if (money < next.upgradeCost)
        {
            row.upgradeButtonText.text = "재화 부족";
            row.upgradeButton.interactable = false;
            return;
        }
        else
        {
            row.upgradeButtonText.text = "업그레이드";
            row.upgradeButton.interactable = true;
        }
    }

    private void ShowDescription(string facility)
    {
        switch (facility)
        {
            case "school":
                _txtDescription.text = "학교는....";
                break;

            case "gym":
                _txtDescription.text = "체육관은 멘탈과 컨디션외 모든 스탯의 성장에 도움을 줍니다.";
                break;

            case "cafeteria":
                _txtDescription.text = "식당은 모든 선수의 멘탈 및 컨디션 관리에 도움을 줍니다.";
                break;

            case "counselingcenter":
                _txtDescription.text = "심리 상담실은 선수 개인의 멘탈 스탯에 큰 도움을 줍니다.";
                break;
        }
    }
}