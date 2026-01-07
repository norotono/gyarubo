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

    public List<int> movementCards = new List<int>(); // 移動カード(1-6)
    public const int MaxCards = 5;
    public List<ItemData> inventory = new List<ItemData>(); // その他のアイテム

    // --- 便利な集計メソッド ---


    // ★追加: カードを入手・購入する際の共通メソッド
    // ショップやイベントからはこのメソッドを呼ぶようにする
    public void BuyOrGetMoveCard()
    {
        // 1. 新しいカードの数値を決定
        int newCardValue = Random.Range(1, 7);

        // 2. 所持数チェック
        if (movementCards.Count < MaxCards)
        {
            // 枠が空いていればそのまま追加
            movementCards.Add(newCardValue);
            Debug.Log($"移動カード [{newCardValue}] を入手しました。");

            // メッセージ表示（EventManagerがあれば使う）
            if (eventManager != null)
                eventManager.ShowMessage($"移動カード [{newCardValue}] を手に入れました！");
        }
        else
        {
            // 3. 枠がいっぱいなら取捨選択画面へ
            StartCoroutine(ShowCardDiscardFlow(newCardValue));
        }
    }

    // ★追加: カード取捨選択フロー
    private IEnumerator ShowCardDiscardFlow(int newCard)
    {
        // MenuManagerに「カード整理モード」での表示を依頼
        // 第3引数はコールバック（ユーザーが選んだインデックスが返ってくる）
        menuManager.OpenCardDiscardMenu(movementCards, newCard, (discardIndex) => {

            // discardIndexが 0～4 なら、その位置の手持ちカードを捨てる
            // discardIndexが 5 (movementCards.Count) なら、今回出た新しいカードを捨てる

            if (discardIndex < movementCards.Count)
            {
                // 手持ちを捨てて、新しいのを入れる
                int dropped = movementCards[discardIndex];
                movementCards[discardIndex] = newCard; // 上書き（入れ替え）
                Debug.Log($"移動カード [{dropped}] を捨てて、[{newCard}] を入手しました。");
            }
            else
            {
                // 今回引いたものを捨てる（何もしない）
                Debug.Log($"移動カード [{newCard}] を諦めました。");
            }
        });

        yield return null;
    }

    // 移動カードを番号ごとに集計して返す辞書
    public Dictionary<int, int> GetCardCounts()
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();
        for (int i = 1; i <= 6; i++) counts[i] = 0; // 1~6を0で初期化

        foreach (var card in movementCards)
        {
            if (counts.ContainsKey(card)) counts[card]++;
        }
        return counts;
    }

    // 通常アイテムを名前ごとに集計して返す辞書
    public Dictionary<string, int> GetItemCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var item in inventory)
        {
            if (item == null) continue;
            if (counts.ContainsKey(item.itemName)) counts[item.itemName]++;
            else counts.Add(item.itemName, 1);
        }
        return counts;
    }

    // --- アイテム使用処理 ---

    // 移動カードの使用
    public void UseMovementCard(int cardValue)
    {
        if (movementCards.Contains(cardValue))
        {
            movementCards.Remove(cardValue); // 1枚消費
            Debug.Log($"移動カード {cardValue} を使用");

            // ダイスの代わりとして移動処理を実行（GameManagerへ依頼）
            // ※GameManager側に MovePlayer(int steps) がある前提
            menuManager.CloseDetail(); // メニューを閉じる
            gameManager.StartCoroutine(gameManager.MovePlayer(cardValue));
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
        // 生徒手帳はここでは使用できない（ボタンが押せない設計にするが念のため）
        else if (itemName == "生徒手帳")
        {
            eventManager.ShowMessage("生徒手帳は「教室マス」に止まった時に自動で使用選択肢が出ます。");
            return;
        }

        if (used)
        {
            inventory.Remove(item); // 1個消費
            menuManager.RefreshItemList(); // 表示更新
            menuManager.CloseDetail();     // パネルを閉じる
        }
    }

    // ... (AddMovementCard などの既存メソッドはそのまま) ...
}