using UnityEngine;

public enum FriendEffectType {
    ScoreDoubler, GPMultiplier, ShopDiscount, DiceReroll, DoubleTileEffect,
    BadToGP, AutoFriend, NullifyGPMinus, MobPromo100, CardGenerator
}
public enum ScoutConditionType {
    Fixed_Ai, Classroom, MaleContact, HighGP_Tile, LowGP_Tile,
    DiceOne, RichGP, HighSpend, HighFriends, HighSteps, Solitude, HighStatus
}

[CreateAssetMenu(fileName = "NewFriend", menuName = "Gyarubo/FriendData")]
public class FriendData : ScriptableObject {
    public string characterName;
    [TextArea] public string introduction;
    public Sprite faceIcon;
    public FriendEffectType effectType;
    
    [Header("Runtime State")]
    public bool isRecruited = false;
    public ScoutConditionType conditionType;
    public int conditionValue;
}