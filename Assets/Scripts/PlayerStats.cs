using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Resources")]
    public int gp = 300;
    public int friends = 5;

    [Header("Status")]
    public int commuLv = 1;
    public int galLv = 1;
    public int lemonLv = 1;

    [Header("Counters (For Friend Conditions)")]
    public int maleContactCount = 0;    // 男子接触回数
    public int gpPlusTileCount = 0;     // 幸福
    public int gpMinusTileCount = 0;    // 不幸
    public int diceOneCount = 0;        // ダイス1の回数
    public int shopSpendTotal = 0;      // 浪費
    public int totalSteps = 0;          // 歩数
    public int soloPlayConsecutive = 0; // 孤独（今回は連続でなく累計なら要調整）

    // 卒業アルバム購入回数
    public int gradAlbumBuyCount = 0;

    // ターン経過数 (リカの能力用)
    public int turnCount = 0;

    [Header("Inventory")]
    public List<ItemData> moveCards = new List<ItemData>(); // 5枚制限
    public List<ItemData> otherItems = new List<ItemData>(); // 無制限

    public const int MaxMoveCards = 5;

    private void Awake() { if (Instance == null) Instance = this; }

    // 給料計算等はそのまま...

    // アイテム削除用
    public void RemoveItem(ItemData item)
    {
        if (moveCards.Contains(item)) moveCards.Remove(item);
        else if (otherItems.Contains(item)) otherItems.Remove(item);
    }
}