using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("--- Shop UI References ---")]
    public GameObject shopPanel;
    public Transform shopContent;
    public GameObject shopItemPrefab;
    public TextMeshProUGUI shopInfoText;
    public GameObject shopCloseButton;
    public ItemManager itemManager;

    // 内部ステート
    private List<ShopItem> shopItems = new List<ShopItem>();
    public bool IsShopOpen { get; private set; } = false;

    // ボタン制御用クラス
    private class ActiveShopButton
    {
        public Button button;
        public int price;
    }
    private List<ActiveShopButton> currentShopButtons = new List<ActiveShopButton>();

    // 外部（GameManager）からアイテム定義を初期化
    // ★修正: アイテム定義の初期化メソッド
    public void InitializeShopItems(int grade, PlayerStats stats)
    {
        shopItems.Clear();

        // --- 1. 生徒手帳 (ItemManager経由で追加) ---
        shopItems.Add(new ShopItem("生徒手帳", 200, "教室用", () => {
            if (itemManager) itemManager.AddStudentHandbook();
            else stats.studentIdCount++; // 安全策
        }));
        // --- 2. 移動カード（変更点） ---
        // 直接追加せず、ItemManagerのメソッドを呼んで上限チェックを行う
        shopItems.Add(new ShopItem("移動カード", 150, "ランダム", () => {
            if (itemManager != null)
            {
                itemManager.BuyOrGetMoveCard();
            }
            else
            {
                Debug.LogError("ShopManagerにItemManagerが設定されていません！");
            }
        }));

        // --- 3. プレゼント ---
        shopItems.Add(new ShopItem("プレゼント", 500, "親密度UP", () => {
            var bfMgr = FindObjectOfType<BoyfriendManager>();
            if (bfMgr)
            {
                string log = bfMgr.IncreaseAffection(30f);
                Debug.Log(log);
            }
        }));

        // --- 5. ステータスUP（変更点：3種類に分割） ---
        shopItems.Add(new ShopItem("会話術の本", 1500, "コミュLv+1", () => {
            stats.commuLv++;
            Debug.Log("コミュ力が上がった！");
        }));
        shopItems.Add(new ShopItem("流行コスメ", 1500, "ギャルLv+1", () => {
            stats.galLv++;
            Debug.Log("ギャル力が上がった！");
        }));
        shopItems.Add(new ShopItem("恋愛小説", 1500, "レモンLv+1", () => {
            stats.lemonLv++;
            Debug.Log("レモン力が上がった！");
        }));

        // --- 6. 3年生限定アイテム ---
        if (grade == 3)
        {
            shopItems.Add(new ShopItem("卒業写真", 100, "友+1", () => stats.friends++));
            shopItems.Add(new ShopItem("卒業アルバム", 1000, "友+10", () => { stats.friends += 10; stats.albumPrice += 500; }));
        }
    }

    // ショップを開く（コルーチンで待機できるようにする）
    public IEnumerator OpenShopSequence(PlayerStats stats, bool isDiscount)
    {
        IsShopOpen = true;
        if (shopPanel) shopPanel.SetActive(true);

        UpdateInfoText(stats.gp, isDiscount);
        GenerateButtons(stats, isDiscount);

        // 閉じるボタンの設定
        if (shopCloseButton)
        {
            shopCloseButton.GetComponent<Button>().onClick.RemoveAllListeners();
            shopCloseButton.GetComponent<Button>().onClick.AddListener(() => IsShopOpen = false);
        }

        // 閉じるまで待機
        while (IsShopOpen)
        {
            yield return null;
        }

        if (shopPanel) shopPanel.SetActive(false);
    }

    void UpdateInfoText(int currentGp, bool isDiscount)
    {
        if (shopInfoText != null)
        {
            string msg = isDiscount ? "<color=red>全品 20% OFF!</color>" : "いらっしゃいませ";
            shopInfoText.text = $"{msg}\n所持金: {currentGp:N0} GP";
        }
    }

    void GenerateButtons(PlayerStats stats, bool isDiscount)
    {
        foreach (Transform child in shopContent) Destroy(child.gameObject);
        currentShopButtons.Clear();

        foreach (var item in shopItems)
        {
            GameObject btnObj = Instantiate(shopItemPrefab, shopContent);

            // 価格計算（この場限りの計算）
            int finalPrice = isDiscount ? (int)(item.price * 0.8f) : item.price;

            // 表示更新
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt)
            {
                txt.text = $"{item.itemName}\n<size=80%>{finalPrice} GP</size>";
            }

            Button btn = btnObj.GetComponent<Button>();

            // リスト登録
            currentShopButtons.Add(new ActiveShopButton { button = btn, price = finalPrice });

            // クリックイベント
            btn.onClick.AddListener(() => {
                if (stats.gp >= finalPrice)
                {
                    stats.gp -= finalPrice;

                    // ★追加: 購買での消費総額を加算 (親友条件: Wasteful用)
                    stats.shopSpentTotal += finalPrice;

                    item.onBuy.Invoke();
                    Debug.Log($"購入: {item.itemName}");

                    // UI更新
                    UpdateInfoText(stats.gp, isDiscount);
                    RefreshButtons(stats.gp);
                }
            });
        }
        // 初期状態のボタン有効無効チェック
        RefreshButtons(stats.gp);
    }

    // 所持金に応じてボタンの有効/無効を更新
    public void RefreshButtons(int currentGp)
    {
        foreach (var btnData in currentShopButtons)
        {
            if (btnData.button != null)
            {
                btnData.button.interactable = (currentGp >= btnData.price);
            }
        }
    }
}