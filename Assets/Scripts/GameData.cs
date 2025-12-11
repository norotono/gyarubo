using UnityEngine;
using System;

[System.Serializable]
public class PlayerStats
{
    public int gp = 300;
    public int friends = 5;

    // 現在の時間
    public int currentGrade = 1; // 1~3年
    public int currentMonth = 4; // 4月開始

    // 所持アイテム（簡易管理）
    public int moveCardSimple = 0; // 通常移動カード
    public int moveCardDouble = 0; // 倍速移動カード
    public bool hasStudentId = false; // 生徒手帳

    // ステータス (Lv0-5)
    public int commuLv = 0;
    public int galLv = 0;
    public int lemonLv = 0;

    public int CalculateSalary(int shinyuCount)
    {
        return 100 + (friends * 10) + (shinyuCount * 50);
    }
}

// 商品データの定義
[System.Serializable]
public class ShopItem
{
    public string itemName;
    public int price;
    public string description;
    public Action onBuy; // 購入時の処理

    public ShopItem(string name, int p, string desc, Action action)
    {
        itemName = name;
        price = p;
        description = desc;
        onBuy = action;
    }
}