using UnityEngine;

public enum ItemType
{
    MoveCard,       // 移動カード
    Recovery,       // 回復アイテム
    EventForce      // イベント強制アイテム
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Gyarubo/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public int basePrice; // 基本価格

    public ItemType itemType;

    [Header("Item Effects")]
    [Range(0, 6)]
    public int moveSteps; // 移動カード用 (1~6)

    public int effectValue; // 回復アイテムなどの効果量
}