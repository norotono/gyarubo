using UnityEngine;

public enum FriendEffectType
{
    None,
    DoubleScoreOnJoin, // 加入時に友達数2倍
    GPMultiplier,      // GP獲得量UP
    NullifyGPMinus,    // GP減少無効
    EventSuccessRateUp,
    DiceControl,
    AutoGP,
    BadEventToGP,      // 悪いイベントをGP変換
    DoubleTileEffect,  // マス効果2倍
    AutoFriend,        // 毎ターン友達増加
    ShopDiscount       // ショップ割引
}

// スカウト条件の種類
public enum ScoutConditionType
{
    None,           // 無条件（またはイベント専用）
    HighGP,         // GPが一定以上
    LowGP,          // GPが一定以下（貧乏神系）
    HighFriends,    // 友達が一定以上
    SoloPlay,       // ソロプレイ回数が一定以上
    Grade2,         // 2年生以上
    Grade3          // 3年生以上
}

[CreateAssetMenu(fileName = "NewFriend", menuName = "Gyarubo/FriendData")]
public class FriendData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;
    [TextArea] public string introduction;
    public Sprite faceIcon;

    [Header("Game Effect")]
    public FriendEffectType effectType;
    public int baseScore = 10; // 卒業時の加算ポイント

    [Header("Scout Condition")]
    public ScoutConditionType conditionType; // 出現条件
    public int conditionValue;               // 閾値 (例: GP 5000以上なら 5000)

    [Header("State")]
    public bool isRecruited = false; // ゲーム中に変更されるフラグ
}