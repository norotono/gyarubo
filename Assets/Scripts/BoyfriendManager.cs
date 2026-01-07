using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 男友達・彼氏データ
[System.Serializable]
public class MaleFriendData
{
    public string name;
    public float currentAffection; // 0-100
    public bool isBoyfriend;
    public BoyfriendType effectType; // TypeA_Gp, TypeB_Friend

    public MaleFriendData(string n)
    {
        name = n;
        currentAffection = 0f;
        isBoyfriend = false;
        effectType = BoyfriendType.None;
    }
}

public enum BoyfriendType { None, TypeA_Gp, TypeB_Friend }

public class BoyfriendManager : MonoBehaviour
{
    public PlayerStats playerStats;
    private string[] nameDatabase = { "タクヤ", "カズマ", "ショウ", "レン", "ユウキ", "ハルト", "リク", "ソラ", "コウセイ", "ヤマト" };

    // 安全リスト取得
    private List<MaleFriendData> SafeMaleList
    {
        get
        {
            if (playerStats == null) return new List<MaleFriendData>();
            if (playerStats.maleFriendsList == null) playerStats.maleFriendsList = new List<MaleFriendData>();
            return playerStats.maleFriendsList;
        }
    }

    // 男友達追加
    public void AddNewMaleFriend()
    {
        string baseName = nameDatabase[Random.Range(0, nameDatabase.Length)];
        string uniqueName = baseName;
        int count = 2;
        while (SafeMaleList.Any(x => x.name == uniqueName) || playerStats.boyfriendList.Any(x => x.name == uniqueName))
        {
            uniqueName = baseName + count;
            count++;
        }

        playerStats.maleFriendsList.Add(new MaleFriendData(uniqueName));
        Debug.Log($"新しい男友達 {uniqueName} ができました！");
    }

    // ★修正: 親密度アップ処理 (指定の計算式)
    public string IncreaseAffection(float baseAmount, int targetIndex = -1)
    {
        if (playerStats.MaleFriendCount == 0) return "男友達がいません。";

        // 1. 分子: イベント基準値 + レモン力Lv
        float numerator = baseAmount + playerStats.GetEffectiveLemonLv();

        // 2. 分母: 1 + (男友達+彼氏-1) * 0.5
        // ※人数-1 がマイナスにならないよう調整
        int totalMales = playerStats.TotalMaleCount;
        float denominator = 1.0f + (Mathf.Max(0, totalMales - 1) * 0.5f);

        // 3. 最終上昇量
        float finalAmount = numerator / denominator;

        string resultMsg = "";

        // 全員アップの場合
        if (targetIndex == -1)
        {
            foreach (var guy in SafeMaleList.ToList()) // ToListでコピーして回す(昇格時の削除対策)
            {
                guy.currentAffection += finalAmount;
                CheckPromotion(guy); // 昇格チェック
            }
            resultMsg = $"男友達全員の親密度 +{finalAmount:F1}\n(補正: {numerator}÷{denominator:F1})";
        }
        else
        {
            // 個別アップの場合 (既存コードの仕様維持)
            if (targetIndex >= 0 && targetIndex < SafeMaleList.Count)
            {
                var guy = SafeMaleList[targetIndex];
                guy.currentAffection += finalAmount;
                CheckPromotion(guy);
                resultMsg = $"{guy.name} の親密度 +{finalAmount:F1}";
            }
        }

        return resultMsg;
    }

    // ★修正: 昇格チェックとリスト移動
    void CheckPromotion(MaleFriendData guy)
    {
        if (!guy.isBoyfriend && guy.currentAffection >= 100f)
        {
            guy.isBoyfriend = true;
            // 能力をランダム決定
            guy.effectType = (Random.Range(0, 2) == 0) ? BoyfriendType.TypeA_Gp : BoyfriendType.TypeB_Friend;

            // ★重要: 男友達リストから削除し、彼氏リストへ移動
            if (playerStats.maleFriendsList.Contains(guy))
            {
                playerStats.maleFriendsList.Remove(guy);
            }
            playerStats.boyfriendList.Add(guy);

            Debug.Log($"【祝】{guy.name} が彼氏になりました！");
        }
    }

    // ★修正: 毎ターンの効果発動 (実際に加算する)
    public string ActivateBoyfriendEffects()
    {
        if (playerStats.BoyfriendCount == 0) return "";

        int totalGp = 0;
        int totalFriend = 0;

        foreach (var bf in playerStats.boyfriendList)
        {
            if (bf.effectType == BoyfriendType.TypeA_Gp) totalGp += 500;
            else if (bf.effectType == BoyfriendType.TypeB_Friend) totalFriend += 2;
        }

        // 実際に加算
        if (totalGp > 0) playerStats.gp += totalGp;
        if (totalFriend > 0) playerStats.friends += totalFriend;

        if (totalGp > 0 || totalFriend > 0)
            return $"彼氏ボーナス: GP+{totalGp}, 友達+{totalFriend}";

        return "";
    }
}