using System.Collections.Generic;

public static class TextRepository
{
    // 男子生徒との会話パターン
    public static string[] BoyConversations = new string[]
    {
        "ねえ、あそこの教室に可愛い子がいるらしいよ？",
        "最近、購買のパンが美味しいんだって。",
        "テスト勉強全然してないわー（嘘）"
    };

    // 親友になった時のセリフ
    public static Dictionary<string, string> ShinyuDialogues = new Dictionary<string, string>()
    {
        { "Ai", "あなたが運命の人？ 私のプロデュース任せたわ！" },
        { "Mirei", "あら、庶民にしては気が利くわね。お友達になってあげる。" },
        // ... 他のキャラもここに追加
    };

    // システムメッセージ
    public const string MSG_SEASON_CHANGE = "季節が変わりました！";
    public const string MSG_SALARY = "お小遣いが入りました。";
}