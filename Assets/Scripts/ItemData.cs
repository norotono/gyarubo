using UnityEngine;

public enum FriendEffectType
{
    ScoreDoubler,       // アイ: 加入時スコア2倍
    GPMultiplier,       // ミレイ: GP獲得1.5倍
    ShopDiscount,       // エミ: 購買20%OFF
    DiceReroll,         // カオル: ダイスリロール(未実装)
    DoubleTileEffect,   // ユナ: マス効果2回
    BadToGP,            // ノア: 不幸をGP変換
    AutoFriend,         // レナ: 毎ターン友達+1
    NullifyGPMinus,     // サオリ: GP減少無効
    MobPromo100,        // マキ: モブ昇格100%(イベント用)
    CardGenerator,      // リカ: 12ターン毎にカード生成
    None                // 効果なし
}

// 出現条件の種類
public enum ScoutConditionType
{
    Fixed_Ai,       // アイ専用 (男子8回 or GP5000)
    Classroom,      // 教室 (生徒手帳が必要)
    // --- ランダム割り当て用 ---
    MaleContact,    // 会話: 男子接触
    HighGP_Tile,    // 幸福: GP増マス
    LowGP_Tile,     // 不幸: GP減マス
    DiceOne,        // ダイス: 1の目
    RichGP,         // 金満: 所持金
    HighSpend,      // 浪費: 購買額
    HighFriends,    // 友達: 人数
    HighSteps,      // 歩数: 合計移動
    Solitude,       // 孤独: ソロプレイ
    HighStatus,     // ステータス: 全ステ
    None
}

[CreateAssetMenu(fileName = "NewFriend", menuName = "Gyarubo/FriendData")]
public class FriendData : ScriptableObject
{
    public string characterName;
    [TextArea] public string introduction;
    public Sprite faceIcon;

    [Header("Ability")]
    public FriendEffectType effectType;
    public int baseScore = 10;

    [Header("Runtime State (自動設定)")]
    public bool isRecruited = false;
    public ScoutConditionType conditionType;
    public int conditionValue;
}