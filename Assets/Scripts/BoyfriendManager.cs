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
    public BoyfriendType effectType;

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
        string newName = $"{baseName}{SafeMaleList.Count + 1}";

        MaleFriendData newGuy = new MaleFriendData(newName);
        SafeMaleList.Add(newGuy);
        Debug.Log($"男友達追加: {newName}");
    }

    // 【修正】親密度アップ
    // targetIndex: -1 なら「全員（制限なし）」、0以上なら「そのインデックスの彼のみ」
    public string IncreaseAffection(float amount, int targetIndex = -1)
    {
        if (SafeMaleList.Count == 0) return "相手がいません";

        string resultMsg = "";

        // ★ -1 (デフォルト) の場合は全員に加算
        if (targetIndex == -1)
        {
            int count = 0;
            foreach (var guy in SafeMaleList)
            {
                if (guy == null) continue;

                guy.currentAffection += amount;
                CheckPromotion(guy);
                count++;
            }
            resultMsg = $"全員({count}名)の親密度が +{amount} されました！";
        }
        else
        {
            // 特定の相手（デートイベントなど）
            if (targetIndex >= 0 && targetIndex < SafeMaleList.Count)
            {
                var guy = SafeMaleList[targetIndex];
                if (guy != null)
                {
                    guy.currentAffection += amount;
                    CheckPromotion(guy);
                    resultMsg = $"{guy.name} の親密度 +{amount} (現在:{guy.currentAffection}%)";
                }
            }
        }

        return resultMsg;
    }

    // 昇格チェック
    void CheckPromotion(MaleFriendData guy)
    {
        if (!guy.isBoyfriend && guy.currentAffection >= 100f)
        {
            guy.isBoyfriend = true;
            guy.effectType = (Random.Range(0, 2) == 0) ? BoyfriendType.TypeA_Gp : BoyfriendType.TypeB_Friend;
            if (playerStats != null) playerStats.boyfriendCount++;
            Debug.Log($"【祝】{guy.name} が彼氏になりました！");
        }
    }

    // 毎ターンの効果
    public string ActivateBoyfriendEffects()
    {
        int totalGp = 0;
        int totalFriend = 0;
        int count = 0;

        foreach (var guy in SafeMaleList)
        {
            if (guy == null) continue;
            if (guy.isBoyfriend)
            {
                count++;
                if (guy.effectType == BoyfriendType.TypeA_Gp) totalGp += 500;
                else if (guy.effectType == BoyfriendType.TypeB_Friend) totalFriend += 2;
            }
        }

        if (totalGp == 0 && totalFriend == 0) return null;

        if (playerStats != null)
        {
            playerStats.gp += totalGp;
            playerStats.friends += totalFriend;
        }

        return $"彼氏({count}人)効果: GP+{totalGp}, 友+{totalFriend}";
    }
}