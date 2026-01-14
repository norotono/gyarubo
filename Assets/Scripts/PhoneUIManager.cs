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
    public void OnItemBtn()
    {
        if (diceArea) diceArea.SetActive(false);
        if (dialogScroll) dialogScroll.SetActive(false);
        if (itemPanel) itemPanel.SetActive(true);

        // 中身はMenuManagerに作らせる
        var menuMgr = FindObjectOfType<MenuManager>();
        if (menuMgr != null)
        {
            menuMgr.RefreshItemList();
        }
    }

    public void RefreshItemList()
    {
        if (diceArea) diceArea.SetActive(false);
        if (dialogScroll) dialogScroll.SetActive(false);
        if (itemPanel) itemPanel.SetActive(true);

        foreach (Transform child in itemListRoot) Destroy(child.gameObject);

        var itemManager = FindObjectOfType<ItemManager>();

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
                        var menuMgr = itemManager.menuManager ? itemManager.menuManager : FindObjectOfType<MenuManager>();
                        if (menuMgr)
                        {
                            menuMgr.CloseDetail();
                            menuMgr.detailPanel.SetActive(true);
                            menuMgr.detailTitle.text = $"移動カード [{num}]";
                            menuMgr.detailDesc.text = "このカードを使って移動しますか？";
                            menuMgr.actionButton.gameObject.SetActive(true);
                            menuMgr.actionBtnText.text = "使う";
                            menuMgr.actionButton.onClick.RemoveAllListeners();
                            menuMgr.actionButton.onClick.AddListener(() => itemManager.UseMovementCard(num));
                        }
                    });
                }
            }

            // B. 通常アイテム
            var invCounts = itemManager.GetItemCounts();
            foreach (var kvp in invCounts)
            {
                string iName = kvp.Key;
                int count = kvp.Value;
                CreateItemButton($"{iName}  x{count}", () => ShowItemDetail(itemManager, iName));
            }
        }

        // C. 特殊アイテム

        // ★修正: 生徒手帳 (0個でも表示して、持っていないことを確認可能にする)
        string handbookText = (playerStats.studentIdCount > 0)
            ? $"生徒手帳 (所持: {playerStats.studentIdCount})"
            : "生徒手帳 (未所持)";

        CreateItemButton(handbookText, () =>
        {
            string desc = (playerStats.studentIdCount > 0)
                ? "【効果】\n教室マスで親友を探せるようになります。\n(※自動的に効果が発揮されます)"
                : "【未所持】\nこれがないと教室の中を詳しく調べられません。\n購買部で購入しましょう。";
            ShowGenericDetail("生徒手帳", desc, null);
        });

        // プレゼント (確認のみ)
        if (playerStats.present > 0)
        {
            CreateItemButton($"プレゼント  x{playerStats.present}", () =>
            {
                ShowGenericDetail("プレゼント", "【効果】\n特定のイベントで男友達に渡すと親密度が上がります。", null);
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
        if (itemButtonPrefab == null) return;
        GameObject btn = Instantiate(itemButtonPrefab, itemListRoot);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btn.GetComponent<Button>().onClick.AddListener(onClick);
    }

    void ShowItemDetail(ItemManager mgr, string itemName)
    {
        var menuMgr = FindObjectOfType<MenuManager>();
        if (menuMgr != null)
        {
            menuMgr.detailPanel.SetActive(true);
            menuMgr.detailTitle.text = itemName;
            menuMgr.detailDesc.text = "このアイテムを使用しますか？";
            menuMgr.actionButton.gameObject.SetActive(true);
            menuMgr.actionBtnText.text = "使う";
            menuMgr.actionButton.onClick.RemoveAllListeners();
            menuMgr.actionButton.onClick.AddListener(() => mgr.UseItemByName(itemName));
        }
    }

    void ShowGenericDetail(string title, string desc, UnityEngine.Events.UnityAction onUse)
    {
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

    [Header("Dice Display")]
    public Image diceDisplayImage; // ★Inspectorで dice_area 内のImageをアタッチ

    // --- ★追加: ダイス画像更新 ---
    public void UpdateDiceImage(Sprite sprite)
    {
        if (diceDisplayImage != null)
        {
            diceDisplayImage.gameObject.SetActive(true);
            diceDisplayImage.sprite = sprite;
        }
    }

    // --- ★修正: 閉じるボタンの挙動 ---
    public void ShowDiceMode()
    {
        // 1. エリア表示
        if (diceArea) diceArea.SetActive(true);
        if (dialogScroll) dialogScroll.SetActive(true); // ログは表示
        if (menuArea) menuArea.SetActive(true);

        // 2. ウィンドウを閉じる
        if (itemPanel) itemPanel.SetActive(false);

        // 3. アイテムボタン等をクリア (LogAreaのボタンを消す)
        // ※ itemListRoot はアイテム一覧用なので消してOK
        foreach (Transform child in itemListRoot) Destroy(child.gameObject);

        // 4. MenuManagerの詳細パネルも閉じる
        var menuMgr = FindObjectOfType<MenuManager>();
        if (menuMgr != null)
        {
            menuMgr.CloseDetail();
            if (menuMgr.fullScreenPanel) menuMgr.fullScreenPanel.SetActive(false);
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