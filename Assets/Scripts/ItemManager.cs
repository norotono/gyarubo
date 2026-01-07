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
    // playerStats.moveCards を参照するため、ローカルのリスト変数は削除しても良いが
    // 既存コードとの兼ね合いで残す場合は同期が必要。
    // 今回は playerStats.moveCards を正として扱います。

    public List<ItemData> inventory = new List<ItemData>();

    // --- アイテム所持数カウントなど (既存のまま) ---
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

    // ★追加: カード購入・入手フロー
    public void BuyOrGetMoveCard()
    {
        // 1. 数字抽選
        int newCardValue = Random.Range(1, 7);

        // 2. MenuManager経由で「入手した数字」を全画面表示
        // OKを押したら所持数チェックへ進む
        menuManager.ShowCardConfirmation(newCardValue, () =>
        {
            CheckCardLimit(newCardValue);
        });
    }

    // ★追加: 所持数チェック
    private void CheckCardLimit(int newCard)
    {
        var currentCards = playerStats.moveCards;

        if (currentCards.Count < MaxCards)
        {
            // 5枚未満ならそのまま入手
            currentCards.Add(newCard);
            Debug.Log($"移動カード[{newCard}]を入手しました。");
        }
        else
        {
            // 5枚以上なら選択画面へ
            ShowDiscardDialog(newCard);
        }
    }

    // ★追加: 捨てる選択画面の呼び出し
    void ShowDiscardDialog(int newCard)
    {
        menuManager.OpenCardDiscardMenu(playerStats.moveCards, newCard, (selectedIndex) =>
        {
            // selectedIndex: 0~4=手持ち, 5=新規
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

    // 移動カードの使用 (既存コード修正: PlayerStatsを参照)
    public void UseMovementCard(int cardValue)
    {
        if (playerStats.moveCards.Contains(cardValue))
        {
            playerStats.moveCards.Remove(cardValue);
            Debug.Log($"移動カード {cardValue} を使用");
            menuManager.CloseDetail();
            gameManager.StartCoroutine(gameManager.MovePlayer(cardValue));
        }
    }

    // アイテムの使用 (既存のまま)
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
            eventManager.ShowMessage("生徒手帳は「教室マス」に止まった時に自動で使用選択肢が出ます。");
        }

        if (used)
        {
            inventory.Remove(item);
            Debug.Log($"{itemName} を使用しました");
            menuManager.CloseDetail();
        }
    }
}