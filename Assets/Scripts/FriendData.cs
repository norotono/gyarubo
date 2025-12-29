// FriendData.cs (Šù‘¶‚ÌC³)
using System;
using UnityEngine;

[Serializable]
public class FriendData
{
    public string characterName;
    public FriendEffectType effectType;
    public ConditionType recruitCondition;
    public bool isRecruited;

    // AI‚©‚Ç‚¤‚©A•”‰®”Ô†‚È‚Ç‚Ì•t‰Áî•ñ
    public bool isAi;
    public string assignedRoom;

    public FriendData(string name, FriendEffectType effect, ConditionType condition = ConditionType.None, bool ai = false)
    {
        characterName = name;
        effectType = effect;
        recruitCondition = condition;
        isRecruited = false;
        isAi = ai;
    }
}