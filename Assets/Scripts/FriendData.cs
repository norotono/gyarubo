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
public class FriendData
{
    public string name;
    [TextArea] public string abilityDescription;
    public bool isAi;
    public FriendEffectType effectType; // The specific ability this friend has

    // --- Dynamic Game Data (Assigned at runtime) ---
    [HideInInspector] public ConditionType assignedCondition;
    [HideInInspector] public string assignedRoom;
    [HideInInspector] public bool isRecruited;
    [HideInInspector] public bool isInfoRevealed;
}