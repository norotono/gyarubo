using UnityEngine;

public enum ItemType
{
    MoveCard_Fixed,     // 1-6の固定移動 (インベントリに入る実体)
    MoveCard_RandomShop,// ショップ用くじ (買うとFixedに変化)
    MoveCard_HighLow,   // 倍額カード
    ClassroomAccess,    // 生徒手帳
    Recovery,           // プレゼント・回復
    ForceEvent,         // イベント強制
    StatusUp,           // ステータスUP
    FriendBoost,        // 卒業写真
    DynamicPrice        // 卒業アルバム
}

public enum TargetStatus
{
    None,
    Commu,
    Gal,
    Lemon,
    All    // 全ステータスUP用
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Gyarubo/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public int basePrice; // 基本価格
    public ItemType itemType;

    [Header("Effects")]
    [Range(0, 6)]
    public int moveSteps;        // 固定カードの場合の数字 (1-6)
    public int effectValue;      // 友達増加数や回復量
    public int priceIncrement;   // 価格上昇額
    public TargetStatus targetStatus; // ステータスUP対象

    [Header("Availability")]
    public bool grade3Only;      // 3階限定フラグ
}