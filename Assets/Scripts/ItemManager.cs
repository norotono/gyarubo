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

    private void Start()
    {
        if (menuManager == null) menuManager = FindObjectOfType<MenuManager>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (playerStats == null) playerStats = PlayerStats.Instance;
        if (eventManager == null) eventManager = FindObjectOfType<EventManager>();
    }

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
        if (menuManager == null) menuManager = FindObjectOfType<MenuManager>();
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
            if (menuManager) menuManager.CloseDetail();
            if (gameManager != null) gameManager.StartCoroutine(gameManager.MovePlayer(cardValue));
        }
    }

    public void UseItemByName(string itemName)
    {
        bool used = false;
        var item = inventory.FirstOrDefault(x => x.itemName == itemName);
        if (item != null)
        {
            inventory.Remove(item);
            used = true;
        }

        // ※イベント強制の処理は削除しました

        if (used)
        {
            if (menuManager) menuManager.CloseDetail();
            var phoneUI = FindObjectOfType<PhoneUIManager>();
            if (phoneUI && phoneUI.itemPanel.activeSelf) phoneUI.itemPanel.SetActive(false);
            Debug.Log($"{itemName} を使用しました");
        }
    }

    // --- ★追加: 生徒手帳の管理ロジック ---

    // 生徒手帳を入手（ショップ等から呼ぶ）
    public void AddStudentHandbook()
    {
        if (playerStats != null)
        {
            playerStats.studentIdCount++;
            Debug.Log($"生徒手帳を入手しました。所持数: {playerStats.studentIdCount}");
        }
    }

    // 所持数を確認
    public int GetHandbookCount()
    {
        return (playerStats != null) ? playerStats.studentIdCount : 0;
    }

    // 生徒手帳を使用（消費成功ならtrue）
    public bool TryUseStudentHandbook()
    {
        if (playerStats != null && playerStats.studentIdCount > 0)
        {
            playerStats.studentIdCount--;
            Debug.Log($"生徒手帳を使用しました。残り: {playerStats.studentIdCount}");
            return true;
        }
        return false;
    }
}