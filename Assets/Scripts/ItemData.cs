using UnityEngine;

public enum ItemType
{
    // --- 新仕様: くじ引きシステム用 ---
    MoveCard_Fixed,      // 1-6確定カード (インベントリ用)
    MoveCard_RandomShop, // ショップ用くじ (買うとFixedに変化)

    // --- 既存・新仕様共通 ---
    MoveCard_HighLow,    // 倍額
    ClassroomAccess,     // 生徒手帳
    Recovery,            // 回復/プレゼント
    ForceEvent,          // イベント強制
    StatusUp,            // ステータスUP
    FriendBoost,         // 卒業写真
    DynamicPrice         // 卒業アルバム (価格変動)
}

public enum TargetStatus
{
    None,
    Commu,
    Gal,
    Lemon,
    All
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Gyarubo/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public int basePrice;
    public ItemType itemType;

    [Header("Effects")]
    [Range(0, 6)]
    public int moveSteps;        // 1-6の固定値
    public int effectValue;      // 友達増加数など
    public int priceIncrement;   // アルバムの価格上昇値
    public TargetStatus targetStatus;

    [Header("Availability")]
    public bool grade3Only;
}