using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerStats : MonoBehaviour
{
    // シングルトン参照
    public static PlayerStats Instance;

    void Awake()
    {
        // シングルトンの設定
        if (Instance == null) Instance = this;
    }

    // --- 基本資産 ---
    public int gp = 300;
    public int friends = 5;

    // --- 時間管理 ---
    [Range(1, 3)] public int currentGrade = 1;
    public int currentTurn = 1;
    public int currentMonth = 4; // 4月スタート

    // --- ステータス (Lv0-5) ---
    public int commuLv = 0;
    public int galLv = 0;
    public int lemonLv = 0;

    // --- 彼氏・男友達 ---
    public int boyfriendCount = 0;
    public int maleFriendCount = 0;
    public int boyfriendAbilityType = 0;

    // --- 所持アイテム ---
    public List<int> moveCards = new List<int>();
    public int studentIdCount = 0;
    public int present = 0;
    public int eventForce = 0; // イベント強制アイテム

    // --- シングルプレイ用: 卒業アルバム価格 ---
    public int albumPrice = 1000;

    // --- 各種カウンタ（親友出現条件用） ---
    public int maleContactCount = 0;
    public int gpIncreaseTileCount = 0;
    public int gpDecreaseTileCount = 0;
    public int diceOneCount = 0;
    public int shopSpentTotal = 0;
    public int soloPlayConsecutive = 0;
    public int totalSteps = 0;

    // --- 給料計算メソッド ---
    public int CalculateSalary(int shinyuCount)
    {
        // 基本給100 + 友達x10 + 親友x50
        return 100 + (friends * 10) + (shinyuCount * 50);
    }

    // --- ステータス判定メソッド ---
    public bool IsAllStatsOver(int value)
    {
        // 彼氏が3人以上なら全ステータス+1扱い（ハーレムボーナス）
        int bonus = (boyfriendCount >= 3) ? 1 : 0;
        return (commuLv + bonus) >= value &&
               (galLv + bonus) >= value &&
               (lemonLv + bonus) >= value;
    }
}