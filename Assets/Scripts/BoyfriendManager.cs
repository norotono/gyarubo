using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 男友達・彼氏データ
[System.Serializable]
public class MaleFriendData
{
    public string name;
    public float currentAffection; // 0-100
    public bool isBoyfriend;
    public BoyfriendType effectType;

    public MaleFriendData(string n)
    {
        name = n;
        currentAffection = 0f;
        isBoyfriend = false;
        effectType = BoyfriendType.None;
    }
}

public enum BoyfriendType { None, TypeA_Gp, TypeB_Friend }

public class BoyfriendManager : MonoBehaviour
{
    public PlayerStats playerStats;
    private string[] nameDatabase = { "タクヤ", "カズマ", "ショウ", "レン", "ユウキ", "ハルト", "リク", "ソラ", "コウセイ", "ヤマト" };

    // ★追加: 安全性チェック用プロパティ
    // リストがnullなら自動生成し、アクセス可能にする
    private List<MaleFriendData> SafeMaleList
    {
        get
        {
            if (playerStats == null)
            {
                Debug.LogError("【Error】BoyfriendManager: PlayerStats がアタッチされていません！");
                return new List<MaleFriendData>(); // 空リストを返してエラー回避
            }
            if (playerStats.maleFriendsList == null)
            {
                playerStats.maleFriendsList = new List<MaleFriendData>();
            }
            return playerStats.maleFriendsList;
        }
    }

    // 男友達追加
    public void AddNewMaleFriend()
    {
        // SafeMaleListを使うことでnullチェック不要
        string baseName = nameDatabase[Random.Range(0, nameDatabase.Length)];
        string newName = $"{baseName}{SafeMaleList.Count + 1}"; // 番号をつけて重複感軽減

        MaleFriendData newGuy = new MaleFriendData(newName);
        SafeMaleList.Add(newGuy);
        Debug.Log($"男友達追加: {newName}");
    }

    // 親密度アップ & 彼氏昇格判定
    public string IncreaseAffection(float baseValue)
    {
        // リストが空なら処理しない
        if (SafeMaleList.Count == 0) return "相手がいません";

        // ★修正: リスト内の null を除外して検索
        var target = SafeMaleList
            .Where(m => m != null && !m.isBoyfriend)
            .OrderByDescending(m => m.currentAffection)
            .FirstOrDefault();

        // 候補がいない（全員彼氏、または全員null）場合
        if (target == null)
        {
            // 彼氏がいるなら最初の彼氏とデート
            target = SafeMaleList.FirstOrDefault(m => m != null && m.isBoyfriend);

            // それでもいなければ（全部nullなど）
            if (target == null) return "デート相手が見つかりませんでした。";
        }

        // 人数による上昇量減衰
        int maleCount = SafeMaleList.Count(m => m != null && !m.isBoyfriend);
        float multiplier = 1.0f + (maleCount * 0.5f);
        float finalGain = baseValue / multiplier;

        target.currentAffection += finalGain;
        string resultMsg = $"{target.name} 親密度+{finalGain:F1} (現在:{target.currentAffection:F1}%)";

        // 100%超えで彼氏昇格
        if (!target.isBoyfriend && target.currentAffection >= 100f)
        {
            PromoteToBoyfriend(target);
            resultMsg += $"\n【祝】{target.name} から告白されました！ 彼氏になった！";
        }

        return resultMsg;
    }

    void PromoteToBoyfriend(MaleFriendData guy)
    {
        if (guy == null) return; // 安全策

        guy.isBoyfriend = true;
        // 能力ランダム決定
        guy.effectType = (Random.Range(0, 2) == 0) ? BoyfriendType.TypeA_Gp : BoyfriendType.TypeB_Friend;

        if (playerStats != null) playerStats.boyfriendCount++;
    }

    // 毎ターンの効果発動
    public string ActivateBoyfriendEffects()
    {
        int totalGp = 0;
        int totalFriend = 0;
        int count = 0;

        // ★修正: foreachで回す際も null チェックを入れる
        foreach (var guy in SafeMaleList)
        {
            if (guy == null) continue; // null要素はスキップ

            if (guy.isBoyfriend)
            {
                count++;
                if (guy.effectType == BoyfriendType.TypeA_Gp) totalGp += 500;
                else if (guy.effectType == BoyfriendType.TypeB_Friend) totalFriend += 2;
            }
        }

        if (totalGp == 0 && totalFriend == 0) return null;

        if (playerStats != null)
        {
            playerStats.gp += totalGp;
            playerStats.friends += totalFriend;
        }

        return $"彼氏({count}人)からのプレゼント: GP+{totalGp}, 友+{totalFriend}";
    }
}