using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // --- 基本資産 ---
    public int gp = 300;
    public int friends = 5;

    // --- 時間管理 ---
    [Range(1, 3)] public int currentGrade = 1;
    public int currentTurn = 1;
    public int currentMonth = 4;

    // --- ステータス ---
    public int commuLv = 0;
    public int galLv = 0;
    public int lemonLv = 0;

    // --- 彼氏・男友達 (リストを分離) ---
    [Header("--- Friends & Love ---")]
    public List<MaleFriendData> maleFriendsList = new List<MaleFriendData>(); // 男友達

    // ★追加: 彼氏リスト (管理を完全に分ける)
    public List<MaleFriendData> boyfriendList = new List<MaleFriendData>();

    // ★追加: 人数カウント用プロパティ
    public int MaleFriendCount => maleFriendsList.Count;
    public int BoyfriendCount => boyfriendList.Count;
    public int TotalMaleCount => maleFriendsList.Count + boyfriendList.Count;

    // --- 所持アイテム ---
    public List<int> moveCards = new List<int>(); // 変数名統一
    public const int MaxCards = 5;

    public int studentIdCount = 0;
    public int present = 0;
    public int eventForce = 0;
    public int albumPrice = 1000;

    // --- 各種パラメータ ---
    public int happiness = 0;
    public int maleContactCount = 0;
    public int gpIncreaseTileCount = 0;
    public int gpDecreaseTileCount = 0;
    public int diceOneCount = 0;
    public int shopSpentTotal = 0;
    public int soloPlayConsecutive = 0;
    public int totalSteps = 0;

    // --- ★追加: ステータスボーナス計算ロジック ---

    // ハーレムボーナス: 彼氏が3人以上なら全ステータス+1扱い
    public int HaremBonus => (BoyfriendCount >= 3) ? 1 : 0;

    // ステータスLv取得（ボーナス込み）
    public int GetEffectiveCommuLv() => commuLv + HaremBonus;
    public int GetEffectiveGalLv() => galLv + HaremBonus;
    public int GetEffectiveLemonLv() => lemonLv + HaremBonus;

    // 各ボーナス効果量
    public int GetCommuFriendBonus() => GetEffectiveCommuLv() * 1;   // 友達獲得数
    public int GetGalGpBonus() => GetEffectiveGalLv() * 100;         // GP獲得額

    public int CalculateSalary(int shinyuCount)
    {
        return 100 + (friends * 10) + (shinyuCount * 50);
    }

    public bool IsAllStatsOver(int value)
    {
        return GetEffectiveCommuLv() >= value &&
               GetEffectiveGalLv() >= value &&
               GetEffectiveLemonLv() >= value;
    }
}