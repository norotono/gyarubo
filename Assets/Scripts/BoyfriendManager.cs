using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 男友達・彼氏のデータクラス
[System.Serializable]
public class MaleFriendData
{
    public string name;
    public float currentAffection; // 現在の好感度 (0-100)
    public bool isBoyfriend;       // 彼氏になったか
    public BoyfriendType effectType; // 彼氏能力タイプ

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
    [Header("--- Settings ---")]
    public PlayerStats playerStats;

    // 男の名前候補（ランダム生成用）
    private string[] nameDatabase = { "タクヤ", "カズマ", "ショウ", "レン", "ユウキ", "ハルト", "リク", "ソラ", "コウセイ", "ヤマト" };

    // --- メイン操作 ---

    // 1. 男友達を追加（男子生徒マス用）
    public void AddNewMaleFriend()
    {
        // 名前をランダム決定（被り回避は簡易的に無視、またはナンバリング）
        string newName = nameDatabase[Random.Range(0, nameDatabase.Length)] + (playerStats.maleFriendsList.Count + 1);

        MaleFriendData newGuy = new MaleFriendData(newName);
        playerStats.maleFriendsList.Add(newGuy);

        Debug.Log($"男友達追加: {newName}");
        // 統計更新
        playerStats.maleFriendCount = playerStats.maleFriendsList.Count(x => !x.isBoyfriend);
    }

    // 2. 好感度を上げる（デート・プレゼント・中間イベント用）
    // targetIndex: -1なら「最も好感度が低い人」または「ランダム」など自動選択
    public string IncreaseAffection(float baseValue, int targetIndex = -1)
    {
        if (playerStats.maleFriendsList.Count == 0) return "相手がいません";

        // 対象決定（指定がなければ、まだ彼氏じゃない人で好感度が高い人を優先、あるいはランダム）
        // ここでは「彼氏になっていないリストの中で、最も進展している人」を優先します
        var target = playerStats.maleFriendsList
            .Where(m => !m.isBoyfriend)
            .OrderByDescending(m => m.currentAffection)
            .FirstOrDefault();

        // 彼氏しかいない、または誰もいない場合は彼氏の親密度を上げる（限界突破）
        if (target == null)
        {
            target = playerStats.maleFriendsList.FirstOrDefault();
            if (target == null) return "男友達がいません";
        }

        // 計算式: (イベント基準値) ÷ (1 ＋ 現在の男友達数 × 0.5)
        // ※レモン力補正は呼び出し元でbaseValueに加算済とする
        int maleCount = playerStats.maleFriendsList.Count(m => !m.isBoyfriend);
        float multiplier = 1.0f + (maleCount * 0.5f);
        float finalGain = baseValue / multiplier;

        target.currentAffection += finalGain;
        string resultMsg = $"{target.name} の親密度 +{finalGain:F1} (現在: {target.currentAffection:F1}%)";

        // 昇格チェック
        if (!target.isBoyfriend && target.currentAffection >= 100f)
        {
            PromoteToBoyfriend(target);
            resultMsg += "\n祝！ 彼氏になりました！";
        }

        return resultMsg;
    }

    // 3. 彼氏昇格処理
    void PromoteToBoyfriend(MaleFriendData guy)
    {
        guy.isBoyfriend = true;

        // 能力抽選 (Type A: GP+500 / Type B: Friend+2)
        int roll = Random.Range(0, 2);
        guy.effectType = (roll == 0) ? BoyfriendType.TypeA_Gp : BoyfriendType.TypeB_Friend;

        // 統計更新
        playerStats.boyfriendCount++;
        playerStats.maleFriendCount = playerStats.maleFriendsList.Count(x => !x.isBoyfriend);

        Debug.Log($"彼氏昇格: {guy.name} / 能力: {guy.effectType}");
    }

    // 4. 毎ターン終了時の彼氏能力発動
    public string ActivateBoyfriendEffects()
    {
        int totalGpGain = 0;
        int totalFriendGain = 0;
        int bfCount = 0;

        foreach (var guy in playerStats.maleFriendsList)
        {
            if (guy.isBoyfriend)
            {
                bfCount++;
                if (guy.effectType == BoyfriendType.TypeA_Gp) totalGpGain += 500;
                else if (guy.effectType == BoyfriendType.TypeB_Friend) totalFriendGain += 2;
            }
        }

        if (totalGpGain == 0 && totalFriendGain == 0) return null;

        playerStats.gp += totalGpGain;
        playerStats.friends += totalFriendGain;

        return $"彼氏({bfCount}人)効果: GP+{totalGpGain}, 友+{totalFriendGain}";
    }
}