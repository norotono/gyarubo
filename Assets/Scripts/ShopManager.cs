using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public GameObject shopPanel;
    public Transform itemGridRoot;
    public GameObject shopItemPrefab; // ShopItemスクリプトがついたボタンプレハブ
    public TextMeshProUGUI playerGPText;

    [Header("--- Discard UI (Over Limit) ---")]
    public GameObject discardPanel;
    public Transform discardGridRoot;
    public GameObject discardButtonPrefab;
    public TextMeshProUGUI discardMessageText;

    [Header("--- Shop Data ---")]
    public List<ItemData> shopItemsData; // Inspectorで販売するアイテムを登録

    private PlayerStats stats;
    private ItemData pendingPurchaseItem;

    // ★追加: GameManagerから呼ばれる初期化用メソッド
    public void SetupItems(int grade)
    {
        // 将来的に「3年生限定アイテム」などを追加する場合はここでリストを操作します。
        // 現状はInspectorで設定したリストをそのまま使うため、処理は空でOKです。
        Debug.Log($"Shop initialized for Grade {grade}");
    }

    // ショップを開く
    public IEnumerator OpenShop(bool isDiscount)
    {
        stats = PlayerStats.Instance;
        if (shopPanel) shopPanel.SetActive(true);
        if (discardPanel) discardPanel.SetActive(false);

        UpdateGPDisplay();
        GenerateShopItems(isDiscount);

        // 閉じるボタン等でパネルが非表示になるまで待機
        while (shopPanel != null && shopPanel.activeSelf)
        {
            yield return null;
        }
    }

    void UpdateGPDisplay()
    {
        if (stats && playerGPText)
            playerGPText.text = $"所持金: {stats.gp:N0} GP";
    }

    // 商品一覧の生成
    void GenerateShopItems(bool isDiscount)
    {
        // 既存のボタンをクリア
        foreach (Transform child in itemGridRoot) Destroy(child.gameObject);

        foreach (var data in shopItemsData)
        {
            GameObject obj = Instantiate(shopItemPrefab, itemGridRoot);
            ShopItem itemScript = obj.GetComponent<ShopItem>();

            // --- 価格計算ロジック ---
            int finalPrice = data.basePrice;

            // 移動カードの場合の価格変動 (Low:等倍, High:倍額)
            if (data.itemType == ItemType.MoveCard)
            {
                // 4マス以上進むカードは倍額
                if (data.moveSteps >= 4)
                {
                    finalPrice *= 2;
                }
            }

            // 購買部の割引イベント (20% OFF)
            if (isDiscount)
            {
                finalPrice = (int)(finalPrice * 0.8f);
            }

            // ボタン設定 (ShopItem.Setupを呼び出す)
            if (itemScript != null)
            {
                itemScript.Setup(data, finalPrice, () => OnItemClicked(data, finalPrice));
            }
        }
    }

    // 商品ボタンが押された時の処理
    void OnItemClicked(ItemData item, int price)
    {
        if (stats.gp < price)
        {
            Debug.Log("お金が足りません");
            return;
        }

        // --- 移動カードの場合の特別処理 ---
        if (item.itemType == ItemType.MoveCard)
        {
            if (stats.IsMoveCardFull())
            {
                // 上限オーバーなので、入れ替え処理へ
                StartCoroutine(OpenDiscardMenu(item, price));
                return;
            }
            else
            {
                // 枠が空いていれば即購入
                stats.gp -= price;
                stats.AddMoveCard(item.moveSteps);
                Debug.Log($"移動カード({item.moveSteps})を購入しました");
            }
        }
        else
        {
            // その他のアイテム
            stats.gp -= price;
            stats.otherItems.Add(item);
            Debug.Log($"{item.itemName}を購入しました");
        }

        UpdateGPDisplay();
    }

    // 捨てるカードを選択するUIを表示
    IEnumerator OpenDiscardMenu(ItemData newItem, int price)
    {
        pendingPurchaseItem = newItem;
        if (discardPanel) discardPanel.SetActive(true);
        if (discardMessageText)
            discardMessageText.text = $"カードがいっぱいです。\n「移動{newItem.moveSteps}」を買うために\n捨てるカードを選んでください。";

        // 所持カード一覧を表示
        foreach (Transform child in discardGridRoot) Destroy(child.gameObject);

        // 「購入をやめる」ボタン
        GameObject cancelBtn = Instantiate(discardButtonPrefab, discardGridRoot);
        cancelBtn.GetComponentInChildren<TextMeshProUGUI>().text = "購入をやめる";
        cancelBtn.GetComponent<Button>().onClick.AddListener(() => {
            if (discardPanel) discardPanel.SetActive(false);
            pendingPurchaseItem = null;
        });

        // 所持カードボタン生成
        for (int i = 0; i < stats.moveCards.Count; i++)
        {
            int cardValue = stats.moveCards[i];
            int index = i; // キャプチャ用

            GameObject btnObj = Instantiate(discardButtonPrefab, discardGridRoot);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = $"移動 {cardValue}";

            // ボタンを押した時の処理（捨てて、新しいのを買って、閉じる）
            btnObj.GetComponent<Button>().onClick.AddListener(() => {
                // 1. お金を払う
                stats.gp -= price;

                // 2. 選んだカードを捨てる
                stats.RemoveMoveCardAt(index);

                // 3. 新しいカードを加える
                stats.AddMoveCard(newItem.moveSteps);

                // 4. 閉じる
                if (discardPanel) discardPanel.SetActive(false);
                UpdateGPDisplay();
            });
        }

        yield return null;
    }
}