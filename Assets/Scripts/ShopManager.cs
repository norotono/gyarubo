using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ★ここに書いてあった ShopItem クラスの定義は削除しました（ShopItem.csを使うため）

public class ShopManager : MonoBehaviour
{
    [Header("--- Shop UI ---")]
    public GameObject shopPanel;
    public Transform shopContent;
    public GameObject shopItemPrefab;
    public TextMeshProUGUI shopInfoText;
    public GameObject shopCloseButton;

    private List<ShopItem> shopItems = new List<ShopItem>();
    public bool IsShopOpen { get; private set; } = false;

    public void SetupItems(int grade)
    {
        shopItems.Clear();
        PlayerStats s = PlayerStats.Instance;

        // アイテム定義
        shopItems.Add(new ShopItem("生徒手帳", 200, "教室用", () => s.studentIdCount++));
        shopItems.Add(new ShopItem("移動カード", 150, "ランダム", () => s.moveCards.Add(Random.Range(1, 7))));
        shopItems.Add(new ShopItem("ステUP", 1500, "Lv+1", () => s.commuLv++));

        if (grade == 3)
        {
            shopItems.Add(new ShopItem("卒業アルバム", 1000, "友+10", () => { s.friends += 10; s.albumPrice += 500; }));
        }
    }

    public IEnumerator OpenShop(bool isDiscount)
    {
        IsShopOpen = true;
        if (shopPanel) shopPanel.SetActive(true);
        PlayerStats stats = PlayerStats.Instance;

        UpdateUI(stats.gp, isDiscount);
        GenerateButtons(stats, isDiscount);

        if (shopCloseButton)
        {
            shopCloseButton.GetComponent<Button>().onClick.RemoveAllListeners();
            shopCloseButton.GetComponent<Button>().onClick.AddListener(() => IsShopOpen = false);
        }

        while (IsShopOpen) yield return null;

        if (shopPanel) shopPanel.SetActive(false);
    }

    void UpdateUI(int gp, bool discount)
    {
        if (shopInfoText)
        {
            string msg = discount ? "<color=red>20% OFF!</color>" : "いらっしゃいませ";
            shopInfoText.text = $"{msg}\n所持金: {gp:N0} GP";
        }
    }

    void GenerateButtons(PlayerStats stats, bool discount)
    {
        foreach (Transform child in shopContent) Destroy(child.gameObject);

        foreach (var item in shopItems)
        {
            GameObject obj = Instantiate(shopItemPrefab, shopContent);
            int price = discount ? (int)(item.price * 0.8f) : item.price;

            var txt = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = $"{item.itemName}\n{price} GP";

            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (stats.gp >= price)
                {
                    stats.gp -= price;
                    item.onBuy.Invoke();
                    UpdateUI(stats.gp, discount);
                    GenerateButtons(stats, discount); // ボタン状態更新のため再描画
                }
            });
            // 所持金不足なら押せないようにする
            btn.interactable = (stats.gp >= price);
        }
    }
}