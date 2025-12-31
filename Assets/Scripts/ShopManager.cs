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
    public GameObject shopItemPrefab;
    public TextMeshProUGUI playerGPText;

    [Header("--- Discard UI ---")]
    public GameObject discardPanel;
    public Transform discardGridRoot;
    public GameObject discardButtonPrefab;
    public TextMeshProUGUI discardMessageText;

    [Header("--- Shop Data ---")]
    public List<ItemData> shopItemsData; // ショップに並べる商品リスト

    [Header("--- Database (New) ---")]
    public List<ItemData> fixedMoveCards; // 実体化する移動カード(1)～(6)をここに登録

    private PlayerStats stats;
    private ItemData pendingPurchaseItem;

    // 初期化メソッド
    public void SetupItems(int grade)
    {
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

        while (shopPanel != null && shopPanel.activeSelf)
        {
            yield return null;
        }
    }

    void UpdateGPDisplay()
    {
        if (stats && playerGPText) playerGPText.text = $"所持金: {stats.gp:N0} GP";
    }

    // 商品一覧生成
    void GenerateShopItems(bool isDiscount)
    {
        foreach (Transform child in itemGridRoot) Destroy(child.gameObject);

        foreach (var data in shopItemsData)
        {
            if (data.grade3Only && stats.currentGrade < 3) continue;

            GameObject obj = Instantiate(shopItemPrefab, itemGridRoot);
            ShopItem itemScript = obj.GetComponent<ShopItem>();

            int finalPrice = data.basePrice;

            // 割引適用
            if (isDiscount) finalPrice = (int)(finalPrice * 0.8f);

            // 倍額カード(HighLow)の表示価格
            if (data.itemType == ItemType.MoveCard_HighLow) finalPrice = 400; // 固定400GP

            // 変動価格(卒業アルバム)
            if (data.itemType == ItemType.DynamicPrice)
            {
                finalPrice += (stats.gradAlbumBuyCount * data.priceIncrement);
            }

            if (itemScript != null)
            {
                itemScript.Setup(data, finalPrice, () => OnItemClicked(data, finalPrice));
            }
        }
    }

    // 購入ボタンクリック時 (ここが重要修正箇所)
    void OnItemClicked(ItemData shopItem, int price)
    {
        if (stats.gp < price)
        {
            Debug.Log("お金が足りません");
            return;
        }

        // 購入しようとしているアイテムの実体を決定
        ItemData actualItem = shopItem;

        // ランダムカードなら、ここで中身(1~6)を抽選して入れ替える
        if (shopItem.itemType == ItemType.MoveCard_RandomShop)
        {
            if (fixedMoveCards == null || fixedMoveCards.Count < 6)
            {
                Debug.LogError("ShopManagerに Fixed Move Cards が登録されていません！Inspectorを確認してください。");
                return;
            }
            int roll = Random.Range(0, 6); // 0~5
            actualItem = fixedMoveCards[roll];
            Debug.Log($"ランダム抽選結果: {actualItem.itemName}");
        }

        // --- 所持チェック ---
        bool isMoveCard = (actualItem.itemType == ItemType.MoveCard_Fixed ||
                           actualItem.itemType == ItemType.MoveCard_HighLow);

        if (isMoveCard)
        {
            // 5枚制限チェック
            if (stats.moveCards.Count >= PlayerStats.MaxMoveCards)
            {
                // 入れ替え画面を出す
                StartCoroutine(OpenDiscardMenu(actualItem, price));
                return;
            }
            stats.moveCards.Add(actualItem);
        }
        else
        {
            stats.otherItems.Add(actualItem);
        }

        // 購入完了処理
        stats.gp -= price;
        stats.shopSpendTotal += price;
        UpdateGPDisplay();

        // ログ出し
        if (shopItem.itemType == ItemType.MoveCard_RandomShop)
        {
            Debug.Log($"くじ結果: {actualItem.itemName} を入手しました！");
        }

        // 卒業アルバム等の価格変動があれば再描画
        if (actualItem.itemType == ItemType.DynamicPrice)
        {
            GenerateShopItems(false);
        }
    }

    // 捨てるカード選択メニュー
    IEnumerator OpenDiscardMenu(ItemData newItem, int price)
    {
        pendingPurchaseItem = newItem;
        if (discardPanel) discardPanel.SetActive(true);
        if (discardMessageText)
            discardMessageText.text = $"カードがいっぱいです。\n「{newItem.itemName}」を入手するために\n捨てるカードを選んでください。";

        foreach (Transform child in discardGridRoot) Destroy(child.gameObject);

        // 購入をやめるボタン
        GameObject cancelBtn = Instantiate(discardButtonPrefab, discardGridRoot);
        cancelBtn.GetComponentInChildren<TextMeshProUGUI>().text = "購入をやめる";
        cancelBtn.GetComponent<Button>().onClick.AddListener(() => {
            if (discardPanel) discardPanel.SetActive(false);
            pendingPurchaseItem = null;
        });

        // 所持カード一覧ボタン
        for (int i = 0; i < stats.moveCards.Count; i++)
        {
            ItemData card = stats.moveCards[i];
            int index = i;

            GameObject btnObj = Instantiate(discardButtonPrefab, discardGridRoot);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = card.itemName;

            btnObj.GetComponent<Button>().onClick.AddListener(() => {
                // 交換処理
                stats.gp -= price;
                stats.shopSpendTotal += price;
                stats.moveCards.RemoveAt(index); // 捨てる
                stats.moveCards.Add(newItem);    // 加える

                if (discardPanel) discardPanel.SetActive(false);
                UpdateGPDisplay();
            });
        }
        yield return null;
    }
}