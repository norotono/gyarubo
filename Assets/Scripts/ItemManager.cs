using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerStats playerStats;
    public EventManager eventManager;
    public MenuManager menuManager; // UI更新用

    // 移動カードリスト（数値で管理）
    public List<int> movementCards = new List<int>();
    public const int MaxCards = 5;

    // 所持アイテムリスト
    public List<ItemData> inventory = new List<ItemData>();

    // アイテムを使用するメソッド
    public void UseItem(ItemData item)
    {
        if (item == null) return;

        Debug.Log($"アイテム使用: {item.itemName}");
        bool consume = true;

        // --- アイテムごとの効果分岐 ---
        if (item.itemName.Contains("強制イベント"))
        {
            // その場でイベントマスの処理を実行
            gameManager.TriggerEventTileFromItem();
        }
        else if (item.itemName == "生徒手帳")
        {
            // フラグを立てる（教室での判定有利化）
            gameManager.ActivateHandbook();
            eventManager.ShowMessage("生徒手帳を確認しました。\n次に教室に入った時、様子を伺えます！");
        }
        else if (item.itemName == "Highカード")
        {
            AddMovementCard(Random.Range(4, 7)); // 4~6
        }
        else if (item.itemName == "Lowカード")
        {
            AddMovementCard(Random.Range(1, 4)); // 1~3
        }
        else
        {
            Debug.LogWarning("未定義のアイテム効果です");
            consume = false;
        }

        // 消費処理
        if (consume)
        {
            inventory.Remove(item);
            // メニューの表示を更新（閉じない）
            menuManager.RefreshItemDisplay();
        }
    }

    // 移動カード追加（5枚制限処理含む）
    public void AddMovementCard(int value)
    {
        if (movementCards.Count < MaxCards)
        {
            movementCards.Add(value);
            Debug.Log($"移動カード({value})を入手");
            menuManager.RefreshItemDisplay(); // UI更新
        }
        else
        {
            // 5枚以上なら入れ替え選択
            // ※この処理中はメニューを閉じた方が安全ですが、今回はChoicePanelを上に被せます
            AskToSwapCard(value);
        }
    }

    void AskToSwapCard(int newValue)
    {
        string[] labels = new string[6];
        UnityEngine.Events.UnityAction[] actions = new UnityEngine.Events.UnityAction[6];

        for (int i = 0; i < 5; i++)
        {
            int index = i; // クロージャ対策
            labels[i] = $"カード[{movementCards[i]}] を捨てる";
            actions[i] = () => {
                movementCards[index] = newValue;
                menuManager.RefreshItemDisplay();
            };
        }

        labels[5] = $"新カード[{newValue}] を諦める";
        actions[5] = () => { /* 何もしない */ };

        eventManager.ShowChoicePanel(
            $"手持ちがいっぱいです！\n新しいカード({newValue})と入れ替えますか？",
            labels,
            actions
        );
    }

    public void AddItem(ItemData item)
    {
        inventory.Add(item);
    }
}