using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerStats playerStats;
    public EventManager eventManager;
    public MenuManager menuManager;

    public const int MaxCards = 5;

    // アイテムデータリスト（インスペクターで設定）
    public List<ItemData> inventory = new List<ItemData>();

    // --- アイテム所持数カウント ---
    public Dictionary<string, int> GetItemCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var item in inventory)
        {
            if (counts.ContainsKey(item.itemName)) counts[item.itemName]++;
            else counts.Add(item.itemName, 1);
        }
        return counts;
    }

    // ★追加: カード枚数カウント（MenuManagerのエラー修正用）
    public Dictionary<int, int> GetCardCounts()
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();
        // 1~6のキーを初期化
        for (int i = 1; i <= 6; i++) counts[i] = 0;

        if (playerStats != null)
        {
            foreach (var card in playerStats.moveCards)
            {
                if (counts.ContainsKey(card)) counts[card]++;
            }
        }
        return counts;
    }

    // カード購入・入手フロー
    public void BuyOrGetMoveCard()
    {
        int newCardValue = Random.Range(1, 7);

        // MenuManager経由で「入手した数字」を全画面表示
        menuManager.ShowCardConfirmation(newCardValue, () =>
        {
            CheckCardLimit(newCardValue);
        });
    }

    // 所持数チェック
    private void CheckCardLimit(int newCard)
    {
        List<int> cards = playerStats.moveCards;

        if (cards.Count < MaxCards)
        {
            cards.Add(newCard);
            Debug.Log($"移動カード[{newCard}]を入手しました。");
        }
        else
        {
            ShowDiscardDialog(newCard);
        }
    }

    // 捨てる選択画面の呼び出し
    void ShowDiscardDialog(int newCard)
    {
        menuManager.OpenCardDiscardMenu(playerStats.moveCards, newCard, (selectedIndex) =>
        {
            if (selectedIndex < playerStats.moveCards.Count)
            {
                int dropped = playerStats.moveCards[selectedIndex];
                playerStats.moveCards[selectedIndex] = newCard; // 入れ替え
                Debug.Log($"カード[{dropped}]を捨てて、[{newCard}]を入手しました。");
            }
            else
            {
                Debug.Log($"新規カード[{newCard}]を諦めました。");
            }
        });
    }

    // 移動カードの使用
    public void UseMovementCard(int cardValue)
    {
        if (playerStats.moveCards.Contains(cardValue))
        {
            playerStats.moveCards.Remove(cardValue);
            Debug.Log($"移動カード {cardValue} を使用");
            menuManager.CloseDetail();
            if (gameManager != null) gameManager.StartCoroutine(gameManager.MovePlayer(cardValue));
        }
    }

    // アイテムの使用
    public void UseItemByName(string itemName)
    {
        var item = inventory.FirstOrDefault(x => x.itemName == itemName);
        if (item == null) return;

        bool used = false;

        if (itemName.Contains("強制イベント"))
        {
            gameManager.TriggerEventTileFromItem();
            used = true;
        }
        else if (itemName == "生徒手帳")
        {
            if (eventManager) eventManager.ShowMessage("生徒手帳は「教室マス」に止まった時に自動で使用選択肢が出ます。");
        }

        if (used)
        {
            inventory.Remove(item);
            Debug.Log($"{itemName} を使用しました");
            menuManager.CloseDetail();
        }
    }
}