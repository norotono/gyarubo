using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Resources")]
    public int gp = 3000;
    public int friends = 5;

    [Header("Status")]
    public int commuLv = 1;
    public int galLv = 1;
    public int lemonLv = 1;
    public int currentGrade = 1;
    public int currentMonth = 4;

    [Header("Counters")]
    public int maleFriendCount = 0;
    public int boyfriendCount = 0;
    public int soloPlayConsecutive = 0;

    [Header("Inventory")]
    // 移動カードは数字(int)で管理。最大5枚
    public List<int> moveCards = new List<int>();
    public const int MaxMoveCards = 5;

    // その他のアイテム（生徒手帳など）
    public List<ItemData> otherItems = new List<ItemData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int CalculateSalary(int shinyuCount)
    {
        return 3000 + (shinyuCount * 1000) + (galLv * 500);
    }

    // --- インベントリ操作 ---

    // 移動カードが満タンか確認
    public bool IsMoveCardFull()
    {
        return moveCards.Count >= MaxMoveCards;
    }

    // 移動カード追加
    public void AddMoveCard(int steps)
    {
        if (moveCards.Count < MaxMoveCards)
        {
            moveCards.Add(steps);
            moveCards.Sort(); // 整理整頓
        }
    }

    // 移動カード削除（インデックス指定）
    public void RemoveMoveCardAt(int index)
    {
        if (index >= 0 && index < moveCards.Count)
        {
            moveCards.RemoveAt(index);
        }
    }

    // 汎用アイテム削除メソッド（GameManager等から呼ばれる）
    public void RemoveItem(ItemData item)
    {
        if (item.itemType == ItemType.MoveCard)
        {
            // 移動カードの場合は数字が一致する最初のものを削除
            if (moveCards.Contains(item.moveSteps))
            {
                moveCards.Remove(item.moveSteps);
            }
        }
        else
        {
            // 通常アイテムの場合
            if (otherItems.Contains(item))
            {
                otherItems.Remove(item);
            }
        }
    }
}