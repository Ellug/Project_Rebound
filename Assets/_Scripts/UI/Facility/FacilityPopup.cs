using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FacilityPopup : UIBase
{
    [Serializable]
    private class FacilityUpgradeRowUI
    {
        public TMP_Text facilityLv;     // 시설 레벨
        public TMP_Text upgradeCost;    // 업그레이드 비용
        public Button upgradeButton;    // 업그레이드 버튼
        public Image iconUpgrade;       // 업그레이드 가능
        public Image iconMoneyLack;     // 재화 부족
        public Image iconLock;          // 조건 부족
        public Image iconMax;           // MAX
        public Image normalImage;       // 기본 이미지
        public Image normalImage2;       // 기본 이미지
        public Image maxImage;          // 만렙 이미지
        public Image maxImage2;          // 만렙 이미지
    }

    [SerializeField] private Button _btnClose;      // 닫기 버튼
    [SerializeField] private FacilityPopup _panelClose;      // 닫기 버튼
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
    // 업그레이드 클릭 시 업그레이드
    private void BindButtons()
    {
        _schoolRow.upgradeButton.onClick.RemoveAllListeners();
        _gymRow.upgradeButton.onClick.RemoveAllListeners();
        _cafeteriaRow.upgradeButton.onClick.RemoveAllListeners();
        _counselingRow.upgradeButton.onClick.RemoveAllListeners();

        _schoolRow.upgradeButton.onClick.AddListener(() => TryUpgrade("school"));
        _gymRow.upgradeButton.onClick.AddListener(() => TryUpgrade("gym"));
        _cafeteriaRow.upgradeButton.onClick.AddListener(() => TryUpgrade("cafeteria"));
        _counselingRow.upgradeButton.onClick.AddListener(() => TryUpgrade("counselingcenter"));
    }

    private void TryUpgrade(string facility)
    {
        Debug.Log("Upgrade Click: " + facility);
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
        if (row == null)
        {
            return;
        }

        row.facilityLv.text = $"LV.{level}";

        row.iconLock.gameObject.SetActive(false);
        row.iconUpgrade.gameObject.SetActive(false);
        row.iconMoneyLack.gameObject.SetActive(false);
        row.iconMax.gameObject.SetActive(false);
        row.upgradeCost.gameObject.SetActive(false);

        // max레벨 일떄
        if (next == null)
        {
            row.iconMax.gameObject.SetActive(true);
            row.upgradeButton.interactable = false;
            row.upgradeButton.image.enabled = false;

            if (row.normalImage != null)
            {
                row.normalImage.gameObject.SetActive(false);
            }

            if (row.maxImage != null)
            {
                row.maxImage.gameObject.SetActive(true);
            }
            if (row.normalImage2 != null)
            {
                row.normalImage2.gameObject.SetActive(false);
            }

            if (row.maxImage2 != null)
            {
                row.maxImage2.gameObject.SetActive(true);
            }

            return;
        }
        else
        {
            if (row.normalImage != null)
            {
                row.normalImage.gameObject.SetActive(true);
            }

            if (row.maxImage != null)
            {
                row.maxImage.gameObject.SetActive(false);
            }
            if (row.normalImage2 != null)
            {
                row.normalImage2.gameObject.SetActive(true);
            }

            if (row.maxImage2 != null)
            {
                row.maxImage2.gameObject.SetActive(false);
            }
        }

        int money = MoneyManager.Instance.Gold;

        if (facility == "school" && !FacilitySystem.Instance.CanUpgradeSchool())
        {
            row.iconLock.gameObject.SetActive(true);
            row.upgradeButton.interactable = false;
            row.upgradeButton.image.enabled = false;
            return;
        }

        int cost = current.upgradeCost;
        if (money < cost)
        {
            row.iconMoneyLack.gameObject.SetActive(true);
            row.upgradeCost.text = cost.ToString();
            row.upgradeCost.gameObject.SetActive(true);
            row.upgradeCost.color = Color.white;
            row.upgradeButton.interactable = false;
            row.upgradeButton.image.enabled = false;
            return;
        }

        row.iconUpgrade.gameObject.SetActive(true);
        row.upgradeCost.text = cost.ToString();
        row.upgradeCost.gameObject.SetActive(true);
        row.upgradeCost.color = Color.black;
        row.upgradeButton.interactable = true;
        row.upgradeButton.image.enabled = true;
    }

    public void CloseFacilityPopup()
    {
        _panelClose.gameObject.SetActive(false);
    }
}