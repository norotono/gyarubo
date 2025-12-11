using UnityEngine;

public enum ConditionType
{
    None,
    Ai_Fixed,       // 固定
    Classroom,      // 教室
    // --- ランダム ---
    Conversation, Happiness, Unhappiness, DiceOne, Rich,
    Wasteful, Popularity, Steps, Solitude, StatusAll2
}

// ★親友の固有能力リスト
public enum FriendEffectType
{
    None,
    DoubleScoreOnJoin, // アイ: 加入時スコア2倍
    GPMultiplier,      // ミレイ: GP獲得1.5倍
    ShopDiscount,      // エミ: 購買20%OFF
    DiceReroll,        // カオル: リロール可能(未実装)
    DoubleTileEffect,  // ユナ: マス効果2倍
    BadEventToGP,      // ノア: 減少/悪イベをGP変換
    AutoFriend,        // レナ: 毎ターン友達+1
    NullifyGPMinus,    // サオリ: GP減少無効
    MobFriendPromote,  // マキ: お出かけでモブ昇格100%
    CardGeneration     // リカ: 12ターン毎にカード生成
}

[System.Serializable]
public class FriendData
{
    public string name;
    [TextArea] public string abilityDescription;
    public bool isAi;
    public FriendEffectType effectType; // 能力タイプ

    // --- ゲーム内変動値 ---
    [HideInInspector] public ConditionType assignedCondition;
    [HideInInspector] public string assignedRoom;
    [HideInInspector] public bool isRecruited;
    [HideInInspector] public bool isInfoRevealed;
}