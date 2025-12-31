using UnityEngine;

public enum ItemType {
    MoveCard_Random, MoveCard_HighLow, ClassroomAccess, Recovery,
    ForceEvent, StatusUp, FriendBoost, DynamicPrice
}
public enum TargetStatus { None, Commu, Gal, Lemon }

[CreateAssetMenu(fileName = "NewItem", menuName = "Gyarubo/ItemData")]
public class ItemData : ScriptableObject {
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public int basePrice;
    public ItemType itemType;
    public int effectValue;
    public int priceIncrement;
    public TargetStatus targetStatus;
    
    [Header("Move Card")]
    [Range(0, 6)] public int moveSteps; // ‹ŒŽd—lŒÝŠ·—p
}