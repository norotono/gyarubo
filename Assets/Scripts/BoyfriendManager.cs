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

    // 男友達追加
    public void AddNewMaleFriend()
    {
        string newName = nameDatabase[Random.Range(0, nameDatabase.Length)] + (playerStats.maleFriendsList.Count + 1);
        MaleFriendData newGuy = new MaleFriendData(newName);
        playerStats.maleFriendsList.Add(newGuy);
        Debug.Log($"男友達追加: {newName}");
    }

    // 親密度アップ & 彼氏昇格判定
    public string IncreaseAffection(float baseValue)
    {
        if (playerStats.maleFriendsList.Count == 0) return "相手がいません";

        // まだ彼氏じゃない人の中で一番好感度が高い人を優先
        var target = playerStats.maleFriendsList
            .Where(m => !m.isBoyfriend)
            .OrderByDescending(m => m.currentAffection)
            .FirstOrDefault();

        // 全員彼氏なら、最初の彼氏とデート
        if (target == null) target = playerStats.maleFriendsList[0];

        // 人数による上昇量減衰
        int maleCount = playerStats.maleFriendsList.Count(m => !m.isBoyfriend);
        float multiplier = 1.0f + (maleCount * 0.5f);
        float finalGain = baseValue / multiplier;

        target.currentAffection += finalGain;
        string resultMsg = $"{target.name} 親密度+{finalGain:F1} (現在:{target.currentAffection:F1}%)";

        // 100%超えで彼氏昇格
        if (!target.isBoyfriend && target.currentAffection >= 100f)
        {
            PromoteToBoyfriend(target);
            resultMsg += $"\n【祝】{target.name} から告白されました！ 彼氏になった！";
        }

        return resultMsg;
    }

    void PromoteToBoyfriend(MaleFriendData guy)
    {
        guy.isBoyfriend = true;
        // 能力ランダム決定
        guy.effectType = (Random.Range(0, 2) == 0) ? BoyfriendType.TypeA_Gp : BoyfriendType.TypeB_Friend;
        playerStats.boyfriendCount++;
    }

    // 毎ターンの効果発動
    public string ActivateBoyfriendEffects()
    {
        int totalGp = 0;
        int totalFriend = 0;
        int count = 0;

        foreach (var guy in playerStats.maleFriendsList)
        {
            if (guy.isBoyfriend)
            {
                count++;
                if (guy.effectType == BoyfriendType.TypeA_Gp) totalGp += 500;
                else if (guy.effectType == BoyfriendType.TypeB_Friend) totalFriend += 2;
            }
        }

        if (totalGp == 0 && totalFriend == 0) return null;

        playerStats.gp += totalGp;
        playerStats.friends += totalFriend;
        return $"彼氏({count}人)からのプレゼント: GP+{totalGp}, 友+{totalFriend}";
    }
}