// ファイル名: GameEnums.cs
public enum TileType
{
    Normal, Start, Goal, Boy, Event, Shop, Classroom,
    Friend_Plus, Friend_Minus, GP_Plus, GP_Minus
}

public enum FriendEffectType
{
    None,
    ShopDiscount,       // 購買20%OFF
    DoubleScoreOnJoin,  // 加入時スコア2倍
    NullifyGPMinus,     // GPマイナス無効
    BadEventToGP,       // 悪いイベントをGP獲得に変換
    AutoFriend,         // 毎ターン友達+1
    DoubleTileEffect,   // マス効果2倍
    GPMultiplier        // GP獲得量1.5倍
}

public enum ConditionType
{
    None, Ai_Fixed, Classroom,
    Solitude, // ぼっち（一人遊び連続）
    Rich,     // GPが多い
    HighStatus // ステータスが高い
}