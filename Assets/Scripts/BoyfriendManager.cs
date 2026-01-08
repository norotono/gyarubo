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

    public MaleFriendData(string n = "名無し")
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

    private List<MaleFriendData> SafeMaleList
    {
        get
        {
            if (playerStats == null) return new List<MaleFriendData>();
            if (playerStats.maleFriendsList == null) playerStats.maleFriendsList = new List<MaleFriendData>();
            return playerStats.maleFriendsList;
        }
    }

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

    public string IncreaseAffection(float baseAmount, int targetIndex = -1)
    {
        if (playerStats.MaleFriendCount == 0) return "男友達がいません。";

        float numerator = baseAmount + playerStats.GetEffectiveLemonLv();
        int totalMales = playerStats.TotalMaleCount;
        float denominator = 1.0f + (Mathf.Max(0, totalMales - 1) * 0.5f);
        float finalAmount = numerator / denominator;

        string resultMsg = "";

        if (targetIndex == -1)
        {
            foreach (var guy in SafeMaleList.ToList()) 
            {
                guy.currentAffection += finalAmount;
                CheckPromotion(guy);
            }
            resultMsg = $"男友達全員の親密度 +{finalAmount:F1}\n(補正: {numerator}÷{denominator:F1})";
        }
        else
        {
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

    void CheckPromotion(MaleFriendData guy)
    {
        if (!guy.isBoyfriend && guy.currentAffection >= 100f)
        {
            PromoteToBoyfriend(guy);
        }
    }

    public void PromoteToBoyfriend(MaleFriendData guy)
    {
        guy.isBoyfriend = true;
        guy.effectType = (Random.Range(0, 2) == 0) ? BoyfriendType.TypeA_Gp : BoyfriendType.TypeB_Friend;

        if (playerStats.maleFriendsList.Contains(guy))
        {
            playerStats.maleFriendsList.Remove(guy);
        }
        playerStats.boyfriendList.Add(guy);

        string ability = (guy.effectType == BoyfriendType.TypeA_Gp) ? "GP+500" : "友達+2";
        Debug.Log($"【祝】{guy.name} が彼氏になりました！ 能力: {ability}");
    }

    // ★追加: 名前をGameManagerの呼び出しに合わせて変更
    public string ActivateTurnEndAbilities()
    {
        if (playerStats.BoyfriendCount == 0) return "";

        int gpGain = 0;
        int friendGain = 0;

        foreach (var bf in playerStats.boyfriendList)
        {
            if (bf.effectType == BoyfriendType.TypeA_Gp) gpGain += 500;
            else if (bf.effectType == BoyfriendType.TypeB_Friend) friendGain += 2;
        }

        if (gpGain > 0) playerStats.gp += gpGain;
        if (friendGain > 0) playerStats.friends += friendGain;

        if (gpGain > 0 || friendGain > 0)
            return $"彼氏ボーナス: GP+{gpGain}, 友達+{friendGain}";
        
        return "";
    }
}