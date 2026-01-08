using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PhoneUIManager : MonoBehaviour
{
    [Header("Areas")]
    public GameObject diceArea;
    public GameObject dialogScroll;
    public GameObject menuArea;

    [Header("Item Menu Elements")]
    public GameObject itemPanel;
    public Transform itemListRoot;
    public GameObject itemButtonPrefab;

    [Header("Text Components")]
    public TextMeshProUGUI logText;
    public TextMeshProUGUI dialogText;
    public Transform choiceRoot;
    public GameObject choiceButtonPrefab;

    [Header("Status & Log Settings")]
    public TextMeshProUGUI headerDateText;
    public TextMeshProUGUI statusText;
    public ScrollRect logScrollRect;
    public PlayerStats playerStats;

    private void Start()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance;
        UpdateStatusUI();
    }

    public void UpdateStatusUI()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance;
        if (playerStats == null) return;

        if (headerDateText != null)
            headerDateText.text = $"{playerStats.currentGrade}年 {playerStats.currentMonth}月";

        if (statusText != null)
        {
            statusText.text = $"GP: {playerStats.gp:N0}\n" +
                              $"友: {playerStats.friends}人\n" +
                              $"コミュ: {playerStats.GetEffectiveCommuLv()}\n" +
                              $"ギャル: {playerStats.GetEffectiveGalLv()}\n" +
                              $"レモン: {playerStats.GetEffectiveLemonLv()}";
        }
    }

    public void AddLog(string message)
    {
        if (logText != null)
        {
            logText.text += $"\n{message}";
            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        else
        {
            Debug.Log($"[PhoneLog] {message}");
        }
    }

    // --- メニュー表示 ---

    public void OnItemBtn()
    {
        RefreshItemList();
    }

    public void RefreshItemList()
    {
        // パネル表示
        if (diceArea) diceArea.SetActive(false);
        if (dialogScroll) dialogScroll.SetActive(false);
        if (itemPanel) itemPanel.SetActive(true);

        // リストクリア
        foreach (Transform child in itemListRoot) Destroy(child.gameObject);

        // 1. ItemManager経由で移動カードなどを取得・表示
        // (ItemManagerが参照できる前提。GameManager等から参照取得してください)
        var itemManager = FindObjectOfType<ItemManager>(); // 簡易的に取得
        if (itemManager != null)
        {
            // A. 移動カード
            var cardCounts = itemManager.GetCardCounts();
            foreach (var kvp in cardCounts)
            {
                int num = kvp.Key;
                int count = kvp.Value;
                if (count > 0)
                {
                    CreateItemButton($"移動カード [{num}]  x{count}", () =>
                    {
                        itemManager.menuManager.CloseDetail();
                        itemManager.menuManager.detailPanel.SetActive(true);
                        itemManager.menuManager.detailTitle.text = $"移動カード [{num}]";
                        itemManager.menuManager.detailDesc.text = "このカードを使って移動しますか？";
                        itemManager.menuManager.actionButton.gameObject.SetActive(true);
                        itemManager.menuManager.actionBtnText.text = "使う";
                        itemManager.menuManager.actionButton.onClick.RemoveAllListeners();
                        itemManager.menuManager.actionButton.onClick.AddListener(() => itemManager.UseMovementCard(num));
                    });
                }
            }

            // B. 通常アイテム (ItemManagerのInventoryリスト)
            var invCounts = itemManager.GetItemCounts();
            foreach (var kvp in invCounts)
            {
                string iName = kvp.Key;
                int count = kvp.Value;
                CreateItemButton($"{iName}  x{count}", () =>
                {
                    ShowItemDetail(itemManager, iName);
                });
            }
        }

        // C. 特殊アイテム (PlayerStatsのint変数で管理されているもの)

        // 生徒手帳 (確認のみ)
        if (playerStats.studentIdCount > 0)
        {
            CreateItemButton($"生徒手帳  x{playerStats.studentIdCount}", () =>
            {
                ShowGenericDetail("生徒手帳", "【効果】\n教室マスに止まった際、クラスの様子を確認したり、特定の行動を選択できるようになります。\n(※この画面では使用できません。教室マスで自動的に効果を発揮します)", null);
            });
        }

        // イベント強制 (使用可能)
        if (playerStats.eventForce > 0)
        {
            CreateItemButton($"イベント強制  x{playerStats.eventForce}", () =>
            {
                ShowGenericDetail("イベント強制", "【効果】\n今いるマスで強制的にイベントを発生させます。\n使用しますか？", () =>
                {
                    if (itemManager != null) itemManager.UseItemByName("イベント強制");
                });
            });
        }

        // プレゼント (使用不可・ショップ用)
        if (playerStats.present > 0)
        {
            CreateItemButton($"プレゼント  x{playerStats.present}", () =>
            {
                ShowGenericDetail("プレゼント", "【効果】\n誰かにあげると親密度が上がります。\n(※特定のイベントで使用します)", null);
            });
        }

        // 閉じるボタン
        CreateItemButton("閉じる", () =>
        {
            itemPanel.SetActive(false);
            if (diceArea) diceArea.SetActive(true);
        });
    }

    void CreateItemButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btn = Instantiate(itemButtonPrefab, itemListRoot);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btn.GetComponent<Button>().onClick.AddListener(onClick);
    }

    void ShowItemDetail(ItemManager mgr, string itemName)
    {
        // ItemManager経由での詳細表示ロジックがあればそれを呼ぶ
        // ここでは簡易的にMenuManagerのUIを借用
        if (mgr.menuManager != null)
        {
            mgr.menuManager.detailPanel.SetActive(true);
            mgr.menuManager.detailTitle.text = itemName;
            mgr.menuManager.detailDesc.text = "このアイテムを使用しますか？";
            mgr.menuManager.actionButton.gameObject.SetActive(true);
            mgr.menuManager.actionBtnText.text = "使う";
            mgr.menuManager.actionButton.onClick.RemoveAllListeners();
            mgr.menuManager.actionButton.onClick.AddListener(() => mgr.UseItemByName(itemName));
        }
    }

    // 汎用詳細表示
    void ShowGenericDetail(string title, string desc, UnityEngine.Events.UnityAction onUse)
    {
        // MenuManagerのUIを探して使う（無ければログ）
        var menuMgr = FindObjectOfType<MenuManager>();
        if (menuMgr != null)
        {
            menuMgr.detailPanel.SetActive(true);
            menuMgr.detailTitle.text = title;
            menuMgr.detailDesc.text = desc;

            if (onUse != null)
            {
                menuMgr.actionButton.gameObject.SetActive(true);
                menuMgr.actionBtnText.text = "使う";
                menuMgr.actionButton.onClick.RemoveAllListeners();
                menuMgr.actionButton.onClick.AddListener(() =>
                {
                    menuMgr.detailPanel.SetActive(false);
                    onUse.Invoke();
                });
            }
            else
            {
                menuMgr.actionButton.gameObject.SetActive(false);
            }
        }
    }

    public void ShowDialogMode(string message)
    {
        if (diceArea) diceArea.SetActive(false);
        if (dialogScroll) dialogScroll.SetActive(true);
        if (itemPanel) itemPanel.SetActive(false);

        if (dialogText) dialogText.text = message;
        foreach (Transform child in choiceRoot) Destroy(child.gameObject);
    }

    public void CreateChoiceButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        if (!choiceButtonPrefab) return;
        GameObject btn = Instantiate(choiceButtonPrefab, choiceRoot);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btn.GetComponent<Button>().onClick.AddListener(onClick);
    }
}