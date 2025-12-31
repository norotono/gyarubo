using UnityEngine;

// Condition types for friend spawning
public enum ConditionType
{
    None,
    Ai_Fixed,       // Fixed condition for Ai
    Classroom,      // Appears in classroom
    // --- Randomly assigned conditions ---
    Conversation,   // Talk to boys 4+ times
    Happiness,      // Land on GP+ 5+ times
    Unhappiness,    // Land on GP- 3+ times
    DiceOne,        // Roll a '1' 3+ times
    Rich,           // Have 3000+ GP
    Wasteful,       // Spend 4000+ GP at shop
    Popularity,     // Have 20+ Friends
    Steps,          // Total steps 80+
    Solitude,       // Select 'Play Alone' 3 times in a row
    StatusAll2      // All stats Lv2+
}

// Unique effects for each friend
public enum FriendEffectType
{
    None,
    DoubleScoreOnJoin, // Ai: Doubles current score on join
    GPMultiplier,      // Mirei: GP gain x1.5
    ShopDiscount,      // Emi: Shop 20% OFF
    DiceReroll,        // Kaoru: Can reroll dice (Not fully implemented yet)
    DoubleTileEffect,  // Yuna: Tile effects x2
    BadEventToGP,      // Noah: Convert bad events/decreases to GP gain
    AutoFriend,        // Rena: +1 Friend every turn
    NullifyGPMinus,    // Saori: Nullify GP decrease
    MobFriendPromote,  // Maki: 100% promotion chance in events
    CardGeneration     // Rika: Generate move card every 12 turns
}

[System.Serializable]
[CreateAssetMenu(fileName = "NewFriend", menuName = "ScriptableObjects/FriendData")]
public class FriendData : ScriptableObject
{
    public string friendName;
    public Sprite faceIcon;
    public bool isAi;

    // 実行時に変動するデータ
    [System.NonSerialized] public bool isRecruited;      // 仲間にしたか
    [System.NonSerialized] public ConditionType assignedCondition; // 割り当てられた条件
    [System.NonSerialized] public string assignedRoom;   // 割り当てられた教室
    [System.NonSerialized] public bool isHintRevealed;   // ★追加: ヒントが開示されたか

    // ... (能力などの定義はそのまま) ...
    public FriendEffectType effectType;

    // ★追加: ヒント用テキスト生成
    public string GetHintText()
    {
        string condText = "";
        switch (assignedCondition)
        {
            case ConditionType.Classroom:
                return $"{friendName} は【{assignedRoom}】にいるみたい。生徒手帳を持って行ってみよう。";
            case ConditionType.Ai_Fixed:
                return $"{friendName} は【男子と8回接触】か【所持金5000GP】で興味を持ってくれるみたい。";
            case ConditionType.Conversation:
                condText = "男子生徒と4回以上会話や接触をする"; break;
            case ConditionType.Happiness:
                condText = "GP増幅マス(プラス)に5回止まる"; break;
            case ConditionType.Unhappiness:
                condText = "GP減少マス(マイナス)に3回止まる"; break;
            case ConditionType.DiceOne:
                condText = "ダイスで「1」の目を3回出す"; break;
            case ConditionType.Rich:
                condText = "所持金を 3,000 GP 以上貯める"; break;
            case ConditionType.Wasteful:
                condText = "購買部で合計 4,000 GP 以上買い物する"; break;
            case ConditionType.Popularity:
                condText = "友達の数を 20人 以上にする"; break;
            case ConditionType.Steps:
                condText = "累計で 80歩 以上移動する"; break;
            case ConditionType.Solitude:
                condText = "イベントマスで「一人で遊ぶ」を3回連続で選ぶ"; break;
            case ConditionType.StatusAll2:
                condText = "全ステータス(コミュ/ギャル/レモン)を Lv2 以上にする"; break;
            default:
                return "まだ情報がないみたい……。";
        }
        return $"{friendName} は【{condText}】と、イベントマスで一人で遊んでいる時に会えるらしいよ！";
    }
}