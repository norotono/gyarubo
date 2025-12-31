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

    [Header("Time")]
    public int currentGrade = 1;
    public int currentMonth = 4;

    [Header("Counters")]
    public int maleContactCount = 0;
    public int gpPlusTileCount = 0;
    public int gpMinusTileCount = 0;
    public int diceOneCount = 0;
    public int shopSpendTotal = 0;
    public int totalSteps = 0;
    public int soloPlayConsecutive = 0;

    public int gradAlbumBuyCount = 0;
    public int turnCount = 0;

    [Header("Inventory")]
    public List<ItemData> moveCards = new List<ItemData>();
    public List<ItemData> otherItems = new List<ItemData>();

    public const int MaxMoveCards = 5;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int CalculateSalary(int shinyuCount)
    {
        return 3000 + (shinyuCount * 1000) + (galLv * 500);
    }

    // ★追加: 5枚制限チェック用メソッド
    public bool IsMoveCardFull()
    {
        return moveCards.Count >= MaxMoveCards;
    }

    public void RemoveItem(ItemData item)
    {
        if (moveCards.Contains(item)) moveCards.Remove(item);
        else if (otherItems.Contains(item)) otherItems.Remove(item);
    }
}