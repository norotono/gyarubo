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
    public List<ItemData> inventory = new List<ItemData>();

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

    public Dictionary<int, int> GetCardCounts()
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();
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

    public void BuyOrGetMoveCard()
    {
        int newCardValue = Random.Range(1, 7);
        menuManager.ShowCardConfirmation(newCardValue, () => CheckCardLimit(newCardValue));
    }

    private void CheckCardLimit(int newCard)
    {
        if (playerStats.moveCards.Count < MaxCards)
        {
            playerStats.moveCards.Add(newCard);
            Debug.Log($"移動カード[{newCard}]を入手しました。");
        }
        else
        {
            ShowDiscardDialog(newCard);
        }
    }

    void ShowDiscardDialog(int newCard)
    {
        menuManager.OpenCardDiscardMenu(playerStats.moveCards, newCard, (selectedIndex) =>
        {
            if (selectedIndex < playerStats.moveCards.Count)
            {
                int dropped = playerStats.moveCards[selectedIndex];
                playerStats.moveCards[selectedIndex] = newCard;
                Debug.Log($"カード[{dropped}]を捨てて、[{newCard}]を入手しました。");
            }
            else
            {
                Debug.Log($"新規カード[{newCard}]を諦めました。");
            }
        });
    }

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

    // ★修正: アイテム使用処理（イベント強制に対応）
    public void UseItemByName(string itemName)
    {
        bool used = false;

        // 1. インベントリ内のアイテム
        var item = inventory.FirstOrDefault(x => x.itemName == itemName);
        if (item != null)
        {
            // ここに通常アイテムの効果分岐を書く
            // ...
            inventory.Remove(item);
            used = true;
        }

        // 2. 特殊パラメータアイテム (イベント強制など)
        if (!used)
        {
            if (itemName == "イベント強制" && playerStats.eventForce > 0)
            {
                playerStats.eventForce--;
                gameManager.TriggerEventTileFromItem();
                used = true;
            }
        }

        if (used)
        {
            Debug.Log($"{itemName} を使用しました");
            if (menuManager) menuManager.CloseDetail();
            // PhoneUIも閉じる場合はここに記述
            var phoneUI = FindObjectOfType<PhoneUIManager>();
            if (phoneUI) phoneUI.itemPanel.SetActive(false);
        }
    }
}