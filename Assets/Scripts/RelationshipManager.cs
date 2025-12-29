using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RelationshipManager : MonoBehaviour
{
    // Inspectorでキャラクターリストを設定
    public List<FriendData> allFriends = new List<FriendData>();

    public void Initialize()
    {
        // Inspectorが空ならデフォルト作成
        if (allFriends.Count == 0)
        {
            allFriends.Add(new FriendData("AIギャル", FriendEffectType.None, ConditionType.Ai_Fixed, true));
            allFriends.Add(new FriendData("エミ", FriendEffectType.ShopDiscount, ConditionType.None));
            allFriends.Add(new FriendData("ノア", FriendEffectType.BadEventToGP, ConditionType.None));
        }

        // 念のためリセット
        foreach (var f in allFriends)
        {
            // AIかつ固定条件なら最初から親友
            if (f.isAi && f.recruitCondition == ConditionType.Ai_Fixed)
                f.isRecruited = true;
            else
                f.isRecruited = false;
        }
    }

    public bool HasEffect(FriendEffectType type)
    {
        return allFriends.Any(f => f.isRecruited && f.effectType == type);
    }

    public FriendData CheckScoutableFriend(PlayerStats stats)
    {
        foreach (var f in allFriends)
        {
            if (f.isRecruited) continue;
            // 条件判定（例）
            if (f.recruitCondition == ConditionType.Solitude && stats.soloPlayConsecutive >= 3) return f;
            if (f.recruitCondition == ConditionType.Rich && stats.gp >= 5000) return f;
            if (f.recruitCondition == ConditionType.None) return f; // 条件なしキャラ
        }
        return null;
    }

    public void RecruitFriend(FriendData friend)
    {
        if (friend == null || friend.isRecruited) return;
        friend.isRecruited = true;
        Debug.Log($"{friend.characterName} が親友になった");
        if (friend.effectType == FriendEffectType.DoubleScoreOnJoin)
            PlayerStats.Instance.friends *= 2;
    }

    public int GetRecruitedCount() => allFriends.Count(f => f.isRecruited);
}